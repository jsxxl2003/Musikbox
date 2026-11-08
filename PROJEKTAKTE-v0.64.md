# Projektakte – Musikbox-Werkstatt Studio v0.64

Stand: 06.11.2026. Gebautes portables Korrekturpaket auf Basis der vollständigen v0.63; reale Ziel-PC-Abnahme offen.

## Verbindlich
C# / .NET 8 / Windows Forms, portable x64. Anthrazit-Lila-Studio, gemeinsame zentrale Liste, Metadaten rechts, Player unten. Keine sichtbaren hellen Windows-Standardscrollleisten. Die bisherige Übermalung ist ausdrücklich abgelöst: eigene Scrollsteuerelemente einschließlich Eckbereich verwenden.

## Nutzertest v0.63
Bestätigt: Coverspeicherung in Originaldateien und Tastatursuche funktionieren. Fehler: Titel/Cover/Metadaten passen nach Titel-/Ordnerwechsel nicht zusammen; Titelliste springt wieder an den Anfang; weiße Scrollleisten blitzen programmweit auf; Mikrofon hängt in der Erkennung und X reagiert nicht.

## Auftrag vom 06.11.2026
Der zuletzt bearbeitete Titel bleibt oben rechts mit seinen Metadaten und zugehörigem Cover; der Player ist unabhängig. Zurück in der Titelliste muss dieser Titel markiert und sichtbar sein. Sortierung, Auswahl und Scrollposition bewahren. Zwei Buttons „Titel“/„Ordner“ direkt an der gemeinsamen Listenüberschrift, in allen Cover-Studio- und Audio-Unterkategorien. Beide Listen mit Sortierung, Spaltenbewegung, Breiten und Sichtbarkeit. Entfernte separate Bearbeitungsfelder im Cover-Studio bleiben entfernt. Ordnercover müssen über die Ordneransicht erreichbar sein. Scrollleisten vollständig neu, kein weiteres Übermalen. Mikrofonabbruch und erneute Aufnahme verlässlich machen; Suchtext bei X erhalten.

## Stand v0.64
Explizite Bearbeitungsreferenz, konsistente Bildzuordnung mit Generationsprüfung, getrennte Listenstände, gemeinsame Umschalter, korrekte Ordneraktionen, neue eigene Scrollrahmen für Listen/Text/Dateidialoge sowie eigene Dropdown-Auswahllisten. Sprachsitzungen neu organisiert mit abbrechbarem Prozess, Zeitlimit, asynchroner Gerätefreigabe und Schutz vor verspäteten Antworten. Bestehende Datei-/Coverlogik, Suche und Audio-Werkzeuge erhalten.

Regulärer Release-Build ohne Fehler; 121 Logik-, Datei- und Prozessprüfungen bestanden. Das Windows-UI-Prüfprogramm wurde kompiliert und nicht ausgeführt. Physisches Mikrofon, reale Darstellung/Flackerfreiheit und Geräte-/Playerbedienung am Ziel-PC bleiben offen. Die neue Programmausführung nicht aus den bestandenen Logiktests ableiten.

## Weiterführung
Als Ausgangspunkt das vollständige v0.64-Paket verwenden. Zuerst die aktuell gemeldeten Sicht-/Umschaltfehler und Mikrofon/X am Ziel-PC prüfen. Bestätigte Coverspeicherung und schriftliche Suche schützen. Neue Befunde sammeln; keine alte Codebasis oder alte Scroll-Painter-Lösung wieder einführen. README und Prüfbericht dieses Pakets sind maßgeblich; frühere Versionsberichte bleiben historische Nachweise.
