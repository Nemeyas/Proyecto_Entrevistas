import io
import os
import base64
import struct
import time
import asyncio
import speech_recognition as sr
from fastapi import FastAPI, UploadFile, File, Form
from fastapi.responses import JSONResponse
import edge_tts
import numpy as np
import cv2
from PIL import Image

# --- LIBRERÍA DE DETECCIÓN DE EMOCIONES (HSEmotion ONNX) ---
from hsemotion_onnx.facial_emotions import HSEmotionRecognizer

# --- NUEVA LIBRERÍA DE GOOGLE ---
from google import genai
from dotenv import load_dotenv

# Cargar la API Key desde el archivo .env (asi no se sube a GitHub)
load_dotenv()
api_key = os.getenv("GEMINI_API_KEY")
if not api_key or api_key == "PONER_TU_API_KEY_AQUI":
    print("=" * 50)
    print("ERROR: No se encontro la API Key de Gemini.")
    print("Crea un archivo .env en Backend_Python/ con:")
    print('  GEMINI_API_KEY=tu_clave_aqui')
    print("=" * 50)

client = genai.Client(api_key=api_key or "")

# ==========================================
# SISTEMA DE REINTENTOS PARA GEMINI
# ==========================================
MAX_REINTENTOS_GEMINI = 5
ESPERA_INICIAL_GEMINI = 2  # segundos

def llamar_gemini_con_reintentos(prompt: str, modelo: str = 'gemini-2.5-flash') -> str:
    """
    Llama a Gemini con reintentos automáticos y backoff exponencial.
    Si Gemini está sobrecargado (503), reintenta hasta MAX_REINTENTOS_GEMINI veces.
    """
    ultimo_error = None
    for intento in range(1, MAX_REINTENTOS_GEMINI + 1):
        try:
            response = client.models.generate_content(
                model=modelo,
                contents=prompt,
            )
            return response.text.strip()
        except Exception as e:
            ultimo_error = e
            error_str = str(e).lower()
            # Detectar errores de sobrecarga/disponibilidad de Gemini
            es_sobrecarga = any(palabra in error_str for palabra in [
                '503', 'unavailable', 'overloaded', 'resource exhausted',
                'rate limit', '429', 'quota', 'high demand', 'temporarily'
            ])
            if es_sobrecarga and intento < MAX_REINTENTOS_GEMINI:
                espera = ESPERA_INICIAL_GEMINI * (2 ** (intento - 1))  # 2, 4, 8, 16...
                print(f"⚠️ Gemini sobrecargado (intento {intento}/{MAX_REINTENTOS_GEMINI}). Reintentando en {espera}s...")
                time.sleep(espera)
            elif not es_sobrecarga:
                # Error no relacionado con sobrecarga, lanzar inmediatamente
                raise e
            else:
                # Último intento fallido
                print(f"❌ Gemini sigue caído después de {MAX_REINTENTOS_GEMINI} intentos.")
                raise e
    raise ultimo_error

# Variable global para recordar la emoción
ultima_emocion_detectada = "neutral"
# Buffer para almacenar las emociones detectadas durante el turno del usuario
buffer_emociones = []

# --- Inicialización de los modelos de detección de emociones ---
# Se cargan UNA SOLA VEZ al prender el servidor (no en cada request)
print(">>> Cargando modelos de detección facial y emociones (HSEmotion ONNX)...")
detector_caras = cv2.CascadeClassifier(cv2.data.haarcascades + 'haarcascade_frontalface_default.xml')
detector_emociones = HSEmotionRecognizer(model_name='enet_b0_8_best_afew')
print(">>> Modelos de emociones cargados correctamente.")

# Diccionario de traducción: HSEmotion -> formato DeepFace (para que Unity no se entere del cambio)
TRADUCCION_EMOCIONES = {
    "Anger": "angry",
    "Contempt": "disgust",
    "Disgust": "disgust",
    "Fear": "fear",
    "Happiness": "happy",
    "Neutral": "neutral",
    "Sadness": "sad",
    "Surprise": "surprise",
}

from database import db
import json
try:
    db.init_db()
    print(">>> Base de datos inicializada correctamente.")
except Exception as e:
    print(f">>> Advertencia: No se pudo conectar a la base de datos MySQL: {e}")

app = FastAPI()

# ==========================================
# TEXT-TO-SPEECH (TTS) con edge-tts
# ==========================================
TTS_VOICE = "es-MX-JorgeNeural"  # Voz masculina mexicana profesional

async def generar_tts_audio(texto: str) -> str:
    """
    Genera audio WAV a partir de texto usando edge-tts y lo devuelve como base64.
    """
    try:
        # 1. Generar MP3 en memoria con edge-tts
        communicate = edge_tts.Communicate(texto, TTS_VOICE)
        mp3_buffer = io.BytesIO()
        async for chunk in communicate.stream():
            if chunk["type"] == "audio":
                mp3_buffer.write(chunk["data"])
        mp3_buffer.seek(0)
        
        # 2. Codificar el MP3 directamente en base64
        # Unity usará UnityWebRequestMultimedia para decodificar MP3
        audio_base64 = base64.b64encode(mp3_buffer.read()).decode('utf-8')
        print(f"🔊 TTS generado: {len(audio_base64)} chars base64")
        return audio_base64
    except Exception as e:
        print(f"❌ Error generando TTS: {e}")
        return ""

# ==========================================
# CONFIGURACIÓN DE LA ENTREVISTA
# ==========================================
TOTAL_TEMAS = 4          # Cantidad de temáticas en la entrevista
PREGUNTAS_POR_TEMA = 2   # Cuántas preguntas se hacen por cada temática
# Total de interacciones = TOTAL_TEMAS * PREGUNTAS_POR_TEMA = 8

# Memoria de sesiones temporales (fallback por si Unity no envía IDSimulacion aún)
sesiones_activas = {}
sesiones_activas[0] = {
    "historial": [],
    "modo": "pasivo",
    "momentos_criticos": [],
    "tema_actual": 1,
    "pregunta_en_tema": 0,
    "emociones_del_tema": [],
    "textos_usuario_tema": [],
    "respuestas_ia_tema": []
}

@app.post("/iniciar_entrevista")
async def iniciar_entrevista(id_postulante: str = Form(...), nombre_postulante: str = Form(...), dificultad: str = Form(...)):
    try:
        db.create_postulante(id_postulante, nombre_postulante)
        id_simulacion = db.start_simulacion(id_postulante, dificultad)
        
        sesiones_activas[id_simulacion] = {
            "historial": [],
            "modo": dificultad,
            "momentos_criticos": [],
            "tema_actual": 1,
            "pregunta_en_tema": 0,
            "emociones_del_tema": [],
            "textos_usuario_tema": [],
            "respuestas_ia_tema": []
        }
        return JSONResponse(content={"status": "exito", "id_simulacion": id_simulacion})
    except Exception as e:
        return JSONResponse(content={"status": "error", "mensaje": str(e)})

# ==========================================
# RUTA 1: LA CÁMARA (Visión)
# ==========================================


@app.post("/analizar_emocion")
async def analizar_emocion(file: UploadFile = File(...)):
    global ultima_emocion_detectada, buffer_emociones
    try:
        # 1. Convertir la imagen que llega de Unity a formato OpenCV
        contents = await file.read()
        nparr = np.frombuffer(contents, np.uint8)
        img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
        
        # 2. Convertir a escala de grises para el detector de caras de OpenCV
        gris = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        
        # 3. Detectar el rostro con OpenCV Haar Cascade
        # CONFIGURACIÓN ANTERIOR: caras = detector_caras.detectMultiScale(gris, scaleFactor=1.1, minNeighbors=5, minSize=(60, 60))
        caras = detector_caras.detectMultiScale(gris, scaleFactor=1.05, minNeighbors=4, minSize=(80, 80))
        
        if len(caras) > 0:
            # 4. Recortar el rostro detectado
            x, y, w, h = caras[0]
            # Convertir de BGR a RGB para HSEmotion
            img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
            
            # --- CÓDIGO ANTERIOR SIN MARGEN ---
            # rostro_recortado = img_rgb[y:y+h, x:x+w]
            
            # --- NUEVO CÓDIGO CON MARGEN (PADDING) ---
            margen_y = int(h * 0.15)
            margen_x = int(w * 0.15)
            
            y_inicio = max(0, y - margen_y)
            y_fin = min(img.shape[0], y + h + margen_y)
            x_inicio = max(0, x - margen_x)
            x_fin = min(img.shape[1], x + w + margen_x)
            
            rostro_recortado = img_rgb[y_inicio:y_fin, x_inicio:x_fin]
            
            # 5. Predecir la emoción con HSEmotion ONNX (mucho más preciso que DeepFace)
            emocion_hsemotion, _ = detector_emociones.predict_emotions(rostro_recortado, logits=True)
            
            # 6. Traducir al formato que Unity ya conoce (capa de compatibilidad)
            emocion_real = TRADUCCION_EMOCIONES.get(emocion_hsemotion, "neutral")
        else:
            # Si no se detectó ningún rostro, asumir neutral
            emocion_real = "neutral"
        
        # 7. Actualizar el estado global para que Gemini lo sepa
        # --- CÓDIGO ANTERIOR SIN BUFFER ---
        # ultima_emocion_detectada = emocion_real
        # print(f"📸 Visión detectó: {emocion_real}")
        
        # --- NUEVO CÓDIGO CON BUFFER (TOP 4) ---
        ultima_emocion_detectada = emocion_real
        buffer_emociones.append(emocion_real)
        
        # Mantener solo las últimas 4 emociones detectadas
        if len(buffer_emociones) > 4:
            buffer_emociones.pop(0)
            
        print(f"📸 Visión detectó: {emocion_real}")
        
        return JSONResponse(content={"status": "exito", "emocion": emocion_real})
        
    except Exception as e:
        print(f"❌ Error en cámara: {e}")
        return JSONResponse(content={"status": "error", "mensaje": str(e)})

# ==========================================
# RUTA 2: EL MICRÓFONO (Oído y Cerebro)
# ==========================================
@app.post("/procesar_audio")
async def procesar_audio(audio: UploadFile = File(...), id_simulacion: int = Form(0)):
    global ultima_emocion_detectada, buffer_emociones
    try:
        # 1. Escuchar el audio
        audio_bytes = await audio.read()
        recognizer = sr.Recognizer()
        with sr.AudioFile(io.BytesIO(audio_bytes)) as source:
            recognizer.adjust_for_ambient_noise(source, duration=0.2)
            audio_data = recognizer.record(source)
            
            # 2. Transcribir a texto
            texto_transcrito = recognizer.recognize_google(audio_data, language="es-CL")
            print(f"🗣️ El usuario dijo: {texto_transcrito}")
            
            # 3. PENSAR (El nuevo cerebro de Gemini en acción)
            sesion = sesiones_activas.get(id_simulacion)
            if not sesion:
                sesion = {
                    "historial": [], "modo": "pasivo", "momentos_criticos": [],
                    "tema_actual": 1, "pregunta_en_tema": 0,
                    "emociones_del_tema": [], "textos_usuario_tema": [], "respuestas_ia_tema": []
                }
                sesiones_activas[id_simulacion] = sesion
            
            # ¿Ya se acabaron todos los temas?
            if sesion["tema_actual"] > TOTAL_TEMAS:
                return JSONResponse(content={
                    "status": "exito", 
                    "transcripcion": texto_transcrito,
                    "respuesta_ia": "La entrevista ha concluido formalmente. Por favor, presiona el botón de Finalizar para generar tu reporte."
                })
            
            # --- Registrar emoción e input del candidato en este tema ---
            # --- CÓDIGO ANTERIOR SIN BUFFER ---
            # emocion_actual = ultima_emocion_detectada.lower()
            
            # --- NUEVO CÓDIGO CON BUFFER (TOP 4) ---
            if buffer_emociones:
                # Obtener la emoción más frecuente (moda) de todo lo grabado en este turno
                emocion_actual = max(set(buffer_emociones), key=buffer_emociones.count).lower()
                print(f"📊 Emoción promedio del turno: {emocion_actual} (calculado de {len(buffer_emociones)} lecturas)")
                buffer_emociones.clear() # Limpiar el buffer para el siguiente turno
            else:
                emocion_actual = ultima_emocion_detectada.lower()
                print(f"📊 Emoción del turno (sin buffer): {emocion_actual}")
                
            sesion["emociones_del_tema"].append(emocion_actual)
            sesion["textos_usuario_tema"].append(texto_transcrito)
            sesion["pregunta_en_tema"] += 1
            
            if emocion_actual in ['fear', 'sad']:
                sesion["momentos_criticos"].append(
                    f"Tema {sesion['tema_actual']}, Pregunta {sesion['pregunta_en_tema']}: Alta ansiedad o tristeza detectada."
                )

            # --- Inyectar la emoción en el historial para que Gemini la lea ---
            sesion["historial"].append({
                "role": "Candidato",
                "content": f"{texto_transcrito} (Emoción detectada: {emocion_actual})"
            })
            historial_str = "\n".join([f"{msg['role']}: {msg['content']}" for msg in sesion["historial"]])

            # Leer el archivo de estilo correspondiente según el modo
            modo_entrevista = sesion['modo'].upper()
            ruta_prompt = os.path.join(os.path.dirname(__file__), f"entrevista{modo_entrevista}.md")
            
            try:
                with open(ruta_prompt, "r", encoding="utf-8") as f:
                    base_prompt = f.read()
            except FileNotFoundError:
                base_prompt = f"Eres un reclutador experto en Ingeniería Informática. Modo de entrevista: {modo_entrevista}."

            prompt_sistema = f"""
            {base_prompt}
            
            HISTORIAL DE LA CONVERSACIÓN (incluye la emoción del candidato en cada intervención):
            {historial_str}
            
            CONTEXTO EN TIEMPO REAL DEL CANDIDATO:
            - Lo que acaba de decir: "{texto_transcrito}"
            - Emoción facial detectada ahora: "{emocion_actual}"
            - TEMA ACTUAL: {sesion['tema_actual']} de {TOTAL_TEMAS}.
            - PREGUNTA: {sesion['pregunta_en_tema']} de {PREGUNTAS_POR_TEMA} en este tema.
            
            INSTRUCCIONES ESTRICTAS:
            1. ADAPTACIÓN EMOCIONAL: Revisa las emociones del candidato. Ajusta tu tono.
            2. OBLIGATORIO: Mantente enfocado EXCLUSIVAMENTE en la temática del Tema {sesion['tema_actual']}.
            3. Sé conciso, máximo 3 o 4 líneas.
            4. FORMATO OBLIGATORIO: Debes responder ÚNICAMENTE con un JSON válido, sin formato markdown, con dos claves:
               - "respuesta": Tu texto de respuesta para el candidato.
               - "animacion": Elige una de estas opciones según tu respuesta: "idle", "talking", "laughing", "clap", "approval", "disapproval".
                 *REGLA DE EXPRESIVIDAD:* Sé muy expresivo corporalmente. Si lo que dice el candidato es correcto, demuestra buena actitud o responde bien técnicamente, prioriza usar "approval". Si el candidato duda, responde de forma deficiente, se contradice o evade el tema, prioriza usar "disapproval". Usa "talking" solo cuando la interacción sea completamente neutra o puramente informativa.
            Ejemplo: {{"respuesta": "Excelente respuesta, me gusta tu enfoque.", "animacion": "approval"}}
            """
            
            # Llamada al modelo con reintentos automáticos
            try:
                respuesta_cruda_raw = llamar_gemini_con_reintentos(prompt_sistema)
            except Exception as gemini_error:
                print(f"❌ Gemini caído permanentemente: {gemini_error}")
                texto_caido = "Gemini está temporalmente fuera de servicio. Por favor, intenta de nuevo en unos segundos."
                audio_caido = await generar_tts_audio(texto_caido)
                return JSONResponse(content={
                    "status": "gemini_caido",
                    "respuesta_ia": texto_caido,
                    "animacion_entrevistador": "idle",
                    "audio_tts": audio_caido
                })
            
            respuesta_cruda = respuesta_cruda_raw
            if respuesta_cruda.startswith("```json"):
                respuesta_cruda = respuesta_cruda[7:-3].strip()
            elif respuesta_cruda.startswith("```"):
                respuesta_cruda = respuesta_cruda[3:-3].strip()
            
            try:
                data = json.loads(respuesta_cruda)
                respuesta_gemini = data.get("respuesta", respuesta_cruda)
                animacion_entrevistador = data.get("animacion", "talking")
            except Exception as e:
                respuesta_gemini = respuesta_cruda
                animacion_entrevistador = "talking"

            print(f"🤖 Reclutador responde: {respuesta_gemini} [Anim: {animacion_entrevistador}]")
            
            sesion["historial"].append({"role": "Reclutador", "content": respuesta_gemini})
            sesion["respuestas_ia_tema"].append(respuesta_gemini)
            
            # --- ¿Se completaron todas las preguntas de este tema? ---
            if sesion["pregunta_en_tema"] >= PREGUNTAS_POR_TEMA:
                # Calcular la emoción predominante (moda) de todo el tema
                emociones = sesion["emociones_del_tema"]
                emocion_predominante = max(set(emociones), key=emociones.count) if emociones else "neutral"
                
                # Unir textos del tema para guardar en un solo registro de BD
                texto_usuario_unido = " | ".join(sesion["textos_usuario_tema"])
                respuesta_ia_unida = " | ".join(sesion["respuestas_ia_tema"])
                
                if id_simulacion != 0:
                    try:
                        db.insert_turno(
                            id_simulacion, sesion["tema_actual"],
                            respuesta_ia_unida, texto_usuario_unido,
                            "neutral", emocion_predominante
                        )
                    except Exception as e:
                        print(f"Error guardando turno temático: {e}")
                
                print(f"✅ Tema {sesion['tema_actual']} completado. Emoción predominante: {emocion_predominante}")
                
                # Avanzar al siguiente tema y reiniciar contadores
                sesion["tema_actual"] += 1
                sesion["pregunta_en_tema"] = 0
                sesion["emociones_del_tema"] = []
                sesion["textos_usuario_tema"] = []
                sesion["respuestas_ia_tema"] = []

            # 4. Generar audio TTS de la respuesta
            audio_tts = await generar_tts_audio(respuesta_gemini)
            
            # 5. Devolver a Unity con info del estado de la entrevista
            entrevista_terminada = sesion["tema_actual"] > TOTAL_TEMAS
            return JSONResponse(content={
                "status": "exito", 
                "transcripcion": texto_transcrito,
                "respuesta_ia": respuesta_gemini,
                "animacion_entrevistador": animacion_entrevistador,
                "tema_actual": sesion["tema_actual"],
                "pregunta_en_tema": sesion["pregunta_en_tema"],
                "tema_completado": sesion["pregunta_en_tema"] == 0 and sesion["tema_actual"] > 1,
                "entrevista_terminada": entrevista_terminada,
                "audio_tts": audio_tts
            })
            
    except sr.UnknownValueError:
        print("❌ No se entendió el audio")
        texto_error = "¿Podrías repetir eso? No te escuché bien."
        audio_error = await generar_tts_audio(texto_error)
        return JSONResponse(content={"status": "error", "respuesta_ia": texto_error, "animacion_entrevistador": "idle", "audio_tts": audio_error})
    except Exception as e:
        print(f"❌ Error en audio: {e}")
        texto_error2 = "Hubo un error de conexión, continuemos."
        audio_error2 = await generar_tts_audio(texto_error2)
        return JSONResponse(content={"status": "error", "respuesta_ia": texto_error2, "animacion_entrevistador": "idle", "audio_tts": audio_error2})


# ==========================================
# RUTA 3: TTS PARA SALUDO INICIAL
# ==========================================
@app.post("/generar_saludo_tts")
async def generar_saludo_tts(texto: str = Form(...)):
    """Genera audio TTS para un texto dado (usado para el saludo inicial)."""
    try:
        audio_tts = await generar_tts_audio(texto)
        return JSONResponse(content={"status": "exito", "audio_tts": audio_tts})
    except Exception as e:
        print(f"❌ Error generando saludo TTS: {e}")
        return JSONResponse(content={"status": "error", "mensaje": str(e)})


@app.post("/finalizar_entrevista")
async def finalizar_entrevista(id_simulacion: int = Form(...)):
    try:
        sesion = sesiones_activas.get(id_simulacion)
        if not sesion:
            return JSONResponse(content={"status": "error", "mensaje": "Sesión no encontrada"})

        historial_str = "\n".join([f"{msg['role']}: {msg['content']}" for msg in sesion["historial"]])
        momentos_str = "\n".join(sesion["momentos_criticos"])

        # Detectar si la entrevista se finalizó de manera incompleta/prematura
        es_incompleta = sesion.get("tema_actual", 1) <= TOTAL_TEMAS
        nota_incompleta = ""
        if es_incompleta:
            nota_incompleta = f"\n[NOTA IMPORTANTE: La entrevista fue finalizada prematuramente por el candidato. Solo se alcanzaron a responder algunas preguntas hasta el Tema {sesion['tema_actual']} de un total de {TOTAL_TEMAS}. Por favor, evalúa de forma justa y constructiva con base ÚNICAMENTE en el historial disponible. Agrega al inicio de tu 'resumen' el prefijo '[Entrevista Incompleta] ' y sugiere recomendaciones que incluyan terminar las futuras sesiones.]\n"

        # =====================================================
        # PILAR 1: Estabilidad Emocional (30 pts) - Python puro
        # =====================================================
        # Base de 30 puntos, se restan 5 por cada momento crítico.
        num_momentos_criticos = len(sesion["momentos_criticos"])
        puntaje_emocional = max(0, 30 - (num_momentos_criticos * 5))
        print(f"📊 Pilar 1 (Emocional): {puntaje_emocional}/30 ({num_momentos_criticos} momentos críticos)")

        # =====================================================
        # PILAR 2: Calidad de Comunicación (70 pts) - Gemini
        # =====================================================
        # Leer la rúbrica desde el archivo .md para incluirla en el prompt
        ruta_rubrica = os.path.join(os.path.dirname(__file__), "rubrica_evaluacion.md")
        try:
            with open(ruta_rubrica, "r", encoding="utf-8") as f:
                texto_rubrica = f.read()
        except FileNotFoundError:
            texto_rubrica = "Claridad (0-25), Competencia (0-25), Profesionalismo (0-20)."

        prompt_reporte = f"""
        Eres un experto en Recursos Humanos evaluando una entrevista simulada.
        {nota_incompleta}
        
        HISTORIAL COMPLETO DE LA ENTREVISTA:
        {historial_str}

        MOMENTOS CRÍTICOS DETECTADOS (Emociones negativas del candidato):
        {momentos_str if momentos_str else "Ninguno detectado."}

        RÚBRICA DE EVALUACIÓN QUE DEBES SEGUIR ESTRICTAMENTE:
        {texto_rubrica}

        INSTRUCCIONES OBLIGATORIAS:
        1. Evalúa SOLO el Pilar 2 (Calidad de Comunicación). El Pilar 1 (Emocional) ya fue calculado por el sistema.
        2. Asigna un puntaje numérico a CADA una de estas 3 métricas respetando los rangos exactos:
           - "claridad": un número entero entre 0 y 25
           - "competencia": un número entero entre 0 y 25
           - "profesionalismo": un número entero entre 0 y 20
        3. También genera las siguientes claves descriptivas:
           - "resumen": un texto de 3-4 líneas resumiendo fortalezas y debilidades del candidato.
           - "estado_emocional": descripción breve de su comportamiento emocional durante la entrevista.
           - "recomendaciones": lista de strings con recomendaciones concretas de mejora.
           - "momentos_criticos": lista de objetos con las claves "pregunta" y "observacion".
        
        Devuelve SOLAMENTE un JSON válido, sin formato Markdown, sin ```json, sin texto adicional.
        Ejemplo de formato esperado:
        {{"claridad": 20, "competencia": 18, "profesionalismo": 16, "resumen": "...", "estado_emocional": "...", "recomendaciones": ["..."], "momentos_criticos": [{{"pregunta": "...", "observacion": "..."}}]}}
        """
        
        try:
            reporte_raw = llamar_gemini_con_reintentos(prompt_reporte)
        except Exception as gemini_error:
            print(f"❌ Gemini caído al generar reporte: {gemini_error}")
            return JSONResponse(content={
                "status": "gemini_caido",
                "mensaje": "Gemini está temporalmente fuera de servicio. Intenta generar el reporte de nuevo en unos segundos."
            })
        
        reporte_json = reporte_raw
        if reporte_json.startswith("```json"):
            reporte_json = reporte_json[7:-3].strip()
        elif reporte_json.startswith("```"):
            reporte_json = reporte_json[3:-3].strip()
            
        data = json.loads(reporte_json)
        
        # Extraer los 3 sub-puntajes de Gemini (Pilar 2)
        claridad = min(25, max(0, int(data.get("claridad", 0))))
        competencia = min(25, max(0, int(data.get("competencia", 0))))
        profesionalismo = min(20, max(0, int(data.get("profesionalismo", 0))))
        puntaje_comunicacion = claridad + competencia + profesionalismo
        print(f"📊 Pilar 2 (Comunicación): {puntaje_comunicacion}/70 (Claridad:{claridad}, Competencia:{competencia}, Profesionalismo:{profesionalismo})")

        # =====================================================
        # PUNTAJE FINAL = Pilar 1 + Pilar 2
        # =====================================================
        puntaje_final = puntaje_emocional + puntaje_comunicacion
        print(f"🏆 PUNTAJE FINAL: {puntaje_final}/100")

        # Enriquecer el reporte con el desglose completo
        data["puntaje"] = puntaje_final
        data["desglose"] = {
            "estabilidad_emocional": {
                "puntaje": puntaje_emocional,
                "maximo": 30,
                "momentos_criticos_detectados": num_momentos_criticos
            },
            "calidad_comunicacion": {
                "puntaje": puntaje_comunicacion,
                "maximo": 70,
                "claridad": claridad,
                "competencia": competencia,
                "profesionalismo": profesionalismo
            }
        }

        # Guardaremos el JSON completo en la columna Resumen
        resumen_full_json = json.dumps(data, ensure_ascii=False)
        
        # Guardaremos los momentos criticos en la nueva columna
        momentos_criticos_resumen = json.dumps(data.get("momentos_criticos", []), ensure_ascii=False)
        
        if id_simulacion != 0:
            db.finish_simulacion(id_simulacion)
            db.insert_reporte(id_simulacion, puntaje_final, resumen_full_json, momentos_criticos_resumen)
            
        return JSONResponse(content={"status": "exito", "reporte": data})
    except Exception as e:
        print(f"❌ Error al generar reporte: {e}")
        return JSONResponse(content={"status": "error", "mensaje": str(e)})


@app.get("/historial_reportes")
async def historial_reportes():
    try:
        resultados = db.get_historial()
        # Convertir DATETIME a string para serializar
        for r in resultados:
            if r["TiempoInicio"]:
                r["TiempoInicio"] = r["TiempoInicio"].strftime("%Y-%m-%d %H:%M:%S")
            # Parsear el resumen JSON si existe
            if r.get("Resumen"):
                try:
                    r["Resumen_JSON"] = json.loads(r["Resumen"])
                except json.JSONDecodeError:
                    r["Resumen_JSON"] = None
            if r.get("ResumenMomentoCritico"):
                try:
                    r["ResumenMomentoCritico_JSON"] = json.loads(r["ResumenMomentoCritico"])
                except json.JSONDecodeError:
                    r["ResumenMomentoCritico_JSON"] = None
        return JSONResponse(content={"status": "exito", "historial": resultados})
    except Exception as e:
        return JSONResponse(content={"status": "error", "mensaje": str(e)})


@app.get("/reporte/{id_simulacion}")
async def get_reporte_endpoint(id_simulacion: int):
    try:
        reporte = db.get_reporte(id_simulacion)
        if not reporte:
            return JSONResponse(content={"status": "error", "mensaje": "Reporte no encontrado"}, status_code=404)
        
        # Parsear el resumen JSON
        if reporte.get("Resumen"):
            try:
                reporte["Resumen_JSON"] = json.loads(reporte["Resumen"])
            except json.JSONDecodeError:
                reporte["Resumen_JSON"] = None
                
        # Parsear ResumenMomentoCritico
        if reporte.get("ResumenMomentoCritico"):
            try:
                reporte["ResumenMomentoCritico_JSON"] = json.loads(reporte["ResumenMomentoCritico"])
            except json.JSONDecodeError:
                reporte["ResumenMomentoCritico_JSON"] = None
                
        return JSONResponse(content={"status": "exito", "reporte": reporte})
    except Exception as e:
        return JSONResponse(content={"status": "error", "mensaje": str(e)})

@app.delete("/reporte/{id_simulacion}")
async def eliminar_reporte(id_simulacion: int):
    try:
        print(f">>> RECIBIDA PETICION DELETE PARA ID: {id_simulacion}")
        exito = db.delete_reporte(id_simulacion)
        if exito:
            print(f">>> ELIMINACION EXITOSA EN DB PARA ID: {id_simulacion}")
            return JSONResponse(content={"status": "exito", "mensaje": "Reporte eliminado correctamente"})
        else:
            print(f">>> ELIMINACION FALLIDA EN DB PARA ID: {id_simulacion}")
            return JSONResponse(content={"status": "error", "mensaje": "No se pudo eliminar el reporte"})
    except Exception as e:
        print(f">>> ERROR CRITICO EN ENDPOINT DELETE: {e}")
        return JSONResponse(content={"status": "error", "mensaje": str(e)})


# ==========================================
# ARRANQUE AUTOMÁTICO DEL SERVIDOR
# ==========================================
if __name__ == "__main__":
    import sys
    import uvicorn
    # Forzar UTF-8 para que los emojis del código no rompan la consola de Windows
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    sys.stderr.reconfigure(encoding='utf-8', errors='replace')
    print("=" * 50)
    print(">>> Servidor de Entrevista IA iniciandose...")
    print(">>> Direccion: http://localhost:8000")
    print(">>> Para detener: Ctrl + C")
    print("=" * 50)
    uvicorn.run(app, host="0.0.0.0", port=8000)
