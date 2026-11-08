@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Bauen.ps1"
if errorlevel 1 (
 echo Build fehlgeschlagen. Details stehen in der Ausgabe.
 pause
 exit /b 1
)
echo Fertiges portables Paket im Unterordner Ausgabe.
pause
