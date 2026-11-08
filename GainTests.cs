using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Musikbox;
class GainTests {
 static int count;
 static void Check(bool b,string text){if(!b)throw new Exception(text);count++;Console.WriteLine("OK "+text);}
 static void Main(string[] args) {
  string input=Path.GetFullPath(args[0]),work=Path.GetFullPath(args[1]);Directory.CreateDirectory(work);
  var warnings=new List<string>();var p=new Project{Folders=Importer.Read(input,warnings),Normalize=true,GainExecutable="/usr/bin/mp3gain",TargetDb=89};
  Check(Gain.Arguments(89.5)=="-r -k -o -s r -d 0.5 input.mp3","Track-Gain, Clipping-Schutz und dezimaler Zielpegel");
  string save=Path.Combine(work,"gain-project.json");Storage.Save(p,save);var loaded=Storage.Load(save);Check(loaded.Normalize&&loaded.TargetDb==89&&loaded.GainExecutable==p.GainExecutable,"Lautstärkeeinstellungen im Projekt gespeichert");
  var sorted=Rules.Sorted(p);Check(sorted.Normalize&&sorted.TargetDb==89&&sorted.GainExecutable==p.GainExecutable,"Einstellungen nach Sortierung erhalten");
  // Vollständiger Export mit realem Linux-MP3Gain, Windows-Engine separat testen.
  var originals=p.Folders.SelectMany(f=>f.Songs).ToDictionary(s=>s.Source,s=>File.ReadAllBytes(s.Source));
  string dest=Exporter.Export(p,work,()=>{},(n,s)=>{});
  var files=Directory.GetFiles(dest,"*.mp3",SearchOption.AllDirectories);Check(files.Length==15,"15 Titel mit echtem MP3Gain exportiert");
  Check(File.Exists(dest+"-Audioverarbeitung.txt")&&File.ReadAllText(dest+"-Audioverarbeitung.txt").Contains("input.mp3"),"Lautstärkeprotokoll geschrieben");
  foreach(var file in files){var t=Tags.Read(file);var source=p.Folders.SelectMany(f=>f.Songs).First(s=>s.Artist==t.Artist&&s.Title==t.Title);var prior=Tags.Read(source.Source);Check(prior.Other.SelectMany(x=>x).SequenceEqual(t.Other.SelectMany(x=>x)),"Cover und weitere ID3-Frames erhalten: "+t.Artist);}
  Check(originals.All(x=>x.Value.SequenceEqual(File.ReadAllBytes(x.Key))),"Originale nach Lautstärkeanpassung bytegleich");
  string unicode=Path.Combine(work,"Ärztin – Grüße.mp3");File.Copy(files[0],unicode);Gain.Apply(unicode,p,()=>{});Check(Tags.Read(unicode).Artist.Length>0,"Dateinamen mit Umlauten und Sonderzeichen verarbeitet");
  byte[] before=File.ReadAllBytes(unicode);int polls=0;bool cancel=false;try{Gain.Apply(unicode,p,()=>{if(++polls>=2)throw new OperationCanceledException();});}catch(OperationCanceledException){cancel=true;}Check(cancel&&before.SequenceEqual(File.ReadAllBytes(unicode)),"Abbruch verändert die vorherige Kopie nicht");
  Check(Directory.GetDirectories(work,".gain-*").Length==0,"Temporäre MP3Gain-Ordner aufgeräumt");
  p.GainExecutable=Path.Combine(work,"nicht-vorhanden.exe");bool rejected=false;try{Gain.Validate(p);}catch{rejected=true;}Check(rejected,"Fehlende MP3Gain-Engine vor Verarbeitung erkannt");
  Console.WriteLine("BESTANDEN: "+count+" MP3Gain-Prüfungen (Linux-Engine)");
 }
}
