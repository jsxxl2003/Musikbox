# Aktueller Auftrag v0.60

- Keine pauschalen SetWindowTheme-Aufrufe an Controls/Unterfenstern.
- Navigation und Metadaten verwenden eigene StudioScrollPanel/StudioScrollRail;
  deren nativen AutoScroll nicht aktivieren, keine Übermal-Timer.
- Neue Module vor Parenting/Handleerzeugung vollständig dunkel gestalten.
- Seitenwechsel gemeinsam abschließen; gemeinsame Titeltabelle nicht neu aufbauen.
- Windows-Redraw-/Theme-Abnahme nicht durch Mono-Tests als bestanden ausgeben.

# Aktueller Auftrag v0.59 (31.10.2026)

- Vorhandene v0.58 als Basis, keine Neuentwicklung. Anthrazit/Violett in allen Ansichten und Dialogen, auch bei Hover/Fokus.
- Einstellungen enthalten keine Titelliste oder Spaltenauswahl. Alle anderen Module behalten ihre Titelauswahl.
- Suchindex auf Metadaten-Snapshots; Hintergrundfilterung, Generation und CancellationToken für veraltete Ergebnisse. Aufnahme und Suche haben getrennte Lebenszyklen.
- X in der Suchleiste stoppt Mikrofon und Erkennung, bewahrt aber erkannten Suchtext und Ergebnisse. Escape leert das Suchfeld.
- Native Windows-Hardware-/Scrollbarprüfungen dürfen nicht durch Mono-Tests als bestanden ausgegeben werden.

# Aktueller Arbeitsauftrag v0.57 (31.10.2026)

Diese ausdrücklichen Nutzervorgaben gehen widersprechenden älteren Regeln unten vor:
- Die tatsächlich vorhandene net8.0-windows-C#-Basis v0.56 weiterentwickeln und wie
  bisher selbstenthaltend für Windows x64 veröffentlichen. .NET-8-spezifische
  Aufnahme-/Hardwareabfragen bleiben in NET8_0_OR_GREATER-Blöcken; gemeinsame
  Such-/Tag-/Audioverarbeitungslogik bleibt mit Mono prüfbar.
- Direkte Änderungen an markierten MP3-Dateien sind für Tag-Konvertierung und
  Tracknummern-Assistent nach Vorschau und ausdrücklichem Speichern autorisiert.
  Andere Audioverarbeitung verwendet weiterhin Ausgabekopien.
- Alle Module verwenden denselben neuesten Import. Die frühere Vorgabe zu
  unabhängigen Moduldateiauswahlen gilt nicht mehr.
- Audio-Werkzeuge und Cover-Studio erhalten die beauftragten Untergruppen;
  Titelliste bleibt verfügbar und Metadatenbereich reicht bis zum Player.
- Aktuelle mehrsprachige Erkennung: lokales Whisper base/small, Sprache automatisch;
  die frühere Beschränkung auf zwei Vosk-Sprachmodelle gilt nicht mehr.

# Projektregeln

- Entwicklung auf dem Branch `entwicklung` oder einem eigens benannten Arbeitsbranch.
- Git und GitHub für die Versionsverwaltung verwenden. Einen lokalen Commit
  niemals als erfolgreichen GitHub-Push darstellen.
- MVC einhalten: Daten und Regeln, Oberfläche und Steuerung trennen.
- Windows-Bedienung als Ausgangspunkt verwenden; aus Programmierer- und
  Nutzerperspektive prüfen.
- Für Windows-Builds ausschließlich .NET-Framework-4.8-kompatible APIs verwenden.
  Unter Linux gegen explizite Framework-Referenzassemblies kompilieren, nicht
  gegen die erweiterten Mono-Laufzeitassemblies. Siehe BUILD-HINWEIS.md.
- Musikquellen unverändert lassen. Jeder erfolgreiche Musikimport (Dateiauswahl,
  Ordnerauswahl oder Drag-and-drop) ersetzt die bisherige Bibliothek vollständig.
  Nur der neue Batch wird nummeriert und in Ordner mit maximal 99 Titeln geteilt.
  Bei Abbruch, Fehler oder ohne gültige neue Titel bleibt die bisherige Bibliothek.
  Freie Quellnummern innerhalb des neuen Batches erhalten.
- Tag-Reihenfolge: Interpret, Titel, Album. Maximal 99 Ordner mit je 99 Titeln;
  Ordnernummern dreistellig am Anfang, beispielsweise 000 Country.
- MP3-Standard bei Neucodierung: 192 kbit/s CBR. VBR und andere CBR-Raten sind optional.
- Musikdateien, persönliche Sammlungsprojekte und Zugangsdaten nicht committen.
- Prüfergebnisse und Grenzen ehrlich dokumentieren. Verarbeitungstests unter
  Mono ersetzen keinen Test der Windows-Oberfläche oder der echten Musikbox.

- Automatische Audioverarbeitung nur auf Arbeitskopien. Passende Importkopien
  beim Export wiederverwenden; keine doppelte Normalisierung. Profil und
  Quelländerungen prüfen. Fehlgeschlagene Importbatches nicht teilweise übernehmen.
- Themenordner-Cover und Titelcover getrennt halten. Online-Cover erst nach
  Nutzerauswahl übernehmen. Optionale Erkennung braucht eigene Zugangsdaten;
  diese ausschließlich benutzerbezogen verschlüsselt, niemals in Git speichern.

- Individuell bearbeitete Album-Tags (Song.AlbumOverride) beim Sortieren und
  Export erhalten; nur unberührte Titel verwenden das Thema als Standardalbum.
- Die fünf Arbeitsbereiche verwenden dieselben Controls und Projektdaten.
  Ausblenden darf weder Datenverlust noch Abbruch von Aufgaben/Audio auslösen.
  Fensterlayout ist eine getrennte Benutzereinstellung, kein Sammlungsprojekt.
- fpcalc.exe gehört zum portablen Windows-x64-Paket. Versionsmanifest,
  Lizenztexte und vollständige Quellen beim Paketbau mitliefern.

- Bildersuche, Coversuche und künftige Titel-/Künstlersuche sollen eine
  Winamp-ähnliche Bedienung anstreben. Keine
  Behauptung identischer Winamp-Algorithmen ohne Nachweis. Keine pauschale
  Übernahme von WCL-Code in das veröffentlichte Projekt; konkrete Komponenten-
  Lizenz vor Wiederverwendung prüfen. Lokale Suche, Online-Coversuche und
  Fingerabdruckdienste haben unterschiedliche Aufgaben.
- Eigene Auswahl- und Bestätigungsdialoge bewahren Abbruch- und Ja/Nein-Semantik.
  Speicherauswahl fragt vor Ersetzen, schreibt selbst keine ausgewählten Dateien.

- Verbindliche Suchvorgabe für künftige Suchfunktionen: mehrere Wortteile ohne
  feste Reihenfolge und ohne Beachtung der Groß-/Kleinschreibung kombinieren.
  Alle eingegebenen Teile müssen im jeweiligen Suchbereich vorkommen dürfen,
  bei freier Musiksuche auch verteilt auf Interpret, Titel und Album.
  Namen statt ausschließlich Dateinamen berücksichtigen. Online-Dienste über
  dokumentierte Abfragen anbinden; Unterschiede und Einschränkungen gegenüber
  lokaler Teilwortsuche ehrlich anzeigen. Keine Nutzereingabe als rohe Suchsyntax.
  Coversuche: MusicBrainz-Wortindex mit maskierten Literalbegriffen plus
  Teilwort-Regulärausdrücken für Buchstaben/Ziffern; Satzzeichenbegriffe literal.
  Browser-Bildersuche folgt weiterhin den Regeln des externen Suchanbieters.

# Verbindliche Studio-Gestaltung

- Konkrete Referenz ist der Nutzer-Screenshot image(20261030-000049).png (vorherige gleichartige Referenz: image(20261029-233556).png)
  (Studio v0.42) mit den Originalwerten dieser Studio-Oberflaeche.
- Keine eigene Interpretation oder neue Gestaltung. Aenderungen an Anordnung,
  Proportionen, Farben, Schriftgroessen und Bedienelementen nur nach ausdruecklicher
  konkreter Nutzervorgabe.
- Keine zusaetzliche Menuezeile und keine Modulrahmen/-Titelleisten im Hauptfenster.
- Suchleiste oben rechts, Navigation links, Bibliothekskarten in der Mitte,
  aktueller Titel und sechs Metadatenfelder rechts, Player unten.
- Die nativen Funktionen erhalten; das Design nicht durch klassische Standard-
  Bedienelemente ersetzen. Vor Bereitstellung die echte gerenderte Ansicht pruefen.

# Portable Paketstruktur

- Hauptordner enthaelt nur Musikbox-Werkstatt-Studio.exe sowie Programm,
  Werkzeuge, Daten, Dokumentation und Quellcode. Keine losen Laufzeit-DLLs.
- Start ueber nativen .NET-Apphost mit relativ gebundener Assembly; Sitzung
  und Werkzeuge immer relativ zum Paketroot, unabhaengig vom Arbeitsverzeichnis.

# Nutzerkorrekturen vom 30.10.2026

- Kopfbereich 64 und Player 72 logische Pixel; alle drei Arbeitsbereiche
  beginnen bei 74 und enden auf gleicher Hoehe oberhalb des Players.
- Modulbeschreibungen nur so hoch wie ihr Text, Werkzeugzeilen dynamisch.
- Fenstertitel und Kopfzeile zeigen die aktuelle Assemblyversion.
- Dateidrops in MP3Gain/Konvertierung sind Moduldateiauswahlen und duerfen
  weder den Bibliotheksimport noch dessen Ordnernummerierung ausloesen.
- Berichte und Protokollflaechen verwenden Studio-Gestaltung.

- Weitere Nutzerkorrektur: Bibliothek und Aktueller Titel paarweise gleich
  hoch (126 logische Pixel, auch bei langen Titeln unveraendert).
  Titelliste und Metadaten haben dieselbe obere Kante. Leere Titelliste nur
  96 Pixel plus gegebenenfalls horizontaler Bildlaufleiste, ohne Leerzeile.

# Nutzerkorrekturen v0.55

- Einfacher Klick ersetzt die Titelauswahl; Strg/Shift erhalten Mehrfachauswahl.
- Cover ueber Button oder Enter endgueltig bestaetigen; Abbruch verwirft die Auswahl.
- Suchdialogzeilen richten ihre Hoehe nach dem Inhalt; keine abgeschnittenen Hinweise.
- Mikrofonsuche prueft deutsche/englische Alternativen gegen die Bibliothek; keine
  feste Umdeutung des Beispiel-Fehltexts. Echten Windows-Mikrofontest offen ausweisen.
