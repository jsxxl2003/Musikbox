# Prüfbericht – Musikbox-Werkstatt Studio v0.61

Stand: 01.11.2026. **Korrekturstand zur Windows-Prüfung; vollständige Abnahme noch offen.**

## Änderungen
- Seitenwechsel ohne rekursive WM_SETREDRAW-/ShowWindow-Manipulationen; Sichtbarkeit, zentrale Tabellenbreite und Modulfläche werden gemeinsam wiederhergestellt.
- Export, Statistiken, Wiedergabelisten und Einstellungen nutzen den Arbeitsbereich ohne zentrale Titelliste und Metadaten.
- Werkzeugzeile bricht bei schmalem Arbeitsbereich um. Modulhöhe berücksichtigt die Höhe der Bedienzeilen. Modulüberschriften/Beschreibungen erhalten gemessene Texthöhen; Coversuchfelder sind links ausgerichtet.
- Native eigenständige Tabellen-Scrollbars werden über OBJID_CLIENT abgefragt; dunkle Leisten bleiben nativ bedienbar. Eigene und native Leisten erhalten violette Hover-Farben.
- Schalter Titelliste/Ordner; Ordner als Detailliste mit Mehrfachauswahl, Spaltenmenü und gespeicherten Breiten, Reihenfolgen und Sichtbarkeiten. Ordner per Doppelklick öffnen. Sortierung bleibt beim Aktualisieren erhalten.
- Ziehen ausgewählter Titel/Ordner mit Bewegungsschwelle und Copy-Effekt. Mehrfachauswahl bleibt bis zum tatsächlichen Klickabschluss erhalten. Drop im Metadatenbereich wählt passende vorhandene Titel statt sie zu importieren.
- MP3Gain-Drops verwenden die eigene Dateiauswahl statt des Bibliotheksimports; Analyse und Verarbeitung verwenden diese Auswahl. 89 dB bleibt Standard.
- Sprachsuche kombiniert Fragment- und Fuzzy-Suche, vermeidet Treffer allein wegen des letzten Künstlerworts und startet auch bei identischem erneut erkanntem Text. Zahlen erhalten keine unscharfe Nachbarzahl-Zuordnung.
- Lokale persönliche Suchzuordnungen unter Einstellungen / Persönliche Sprachkorrekturen: Eingabe bzw. ausgewählter Titel als Vorschlag, Bearbeiten, Löschen und Wiederverwendung nach Neustart. Dies ist Suchzuordnung, kein Training des Whisper-Modells.
- Mikrofon-Ereignisse werden vor Aufnahmestart angemeldet. Unerwartetes Aufnahmeende wird behandelt, erneuter Start bleibt möglich. X stoppt mit erhaltenem Suchtext; Escape leert die Suche. Temporäre Aufnahmen werden wie bisher nach Erkennung gelöscht; kein neues Aufnahme-Archiv.
- Prüfung fehlender Dateiverweise und Laden von Titelcovern im Hintergrund; veraltete Coverergebnisse werden verworfen.
- Zusatzspalten laden Dateieigenschaften nur bei eingeblendeten Zusatzspalten; veraltete Ladevorgänge werden verworfen. Keine wiederholte Shell-Bitratenabfrage für ausgeblendete Spalten.

## Tatsächlich ausgeführte Prüfungen
- Vollständiger C#-Quellcode für .NET 8 / Windows Forms / x64 kompiliert: keine Compilerfehler. Bestandswarnungen zu WebRequest und NAudio-Referenzvereinheitlichung; siehe Buildprotokoll.
- 29 Regressionstests erfolgreich gegen die tatsächlich paketierte Anwendungs-DLL: Wortfragmente, Reihenfolge, Schreibvarianten, AC/DC, Jahr/Genre, Suchindex/Abbruch, persönliche Korrekturen, Mehrfach-Tagbearbeitung, Tracknummern-Vorschau, Pfadzuordnung und synthetische PCM-Segmentierung. Kein Stub der Song-Datenklasse.
- Windows-UI-Testprogramm kompiliert. Es enthält sämtliche 17 Navigationsziele als vollständige Wechselmatrix bei drei Fenstergrößen sowie einen Coversuchdialogtest. **Hier nicht ausführbar; nicht als bestanden gewertet.**
- Native Startdatei mit offiziellem .NET-SDK HostWriter.CreateAppHost erzeugt. PE-x64, Windows-GUI-Subsystem und relativer DLL-Pfad geprüft.
- Alle Werkzeuge, Sprachmodelle und unveränderten Laufzeitdateien per SHA-256 mit v0.60 verglichen: unverändert. Benutzerdateien des PCs wurden nicht verwendet.

## Buildweg und Grenzen
Der vollständige MSBuild-/dotnet-publish-Aufruf ist in dieser Linux-Sitzung weiter durch inkonsistente Prozessinformationen blockiert. Deshalb wurde die Anwendung direkt mit Roslyn gegen die .NET-8-/Windows-Forms-Referenzen kompiliert und der native Apphost mit dem offiziellen SDK erzeugt. Die vollständige selbstenthaltende Windows-x64-Laufzeit und Werkzeuge stammen unverändert aus v0.60; das Dependency-Manifest wurde auf 0.61.0 aktualisiert. **Dies ist kein erfolgreich hier ausgeführter dotnet-publish-Lauf.** Der reguläre Neubau unter Windows bleibt über Quellcode/Neu-bauen.cmd verfügbar.

Der vorherige, ältere Korrekturstand wurde auf dem Nutzer-PC erfolgreich mit SDK 8.0.427 publiziert; das belegt nicht den Windows-Publish oder die Ausführung dieser später ergänzten Änderungen.

## Noch erforderliche Abnahme
- Reale Windows-Ausführung dieses Pakets, Wechselmatrix, Hover, Kontextmenüs und Darstellung bei tatsächlichen 100/125/150/200-Prozent-DPI-Einstellungen.
- Reales Mikrofon, Berechtigungen, verfügbare Geräte, Erkennungsqualität bei deutscher Aussprache englischer Titel, Stop/Neustart und persönliche Korrekturen.
- Praktische Integrationstests mit Musikdateien für Import/Dateiabgleich, Tag-/Ordnerbearbeitung, Coverauswahl, MP3Gain, Konvertierung, Musikerkennung, Export, Wiedergabelisten und Audioausgabe.
- Der Compiler- und Logiktest bestätigt weder die Beseitigung sämtlicher Darstellungsfehler noch die reale Mikrofonzuverlässigkeit. Die ursprünglichen vollständigen Abnahmekriterien sind daher noch nicht erfüllt.

Tests unter Windows: im Quellcodeordner `powershell -NoProfile -ExecutionPolicy Bypass -File .\Pruefen.ps1`; UI-Matrix zusätzlich mit `-Oberflaeche`. Tests verwenden temporäre Projekte und simulierte Playerbefehle; sie ersetzen keinen Hör-/Mikrofontest. Die originale Testsammlung bleibt erhalten.
