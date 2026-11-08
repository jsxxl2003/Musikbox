# Quellen und Austausch von fpcalc

Die drei Archive im Ordner `sources` enthalten die vollständigen, unveränderten
Upstream-Quellen. Copyright- und Lizenzhinweise darin gehören zu den Quellen.
Die Originalversionen und SHA-256-Werte stehen in `manifest.json`.

Der offizielle Chromaprint-Build verwendet FFmpeg 8.0, statische Bibliotheken
und MinGW für Windows x86-64. Seine Optionen stehen in
`chromaprint-1.6.2/package/build.sh` und
`ffmpeg-build-8.0-1/common.sh` / `build-windows.sh`. Die zugehörigen
Toolchain-Dateien sind im Chromaprint-Archiv enthalten.

Zum eigenen Neubau auf einem Linux-System mit CMake, Make, C/C++-Compiler,
MinGW-w64 und den von FFmpeg benötigten Buildwerkzeugen:

1. Alle drei Archive in ein Arbeitsverzeichnis entpacken.
2. `ffmpeg-8.0.tar.xz` in den entpackten FFmpeg-Build-Ordner kopieren. In
   `common.sh` die Variable `FFMPEG_TARBALL` auf `ffmpeg-8.0.tar.xz` setzen.
   `tar -xf` im Buildskript verarbeitet auch dieses Kompressionsformat.
3. Im FFmpeg-Build-Ordner `ARCH=x86_64 ./build-windows.sh` ausführen. Die
   Decoder-Konfiguration steht vollständig in `common.sh`.
4. Im Chromaprint-Build `package/build.sh` die fertigen lokalen
   FFmpeg-Bibliotheken aus Schritt 3 anstelle des dortigen Downloadarchivs in
   `$FFMPEG_DIR` kopieren. Den `curl ... | tar xz`-Schritt dadurch ersetzen.
   Die restlichen Optionen und Toolchain-Dateien unverändert verwenden.
5. `OS=windows ARCH=x86_64 GITHUB_REF=refs/tags/v1.6.2 ./package/build.sh`
   ausführen. Das Ergebnis liegt in `artifacts/bin/fpcalc.exe`.

Für einen geänderten Build können die Optionen nach Bedarf angepasst werden.
Dies ist eine Anleitung zum funktionsgleichen Neubau; unterschiedliche Compiler
und Buildumgebungen können andere Binärbytes erzeugen.

Das Ergebnis kann `tools/fpcalc/fpcalc.exe` im entpackten Programmpaket ersetzen.
Es gibt keine Signatur-/Hash-Sperre in Musikbox-Werkstatt und keine notwendige
Neukompilierung der Hauptanwendung. Die übliche fpcalc-JSON-Schnittstelle muss
erhalten bleiben. `Neu-bauen.cmd` bereitet dagegen gezielt die unveränderte
offizielle Version aus dem Manifest vor.
