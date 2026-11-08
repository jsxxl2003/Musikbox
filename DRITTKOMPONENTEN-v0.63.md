# Drittkomponenten v0.63

Die vorhandenen .NET-, NAudio-, TagLibSharp-, Whisper-, FFmpeg-, MP3Gain-, fre:ac- und Fingerprint-Komponenten sind unverändert aus dem vollständigen v0.62-Paket übernommen. Ihre vorhandenen Lizenz- und Herkunftsdateien bleiben im Paket erhalten.

| Neue Komponente | Paketdatei | Version | Lizenz/Herkunft |
|---|---|---|---|
| yt-dlp | Werkzeuge/yt-dlp/yt-dlp.exe | 2026.10.26, Windows x64 | Hauptprojekt Unlicense; beigefügte THIRD_PARTY_LICENSES-Datei für gebündelte Komponenten beachten. https://github.com/yt-dlp/yt-dlp/releases/tag/2026.10.26 |
| Deno | Werkzeuge/yt-dlp/deno.exe | 2.9.8, Windows x64 | MIT, inklusive Herkunftsverweis auf das Projekt und dessen eingebettete Abhängigkeiten. https://github.com/denoland/deno/releases/tag/v2.9.8 |

Die SHA-256-Werte sind in `Werkzeuge/yt-dlp/manifest.json` festgehalten. yt-dlp.exe wurde mit der offiziellen SHA2-256SUMS-Datei verglichen; das Deno-Archiv mit dem SHA-256-Digest der offiziellen Release-Metadaten. Beide Vergleiche stimmten überein. Das ist ein Hashvergleich, keine eigenständige Signaturprüfung. Deno ist für von yt-dlp benötigte JavaScript-Auswertung beigefügt. Die offiziellen Windows-yt-dlp-Builds enthalten ihre eigenen erforderlichen Komponenten; keine Stacher-Oberfläche wird eingebettet.

Es werden keine Login-Cookies, DRM-Umgehung oder benutzerdefinierten yt-dlp-Plugins eingebunden. Aktualisierungen der separat ausführbaren Komponenten können bei veränderten Plattformen nötig werden; dafür das jeweilige offizielle Release verwenden und dessen aktuelle Lizenz- und Prüfsummendateien mit übernehmen.
