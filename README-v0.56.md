# Prüfungen v0.56

LatestImportTests: Arbeitsverzeichnis und echte MP3-Testdatei als Argumente.
LatestImportUiTests: Arbeitsverzeichnis und echte MP3-Testdatei; Mono/Xvfb,
FFmpeg unter /usr/bin/ffmpeg, PlaybackTestDevice und TagLibSharp erforderlich.
Die UI-Prüfung bestätigt den echten AppFolderDialog per Timer und ruft die
produktive Dateidrop-Routingstrecke auf. Ein nativer Explorer-Drop unter Windows
wird dadurch nicht nachgewiesen.
DropTests und TagDropTests: fünf Referenzquellordner mit insgesamt 15 MP3-Dateien,
auf vier Albumthemen verteilt, sowie ein separates frisches Arbeitsverzeichnis.
StructureDropTests: frisches Arbeitsverzeichnis und gültige MP3-Testdatei; erzeugt
unter anderem 8098 beschriftete Testtitel. Keine privaten Musikdaten verwenden.
Alle Testprotokolle befinden sich im Paket unter Dokumentation/Pruefungen/v0.56.
