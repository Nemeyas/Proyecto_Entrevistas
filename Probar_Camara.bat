@echo off
title Prueba de Emociones (HSEmotion)
echo ==================================================
echo     PROBADOR DE EMOCIONES EN VIVO
echo ==================================================
echo.
echo  Cargando la inteligencia artificial...
echo  Se abrira una ventana con tu camara.
echo  Para salir, haz clic en la camara y presiona 'q'.
echo.
echo ==================================================

cd /d "%~dp0Backend_Python"

if not exist "venv\Scripts\python.exe" (
    echo.
    echo  [ERROR] No se encontro el entorno virtual.
    echo  Asegurate de haber ejecutado la instalacion primero.
    echo.
    pause
    exit /b 1
)

call venv\Scripts\activate
python test_emociones.py

echo.
echo ==================================================
echo  Prueba finalizada.
echo ==================================================
pause
