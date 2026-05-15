import cv2
import numpy as np
from hsemotion_onnx.facial_emotions import HSEmotionRecognizer

def main():
    print(">>> Iniciando modelos (esto puede tardar unos segundos)...")
    
    # Inicializar detector de caras con OpenCV (ya viene incluido, no necesita PyTorch)
    detector_caras = cv2.CascadeClassifier(cv2.data.haarcascades + 'haarcascade_frontalface_default.xml')
    
    # Inicializar detector de emociones con HSEmotion ONNX (sin parámetro device)
    detector_emociones = HSEmotionRecognizer(model_name='enet_b0_8_best_afew')
    
    print(">>> Modelos cargados. Abriendo camara...")

    # Abrir la cámara web (el 0 suele ser la cámara por defecto)
    cap = cv2.VideoCapture(0)

    if not cap.isOpened():
        print("No se pudo abrir la camara.")
        return

    print(">>> Camara activa. Presiona la tecla 'q' para salir.")

    while True:
        # Leer un frame de la cámara
        ret, frame = cap.read()
        if not ret:
            break

        # Convertir a escala de grises para el detector de caras de OpenCV
        gris = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        
        # Detectar caras con OpenCV Haar Cascade
        caras = detector_caras.detectMultiScale(gris, scaleFactor=1.1, minNeighbors=5, minSize=(60, 60))

        if len(caras) > 0:
            # Tomar la primera cara detectada
            x, y, w, h = caras[0]
            
            # Convertir de BGR a RGB para HSEmotion
            frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            
            # Recortar el rostro
            rostro_recortado = frame_rgb[y:y+h, x:x+w]

            # Verificar que el recorte sea válido
            if rostro_recortado.size > 0:
                # Predecir emoción con HSEmotion ONNX
                emocion, _ = detector_emociones.predict_emotions(rostro_recortado, logits=False)
                
                # Dibujar rectángulo verde alrededor de la cara
                cv2.rectangle(frame, (x, y), (x+w, y+h), (0, 255, 0), 2)
                
                # Poner el texto de la emoción encima del rectángulo
                cv2.putText(frame, emocion, (x, y - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.9, (0, 255, 0), 2)

        # Mostrar el video en una ventana
        cv2.imshow('Test Emociones HSEmotion', frame)

        # Salir si se presiona la tecla 'q'
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    # Limpiar
    cap.release()
    cv2.destroyAllWindows()

if __name__ == "__main__":
    main()
