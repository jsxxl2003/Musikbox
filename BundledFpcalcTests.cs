using System;
using System.IO;
using Musikbox;
class BundledFpcalcTests {
 static int count;static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Console.WriteLine("OK "+message);}
 static void Main(string[] args){string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);string original=Environment.CurrentDirectory,old=Path.Combine(root,"alt.exe"),first=Path.Combine(root,"Portable alt"),next=Path.Combine(root,"Portable neu ä");
  File.WriteAllText(old,"vorher konfiguriert");Directory.CreateDirectory(Path.GetDirectoryName(BundledFpcalc.PathIn(first)));File.WriteAllText(BundledFpcalc.PathIn(first),"mitgeliefert");
  Check(BundledFpcalc.Resolve(null,first)==BundledFpcalc.PathIn(first),"Ohne Dateiauswahl wird das mitgelieferte Werkzeug gefunden");
  Check(BundledFpcalc.Resolve(old,first)==BundledFpcalc.PathIn(first),"Mitgeliefertes Werkzeug hat Vorrang vor altem Einstellungs-Pfad");
  Directory.Move(first,next);Check(BundledFpcalc.Resolve(BundledFpcalc.PathIn(first),next)==BundledFpcalc.PathIn(next),"Verschieben des gesamten portablen Ordners repariert alten Werkzeugpfad automatisch");
  try{Environment.CurrentDirectory=Path.GetTempPath();Check(BundledFpcalc.Resolve(null,next)==BundledFpcalc.PathIn(next),"Arbeitsverzeichnis beeinflusst Erkennung im Programmordner nicht");}finally{Environment.CurrentDirectory=original;}
  Check(BundledFpcalc.Resolve(old,Path.Combine(root,"fehlend"))==old,"Bestehende Installationen ohne Bundle können ihr vorhandenes Werkzeug weiter verwenden");
  Check(BundledFpcalc.Resolve("veraltet.exe",Path.Combine(root,"fehlend"))==null,"Fehlendes Bundle und veralteter Ersatzpfad werden nicht als einsatzbereit gemeldet");
  Check(BundledFpcalc.Version=="1.6.2","Mitgelieferte Version ausdrücklich ausgewiesen");
  Console.WriteLine("BESTANDEN: "+count+" Prüfungen der portablen fpcalc-Einbindung; Testdateien sind keine ausführbaren Programme");
 }
}
