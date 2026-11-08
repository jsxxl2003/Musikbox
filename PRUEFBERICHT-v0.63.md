# Prüfbericht – Musikbox-Werkstatt Studio v0.63

Stand: 05.11.2026. Status: gebautes portables Testpaket, reale Windows-Abnahme offen.

## Grundlage
Die vollständige v0.62 einschließlich Laufzeit und Werkzeugen wurde mit dem gesicherten v0.63-Arbeitsstand vom 05.11.2026 abgeglichen. Alle dort enthaltenen neuen Quelldateien wurden übernommen. Der neue Auftrag löst die ältere Importregel „Bibliothek ersetzen“ ab: bestehende Bibliotheksdaten bleiben erhalten, neu hinzugefügte Pfade bilden die aktuelle Importansicht. Design und technische Plattform bleiben verbindlich unverändert.

## Aufgabenstatus
| Nr. | Bereich | Umsetzung und nachgewiesener Stand |
|---|---|---|
| 1 | Arbeitsgrundlage | Projektakte/README, v0.62-Paket und gesicherter v0.63-Quellstand abgeglichen; kein Neustart des Projekts. |
| 2 | Zentrale Titelliste | Dieselbe Grid-Instanz bleibt beim Werkzeugwechsel bestehen; separate Werkzeug-Dateilisten entfernt. Windows-Auswahltest vorbereitet, nicht ausgeführt. |
| 3 | Titel/Ordner und Spalten | Detaillisten, Sortierung, Verschieben, Sichtbarkeit, Speicherung, Schema einschließlich AcoustID und Startstandard vorhanden. Windows-Bedienprüfung offen. |
| 4 | Scrollleisten | Zentraler Zeichenweg für native Scrollflächen korrigiert; eigene Studio-Scrollbereiche für Navigation, Metadaten und Werkzeugleisten. Native Eingabewege bleiben erhalten. Dunkeldarstellung, Ecken und Hover real noch abzunehmen. |
| 5 | Layout | Zwischengeometrien der Tabelle vermieden, Layout gegen Rekursion geschützt, kurzer Redraw-Block beim Seitenwechsel, umbrochene Werkzeugleisten scrollbar. Kein zweites Grid erzeugt. Sichtbarer Doppelzeichnungsfehler real noch zu bestätigen. |
| 6 | Tastatursuche | Gemeinsamer Index, Fragmente, Reihenfolgeunabhängigkeit, Sonderzeichen und unscharfe Suche geprüft. Suchtexte bleiben auch im Werkzeug nutzbar. Eingabe während Import wird wieder aufgenommen. |
| 7 | Mikrofonsuche | Bestehender lokaler Whisper-Aufnahmeweg mit wiederholbarer Segmentierung, X-Abbruch und Korrekturverzeichnis erhalten; Such- und PCM-Segmentlogik geprüft. Reale Mikrofon-/Whisper-Ausführung offen. |
| 8 | Import | Bis zu vier parallele Dateileser, geordnete Ausgabe, serialisierte Rückmeldungen, keine automatische Audio-Neucodierung beim normalen Import, Bibliotheks-Merge und Pfaddeduplizierung. Drei Zähler, Test mit echten synthetischen MP3s bestanden. |
| 9 | Tastatur/Drag-and-drop | Vorhandene Grid-Auswahl und Drag-and-drop-Wege erhalten. Entfernen aus Projekt verändert keine Originaldateien. Reale Drag-and-drop-Abnahme offen. |
| 10 | Tag-Editor | Werkzeuge-Menü und untergeordneter Audio-Eintrag zusammengeführt; kein eigenständiger Hauptnavigationspunkt. Direkte Datei-/Stapelbearbeitung integriert, unnötige Textdatei-/Ziel-Tagfeld-Abläufe aus dem sichtbaren Werkzeug entfernt. |
| 11 | Ausgabeformat | Bearbeitbare Platzhalterauswahl, Reihenfolge, freie Eingabe und Speicherung. Dateiname-Parsing geprüft. Separates Eingabemuster für Dateiname → Dateiname bleibt möglich. |
| 12 | Metadatenleiste | Kommentar dort entfernt; sichtbare Felder und Reihenfolge folgen dem Muster; Ordnerfelder schreibgeschützt. Verdeckte Originaltags werden nicht pauschal überschrieben. Windows-Layoutkontrolle offen. |
| 13 | Genre/ID3 | TCON als Genretext statt numerischer Kennung; (2) und 2 nach Country normalisiert. ID3v2.3/v2.4-Versionserhalt sowie Ergänzung bei ID3v1-Dateien geprüft. FFprobe bestätigt Country. Explorer/MP3tag offen. |
| 14 | Coverspeicherung | JPEG/PNG tatsächlich als eingebettete Bilder gespeichert, Frontcover ersetzen oder Bilder ergänzen, Mehrfachauswahl, erneutes Byte-Einlesen, Rücknahme. FFmpeg liest APIC unabhängig korrekt aus. Neustart wird durch erneutes Öffnen der Dateien simuliert; echter Programmneustart mit MP3tag/Explorer offen. |
| 15 | MP3Gain | Zentrale Auswahl, Originalpfade, 89 dB, Titel-/Albumanalyse, Gain, Rücknahme, Fortschritt und Abbruch. Keine Zielordner-/Arbeitskopie-Oberfläche. Windows-MP3Gain-Engine nicht hier ausgeführt. |
| 16 | fre:ac | Zentrale Auswahl, Speicherortwahl und persistenter Zielordner, Qualitätsparameter. Windows-fre:ac-Engine nicht hier ausgeführt. |
| 17 | FFmpeg | Zentrale Auswahl, Zielordnerverwaltung, bestehende Konvertierungsfunktionen erhalten. Eigene lokale Audio-Konvertierung mit lesbarer Ausgabe geprüft. |
| 18 | Qualitätsstufen | MP3-VBR V4/V2/V1/V0 korrekt zugeordnet, V2 vorausgewählt, CBR-Bitrate separat. Parameterprüfungen bestanden. |
| 19 | Lokale Videos | Medienerkennung und Audio-Wiedergabeweg über vorhandenen Player integriert; AAC-Extraktion per Streamkopie und MP3-Konvertierung intern geprüft; Video unverändert. Playerausführung real offen. |
| 20 | Video-Audio-Import | yt-dlp und Deno portabel beigefügt, FFmpeg bei Bedarf, Fortschritt/Fehler/Abbruch und anschließender Import. Eigener HTTP-Downloadweg geprüft. Windows-EXE und aktuelle öffentliche Plattformen nicht live geprüft. |
| 21 | Speicherorte | Zentral gespeicherte Dialogordner sowie getrennte Ziele für fre:ac, FFmpeg und Video-Audio-Import, gültiger Fallback. Reale Dialogbedienung offen. |
| 22 | Track-Nummern-Assistent | Start, Gesamtzahl, Nullen, Ordnerneustart und Auswahlreihenfolge auf Dateitags geprüft. |
| 23 | Bestand/Player | Vorhandene Player-, Wiedergabelisten- und Drag-and-drop-Implementierungen erhalten; nächster/vorheriger Titel wird bei Bedarf sichtbar gescrollt. Reale Wiedergabe und Bedienung offen. |
| 24 | Qualitätssicherung | Kompilierung, Kern-, Datei-, Such-, Import- und externe Tag-/Audioprüfung durchgeführt. Vollständiger realer Windows-Arbeitsablauf ist ausdrücklich nicht als bestanden bewertet. |
| 25 | Übergabe | Vollständiges portables ZIP, zusätzlicher Quellcode, Auftrag, aktuelle Dokumentation und Prüfprotokolle. |

## Tatsächlich ausgeführte Prüfungen
- 28 Such-/Import-Zählerprüfungen einschließlich Ordner und mehrfach ausgewählter Datei.
- 35 aktuelle Integritätsprüfungen ohne Video-Wiederholung: bewusst geänderte Felder, Mehrfachauswahl, externe/unbekannte Tags, Unicode, Cover einbetten/ersetzen/ergänzen, erneutes Einlesen, bytegenaue Rücknahme, ungültige Eingaben, Pending-Felder nach Projektneustart, Genreversionen, Tracknummern, Merge und Encoderparameter.
- 29 bestehende Regressionsprüfungen: Suche, Phonetik, Korrekturverzeichnis, Stapelbearbeitung, Tracknummern, Pfadzuordnung und wiederholte PCM-Segmente.
- 7 bestehende Tag-Editor-Prüfungen.
- 9 bestehende Dateispeicherprüfungen; die Planprüfung wurde auf ausdrücklich geänderte Felder aktualisiert. Unveränderte individuelle Titel bleiben nach echtem Dateischreiben erhalten.

Damit bestehen 108 Kernprüfungen am abschließenden Quellstand. Zuvor wurden zusätzlich 6 lokale Medienprüfungen sowie 5 Downloadprüfungen erfolgreich ausgeführt: eigenes intern erzeugtes HTTP-Testvideo, MP3-Ausgabe, Fortschritt, nicht erreichbare Quelle, Abbruch und Aufräumen. Diese Medienprüfungen wurden nach dem Nutzerhinweis nicht erneut ausgeführt. Es werden keine Testvideos ausgeliefert.

Unabhängige Kontrolle: FFmpeg liest die eingebetteten Bildbytes wieder exakt aus; FFprobe bestätigt Country in ID3v2.3, ID3v2.4 und der ergänzten ID3v1-Datei. Dekodierte Audio-MD5-Werte bleiben nach Tag-/Coveränderungen unverändert. AAC-Pakete vor und nach Streamkopie sind bytegleich. Protokolle liegen neben diesem Bericht. Ein paralleler Wiederholungslauf verwendete versehentlich dasselbe temporäre Testverzeichnis; dessen abgebrochener Medienlauf ist nicht als Erfolg gezählt. Die abschließenden Kernprüfungen wurden anschließend getrennt ausgeführt.

## Build
Der vollständige Anwendungscode wurde mit dem .NET-8-Roslyn-Compiler gegen die im v0.62-Paket enthaltenen Windows-Assemblies kompiliert. Kein Kompilierfehler. Drei vorhandene WebRequest-Veraltungswarnungen und eine unbenutzte Ereignisdeklaration bleiben; keine davon wurde als erfolgreich behobener Fehler ausgegeben. Native GUI-EXE mit offiziellem Windows-x64-Apphost 8.0.33 erzeugt; .NET- und Windows-Forms-Laufzeit 8.0.33 aus der vollständigen v0.62 unverändert übernommen. Versionskennungen von Assembly, EXE und Paket auf 0.63 aktualisiert.

Ein normaler MSBuild-/dotnet-publish-Aufruf scheitert in dieser Linux-Umgebung an nicht abrufbaren Prozessinformationen. Die direkte Kompilierung und Apphost-Erstellung wurden stattdessen tatsächlich ausgeführt; es wurde keine alte Anwendungs-DLL als v0.63 umbenannt. Auf Windows ist der reguläre Neubau mit dem beigefügten Skript vorgesehen. Der aktualisierte Windows-UI-Test wurde kompiliert, nicht ausgeführt.

## Noch offene Abnahme und Einschränkungen
- Eigene dunkle Programmoberfläche, Scrollleisten, freie Flächen, kleine Auflösungen und echte DPI-Wechsel auf dem Ziel-PC kontrollieren.
- Cover mit Original-MP3tag und Explorer öffnen, Programm beenden und neu starten, Mehrfachauswahl wiederholen. FFmpeg/TagLib ersetzen diesen ausdrücklich geforderten Nutzertest nicht.
- Tatsächliche Mikrofonaufnahme, wiederholte Whisper-Erkennung und X-Abbruch am Gerät prüfen.
- MP3Gain und fre:ac als mitgelieferte Windows-EXE ausführen; ihre Engines wurden nicht in dieser Umgebung gestartet.
- Player, vorheriger/nächster Titel, Wiedergabelisten und beide Drag-and-drop-Richtungen real prüfen.
- Öffentliche Videoquellen können sich ändern oder Downloads sperren. Es gibt keine Garantie für alle von yt-dlp genannten Plattformen und keinen eingebauten Rechteprüfer.
- Videoton wird für den vorhandenen Audioplayer bei Bedarf in einem lokalen Cache dekodiert; keine zweite Playeroberfläche und kein eigenes Videomodul. Dieser erste Dekodiervorgang läuft noch im bestehenden Player-Aufruf und kann bei großen Videos den Start verzögern.
- Die Dateisicherung und eigentliche Übernahme sind transaktional vorbereitet, aber kein Ersatz für eine Datenträgersicherung oder Schutz vor Stromausfall während mehrerer Dateiersetzungen.

Nicht als erledigt behauptet: vollständige Windows-Laufzeitabnahme oder Garantie, dass keine getestete Funktion regressiert. Der Quellstand ist vollständig erhalten und die verbleibende Prüfung konkret dokumentiert.
