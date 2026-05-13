# Entrevista IA — Simulador de Entrevistas con Inteligencia Artificial

Sistema de entrevistas técnicas simuladas que usa visión por computador, reconocimiento de voz e inteligencia artificial para crear una experiencia de entrevista realista.

## Cómo funciona

El candidato se sienta frente al computador. Una cámara analiza sus expresiones faciales en tiempo real, un micrófono captura sus respuestas, y un entrevistador IA (Gemini) responde de forma inteligente adaptándose tanto a lo que dice como a cómo se siente.

```
┌─────────────┐     Imagen/Audio      ┌──────────────────┐
│   Unity 3D  │ ──────────────────────>│  Servidor Python  │
│  (Frontend) │                        │    (Backend)      │
│             │ <──────────────────────│                   │
│  - Cámara   │   Emoción + Respuesta  │  - DeepFace (CV)  │
│  - Micro    │         IA             │  - SpeechRecog.   │
│  - UI Chat  │                        │  - Gemini AI      │
└─────────────┘                        └──────────────────┘
```

## Requisitos previos

- **Windows 10/11** (64-bit)
- **Conexión a Internet** (para la instalación y la API de Gemini)
- **Unity Hub** con Unity **6000.4.1f1** o superior ([Descargar Unity Hub](https://unity.com/download))
- **Cámara web** (o [DroidCam](https://www.dev47apps.com/) para usar el celular como cámara)
- **Micrófono**
- Una **API Key de Google AI Studio** ([Obtener gratis](https://aistudio.google.com/apikey))

> **Nota:** No necesitas instalar Python manualmente. El instalador lo hace por ti.

## Instalación rápida

### 1. Clonar el repositorio

```bash
git clone https://github.com/TU_USUARIO/Proyecto_Entrevistas.git
cd Proyecto_Entrevistas
```

### 2. Ejecutar el instalador

Hacer **doble clic** en:

```
📄 Instalar.bat
```

Esto automáticamente:
- ✅ Descarga e instala Python 3.12 (si no lo tienes)
- ✅ Habilita soporte de rutas largas en Windows
- ✅ Crea un entorno virtual aislado
- ✅ Instala todas las dependencias (TensorFlow, DeepFace, OpenCV, etc.)

> La instalación toma ~5-10 minutos dependiendo de tu Internet.

### 3. Configurar la API Key de Gemini

Copiar el archivo de ejemplo y poner tu clave:

```bash
cd Backend_Python
copy .env.example .env
```

Abrir `Backend_Python/.env` y reemplazar:

```
GEMINI_API_KEY=tu_clave_real_aqui
```

> Tu API Key nunca se sube a GitHub (el archivo `.env` está en `.gitignore`).

## Cómo ejecutar

### Paso 1 — Iniciar el servidor

Hacer **doble clic** en:

```
📄 Iniciar_Servidor.bat
```

Esperar a que aparezca:
```
INFO:     Uvicorn running on http://0.0.0.0:8000
```

> **No cerrar esta ventana** mientras uses la app.

### Paso 2 — Abrir Unity

Hacer **doble clic** en:

```
📄 Abrir_Unity.bat
```

O abrir Unity Hub y seleccionar la carpeta `Frontend_Unity`.

### Paso 3 — Presionar Play en Unity

¡Listo! La cámara se activará, el entrevistador te saludará, y puedes empezar a responder.

## Estructura del proyecto

```
Proyecto_Entrevistas/
├── Backend_Python/
│   ├── main.py              # Servidor FastAPI (emociones + audio + Gemini)
│   ├── requirements.txt     # Dependencias Python
│   └── venv/                # Entorno virtual (se crea con Instalar.bat)
├── Frontend_Unity/
│   ├── Assets/
│   │   ├── WebcamSender.cs  # Captura webcam + micrófono + envío al servidor
│   │   ├── GestorChat.cs    # Manejo del historial de chat en la UI
│   │   └── Scenes/          # Escena principal de Unity
│   ├── Packages/
│   └── ProjectSettings/
├── Instalar.bat             # Instalador automático (ejecutar 1 vez)
├── Iniciar_Servidor.bat     # Arranca el backend Python
├── Abrir_Unity.bat          # Abre Unity directamente en el proyecto
└── README.md                # Este archivo
```

## Tecnologías utilizadas

| Componente | Tecnología |
|---|---|
| Frontend | Unity 6 (C#) |
| Backend | Python + FastAPI |
| Visión por computador | OpenCV + DeepFace |
| Reconocimiento de voz | SpeechRecognition (Google) |
| Inteligencia artificial | Gemini 2.5 Flash (Google) |
| Comunicación | HTTP REST (UnityWebRequest ↔ FastAPI) |

## Solución de problemas

| Problema | Solución |
|---|---|
| "No se reconoce como comando" al ejecutar .bat | Haz clic derecho → Ejecutar como administrador |
| La cámara no se detecta | Verificar que DroidCam esté conectado antes de abrir Unity |
| Error de API Key | Verificar que la key en `main.py` sea válida |
| El servidor no arranca | Verificar que el puerto 8000 no esté ocupado |
| Errores de TensorFlow al instalar | Ejecutar `Instalar.bat` como administrador |

## Licencia

Proyecto académico — Tecnologías Emergentes.
