using System;using System.IO;using Musikbox;
class PortablePathsTests {
 static int count;static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("OK "+name);}
 static void Main(){string root=Path.Combine(Path.GetTempPath(),"Musikbox-Pfade-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);try{
  Check(PortablePaths.RootFor(root)==root,"Bisheriges flaches Paket behaelt seinen Datenordner");
  string program=Path.Combine(root,"Programm");Directory.CreateDirectory(program);
  Check(PortablePaths.RootFor(program)==program,"Ein beliebiger Ordner Programm wird nicht als Paket umgedeutet");
  File.WriteAllText(Path.Combine(root,"Musikbox-Werkstatt-Studio.exe"),"Test-Startdatei");
  Check(PortablePaths.RootFor(program+Path.DirectorySeparatorChar)==root,"Strukturiertes Paket verwendet Daten im Hauptordner");
  string legacy=Path.Combine(root,"tools","fpcalc","fpcalc.exe");Directory.CreateDirectory(Path.GetDirectoryName(legacy));File.WriteAllText(legacy,"");
  Check(BundledFpcalc.Resolve(null,root)==legacy,"Vorhandene alte tools-Struktur bleibt lesbar");
  string current=Path.Combine(root,"Werkzeuge","fpcalc","fpcalc.exe");Directory.CreateDirectory(Path.GetDirectoryName(current));File.WriteAllText(current,"");
  Check(BundledFpcalc.Resolve(legacy,root)==current,"Neue Werkzeuge-Struktur hat Vorrang vor veraltetem gespeicherten Pfad");
  string moved=root+"-verschoben";Directory.Move(root,moved);try{
   Check(PortablePaths.RootFor(Path.Combine(moved,"Programm"))==moved,"Paket laesst sich als Ganzes verschieben");
   Check(BundledFpcalc.Resolve(current,moved)==Path.Combine(moved,"Werkzeuge","fpcalc","fpcalc.exe"),"Werkzeuge nach Paketverschiebung gefunden");
  }finally{Directory.Move(moved,root);}
 }finally{Directory.Delete(root,true);}Console.WriteLine("BESTANDEN: "+count+" Portable-Pfadpruefungen.");}
}
