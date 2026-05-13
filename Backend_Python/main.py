import io
import os
import speech_recognition as sr
from fastapi import FastAPI, UploadFile, File, Form
from fastapi.responses import JSONResponse
import numpy as np
import cv2
from deepface import DeepFace

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

from database import db
import json
try:
    db.init_db()
    print(">>> Base de datos inicializada correctamente.")
except Exception as e:
    print(f">>> Advertencia: No se pudo conectar a la base de datos MySQL: {e}")

app = FastAPI()

# Memoria de sesiones temporales (fallback por si Unity no envía IDSimulacion aún)
sesiones_activas = {}
sesiones_activas[0] = {
    "historial": [],
    "modo": "pasivo",
    "momentos_criticos": [],
    "turno": 0
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
            "turno": 0
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
        
        # 2. Analizar con DeepFace
        enforce_detection=False #evita que el servidor explote si parpadeas o te mueves
        resultados = DeepFace.analyze(img, actions=['emotion'], enforce_detection=False)
        
        # 3. Extraer la emoción más fuerte
        emocion_real = resultados[0]['dominant_emotion']
        
        # 4. Actualizar el estado global para que Gemini lo sepa
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
                sesion = {"historial": [], "modo": "pasivo", "momentos_criticos": [], "turno": 0}
                sesiones_activas[id_simulacion] = sesion
            
            if sesion["turno"] >= 4:
                return JSONResponse(content={
                    "status": "exito", 
                    "transcripcion": texto_transcrito,
                    "respuesta_ia": "La entrevista ha concluido formalmente. Por favor, presiona el botón de Finalizar para generar tu reporte."
                })
                
            sesion["turno"] += 1
            historial_str = "\n".join([f"{msg['role']}: {msg['content']}" for msg in sesion["historial"]])
            
            emocion_actual = ultima_emocion_detectada.lower()
            if emocion_actual in ['fear', 'sad']:
                sesion["momentos_criticos"].append(f"Turno {sesion['turno']}: Alta ansiedad o tristeza detectada.")

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
            
            HISTORIAL DE LA CONVERSACIÓN:
            {historial_str}
            
            CONTEXTO EN TIEMPO REAL DEL CANDIDATO:
            - Lo que acaba de decir: "{texto_transcrito}"
            - Su emoción facial detectada en este milisegundo es: "{ultima_emocion_detectada}"
            - TURNO ACTUAL DE LA ENTREVISTA: {sesion['turno']} de 4.
            
            INSTRUCCIONES ESTRICTAS:
            1. OBLIGATORIO: Inicia tu respuesta comentando sobre su emoción facial para darle realismo.
            2. Evalúa brevemente lo que acaba de decir.
            3. OBLIGATORIO: Revisa en tus instrucciones la ESTRUCTURA DE LA ENTREVISTA y formula tu pregunta/respuesta enfocada EXCLUSIVAMENTE en el tema del Turno {sesion['turno']}.
            4. Sé conciso, máximo 3 o 4 líneas.
            """
            
            # Así se llama al modelo en la librería nueva
            response = client.models.generate_content(
                model='gemini-2.5-flash', 
                contents=prompt_sistema,
            )
            
            respuesta_gemini = response.text
            print(f"🤖 Reclutador responde: {respuesta_gemini}")
            
            sesion["historial"].append({"role": "Candidato", "content": texto_transcrito})
            sesion["historial"].append({"role": "Reclutador", "content": respuesta_gemini})
            
            if id_simulacion != 0:
                try:
                    db.insert_turno(id_simulacion, sesion["turno"], respuesta_gemini, texto_transcrito, "neutral", ultima_emocion_detectada)
                except Exception as e:
                    print(f"Error guardando turno: {e}")

            # 4. Devolver a Unity
            return JSONResponse(content={
                "status": "exito", 
                "transcripcion": texto_transcrito,
                "respuesta_ia": respuesta_gemini
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

        prompt_reporte = f"""
        Eres un experto en Recursos Humanos analizando una entrevista simulada.
        HISTORIAL DE LA ENTREVISTA:
        {historial_str}

        MOMENTOS CRÍTICOS DETECTADOS (Emociones negativas repetidas):
        {momentos_str}

        Genera un reporte final en formato JSON estricto con las siguientes claves:
        - "puntaje": (número del 1 al 10 evaluando el desempeño general)
        - "resumen": (un texto de 3-4 líneas resumiendo fortalezas, debilidades y recomendaciones basado en la charla y emociones)
        No uses formato Markdown, solo devuelve el string JSON válido.
        """
        response = client.models.generate_content(
            model='gemini-2.5-flash',
            contents=prompt_reporte,
        )
        
        reporte_json = response.text.strip()
        if reporte_json.startswith("```json"):
            reporte_json = reporte_json[7:-3]
        elif reporte_json.startswith("```"):
            reporte_json = reporte_json[3:-3]
            
        data = json.loads(reporte_json)
        puntaje = float(data.get("puntaje", 0))
        resumen = data.get("resumen", "Sin resumen disponible")
        
        if id_simulacion != 0:
            db.finish_simulacion(id_simulacion)
            db.insert_reporte(id_simulacion, puntaje, resumen)
            
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
        return JSONResponse(content={"status": "exito", "historial": resultados})
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
