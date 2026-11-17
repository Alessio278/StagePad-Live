@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Genera-Screenshot.ps1"
if errorlevel 1 (
 echo.
 echo Acquisizione non completata. Leggi il messaggio qui sopra.
 pause
)
