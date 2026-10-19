@echo off
setlocal
cd /d "%~dp0"
set "MUSIKBOX_CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%MUSIKBOX_CSC%" set "MUSIKBOX_CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%MUSIKBOX_CSC%" (
 echo C#-Compiler nicht gefunden. .NET Framework 4.8 wird benoetigt.
 pause
 exit /b 1
)
"%MUSIKBOX_CSC%" /nologo /target:winexe /optimize+ /out:Musikbox-Werkstatt.exe /reference:System.Core.dll /reference:System.Xml.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Runtime.Serialization.dll src\*.cs
if errorlevel 1 (
 echo Erstellen fehlgeschlagen.
) else (
 echo Musikbox-Werkstatt.exe erfolgreich erstellt.
)
pause
