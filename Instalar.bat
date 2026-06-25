@echo off
title Instalador - Entrevista IA
echo ==================================================
echo     INSTALADOR - ENTREVISTA IA
echo ==================================================
echo.
echo  Este script instalara todo lo necesario para
echo  ejecutar el proyecto. Solo necesitas tener
echo  conexion a Internet.
echo.
echo  Presiona cualquier tecla para comenzar...
pause >nul
echo.

:: --------------------------------------------------
:: PASO 1: Verificar si Python 3.12 esta instalado
:: --------------------------------------------------
echo [1/4] Verificando Python 3.12...

py -3.12 --version >nul 2>&1
if %errorlevel% equ 0 (
    echo        Python 3.12 encontrado. OK
    goto :crear_venv
)

echo        Python 3.12 no encontrado. Descargando...
echo.

:: Descargar Python 3.12.10
powershell -Command "Invoke-WebRequest -Uri 'https://www.python.org/ftp/python/3.12.10/python-3.12.10-amd64.exe' -OutFile '%TEMP%\python-3.12.10-amd64.exe'"
if %errorlevel% neq 0 (
    echo.
    echo  [ERROR] No se pudo descargar Python 3.12.
    echo  Verifica tu conexion a Internet e intenta de nuevo.
    echo  Tambien puedes descargarlo manualmente desde:
    echo  https://www.python.org/downloads/release/python-31210/
    pause
    exit /b 1
)

echo        Instalando Python 3.12 (esto puede tardar unos minutos)...
start /wait "" "%TEMP%\python-3.12.10-amd64.exe" /quiet InstallAllUsers=0 PrependPath=0 Include_launcher=1 Include_pip=1
if %errorlevel% neq 0 (
    echo.
    echo  [ERROR] La instalacion de Python fallo.
    echo  Intenta ejecutar manualmente: %TEMP%\python-3.12.10-amd64.exe
    pause
    exit /b 1
)
echo        Python 3.12 instalado correctamente. OK

:: --------------------------------------------------
:: PASO 2: Habilitar Long Paths en Windows
:: --------------------------------------------------
echo.
echo [2/4] Habilitando soporte de rutas largas en Windows...
powershell -Command "Start-Process powershell -Verb RunAs -ArgumentList '-Command', 'New-ItemProperty -Path \"HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem\" -Name \"LongPathsEnabled\" -Value 1 -PropertyType DWORD -Force' -Wait" >nul 2>&1
echo        Rutas largas habilitadas. OK

:: --------------------------------------------------
:: PASO 3: Crear entorno virtual
:: --------------------------------------------------
:crear_venv
echo.
echo [3/4] Preparando entorno virtual...

cd /d "%~dp0Backend_Python"

if exist "venv\Scripts\python.exe" (
    echo        Entorno virtual ya existe. Saltando...
    goto :instalar_deps
)

py -3.12 -m venv venv
if %errorlevel% neq 0 (
    echo.
    echo  [ERROR] No se pudo crear el entorno virtual.
    pause
    exit /b 1
)
echo        Entorno virtual creado. OK

:: --------------------------------------------------
:: PASO 4: Instalar dependencias
:: --------------------------------------------------
:instalar_deps
echo.
echo [4/4] Instalando dependencias (esto puede tardar varios minutos)...
echo        Descargando HSEmotion ONNX, OpenCV, etc.
echo.

venv\Scripts\pip.exe install fastapi uvicorn opencv-python hsemotion-onnx onnxruntime SpeechRecognition google-genai python-multipart numpy python-dotenv edge-tts mysql-connector-python pillow 2>&1 | findstr /i "successfully error"

if %errorlevel% neq 0 (
    echo.
    echo        Verificando instalacion...
)

:: --------------------------------------------------
:: PASO 4b: Parchar bug conocido de hsemotion_onnx
:: --------------------------------------------------
echo.
echo        Aplicando parche de compatibilidad a HSEmotion...
venv\Scripts\python.exe -c "import os; p=os.path.join('venv','Lib','site-packages','hsemotion_onnx','facial_emotions.py'); f=open(p,'r'); c=f.read(); f.close(); c=c.replace('import urllib\n','import urllib\nimport urllib.request\n') if 'import urllib.request' not in c else c; f=open(p,'w'); f.write(c); f.close(); print('        Parche aplicado. OK')"

:: --------------------------------------------------
:: PASO 4c: Pre-descargar modelo de emociones
:: --------------------------------------------------
echo        Descargando modelo de IA para emociones (solo la primera vez)...
venv\Scripts\python.exe -c "from hsemotion_onnx.facial_emotions import HSEmotionRecognizer; HSEmotionRecognizer(model_name='enet_b0_8_best_afew'); print('        Modelo descargado. OK')"

:: Verificar que todo se instalo correctamente
venv\Scripts\python.exe -c "import cv2; import fastapi; import hsemotion_onnx; import speech_recognition; import google.genai; import uvicorn; import edge_tts; import mysql.connector; from PIL import Image; print('OK')" >nul 2>&1
if %errorlevel% equ 0 (
    echo.
    echo ==================================================
    echo     INSTALACION COMPLETADA CON EXITO
    echo ==================================================
    echo.
    echo  Todo esta listo. Para usar el proyecto:
    echo.
    echo  1. Ejecuta "Iniciar_Servidor.bat" para el backend
    echo  2. Abre Unity con "Abrir_Unity.bat" o Unity Hub
    echo  3. Presiona Play en Unity
    echo.
    echo ==================================================
) else (
    echo.
    echo ==================================================
    echo     HUBO PROBLEMAS EN LA INSTALACION
    echo ==================================================
    echo.
    echo  Algunas dependencias no se instalaron correctamente.
    echo  Intenta ejecutar manualmente:
    echo.
    echo    cd Backend_Python
    echo    venv\Scripts\pip.exe install fastapi uvicorn opencv-python hsemotion-onnx onnxruntime SpeechRecognition google-genai python-multipart numpy python-dotenv edge-tts mysql-connector-python pillow
    echo.
    echo ==================================================
)

pause
