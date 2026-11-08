# Prüfbericht – Musikbox-Werkstatt Studio v0.64

06.11.2026. Ausgangspunkt: vollständiges ausgeliefertes v0.63-Paket einschließlich Quellen und Ressourcen.

## Bestätigte Ausgangsbefunde
Der Nutzer hat die Coverspeicherung und die schriftliche Suche in v0.63 als funktionierend bestätigt. Gemeldet wurden vermischte Titel-, Cover- und Metadatenanzeigen nach Ordner-/Titelwechsel, Zurückspringen der Liste zum Anfang, weiterhin weiße Scrollleisten und eine bei „Sprache wird lokal erkannt“ hängende Mikrofonsuche einschließlich unwirksamem X. Beauftragt wurden zudem die gemeinsamen Titel-/Ordner-Buttons in den Cover- und Audio-Unterkategorien sowie ein echter Neubau der Scrollleisten anstelle weiterer Übermalung.

## Änderungen und Prüfbarkeit
| Bereich | Implementierung | Tatsächliche Prüfung |
|---|---|---|
| Aktueller Titel und Cover | Explizite Bearbeitungsreferenz statt zufälliger CurrentRow nach Tabellenaufbau. Titel und Bild werden gemeinsam gesetzt. Alte Bildladeergebnisse werden anhand Generation und Quelldatei verworfen. | Vollständig kompiliert; UI-Szenario vorbereitet. Nicht real ausgeführt. |
| Metadatenleiste | Auswahl beim Benutzerwechsel erfassen; bei Ansichts-/Speicheraktualisierung bewahren. Zuletzt bearbeiteter Titel bleibt unabhängig vom Player. | Dateispeicherprüfungen bestanden. Sichtbare Anzeige real offen. |
| Titel-/Ordnerwechsel | Getrennte Listenstände mit Sortierspalte, Richtung, Auswahl, aktueller Zelle, erster sichtbarer Zeile und horizontaler Position. Bearbeitungstitel wird wieder markiert und bei Bedarf in Sicht gebracht. | UI-Prüfprogramm kompiliert, nicht ausgeführt. |
| Titel-/Ordner-Buttons | In der gemeinsamen Listenüberschrift in Bibliothek, Cover- und Audio-Unterkategorien. Sortiermenü folgt dem jeweils sichtbaren Listentyp; Spaltenfunktionen erhalten. | Quellcode-/Buildprüfung; UI-Prüfprogramm kompiliert. |
| Ordnerzuordnung | Tatsächlich markierter Ordner ist Arbeitsziel für Ordnercover; Audio-Werkzeuge verwenden die enthaltenen Titel. Ordnercoverexport verwechselt das Ziel nicht mit dem letzten Titel. | Quellcode-/Buildprüfung; reale Bedienung offen. |
| Scrollleisten | Der alte Painter/GetWindowDC-Weg wurde entfernt. Eigene Schienen mit Drag, Mausrad, Seitensprung, Home/End und beiden Richtungen. Reservierter dunkler Rahmen einschließlich Ecke. Native Styles bei Erzeugung/Stylewechsel ausgeschlossen. DataGridView verwendet ScrollBars.None und wird über seine echten Scrolleigenschaften bewegt. Andere Listen/Textbereiche sind ebenso angebunden; Dropdowns verwenden eine eigene Auswahlliste. | Regulärer Build erfolgreich; UI-Prüfprogramm prüft Stilbits, echte Zeilen-/Spaltenbewegung und Ansichtserhalt auf dem ausführenden Windows-PC. Hier nicht ausgeführt. Keine sichtbare Fehlerfreiheit behauptet. |
| Mikrofon/X | Sitzungen haben eigene Generationen und Abbruchsignale. Gerätefreigabe im Hintergrund. Transkription mit abbrechbarem Prozess, vollständigem Ausgabeeinlesen und 45-s-Grenze. Verspätete Ergebnisse und alte Geräte-Abschlussmeldungen greifen nicht auf neue Sitzungen zu. | 13 ausgeführte Sitzungs-/Prozessprüfungen bestanden. Aufnahmegerät/Transkription dabei austauschbar; reale Prozessabbrüche und Timeout unter Linux getestet. Kein physisches Mikrofon und keine reale Whisper-Erkennung ausgeführt. |
| Player | Eigene Titel-/Coverreferenz; Bildladevorgänge gegen veraltete Antworten geschützt. Wiedergabelogik bleibt erhalten. | Bestehende Logik erhalten; tatsächliche Wiedergabe auf dem Ziel-PC offen. |
| Datei-/Coverintegrität | Bestehende Dateischreiblogik beibehalten; neu geladene Bildanzeige nach Dateispeichern invalidieren. | 35 Integritäts- und 9 Dateispeicherprüfungen auf eigenen synthetischen MP3s bestanden. Eingebettetes Cover unabhängig mit FFmpeg gelesen; dekodierte Audiodaten unverändert. |

## Ausgeführte Prüfungen
- 13 Sprachsitzungs-/Prozessprüfungen: wiederholte Aufnahme, X, Gerätefreigabe, Abbruch ohne Warten auf Geräte-Dispose/Erkennung, sofortiger Neustart, alte Ergebnisse, alte Gerätemeldungen, Enginefehler, tatsächlicher Prozessabbruch, tatsächliches Timeout und Ausgabeeinlesen.
- 28 Such-/Importzählerprüfungen einschließlich tatsächlichem Ordner-/Dateiimport mit drei eigenen MP3s.
- 29 vorhandene Regressionstests für Suche, Aussprachezuordnung, Mehrfachbearbeitung, Tracknummern, Pfadzuordnung und PCM-Segmentierung.
- 7 vorhandene Tag-Editor-Logikprüfungen.
- 35 Integritätsprüfungen ohne Videos: Tags, Unicode, fremde/unveränderte Tags, Cover einbetten/ergänzen/ersetzen, erneutes Einlesen, bytegenaue Rücknahme, ungültige Eingaben, vorgemerkte Felder nach Projektneustart, Genreversionen, Nummerierung und Encoderparameter.
- 9 vorhandene Dateispeicherprüfungen.

Insgesamt: **121 ausgeführte Prüfungen bestanden.** Die Zahl enthält keine hier nicht ausgeführten UI-Tests. Die Vergleichswerte für Audiodaten werden ausdrücklich nur aus der Audiospur gebildet; eingebettete Bilder sind zusätzliche MP3-Streams und gehören nicht in diesen Audiovergleich.

## Build
Regulärer `dotnet build`, Release, .NET SDK 8.0.408, Windows-Targeting: erfolgreich, 0 Fehler. Eine vorhandene Warnung zu einem unbenutzten Ereignis bleibt. Die in diesem Paket verwendete DLL stammt aus diesem regulären Build. Die zusätzlich vorbereiteten UI-Tests wurden gegen diese DLL kompiliert.

Die selbstenthaltende .NET-/Windows-Forms-Laufzeit sowie Audio-Werkzeuge, Modelle und Lizenzen werden unverändert aus v0.63 übernommen. Native GUI-Startdatei mit offiziellem Windows-x64-Apphost 8.0.33 und Ressourcen der neuen DLL erstellt. Programm-/Dateiversion 0.64.0.0; keine alte DLL umbenannt. Quellcode, Buildskripte, aktuelle Dokumentation und Prüfprotokolle sind enthalten.

## Noch offene Abnahme
Die eigene Studio-Anwendung kann hier nicht auf einem realen Windows-PC gestartet werden. Daher offen: Darstellung/Flackerfreiheit bei Ansichtswechsel, Speichern, Größen-/DPI-Wechsel und allen Dialogen; tatsächliche Griff-/Mausradbedienung; Erhalt des zuletzt bearbeiteten Titels mit Bild und Metadaten im konkreten Katalog; echte Aufnahme und Whisper-Erkennung am Nutzermikrofon; Player, Drag-and-drop sowie Windows-MP3Gain/fre:ac.

Das vorbereitete UI-Prüfprogramm verwendet 240 eigene Datensätze, unterschiedliche Cover, einen separaten Wiedergabetitel und mehrere Fenstergrößen. Es prüft die neuen Umschalter, Ansichtserhalt, Coverzuordnung, echte Scrollbewegung und fehlende native Scrollbar-Stile. Es wurde hier nur kompiliert. Die vom Nutzer bereits bestätigte Originaldateispeicherung wird nicht durch diese noch offene Darstellungsabnahme zurückgenommen.

Keine neuen Videotests, keine Testmedien im Auslieferungspaket. Keine Änderungen an Downloadkomponenten, Konvertierungsparametern oder MP3Gain-Engine.

## Technische Referenz
Für die Scrollanbindung wurde die Microsoft-Implementierung der öffentlichen DataGridView-Scrolleigenschaften herangezogen: https://github.com/dotnet/winforms/blob/v8.0.0/src/System.Windows.Forms/src/System/Windows/Forms/DataGridView.cs. Die Style-Sperre folgt WM_STYLECHANGING: https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-stylechanging. Dies sind technische Quellen, keine Abnahme der konkreten Programmoberfläche.
