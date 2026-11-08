# Prüfbericht – Musikbox-Werkstatt Studio v0.64 / QS 08.11.2026

## Prüfgrundlage

Bestehendes Repository `jsxxl2003/Musikbox`, Branch `main`. Ausgangscommit: `0e4bff97b9b148eeff0a70db02e8fbba04fd83f7`. Geprüfter Programmstand dieses Pakets: `e50620447f2044cc5580472f673dcddf7ba7b8df`.

Windows-Nachweis: https://github.com/jsxxl2003/Musikbox/actions/runs/42505460780

Version 0.64.0.0; Paketstand durch Datum/QS im Dateinamen gekennzeichnet. Nachfolgende Dokumentationscommits ändern keine Programmquellen. Die Anwendung des Pakets stammt aus dem Windows-Publish dieses Programmstands, nicht aus einer umbenannten älteren EXE.

## Ergebnis nach Arbeitsauftrag

| Nr. | Bereich | Implementierung und ausgeführter Nachweis | Abnahmegrenze |
|---|---|---|---|
| 1 | Weiße Bedienelemente / Coversuche | Ganze Dropdown-Fläche einschließlich Pfeil und Rahmen selbst gezeichnet, statt nach nativem Zeichnen darüberzumalen. Cover-Designer-Aktionen mit Studio-Buttons; helle Startfarben entfernt. Windows prüft dunkle Bedienelemente und gerenderten Pfeilbereich. Gerenderte Cover-Suchansicht geprüft. | Keine pauschale Abnahme jedes Dialogzustands und jeder Windows-/DPI-Konfiguration. |
| 2 | Dropdowns / Flackern | Reproduzierten Hänger beseitigt: keine Layoutberechnung aus WM_PAINT; keine konkurrierende ListBox-Höhenrundung; unveränderte Rail-Bereiche werden nicht invalidiert. Öffnen/Schließen viermal auf Windows geprüft. Eigene Zeichenfläche hat keinen zweiten nativen Übermal-Durchgang. | Zeitliche Flackerfreiheit auf dem Ziel-PC bleibt ein manueller Sichttest. |
| 3 | Leerflächen / Anordnung | Unsichtbare Audio-Füllprotokolle durch bedarfsweise kompakte Ausgabe ersetzt; Fehler-/Fortschrittsmeldung erhalten. Zielpfad vor Auswahlbutton. Gemeinsame Liste direkt unter Werkzeugfläche; Mindesthöhe für echte Titelzeilen, lange Werkzeuge scrollbar. Neun Cover-/Audio-Bereiche und zwei Fenstergrößen geprüft. | Nicht jede mögliche Kombination aus DPI, Schrift und Fenstergröße durchlaufen. |
| 4 | Player | Zweiten Timer-Handler entfernt. Ein Tick startet genau einen Folgetitel; sichtbare Sortierung beziehungsweise Wiedergabeliste; nächster Titel markiert und sichtbar. Auf Windows mit austauschbarem Audiogerät geprüft. | Hörbare Wiedergabe am echten Audioausgang nicht ausgeführt. |
| 5 | Metadaten | Sofortige Feldabwahl und Reihenfolge im Dialog geprüft; vorhandene synchrone Speicherung erhalten; eigene Scrollanbindung der Feldauswahl. 7 Bearbeitungs-, 9 Dateispeicher- und 35 Integritätsprüfungen bestanden. Tags, Unicode, Genretext, unveränderte/fremde Tags, APIC, Rücknahme und Projektneustart mit vorgemerkten Feldern geprüft. | MP3tag-/Explorer-Anzeige auf dem Ziel-PC nicht neu geprüft; Auswahlpersistenz über echten Anwendungsneustart nicht automatisiert abgenommen. |
| 6 | Suche / Mikrofon / X | 28 Such-/Importzählerprüfungen sowie Suchfälle im Regressionstest. Echte Live-Suche in Windows-Oberfläche; X bewahrt Suchtext. 13 Sprachsitzungs-/Prozessprüfungen: Wiederholung, Abbruch, Neustart, verspätete Ergebnisse, Gerätefreigabe, Fehler, Prozessabbruch und Timeout. | Aufnahmegerät/Erkennung ausgetauscht; kein physisches Mikrofon und keine tatsächliche Whisper-Transkription. |
| 7 | Navigation Titel/Ordner | Unabhängige Sortierung; Richtungswechsel ignoriert unsichtbare Liste. Wiederherstellung nach abschließendem Layout. Windows mit 240 Datensätzen, unterschiedlichen Covern und unabhängigem Player: zehn Kategorien, Auswahl, Metadaten, Bildzuordnung, Sortierung und beide Scrollpositionen geprüft. | Persönlicher großer Katalog nicht als Testdaten verwendet. |
| 8 | Regression | 121 Logik-/Datei-/Prozessprüfungen nach letztem Programmstand bestanden. Zusätzlich 179 Windows-Oberflächen-/Ansichtsprüfungen bestanden; bestehende Logik-/Sprach-/Tagtests zusätzlich auf Windows ausgeführt. | Physisches Drag-and-drop, alle externen Audio-Werkzeuge und komplette reale Benutzerabläufe nicht vollständig abgenommen. |
| 9 | Windows-Build | .NET SDK 8.0.427; Release-Build und vollständiger self-contained Windows-x64-Publish im GitHub-Windows-Runner erfolgreich. Zwei vorhandene Warnungen CS0067 für unbenutzte SideTagEditor-Ereignisse, keine Buildfehler. | Kein Beleg für fehlerfreie Hardwarebedienung allein durch Build. |
| 10 | Gemeinsames Paket | Eine ZIP mit Startdatei, Programm, Werkzeugen, Datenordner, Dokumentation und vollständigen aktuellen Quellen. Paketdateien und Werkzeuge gegen Vorgänger/Build geprüft; SHA-256-Dateiverzeichnis enthalten. | Bestehende Werkzeugbinärdateien unverändert übernommen; deren komplette Funktion nicht neu abgenommen. |

## Tatsächlich ausgeführte Prüfungen

### Logik, Dateien und Prozesse – 121 Prüfungen

Auf Linux mit .NET 8 gegen den aktuellen Anwendungscode ausgeführt; für reine Logik-/Dateiprüfungen wurde die .NETCore-Laufzeit ohne WindowsDesktop aktiviert. Das ist kein Ersatz für Windows-UI-Tests.

- Regression61: 29.
- Voice64: 13, mit austauschbarem Aufnahmegerät und echtem Unterprozessabbruch/Timeout.
- TagEditing: 7.
- SearchCounter62: 28, einschließlich Import von drei eigenen synthetischen MP3s und Dublettenprüfung.
- Integrity63: 35, ohne optionale Videoprüfungen.
- MetadataSave62: 9, auf eigenen synthetischen MP3-Dateien.

Die Dateiprüfungen arbeiten in getrennten temporären Testordnern. Es wurden keine Originalmusikdateien verändert. Nach der Integritätsprüfung wurden die dekodierten Audiodaten ausdrücklich nur aus der Audiospur (`-map 0:a:0`) verglichen: unverändert. Das eingebettete APIC wurde unabhängig mit FFmpeg ausgelesen und bytegenau mit der Coverdatei verglichen: identisch.

### Windows – 179 Oberflächenprüfungen

Ausgeführt im GitHub-Runner Windows Server 2025 mit Windows Forms und echter `Application.Run`-Nachrichtenschleife:

- QS-Windows: 59 Prüfungen für Coversuch-Steuerelemente, eigene Dropdowns, Feldwahl/Reihenfolge, Sortierbedienung, Player-Tick/Playlist, kompakte Modulpositionen, Pfad-/Buttonreihenfolge, Live-Suche und X.
- Workspace64: 120 Prüfungen für zehn Kategorien, Titel-/Ordnerwechsel, zugeordnetes Cover/Metadaten, Auswahl, Sortier- und Scrollzustände, echte vertikale und horizontale Scrollbewegung bei 1000×680 und 1700×1000 sowie ausgeschlossene native Scrollbar-Stile.
- Regression61 (29), Voice64 (13) und TagEditing (7) zusätzlich auf Windows bestanden. Diese Wiederholungen werden nicht als zusätzliche unterschiedliche Prüffälle auf die 121 Logikprüfungen addiert.

Bildnachweise aus dem Runner liegen bei den Prüfprotokollen. Speicherabbildungen der Fenster sind keine Messung zeitlichen Flackerns und keine Abnahme der nativen Fensterrahmen/DWM-Darstellung auf Windows 11.

## Behobene Ursachen

1. Zwei konkurrierende Player-Timer-Handler: nur noch die zentrale Weiterschaltung.
2. Gemeinsame Sortierbedienungsvariablen: aktive Liste ist jetzt Quelle für Spalte/Richtung.
3. Wiederherstellung vor Abschluss des Kategorienlayouts: final nach dem Layout wiederherstellen.
4. Zu geringe Listenresthöhe bei langen Werkzeugleisten: echte Titelzeilen reservieren.
5. WM_PAINT → Scrollbereich → Größenänderung → erneutes Zeichnen: Layout aus Zeichennachrichten entfernt, feste Listenhöhe und idempotente Bereichsaktualisierung.
6. Native helle Dropdown-Pfeilfläche: ganze Zeichenfläche selbst übernehmen, keine nachträgliche Overlay-Zeichnung.
7. Unsichtbare Audio-Ausgaben: bei Textbedarf kompakt anzeigen, Status-/Fehlertexte sichtbar halten.
8. Verschobene Testprojektpfade und unpassende Paketstruktur: Referenzen, Prüfskript, Publish-Struktur und Paketbau korrigiert.

Die zunächst auftretenden OLE-/STA-Probleme beim Schließen des Prüfprogramms wurden mit dessen korrekter Application.Run-Lebensdauer behoben. Sie werden nicht als nachgewiesener Produktfehler ausgegeben. Zusätzliche Schutzabfragen während Layoutfreigabe und Abbruch laufender Suchaktualisierungen vor dem Schließen bleiben im Anwendungscode.

## Noch offene reale Abnahme

- Physisches Mikrofon, wiederholte echte Whisper-Erkennung und Mikrofonberechtigung am Ziel-PC.
- Hörbare Wiedergabe und tatsächlicher Audioausgang; physisches Drag-and-drop in beide Richtungen.
- Zeitliche Flackerfreiheit bei Speichern, Navigation, Dropdowns und DPI-Wechseln auf dem Ziel-PC.
- Sämtliche Dialogvarianten, die komplette Online-Coversuche und echte Verarbeitung durch MP3Gain/fre:ac/Video-Audio-Import wurden hier nicht vollständig durchlaufen.
- Windows-Explorer/MP3tag nach echtem Programmneustart nicht neu geprüft. Die frühere Nutzerbestätigung der Coverspeicherung bleibt erhalten.

Diese Punkte werden ausdrücklich nicht als erfolgreich abgenommen ausgegeben. Das Paket ist ein konkret gebauter und automatisiert geprüfter Korrekturstand; keine pauschale Behauptung vollständiger Fehlerfreiheit. Keine Testvideos erzeugt oder mitgeliefert.
