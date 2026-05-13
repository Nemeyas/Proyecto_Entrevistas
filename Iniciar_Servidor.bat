@echo off
title Servidor Entrevista IA

echo ==================================================
echo     SERVIDOR DE ENTREVISTA IA
echo ==================================================
echo.
echo  Iniciando el servidor backend...
echo  NO cierres esta ventana mientras uses la app.
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
python main.py

echo.
echo ==================================================
echo  El servidor se ha detenido.
echo ==================================================
pause
