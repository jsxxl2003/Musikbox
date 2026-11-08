# Musikbox-Werkstatt Studio v0.64 – Cover, Suche und Mikrofon

C# / .NET 8 / Windows Forms, portable Windows x64. Grundlage: GitHub `jsxxl2003/Musikbox`, `main`, Ausgangscommit `0e4bff9`. Das bestehende Anthrazit-Lila-Studio und die gemeinsame Titelliste bleiben erhalten.

## Start und Paket

Das gesamte ZIP in einen neuen Ordner entpacken. `Musikbox-Werkstatt-Studio.exe` im Hauptordner starten. `Programm`, `Werkzeuge`, `Daten`, `Dokumentation` und `Quellcode` gehören zusammen. Bisherige Einstellungen bei geschlossenem Programm über den bisherigen Ordner `Daten` übernehmen. Originaldateien enthalten weiterhin ihre gespeicherten Tags und Cover.

## Nachkorrektur vom 08.11.2026

- Nach dem Coverwechsel bleibt eine ausstehende Neuladung als solche markiert. Mehrere unmittelbar aufeinanderfolgende Anzeigeaktualisierungen können dadurch nicht das alte Cover aus dem Cache zurückholen. Titelkarte und Player laden das neue Cover getrennt und passend zum jeweiligen Titel.
- Neue Suchergebnisse erscheinen nach Trefferqualität. Eine genaue Titelübereinstimmung wie „Neon Moon“ steht vor unscharfen Treffern, auch wenn zuvor nach Ordner sortiert war. Die neue Ergebnisliste startet oben; beim Leeren der Suche wird die vorherige Sortierung wiederhergestellt.
- Der Mikrofonstatus steht sichtbar unter dem Suchfeld, auch wenn dort bereits Text steht. Der Mikrofonbutton bleibt während des Gerätestarts bedienbar. X und ein erneuter Klick beenden die Sprachsitzung; der Suchtext bleibt erhalten. Ein nicht reagierender Gerätestart endet nach zehn Sekunden mit einer konkreten Fehlermeldung.
- Physisches Mikrofon und tatsächliche Whisper-Erkennung auf Jens Rechner sind weiterhin nicht hier abgenommen. Die automatisierten Windows-Tests verwenden ein austauschbares Aufnahmegerät und prüfen den echten Controller-/Oberflächenablauf.

## Korrekturen dieses Prüfstands

- Nur noch eine zentrale Player-Timer-Verarbeitung; automatische Weiterschaltung folgt der sichtbaren Sortierung beziehungsweise einer aktiven Wiedergabeliste. Der nächste sichtbare Titel wird markiert und eingeblendet, am Listenende endet die automatische Wiedergabe.
- Die Sortierbedienung liest Spalte und Richtung aus der aktiven Liste. Ereignisse der unsichtbaren Liste verändern die gewählte Sortierspalte nicht mehr. Titel- und Ordnerzustände bleiben getrennt.
- Audio-Protokolle erscheinen erst bei Ausgabe als kompakte Fläche; Status, Fehler und Abbruchmeldungen bleiben sichtbar. Keine leere Füllfläche über der gemeinsamen Liste. In fre:ac und FFmpeg steht der Zielpfad vor „Speicherort wählen“.
- Dropdowns zeichnen ihre gesamte eigene Fläche einschließlich Pfeil und Rahmen vor jeder nativen Zeichnung; es gibt keinen zweiten darübergelegten Zeichenlauf. Die Scrollanbindung verändert während WM_PAINT kein Layout mehr. Listen passen ihre Höhe nicht zusätzlich auf ganze Einträge an; unveränderte Scrollbereiche werden nicht erneut invalidiert.
- Beim Kategorienwechsel wird der Listenstand nach dem endgültigen Layout wiederhergestellt. Auch bei schmalem Fenster bleiben echte Titelzeilen unter den Werkzeugen; längere Werkzeugzeilen verwenden ihren eigenen Bildlauf.
- Die Metadaten-Feldauswahl verwendet jetzt ebenfalls die Studio-Scrollanbindung. Sofortiges Speichern nach An-/Abwahl berücksichtigt bereits die neue Auswahl. Vorhandene Korrekturen für Coversuche, Vorschaubild und Dropdown-Zeichenablauf sind erhalten.
- Verbleibende helle Initialfarben in Tabellen/Seitenbereichen wurden beseitigt; Cover-Designer-Aktionen verwenden Studio-Schaltflächen.
- Projektverweise und Prüfskripte passen zur tatsächlichen Repository-Struktur. GitHub Actions führt zusätzlich zum Build Logik-, Sprachsitzungs- und Windows-Steuerelementprüfungen aus.
- Portable Veröffentlichung erzeugt wieder den vorgesehenen Hauptordner mit einer Startdatei und separatem `Programm`-Ordner. Das vollständige Übergabepaket enthält auch Werkzeuge, Quellen und Prüfbericht.

## Neubau und Prüfung

Windows mit .NET SDK 8: `Quellcode/Neu-bauen.cmd` oder `Quellcode/Bauen.ps1`. Die Werkzeuge werden aus dem benachbarten vollständigen `Werkzeuge`-Ordner übernommen. Für einen reinen Repository-Checkout `Bauen.ps1 -WerkzeugePfad <vollständiger Werkzeugordner>` verwenden. Fehlende Komponenten führen zum Abbruch statt zu einem unvollständigen Paket.

`Pruefen.ps1 -QS` prüft Logik, Sprachsitzungen und Windows-Steuerelemente. `-Oberflaeche` prüft die Windows-Ansichten; `-Sprache` verwendet ein austauschbares Aufnahmegerät. Dateiprüfungen über `-Dateitags -Testmusikordner <eigener Testordner>` benötigen eigene MP3-/Bilddateien gemäß Skripthinweis. Keine Originalsammlung als Testdaten verwenden.

Die tatsächlichen Ergebnisse und offenen Abnahmen stehen in `Dokumentation/PRUEFBERICHT-v0.64.md`. Physisches Mikrofon, hörbare Wiedergabe und zeitliches Flackern auf Jens Ziel-PC sind durch simulierte Geräte und automatische Steuerelementprüfungen nicht nachgewiesen. Es wurden keine Testvideos erzeugt.
