# Build v0.60

Aktuell: vorhandenes net8.0-windows-Projekt mit .NET SDK 8 bauen, Bauen.ps1 nutzen. Das fertige portable Paket enthält die Windows-x64-Laufzeit. Nachstehende Angaben zu Framework-4.8-Builds sind historisch.

# Nativer Studio-Build

Die Anwendung basiert auf der ursprünglichen C#-Codebasis v0.19. Die vorhandenen
Module wurden weitergeführt; Oberfläche und Erweiterungen sind native WinForms.

Der produktive portable Build verwendet .NET 8 für Windows x64. Die Laufzeit
wird mit `dotnet publish -c Release -r win-x64 --self-contained true` mitgeliefert.
Auf Windows genügt das bereits installierte .NET SDK 8 für `Neu-bauen.cmd`.
Die benötigten NuGet-Pakete werden beim ersten Neubau geladen. Werkzeuge und
Offline-Sprachmodelle liegen im vollständigen Paket unter `Werkzeuge`.

Unter Linux kann das Windows-Ziel mit EnableWindowsTargeting gebaut werden.
Verarbeitungstests und Oberflächenprüfungen unter Mono ersetzen keine echte
Windows-Abnahme für Audioausgabe, Mikrofon, Windows-Spracheingabe oder DPI.
Der 4.8-kompatible Kern wird zusätzlich gegen Monos explizites 4.8-Referenzprofil
kompiliert. Produktionsspezifische Sprachbibliotheken sind bedingt eingebunden.

Der Publish-Schritt verschiebt Laufzeit und Bibliotheken nach `Programm` und
bindet die native Start-EXE ueber den offiziellen SDK-Task CreateAppHost an
`Programm/Musikbox-Werkstatt-Studio.dll`. Kein Skriptstarter, kein zweiter
Programmprozess und keine global installierte .NET-Laufzeit erforderlich.
`Quellcode` greift beim Neubauen auf den benachbarten Ordner `Werkzeuge` zu.
