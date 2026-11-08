# Prüfungen v0.55

Die Protokolle sind im portablen Paket unter Dokumentation/Pruefungen/v0.55.
Alle UI-Prüfungen laufen unter Mono/Xvfb mit explizitem 4.8-Referenzprofil.
StudioFix55Tests benötigt Linux X11/XTest für echte Maus- und Tastaturereignisse.
SpeechPhrasesTests benötigt keine Spracheingabe und keinen Online-Dienst.
OfflineSpeechAudioTests verwendet die produktive OfflineSpeech-Implementierung
mit NET8_0_OR_GREATER, Vosk.dll, Linux libvosk.so und beiden mitgelieferten Modellen.
Argumente: Sprachmodell-Elternordner, rohe PCM-Datei (16 kHz, mono, 16 Bit LE).
Das Audiosample wurde mit espeak-ng synthetisch erzeugt; nicht mit dem Mikrofon.
Die übrigen Testvorgaben stehen jeweils im Main der Testdateien.
Windows-Verhalten wird durch diese Prüfungen nicht bestätigt.
