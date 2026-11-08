# Prüfbericht v0.64 – Cover, Suche und Mikrofon

**Stand:** 08.11.2026. Bestehendes Repository `jsxxl2003/Musikbox`, Branch `main`. Ausgangspunkt: vorheriges vollständiges QS-Paket, Dokumentationscommit `2ac312a5c78b1d6567adc00f1ff9fd06fdea9e2d`.

**Geprüfter Korrekturstand:** `783f4ad5eda882d8954fd0ddfa138d442a248432`.
**Erfolgreicher Windows-Build und Prüfungen:** https://github.com/jsxxl2003/Musikbox/actions/runs/42508800111

Der oben genannte Lauf belegt den bisherigen Korrekturstand. Neue Actions-Pakete enthalten das Publish-Ergebnis ihres eigenen Laufs; Commit und aktuelle Paketpruefung stehen in `Paket.txt`. Programmversion bleibt 0.64.0.0.

## Neue Nutzermeldungen und Korrekturen

| Nutzermeldung | Ursache / Korrektur | Ausgeführter Nachweis |
|---|---|---|
| Neues Cover nur im Player, oben rechts altes Bild | Die asynchrone Cover-Neuladung wurde vor ihrem Abschluss als erledigt markiert. Eine unmittelbar folgende Aktualisierung konnte die neue Ladegeneration verwerfen und das alte Bild wiederverwenden. Das Ladeflag bleibt jetzt bis zum tatsächlichen Einlesen gesetzt; Invalidieren verwirft alte Generationen. | Windows: bereits angezeigtes rotes Cover durch blaues ersetzen, mehrere unmittelbare Metadaten-/Coveraktualisierungen auslösen, neue Bildpixel sowohl oben rechts als auch im Player prüfen. Bestanden. |
| „Neon Moon“ trotz genauer Eingabe nicht zuerst | Nach dem Filtern wurde die gespeicherte Ordnersortierung erneut angewandt. Ergebnisrelevanz wird jetzt nach Wiederherstellung des Listenstands angewandt; genaue Titel vor unscharfen Treffern. Neue Suche beginnt oben. | Windows: frühere Ordnersortierung absteigend, exakter Titel „Neon Moon“ und unscharfer Treffer „None Vero“ mit passenden weiteren Metadaten. Exakter Titel ist erste Zeile. Bei leerer Suche wird die vorherige Ordnersortierung wiederhergestellt. Bestanden. |
| Mikrofon ohne sichtbare Reaktion | Status war in ausgeblendeten Anzeigen bzw. im bei vorhandenem Suchtext unsichtbaren Platzhalter. Button wurde beim Gerätestart deaktiviert; ein blockierter Start hatte kein Zeitlimit. | Status jetzt sichtbar unter dem Suchfeld, auch bei vorhandenem Text. Button bleibt zum Beenden bedienbar; X bewahrt den Text. Gerätestart nach zehn Sekunden mit konkretem Fehler abbrechen. Windows-Controller mit austauschbarem Aufnahmegerät: Start, zweimalige Erkennung, X, Neustart und erneuter Klick zum Stoppen bestanden. Blockierter Gerätestart im Service-Test bestanden. |

**Wichtig:** Der Mikrofontest verwendet ein austauschbares Aufnahmegerät und eine austauschbare Erkennung. Er bestätigt den Controller-/Oberflächenablauf, nicht Jens physisches Mikrofon, Windows-Geräteberechtigung oder echte Whisper-Transkription. Eine technische Ursache am persönlichen Aufnahmegerät ist damit nicht abschließend festgestellt.

## Ausgeführte Prüfungen

- Windows Release-Build und self-contained Windows-x64-Publish: erfolgreich, keine Buildfehler. Zwei bestehende CS0067-Warnungen zu unbenutzten SideTagEditor-Ereignissen.
- **69 Windows-QS-Prüfungen:** einschließlich der neuen Cover-/Such-/Mikrofonfälle, dunkler Bedienelemente, eigener Dropdowns, Feldauswahl/Reihenfolge, Player-Weiterschaltung, Werkzeuganordnung und X.
- **120 Windows-Ansichtsprüfungen:** zehn Kategorien, Titel-/Ordnerwechsel, Cover-/Metadatenzuordnung, Auswahl, Sortierung, Scrollpositionen, echte horizontale und vertikale Bildlaufbewegung bei zwei Fenstergrößen und ausgeschlossene native Scrollbar-Stile.
- Windows zusätzlich: Regression61 29, Voice64 14, TagEditing 7. Audioausgang und Aufnahmegerät dabei simuliert.
- Linux mit .NET 8, reine Logik-/Datei-/Prozessprüfungen: Regression61 29, Voice64 14, SearchCounter62 28, Integrity63 35, MetadataSave62 9 erneut bestanden. Die 7 TagEditing-Prüfungen sind auf Windows bestanden. Insgesamt **122 unterschiedliche Logik-/Datei-/Prozessprüfungen**, zusätzlich **189 Windows-Oberflächen-/Ansichtsprüfungen**. Wiederholungen auf beiden Plattformen werden nicht doppelt als unterschiedliche Prüffälle gezählt.
- Die Dateiprüfungen verwenden getrennte eigene synthetische MP3-/Bilddateien. Keine Originalsammlung verändert, keine Testvideos erzeugt. Cover-/Metadaten-Schreiblogik blieb unverändert; tatsächliches APIC-Einbetten, Genretext, Unicode, Erhalt fremder Tags, erneutes Einlesen und bytegenaue Rücknahme sind durch die Dateiprüfungen abgedeckt.
- Die neue Cover-Anzeigeprüfung beginnt nach abgeschlossener Live-Suche; sie prüft gezielt mehrere überlappende Bildaktualisierungen. X wird gegen den tatsächlich erkannten Suchtext geprüft, einschließlich dessen Groß-/Kleinschreibung.

Vollständige Protokolle unter `Dokumentation/Pruefprotokolle`. Ältere Logs sind anhand ihres Dateinamens/Datums historische Nachweise; maßgeblich für diese Nachkorrektur sind `Windows-Cover-Suche-Mikrofon-Build.txt` und die Windows-Tests dieses Builds.

## Status der übrigen Aufgabenbereiche

| Bereich | Aktueller Prüfstatus |
|---|---|
| Weiße Bedienelemente / Dropdown-Zeichenablauf | Vorherige Korrekturen erhalten; vorhandene Windows-Steuerelement-/Pfeilprüfungen bestanden. Keine neue komplette Sichtabnahme jedes Dialogzustands. |
| Werkzeug-Leerflächen / gemeinsame Liste | Vorherige Korrekturen erhalten; Anordnung und echte Scrollbewegung erneut auf Windows bestanden. |
| Player / Sortierung / Markierung | Vorhandene Windows-Prüfungen erneut bestanden, Audiogerät ausgetauscht. |
| Metadaten / Coverspeicherung | Dateiprüfungen erneut bestanden, keine Änderung an der Schreiblogik. Explorer-/MP3tag-Anzeige nicht neu geprüft. |
| Titel-/Ordnernavigation | Windows-Ansichtstests erneut bestanden. |
| Import / Dubletten / Suche | Logik-/Importzählerprüfungen erneut bestanden; genaue Suchrelevanz zusätzlich im Windows-Controller geprüft. |
| MP3Gain / fre:ac / FFmpeg / Videoimport | Verarbeitungscode unverändert. Bedienelemente und Listenanordnung geprüft; externe Verarbeitung nicht vollständig erneut ausgeführt. |
| Physisches Drag-and-drop / Mikrofon / Audioausgang | Weiterhin echte Ziel-PC-Abnahme erforderlich. |
| Zeitliche Flackerfreiheit und sämtliche DPI-Varianten | Kein pauschaler Nachweis durch Standbilder oder Steuerelementtests. |
| Auslieferung | Ein gemeinsamer Versionsordner mit Anwendung, Werkzeugen, Datenordner, vollständigen aktuellen C#-Quellen, README, Projektakte und Prüfbericht. Dateiprüfsummen enthalten. |

Das Paket ist ein implementierter und automatisiert geprüfter Korrekturstand. Die ausdrücklich offenen Hardware- und manuellen Abnahmen werden nicht als bestanden ausgegeben.

## Paketkorrektur nach Fehler 0x80010135

Neuer Paketbau: kurzes Actions-Artefakt `MW-064`, vollstaendige Werkzeuge und Quellen, maximal 120 Zeichen pro Archivpfad, ZIP-Erstellung und Entpacken in einen langen Zielpfad. Die erfolgreiche Ausfuehrung wird vom ausliefernden Actions-Lauf nachgewiesen; bei Fehlern wird kein Komplettpaket hochgeladen. Diese Paketpruefung ersetzt keine Abnahme auf Jens Rechner.

## Kompakte Bereiche und feste Metadaten-Startansicht

Grosse Bereichsueberschriften und Einleitungstexte entfallen zentral in Bibliothek und Werkzeugseiten. Bedienelemente ruecken nach oben; die Navigation bezeichnet den aktuellen Bereich. Der Track-Nummern-Assistent ist wieder als eigener Button im Metadatenbereich vorhanden. Bei jedem Programmstart gelten Interpret, Titel, Album, Genre, Track, Jahr und Ordner in dieser Reihenfolge. Die Feldauswahl kann fuer die laufende Sitzung geaendert werden; gespeicherte Dateinamen-/Tag-Ausgabeformate bleiben erhalten. Windows-QS prueft diese Startansicht auch bei abweichend gespeichertem Ausgabeformat, die Bedienbarkeit und ueberlappungsfreie Lage des Assistenten sowie die kompakten Bereichskoepfe. Nachweis fuer diesen neuen Stand: zugehoeriger Actions-Lauf und mitgelieferte aktuelle QS-Protokolle.

Nutzerkorrektur: Der nicht angeforderte Button „Felder auswählen“ unter dem Track-Nummern-Assistenten ist entfernt; der reservierte Platz entfaellt ebenfalls. Die sieben Startfelder und der Assistent bleiben erhalten.
