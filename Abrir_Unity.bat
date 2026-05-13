@echo off
title Abriendo Unity...
echo ==================================================
echo     Abriendo el proyecto en Unity Editor...
echo     Esto puede tardar unos segundos.
echo ==================================================
start "" "C:\Program Files\Unity\Hub\Editor\6000.4.1f1\Editor\Unity.exe" -projectPath "%~dp0Frontend_Unity"
exit
