CHROMAPRINT / FPCALC 1.6.2 — WINDOWS x86-64

Unverändertes offizielles fpcalc.exe aus dem Chromaprint-Projekt.
Homepage: https://acoustid.org/chromaprint
Release: https://github.com/acoustid/chromaprint/releases/tag/v1.6.2
Die genauen Downloadadressen und SHA-256-Prüfsummen stehen in manifest.json.

Musikbox-Werkstatt startet dieses eigenständige Werkzeug als separaten Prozess
für genau den ausgewählten Titel. Es wird nichts installiert. Die Audioanalyse
bleibt lokal; erst der ausdrücklich gestartete AcoustID-Abgleich überträgt den
Fingerabdruck und die Dauer. Der eigene AcoustID-Anwendungsschlüssel ist nicht
Bestandteil dieses Pakets.

Chromaprint enthält MIT- und LGPL-2.1-Code. Der offizielle Windows-Build bindet
FFmpeg 8.0 statisch ein. Die beigefügten Lizenzdateien, Copyright-Hinweise und
vollständigen Quellen gelten für die jeweiligen Komponenten, unabhängig vom
Programm Musikbox-Werkstatt. FFT/Decoder-Quellen und ihre Hinweise sind in den
Quellarchiven enthalten. Keine Änderungen am mitgelieferten fpcalc.exe.

sources/ enthält die vollständigen Chromaprint-1.6.2-, FFmpeg-8.0- und
AcoustID-FFmpeg-Build-v8.0-1-Quellen mit ihren Buildskripten. Zum Neubauen siehe
REBUILD.md. Das Werkzeug darf durch eine selbst gebaute kompatible Version
ersetzt werden; die Anwendung erzwingt keine Prüfsumme zur Laufzeit.

Die Prüfsummen werden nur beim Erstellen/Vorbereiten des offiziellen Pakets
geprüft. tools/Prepare-Fpcalc.ps1 stellt die unveränderten Dateien beim Neubauen
aus Git wieder her. Im fertig entpackten Paket ist kein Download erforderlich.

Wichtig: fpcalc ist ein Fingerabdruck-Werkzeug. Das enthaltene FFmpeg dient
seinem Decoder und ersetzt keine separate ffmpeg.exe für Audioexport oder
Lautstärkeanpassung.
