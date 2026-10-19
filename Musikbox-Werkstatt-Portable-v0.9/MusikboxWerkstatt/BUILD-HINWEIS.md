# Windows-kompatibler Build

Unter Windows `Neu-bauen.cmd` mit dem .NET-Framework-Compiler ausführen.

Bei Linux-Builds niemals die standardmäßigen Mono-Laufzeitassemblies als
Kompilierziel verwenden. Sie enthalten String-Überladungen, die unter
Windows/.NET Framework 4.8 fehlen können.

Stattdessen ausschließlich explizite Referenzen aus einem vollständigen
.NET-Framework-4.8-Referenzprofil verwenden, mit `-noconfig -nostdlib+`.
Benötigte Assemblies: mscorlib, System, System.Core, System.Xml,
System.Runtime.Serialization, System.Drawing und System.Windows.Forms.

Diese Vorgabe gilt auch für die Testprogramme. Die Windows-Oberfläche muss
zusätzlich auf Windows getestet werden; Mono-Verarbeitungstests ersetzen das nicht.
