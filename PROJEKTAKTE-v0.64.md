# Projektakte – Musikbox-Werkstatt Studio v0.64

Stand: 08.11.2026. Maßgeblich ist der aktuelle Quellcode im bestehenden GitHub-Repository `jsxxl2003/Musikbox`, Branch `main`. Ausgangspunkt dieses Auftrags: `0e4bff9`. Kein neues Projekt und kein neuer Entwicklungszweig.

## Verbindliche Entscheidungen

C# / .NET 8 / Windows Forms, portable Windows x64. Anthrazit-Lila-Studio, Navigation links, gemeinsame zentrale Titel-/Ordnerliste, Metadaten rechts, Player unten. Keine weißen Windows-Standardflächen, keine Scrollleisten-Übermalung. Bestehende Coverspeicherung und schriftliche Suche wurden vom Nutzer bestätigt und müssen erhalten bleiben. Drag-and-drop in beide Richtungen schützen.

Der zuletzt ausgewählte/bearbeitete Titel behält seine passende Cover-/Metadatenzuordnung, unabhängig vom Player. Titel-/Ordneransicht bewahrt Auswahl, Sortierung, Spalten und Position. Gemeinsame Titel-/Ordner-Schalter in Cover- und Audio-Unterkategorien. Keine separaten Metadatenfelder im Cover-Studio. Mikrofon wiederholbar, X beendet Aufnahme/Erkennung und bewahrt den Suchtext.

## Arbeitsauftrag vom 08.11.2026

Weiße Bedienelemente insbesondere in der Coversuche ursächlich beseitigen; Dropdown-Flackern beheben; überflüssige Leerflächen entfernen; Titelliste direkt unter Werkzeugen; Player-Weiterschaltung/Sortierung/Markierung; Metadaten-Feldauswahl/Reihenfolge/Persistenz/Dateischreiben; Tastatur-/Mikrofonsuche und X; Titel-/Ordnernavigation; Regressionstests; vollständiger Windows-Build; ein gemeinsames Auslieferungspaket. Als behoben nur ausweisen, was implementiert und erfolgreich überprüft wurde.

## Befunde und Änderungen

Der QS-Bericht vom 08.11. benannte zwei Timer-Handler, geteilte Sortierbedienungsvariablen, unsichtbare Audio-Protokolle und falsche Pfad-/Buttonreihenfolge. Diese Ursachen sind im Code korrigiert. Zusätzlich waren Test-Projektverweise um eine Verzeichnisebene verschoben; das Prüfskript verwies auf nicht vorhandene `Tests`-Unterordner. Auch der alte Paketbau passte nicht zum tatsächlich veröffentlichten Single-File-Paket. Projektverweise, Skripte und portable Veröffentlichung wurden angepasst.

Metadaten-Feldauswahl auf Studio-Steuerelemente mit eigener Scrollanbindung umgestellt. Helle Tabellen-/Seiten-Initialfarben entfernt, Cover-Designer-Aktionen vereinheitlicht. Bereits im Ausgangscommit vorhandene Coversuch-/Dropdown-/Metadatenkorrekturen beibehalten.

Die Windows-Prüfung reproduzierte zusätzlich eine Layout-/Zeichenrückkopplung beim Öffnen von Dropdowns. WM_PAINT löst jetzt keine Layoutberechnung mehr aus; feste Listenrahmen verwenden keine zusätzliche IntegralHeight-Rundung. Scrollbereiche werden nur bei geänderten Werten invalidiert. Die vollständige Dropdown-Zeichenfläche einschließlich Pfeil ersetzt die native Fläche, ohne sie nachträglich zu übermalen.

Listenstände nach dem endgültigen Kategorienlayout wiederherstellen. Mindesthöhe für echte Titelzeilen unter langen Werkzeugleisten vorsehen; Werkzeugbereich bei Bedarf separat scrollbar. Laufende Suchaktualisierungen vor dem Schließen abbrechen und Layout während Freigabe verhindern. Windows-Prüfprogramme verwenden dieselbe echte Application.Run-Nachrichtenschleife wie die Anwendung; Prüfprogrammfehler sind keine Produktfehler.

## Prüfung und Übergabe

Aktueller ausführlicher Nachweis: `PRUEFBERICHT-v0.64.md`, Protokolle unter `Dokumentation/Pruefprotokolle`. Frühere Berichte sind historische Nachweise. Windows-Steuerelementtests sind von echter Mikrofon-/Audiogeräteabnahme und sichtbarer Flackerfreiheit auf dem Ziel-PC zu unterscheiden.

Jede Übergabe enthält künftig portable Anwendung, vollständigen aktuellen Quellcode, Werkzeuge, README, Projektakte und Prüfbericht in einem gemeinsamen Versionsordner/ZIP. Keine persönlichen Daten oder Testmedien mitliefern.

## Nutzertest und Nachkorrektur: Cover / Suche / Mikrofon

Nach Übergabe des ersten QS-Pakets meldete Jen: neues Titelcover nur im Player, oben rechts weiterhin altes Cover; „Neon Moon“ trotz genauer Eingabe nicht erster Treffer; Mikrofon ohne sichtbare Reaktion.

Ursachen: Die Cover-Neuladung wurde bereits vor Abschluss des asynchronen Lesens als erledigt markiert; ein weiterer Aufruf konnte die neue Ladegeneration verwerfen und das alte Bild verwenden. Die gespeicherte Spaltensortierung überschrieb die Suchrelevanz. Mikrofonstatus wurde nur in ausgeblendeten Anzeigen beziehungsweise als Platzhalter im bereits gefüllten Suchfeld gesetzt; der Button war während eines möglicherweise blockierten Gerätestarts deaktiviert.

Korrektur: Ladeflag erst bei abgeschlossenem Einlesen zurücksetzen; alte Generation beim Invalidieren verwerfen. Suchrelevanz nach Wiederherstellung des Listenstands anwenden, genaue Titel bevorzugen und neue Suche oben beginnen; bisherige Sortierung bei leerer Suche erhalten. Sichtbare Statuszeile, abbrechbarer Gerätestart, zehn Sekunden Startzeitlimit. Neue Windows-Regressionen prüfen Coverwechsel unter mehrfacher Aktualisierung, exakten Treffer bei alter Ordnersortierung, Mikrofonsteuerung bei gefüllter Suche, wiederholte Erkennung sowie X und Neustart. Hardwareaussagen bleiben ausdrücklich offen.

## Auslieferung ueber GitHub Actions

Nutzermeldung: Explorer-Fehler 0x80010135 beim Entpacken langer Paketpfade. Das Actions-Artefakt wird auf `MW-064` verkuerzt. Anwendung, Werkzeuge, vollstaendiger versionierter Quellcode und Pruefbericht werden gemeinsam gebaut. Keine verschachtelten ZIPs. Paketbau mit 120-Zeichen-Grenze fuer Archivpfade und realer Entpackpruefung; generierte bin/obj-Verzeichnisse werden nicht als Quellcode kopiert. Werkzeugbereitstellung und Cache laufen auf dem Windows-Runner. Physische Mikrofon- und Ziel-PC-Abnahmen bleiben offen.

## Kompakte Bereiche und feste Metadaten-Startansicht

Grosse Bereichsueberschriften und Einleitungstexte entfallen zentral in Bibliothek und Werkzeugseiten. Bedienelemente ruecken nach oben; die Navigation bezeichnet den aktuellen Bereich. Der Track-Nummern-Assistent ist wieder als eigener Button im Metadatenbereich vorhanden. Bei jedem Programmstart gelten Interpret, Titel, Album, Genre, Track, Jahr und Ordner in dieser Reihenfolge. Die Feldauswahl kann fuer die laufende Sitzung geaendert werden; gespeicherte Dateinamen-/Tag-Ausgabeformate bleiben erhalten. Windows-QS prueft diese Startansicht auch bei abweichend gespeichertem Ausgabeformat, die Bedienbarkeit und ueberlappungsfreie Lage des Assistenten sowie die kompakten Bereichskoepfe. Nachweis fuer diesen neuen Stand: zugehoeriger Actions-Lauf und mitgelieferte aktuelle QS-Protokolle.

Nutzerkorrektur: Der nicht angeforderte Button „Felder auswählen“ unter dem Track-Nummern-Assistenten ist entfernt; der reservierte Platz entfaellt ebenfalls. Die sieben Startfelder und der Assistent bleiben erhalten.

Das vereinbarte Rechtsklick-Kontextmenue zur Metadaten-Feldauswahl bleibt erhalten. Der zentrale Menustil wird jetzt auch vor den fruehen Rueckgaben fuer StudioPanel/StudioExact angewandt. Eigene Darstellung von Menuflaeche, Auswahl, Texfarbe und Bildrand verhindert weisse Systemfarben; der bildlose Metadatenmenueeintrag benoetigt keinen Iconrand. Der zusaetzliche Button bleibt entfernt. Windows-QS prueft das vorhandene Kontextmenue und dessen normale sowie ausgewaehlte Darstellung.
