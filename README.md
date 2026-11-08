# Musikbox-Werkstatt Studio v0.64

Stand: 06.11.2026. Portable Windows-x64-Anwendung in C# / .NET 8 / Windows Forms. Ausgangspunkt ist das vollständige v0.63-Paket. Das Anthrazit-Lila-Studio-Design bleibt verbindlich.

## Start
ZIP vollständig in einen neuen Ordner entpacken und `Musikbox-Werkstatt-Studio.exe` starten. Die Ordner Programm, Werkzeuge, Quellcode und Dokumentation gehören dazu. Für bisherige portable Einstellungen und Sicherungen den vorhandenen Ordner `Daten` aus v0.63 übernehmen. Gespeicherte Musikbox-Projekte über Projekt → Öffnen laden. Bereits in MP3-Dateien gespeicherte Tags und Cover bleiben in den Originaldateien erhalten.

## Korrekturen v0.64
- Oben rechts und in der Metadatenleiste bleibt der zuletzt ausgewählte beziehungsweise bearbeitete Titel. Der Player hat eine eigene Titel- und Coverzuordnung. Zeilenneuaufbau und ausstehende Bildladevorgänge dürfen die Anzeigen nicht vermischen.
- Titel ↔ Ordner merkt getrennte Sortierung, Auswahl, erste sichtbare Zeile und horizontale Position. Beim Zurückwechseln wird der bearbeitete Titel wieder markiert und sichtbar. Spaltenklick, Verschieben, Breiten und Sichtbarkeit bleiben erhalten.
- „Titel“ und „Ordner“ sind direkt in der gemeinsamen Listenüberschrift verfügbar, auch in allen Cover-Studio- und Audio-Unterkategorien. Die gemeinsame Liste wird weiterverwendet. Separate Bearbeitungsfelder im Cover-Studio werden nicht zurückgebracht.
- Ordneraktionen beziehen sich auf den tatsächlich markierten Ordner. Audio-Werkzeuge übernehmen die Titel der ausgewählten Ordner. Beim Coverexport in der Ordneransicht wird das Ordnercover verwendet.
- Die bisherigen nativen Scrollleisten werden nicht mehr überzeichnet. Eigene vertikale und horizontale Studio-Schienen liegen in einem reservierten Rahmen außerhalb des Listeninhalts. Die Ecke gehört zum dunklen Rahmen. Native Scrollbar-Stile werden bereits bei der Fenstererzeugung ausgeschlossen. Listen, Dateiübersichten, mehrzeilige Texte und Dropdown-Auswahlen verwenden die neue Implementierung; vorhandene eigene Studio-Scrollbereiche bleiben erhalten.
- X beendet die Sprachsitzung, signalisiert dem Prozess den Abbruch und bewahrt den Suchtext. Gerätefreigabe erfolgt im Hintergrund. Ein neuer Start wartet nicht auf ein verspätetes altes Erkennungsergebnis. Fehler und Zeitüberschreitungen lassen einen erneuten Start zu. „Automatisch“ verwendet das mitgelieferte Basismodell; ein ausdrücklich gewähltes größeres Modell bleibt möglich, sofern vorhanden.

## Bestehende Funktionen
Coverspeicherung als eingebettetes Bild, gezieltes Metadatenspeichern in Originaldateien, Genretext-Kompatibilität, Stapelbearbeitung und Track-Nummern-Assistent bleiben erhalten. Die Coverspeicherung und Tastatursuche wurden vom Nutzer in v0.63 bestätigt; ihre Dateilogik wurde für diese Korrektur nicht neu entwickelt.

MP3Gain bleibt getrennt von Audiokonvertierung und verarbeitet markierte Original-MP3s mit 89 dB als Standard. fre:ac, FFmpeg und Video-Audio-Import behalten ihre Qualitäts- und Zielordnerverwaltung. Neue Importe ergänzen die Bibliothek ohne doppelte vollständige Pfade. Der normale Import zeigt die hinzugefügten Titel in der aktuellen Importansicht. Die bisherigen Musikbox-Grenzen bleiben erhalten.

Im Tag-Editor bestimmt das gespeicherte Ausgabeformat die sichtbaren Felder rechts und ihre Reihenfolge. Nicht angezeigte Metadaten bleiben erhalten. „Speichern“ in der Metadatenleiste schreibt in die Originaldateien; Projekt speichern bewahrt die Zusammenstellung und noch vorgemerkte Änderungen. Rücknahmedaten bleiben unter `Daten/Datei-Rueckgaengig` verfügbar.

## Prüfstand
Regulärer Release-Build mit .NET SDK 8 erfolgreich, ohne Kompilierfehler. 121 Logik-, Datei- und Prozessprüfungen bestanden. Darunter sind Abbruch eines realen Unterprozesses, Zeitlimit, Neustart und verspätete Sprachergebnisse mit einem austauschbaren Aufnahmegerät. Das ist keine Aufnahme mit dem realen Mikrofon.

Die neuen Ansichts-, Scrollleisten- und Bildzuordnungstests sind als Windows-UI-Prüfprogramm enthalten und wurden kompiliert. Die Ausführung der eigenen Studio-Anwendung auf dem Ziel-PC, ihre tatsächliche Darstellung und das physische Mikrofon konnten in dieser Linux-Umgebung nicht geprüft werden. Diese Punkte sind keine bestandene Abnahme. Einzelheiten im Prüfbericht. Es wurden keine neuen Videotests durchgeführt und keine Testmedien mitgeliefert.

## Quellcode und Neubau
Vollständige Quellen unter `Quellcode/src`. Auf Windows mit .NET SDK 8 über `Quellcode/Neu-bauen.cmd` bauen. Der Werkzeuge-Ordner liegt beim vollständigen Paket neben Quellcode. Für ein separat entpacktes Quellcodearchiv die Werkzeuge des vollständigen Pakets daneben bereitstellen.

`Pruefen.ps1 -Oberflaeche` startet das aktuelle Ansichts-/Scrollleisten-Prüfprogramm; `-Sprache` die Sprachsitzungs-/Prozessprüfungen mit austauschbarem Aufnahmegerät. Die Tests erzeugen lediglich temporäre eigene Dateien; reale Mikrofonabnahme erfolgt separat. Historische Berichte beziehen sich auf ihre damaligen Versionen.
