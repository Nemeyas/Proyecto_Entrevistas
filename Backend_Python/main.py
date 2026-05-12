import io
import speech_recognition as sr
from fastapi import FastAPI, UploadFile, File, Form
from fastapi.responses import JSONResponse
import numpy as np
import cv2
from deepface import DeepFace

# --- NUEVA LIBRERÍA DE GOOGLE ---
from google import genai

# Configura tu cliente con la API Key
client = genai.Client(api_key="APY KEY")

# Variable global para recordar la emoción
ultima_emocion_detectada = "neutral"

app = FastAPI()

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
async def procesar_audio(audio: UploadFile = File(...)):
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
            prompt_sistema = f"""
            Eres un reclutador experto en Ingeniería Informática de una empresa top.
            
            CONTEXTO EN TIEMPO REAL DEL CANDIDATO:
            - Lo que acaba de decir: "{texto_transcrito}"
            - Su emoción facial detectada en este milisegundo es: "{ultima_emocion_detectada}"
            
            INSTRUCCIONES ESTRICTAS:
            1. OBLIGATORIO: Inicia tu respuesta haciendo un comentario directo pero profesional sobre su emoción (Ej: Si es 'happy' nota su entusiasmo, si es 'fear' o 'sad' dale calma, si es 'neutral' valora su serenidad, etc).
            2. Evalúa brevemente lo que te acaba de decir.
            3. Termina siempre con una nueva pregunta técnica o situacional.
            4. Sé conciso, máximo 3 o 4 líneas.
            """
            
            # Así se llama al modelo en la librería nueva
            response = client.models.generate_content(
                model='gemini-2.5-flash', 
                contents=prompt_sistema,
            )
            
            respuesta_gemini = response.text
            print(f"🤖 Reclutador responde: {respuesta_gemini}")
            
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