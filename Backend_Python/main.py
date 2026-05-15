import io
import os
import speech_recognition as sr
from fastapi import FastAPI, UploadFile, File, Form
from fastapi.responses import JSONResponse
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

# Variable global para recordar la emoción
ultima_emocion_detectada = "neutral"

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
    global ultima_emocion_detectada
    try:
        # 1. Convertir la imagen que llega de Unity a formato OpenCV
        contents = await file.read()
        nparr = np.frombuffer(contents, np.uint8)
        img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
        
        # 2. Convertir a escala de grises para el detector de caras de OpenCV
        gris = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        
        # 3. Detectar el rostro con OpenCV Haar Cascade
        caras = detector_caras.detectMultiScale(gris, scaleFactor=1.1, minNeighbors=5, minSize=(60, 60))
        
        if len(caras) > 0:
            # 4. Recortar el rostro detectado
            x, y, w, h = caras[0]
            # Convertir de BGR a RGB para HSEmotion
            img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
            rostro_recortado = img_rgb[y:y+h, x:x+w]
            
            # 5. Predecir la emoción con HSEmotion ONNX (mucho más preciso que DeepFace)
            emocion_hsemotion, _ = detector_emociones.predict_emotions(rostro_recortado, logits=True)
            
            # 6. Traducir al formato que Unity ya conoce (capa de compatibilidad)
            emocion_real = TRADUCCION_EMOCIONES.get(emocion_hsemotion, "neutral")
        else:
            # Si no se detectó ningún rostro, asumir neutral
            emocion_real = "neutral"
        
        # 7. Actualizar el estado global para que Gemini lo sepa
        ultima_emocion_detectada = emocion_real
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
    global ultima_emocion_detectada
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
            emocion_actual = ultima_emocion_detectada.lower()
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
            1. ADAPTACIÓN EMOCIONAL: Revisa las emociones entre paréntesis en el historial. Si el candidato muestra nerviosismo, miedo o tristeza persistente, ajusta tu tono (más empático o más desafiante según el modo). Si muestra confianza, puedes subir la exigencia.
            2. Evalúa brevemente lo que acaba de decir.
            3. OBLIGATORIO: Mantente enfocado EXCLUSIVAMENTE en la temática del Tema {sesion['tema_actual']}.
            4. Si es la pregunta 1 del tema, presenta el tema y haz la pregunta principal. Si es la pregunta {PREGUNTAS_POR_TEMA} (última del tema), haz una pregunta de profundización o cierre del tema.
            5. Sé conciso, máximo 3 o 4 líneas.
            """
            
            # Llamada al modelo
            response = client.models.generate_content(
                model='gemini-2.0-flash', 
                contents=prompt_sistema,
            )
            
            respuesta_gemini = response.text
            print(f"🤖 Reclutador responde: {respuesta_gemini}")
            
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

            # 4. Devolver a Unity con info del estado de la entrevista
            entrevista_terminada = sesion["tema_actual"] > TOTAL_TEMAS
            return JSONResponse(content={
                "status": "exito", 
                "transcripcion": texto_transcrito,
                "respuesta_ia": respuesta_gemini,
                "tema_actual": sesion["tema_actual"],
                "pregunta_en_tema": sesion["pregunta_en_tema"],
                "tema_completado": sesion["pregunta_en_tema"] == 0 and sesion["tema_actual"] > 1,
                "entrevista_terminada": entrevista_terminada
            })
            
    except sr.UnknownValueError:
        print("❌ No se entendió el audio")
        return JSONResponse(content={"status": "error", "respuesta_ia": "¿Podrías repetir eso? No te escuché bien."})
    except Exception as e:
        print(f"❌ Error en audio: {e}")
        return JSONResponse(content={"status": "error", "respuesta_ia": "Hubo un error de conexión, continuemos."})


@app.post("/finalizar_entrevista")
async def finalizar_entrevista(id_simulacion: int = Form(...)):
    try:
        sesion = sesiones_activas.get(id_simulacion)
        if not sesion:
            return JSONResponse(content={"status": "error", "mensaje": "Sesión no encontrada"})

        historial_str = "\n".join([f"{msg['role']}: {msg['content']}" for msg in sesion["historial"]])
        momentos_str = "\n".join(sesion["momentos_criticos"])

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
        
        response = client.models.generate_content(
            model='gemini-2.0-flash',
            contents=prompt_reporte,
        )
        
        reporte_json = response.text.strip()
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
        
        if id_simulacion != 0:
            db.finish_simulacion(id_simulacion)
            db.insert_reporte(id_simulacion, puntaje_final, resumen_full_json)
            
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
                
        return JSONResponse(content={"status": "exito", "reporte": reporte})
    except Exception as e:
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
