using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Musikbox;
class CoreTests {
 static int tests;
 static void Assert(bool b,string message){if(!b)throw new Exception(message);tests++;Console.WriteLine("OK "+message);}
 static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b));}
 static byte[] Audio(string file){var t=Tags.Read(file);var b=File.ReadAllBytes(file);return b.Skip((int)t.AudioStart).Take((int)(t.AudioEnd-t.AudioStart)).ToArray();}
 static void Main(string[] args){
  string root=Path.GetFullPath(args[0]),work=Path.GetFullPath(args[1]);Directory.CreateDirectory(work);
  var warnings=new List<string>();var p=new Project{Folders=Importer.Read(root,warnings)};
  Assert(warnings.Count==0,"Alle Beispieldateien lesbar");Assert(p.Folders.Count==5,"Fünf Beispielordner erkannt");Assert(p.Folders.Sum(f=>f.Songs.Count)==15,"15 Titel erkannt");
  var before=p.Folders.SelectMany(f=>f.Songs).ToDictionary(s=>s.Source,s=>Hash(File.ReadAllBytes(s.Source)));
  foreach(var f in p.Folders.Where(f=>f.Theme.StartsWith("Country")))f.Group="Country";
  var sorted=Rules.Sorted(p);var country=sorted.Folders.Where(f=>f.Group=="Country").SelectMany(f=>f.Songs).ToList();Assert(country.First().Artist=="Danny Vera"&&country.Last().Artist=="Deana Carter","Country über Ordnergrenzen sortiert");
  string project=Path.Combine(work,"test.json");Storage.Save(p,project);Storage.Save(p,project);Assert(Storage.Load(project).Folders.Count==5,"Projekt speichern, überschreiben und laden");
  string dest=Exporter.Export(p,work,()=>{},(n,s)=>{});
  Assert(Directory.GetFiles(dest,"*.mp3",SearchOption.AllDirectories).Length==15,"15 MP3-Kopien exportiert");
  int count=0;
  foreach(var f in sorted.Folders)foreach(var song in f.Songs){string file=Directory.GetFiles(Path.Combine(dest,f.Name),"*.mp3").First(x=>{var t=Tags.Read(x);return t.Artist==song.Artist&&t.Title==song.Title;});var tags=Tags.Read(file);Assert(tags.Album==f.Theme,"Album korrekt: "+song.Artist);Assert(Hash(Audio(file))==Hash(Audio(song.Source)),"Audio unverändert: "+song.Artist);Assert(tags.Other.SelectMany(x=>x).SequenceEqual(Tags.Read(song.Source).Other.SelectMany(x=>x)),"Weitere ID3-Frames erhalten: "+song.Artist);count++;}
  Assert(before.All(x=>Hash(File.ReadAllBytes(x.Key))==x.Value),"Alle Originaldateien bytegleich");
  var one=p.Folders[0].Songs[0].Copy();one.Artist="Ärzte & Österreicher";one.Title="Grüße – Straße 🎵";string special=Path.Combine(work,"unicode.mp3");Tags.WriteCopy(one,"Übung",special,()=>{});Assert(Tags.Read(special).Title==one.Title&&Tags.Read(special).Artist==one.Artist,"Unicode bleibt in ID3v2.3 erhalten");
  var big=new Project();for(int i=0;i<2;i++)big.Folders.Add(new BoxFolder{Name="03"+i+" Country",Theme="Country",Group="Country"});for(int i=197;i>=0;i--)big.Folders[i%2].Songs.Add(new Song{Artist="Artist "+i.ToString("D3"),Title="Titel",Source=one.Source});
  var bigSorted=Rules.Sorted(big);Assert(bigSorted.Folders.All(f=>f.Songs.Count==99),"198 Titel auf zweimal 99 verteilt");Assert(bigSorted.Folders[1].Songs[0].Artist=="Artist 099","Alphabetische Fortsetzung bei Titel 100");
  big.Folders[0].Songs.Add(one);var grown=Rules.Sorted(big);Assert(grown.Folders.Count==3&&grown.Folders[2].Songs.Count==1,"199 Titel erzeugen automatisch dritten Ordner");
  var invalid=new Project();for(int i=0;i<100;i++)invalid.Folders.Add(new BoxFolder{Name="Ordner "+i,Theme="Test"});Assert(Rules.Validate(invalid).Any(e=>e.Contains("99 Ordner")),"100 Ordner blockiert");
  Assert(Rules.SafeName("CON")=="_CON"&&Rules.SafeName("AC/DC: Live?")=="AC_DC_ Live_","Windows-Dateinamen bereinigt");
  var moved=new Project();moved.Folders.Add(new BoxFolder{Name="031 Country",Theme="Country"});moved.Folders.Add(new BoxFolder{Name="032 Rock",Theme="Rock"});Rules.MoveFolder(moved,1,0);Assert(moved.Folders[0].Name=="031 Rock"&&moved.Folders[1].Name=="032 Country","Ordner verschieben tauscht Nummern und Reihenfolge");
  moved.Folders[1].Name="031 Doppelt";Assert(Rules.Validate(moved).Any(e=>e.Contains("mehrfach")),"Doppelte Ordnernummer erkannt");
  string unsupported=Path.Combine(work,"v24.mp3");var fake=File.ReadAllBytes(one.Source);fake[3]=4;File.WriteAllBytes(unsupported,fake);bool formatBlocked=false;try{Tags.Read(unsupported);}catch{formatBlocked=true;}Assert(formatBlocked,"Nicht unterstütztes Tag-Format sicher blockiert");
  int complete=Directory.GetDirectories(work,"Musikbox-Export-*").Length;bool cancelled=false;try{Exporter.Export(p,work,()=>{throw new OperationCanceledException();},(n,s)=>{});}catch(OperationCanceledException){cancelled=true;}Assert(cancelled&&Directory.GetDirectories(work,"Musikbox-Export-*").Length==complete&&Directory.GetDirectories(work,".Musikbox-unvollstaendig-*").Length==0,"Abbruch hinterlässt keinen fertigen oder unvollständigen Export");
  bool targetRejected=false;try{Exporter.Export(p,Path.GetDirectoryName(one.Source),()=>{},(n,s)=>{});}catch{targetRejected=true;}Assert(targetRejected,"Export in Musikquelle blockiert");
  var batch=new Project();var theme=new BoxFolder{Name="031 Country von damals bis heute",Theme="Country von damals bis heute",Cover="cover.jpg",SourceDirectory="source"};batch.Folders.Add(theme);batch.Folders.Add(new BoxFolder{Name="032 Rock",Theme="Rock"});
  for(int n=471;n>=0;n--)theme.Songs.Add(new Song{Artist="Interpret "+n.ToString("D3"),Title="Titel",Album="Alt",Source=one.Source});
  var split=Rules.Sorted(batch);Assert(split.Folders.Take(5).Select(f=>f.Songs.Count).SequenceEqual(new[]{99,99,99,99,76}),"472 Titel auf 99/99/99/99/76 verteilt");
  Assert(split.Folders.Take(5).Select(f=>f.Name.Substring(0,3)).SequenceEqual(new[]{"031","032","033","034","035"})&&split.Folders[5].Name=="036 Rock","Fortlaufende Nummern, bestehender Folgeordner verschoben");
  Assert(split.Folders.Take(5).All(f=>f.Theme==theme.Theme&&f.Songs.All(song=>song.Album==theme.Theme)&&f.Cover==theme.Cover&&f.Group==split.Folders[0].Group),"Thema, Album, Gruppe und Cover einheitlich");
  Assert(split.Folders.Take(5).SelectMany(f=>f.Songs).Select(song=>song.Artist).SequenceEqual(Enumerable.Range(0,472).Select(n=>"Interpret "+n.ToString("D3"))),"Alphabetische Reihenfolge über alle fünf Ordner");
  Assert(batch.Folders.Count==2&&theme.Songs.Count==472&&theme.Songs[0].Album=="Alt","Aufteilung verändert Eingangsprojekt nicht");
  var again=Rules.Sorted(split);Assert(again.Folders.Count==6&&again.Folders[4].Songs.Count==76,"Wiederholte Aufteilung erzeugt keine zusätzlichen Ordner");
  var renamed=new Project();renamed.Folders.Add(new BoxFolder{Name="Mein Thema",Theme="Mein Thema",Songs=Enumerable.Range(0,100).Select(n=>one.Copy()).ToList()});var numbered=Rules.Sorted(renamed);Assert(numbered.Folders[0].Name=="000 Mein Thema"&&numbered.Folders[1].Name=="001 Mein Thema","Ordner ohne Nummer werden automatisch nummeriert");
  foreach(int amount in new[]{0,1,99,100,198,199,472,9801}){var edge=new Project();edge.Folders.Add(new BoxFolder{Name="000 Test",Theme="Test",Songs=Enumerable.Range(0,amount).Select(n=>one.Copy()).ToList()});var r=Rules.Sorted(edge);Assert(r.Folders.Count==Math.Max(1,(amount+98)/99)&&r.Folders.Sum(f=>f.Songs.Count)==amount&&r.Folders.All(f=>f.Songs.Count<=99),"Automatische Grenze: "+amount+" Titel");}
  var overflow=new Project();overflow.Folders.Add(new BoxFolder{Name="000 Test",Theme="Test",Songs=Enumerable.Range(0,9802).Select(n=>one.Copy()).ToList()});bool capacityBlocked=false;try{Rules.Sorted(overflow);}catch{capacityBlocked=true;}Assert(capacityBlocked&&overflow.Folders.Count==1&&overflow.Folders[0].Songs.Count==9802,"9802 Titel sicher abgelehnt, Eingabe erhalten");
  var maxed=new Project();for(int n=0;n<99;n++)maxed.Folders.Add(new BoxFolder{Name=n.ToString("D3")+" Thema",Theme="Thema"});maxed.Folders[0].Songs=Enumerable.Range(0,100).Select(n=>one.Copy()).ToList();bool limitBlocked=false;try{Rules.Sorted(maxed);}catch{limitBlocked=true;}Assert(limitBlocked&&maxed.Folders.Count==99,"Zusätzlicher Folgeordner bei 99 belegten Ordnerplätzen blockiert");
  Console.WriteLine("BESTANDEN: "+tests+" Prüfungen");
 }
}
