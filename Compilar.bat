@echo off
title Compilando Entrevista IA...

echo.
echo ==================================================
echo     COMPILADOR DE ENTREVISTA IA
echo     Generando ejecutable Windows (.exe)
echo ==================================================
echo.

set UNITY_EXE="C:\Program Files\Unity\Hub\Editor\6000.4.1f1\Editor\Unity.exe"
set PROJECT_PATH=%~dp0Frontend_Unity
set LOG_FILE=%~dp0Build\build_log.txt

if not exist %UNITY_EXE% (
    echo  [ERROR] No se encontro Unity en:
    echo    %UNITY_EXE%
    echo.
    echo  Verifica que Unity 6000.4.1f1 este instalado.
    echo.
    pause
    exit /b 1
)

if not exist "%~dp0Build" (
    mkdir "%~dp0Build"
)

echo  [1/3] Cerrando instancias previas de Unity...
taskkill /f /im Unity.exe >nul 2>&1
timeout /t 2 /nobreak >nul

echo  [2/3] Compilando el proyecto (esto toma varios minutos)...
echo         Unity trabaja en segundo plano, NO cierres esta ventana.
echo         Log en: %LOG_FILE%
echo.

%UNITY_EXE% -batchmode -quit -projectPath "%PROJECT_PATH%" -executeMethod BuildProject.BuildWindows -logFile "%LOG_FILE%"

if exist "%~dp0Build\EntrevistaIA.exe" (
    echo.
    echo ==================================================
    echo   COMPILACION EXITOSA
    echo ==================================================
    echo.
    echo  El ejecutable esta en:
    echo    %~dp0Build\EntrevistaIA.exe
    echo.
    echo  IMPORTANTE: Para usar la app necesitas:
    echo    1. Ejecutar "Iniciar_Servidor.bat" PRIMERO
    echo    2. Luego abrir "Build\EntrevistaIA.exe"
    echo.
) else (
    echo.
    echo ==================================================
    echo   ERROR EN LA COMPILACION
    echo ==================================================
    echo.
    echo  Revisa el log para detalles:
    echo    %LOG_FILE%
    echo.
)

pause
