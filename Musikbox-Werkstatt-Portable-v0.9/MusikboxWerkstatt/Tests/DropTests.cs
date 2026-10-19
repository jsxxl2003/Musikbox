using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Musikbox;
class DropTests {
 static int tests;
 static void Check(bool b,string text){if(!b)throw new Exception(text);tests++;Console.WriteLine("OK "+text);}
 static void Main(string[] args) {
  string sourceRoot=Path.GetFullPath(args[0]),work=Path.GetFullPath(args[1]);Directory.CreateDirectory(work);string file=Directory.GetFiles(sourceRoot,"*.mp3",SearchOption.AllDirectories).First();byte[] original=File.ReadAllBytes(file);
  var p=new Project();var single=DropImport.Read(p,new[]{file},-1,()=>{});Check(single.FilesAdded==1&&single.Project.Folders.Count==1&&single.Project.Folders[0].Theme==Tags.Read(file).Album,"Einzeldatei ohne Ziel wird nach Album-Tag zugeordnet");Check(p.Folders.Count==0,"Eingangsprojekt bleibt unverändert");
  var duplicate=DropImport.Read(single.Project,new[]{file,file},0,()=>{});Check(duplicate.FilesAdded==1&&duplicate.Skipped==1&&duplicate.Project.Folders[0].Songs.Count==1&&duplicate.Project.Folders.Count==2,"Doppelte Dateipfade innerhalb desselben Drops werden übersprungen");
  var target=new Project{Folders=new List<BoxFolder>{new BoxFolder{Name="000 Links",Theme="Links"},new BoxFolder{Name="001 Rechts",Theme="Rechts"}}};var placed=DropImport.Read(target,new[]{file},1,()=>{});Check(placed.Project.Folders[0].Songs.Count==0&&placed.Project.Folders[1].Songs.Count==0&&placed.Project.Folders[2].Songs.Count==1,"Album-Tag bestimmt das Thema vor dem ausgewählten Ziel");
  var full=DropImport.Read(new Project(),new[]{sourceRoot},-1,()=>{});Check(full.FilesAdded==15&&full.Project.Folders.Count==5,"Sammlungsordner mit fünf Themenordnern importiert");
  string child=Directory.GetDirectories(sourceRoot).First();var overlapping=DropImport.Read(new Project(),new[]{sourceRoot,child,file},-1,()=>{});Check(overlapping.FilesAdded==15&&overlapping.Project.Folders.Count==5,"Überlappende Ordner und mitgezogene Einzeldatei nicht doppelt importiert");
  var reimport=DropImport.Read(full.Project,new[]{sourceRoot},0,()=>{});Check(reimport.FilesAdded==15&&reimport.Project.Folders.Count==10,"Erneuter Ordnerimport erstellt neue Sammlung neben bestehenden Ordnern");
  string text=Path.Combine(work,"Notiz.txt");File.WriteAllText(text,"Keine Musik");var invalid=DropImport.Read(new Project(),new[]{text,Path.Combine(work,"fehlt.mp3")},-1,()=>{});Check(invalid.FilesAdded==0&&invalid.Warnings.Count==2&&invalid.Project.Folders.Count==0,"Ungeeignete und fehlende Dateien verständlich gemeldet");
  string fixture=Path.Combine(work,"Basis.mp3");var tag=Tags.Read(file);using(var src=File.OpenRead(file)){src.Position=tag.AudioStart;byte[] audio=new byte[8192];int n=src.Read(audio,0,audio.Length);File.WriteAllBytes(fixture,audio.Take(n).ToArray());}
  string many=Path.Combine(work,"472 Dateien");Directory.CreateDirectory(many);var paths=new List<string>();for(int i=471;i>=0;i--){string dest=Path.Combine(many,i.ToString("D3")+".mp3");Tags.WriteCopy(new Song{Source=fixture,Artist="Interpret "+i.ToString("D3"),Title="Titel"},"Alt",dest,()=>{});paths.Add(dest);}
  var split=DropImport.Read(new Project(),paths.ToArray(),-1,()=>{});Check(split.FilesAdded==472&&split.Project.Folders.Select(f=>f.Songs.Count).SequenceEqual(new[]{99,99,99,99,76}),"472 fallengelassene Dateien auf fünf Ordner verteilt");Check(split.Project.Folders.SelectMany(f=>f.Songs).Select(s=>s.Artist).SequenceEqual(Enumerable.Range(0,472).Select(i=>"Interpret "+i.ToString("D3"))),"Alphabetische Reihenfolge über Ordnergrenzen erhalten");
  var sameAgain=DropImport.Read(split.Project,new[]{paths[0]},0,()=>{});Check(sameAgain.FilesAdded==1&&sameAgain.Project.Folders.Count==6,"Erneut gezogene Datei landet in neuem Ordner statt bestehendem Folgeordner");
  int checks=0;bool cancelled=false;try{DropImport.Read(target,paths.ToArray(),1,()=>{if(++checks==12)throw new OperationCanceledException();});}catch(OperationCanceledException){cancelled=true;}Check(cancelled&&target.Folders.All(f=>f.Songs.Count==0),"Abbruch hinterlässt kein teilweise geändertes Projekt");
  var maxed=new Project();for(int n=0;n<99;n++)maxed.Folders.Add(new BoxFolder{Name=n.ToString("D3")+" Thema",Theme="Thema"});bool capacity=false;try{DropImport.Read(maxed,new[]{file},-1,()=>{});}catch{capacity=true;}Check(capacity&&maxed.Folders.Count==99&&maxed.Folders.All(f=>f.Songs.Count==0),"99-Ordner-Grenze ohne Teiländerungen eingehalten");
  Check(original.SequenceEqual(File.ReadAllBytes(file)),"Originaldatei nach allen Drop-Vorgängen bytegleich");
  var collision=DropImport.Read(new Project{Folders=new List<BoxFolder>{new BoxFolder{Name=Path.GetFileName(child),Theme="Vorhanden"}}},new[]{child},-1,()=>{});Check(collision.Project.Folders.Select(f=>f.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()==collision.Project.Folders.Count,"Namenskollision beim Ordnerimport aufgelöst");
  Console.WriteLine("BESTANDEN: "+tests+" Drag-and-drop-Verarbeitungsprüfungen");
 }
}
