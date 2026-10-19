using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Musikbox;
class TagDropTests {
 static int tests;
 static void Check(bool b,string text){if(!b)throw new Exception(text);tests++;Console.WriteLine("OK "+text);}
 static void Main(string[] args){
  string input=Path.GetFullPath(args[0]),work=Path.GetFullPath(args[1]);Directory.CreateDirectory(work);string original=Directory.GetFiles(input,"*.mp3",SearchOption.AllDirectories).First();byte[] before=File.ReadAllBytes(original);
  string basis=Path.Combine(work,"basis.mp3");var t=Tags.Read(original);using(var f=File.OpenRead(original)){f.Position=t.AudioStart;byte[] audio=new byte[8192];int n=f.Read(audio,0,audio.Length);File.WriteAllBytes(basis,audio.Take(n).ToArray());}
  string source=Path.Combine(work,"Falscher Ordnername");Directory.CreateDirectory(source);
  string a=Path.Combine(source,"zzz.mp3"),b=Path.Combine(source,"aaa.mp3"),c=Path.Combine(source,"anderer Name.mp3"),missing=Path.Combine(work,"ohne Album.mp3");
  Tags.WriteCopy(new Song{Source=basis,Artist="ABBA",Title="Zweiter Titel"},"Rock",a,()=>{});
  Tags.WriteCopy(new Song{Source=basis,Artist="ABBA",Title="Erster Titel"},"Rock",b,()=>{});
  Tags.WriteCopy(new Song{Source=basis,Artist="Johnny Cash",Title="Titel"},"Country",c,()=>{});
  Tags.WriteCopy(new Song{Source=basis,Artist="Ohne Album",Title="Titel"},"",missing,()=>{});
  var p=new Project{Folders=new List<BoxFolder>{new BoxFolder{Name="000 Ausgewählt",Theme="Ausgewählt"}}};
  var result=DropImport.Read(p,new[]{a,b,c},0,()=>{});Check(result.Project.Folders.Count==3&&result.Project.Folders[0].Songs.Count==0,"Vorhandene Album-Tags haben Vorrang vor ausgewähltem Ordner");
  var rock=result.Project.Folders.First(f=>f.Theme=="Rock");Check(rock.Songs.Count==2&&rock.Songs[0].Title=="Erster Titel"&&rock.Songs[1].Title=="Zweiter Titel","Sortierung nach Interpret und Titel statt Dateiname");Check(result.Project.Folders.First(f=>f.Theme=="Country").Songs.Single().Artist=="Johnny Cash","Unterschiedliche Album-Tags ergeben getrennte Themen");
  var again=DropImport.Read(result.Project,new[]{a},0,()=>{});Check(again.FilesAdded==1&&again.Project.Folders.Count==result.Project.Folders.Count+1&&again.Project.Folders[1].Songs.Count==2,"Erneuter Drop legt neue Ordner an und lässt bestehende unverändert");
  var fallback=DropImport.Read(p,new[]{missing},0,()=>{});Check(fallback.Project.Folders.Count==2&&fallback.Project.Folders[0].Songs.Count==0&&fallback.Project.Folders[1].Songs[0].Album=="Ausgewählt","Fehlender Album-Tag nutzt ausgewähltes Thema als Vorlage für neuen Ordner");
  var noTarget=DropImport.Read(new Project(),new[]{missing},-1,()=>{});Check(noTarget.Project.Folders.Single().Theme=="Neue Musik","Ohne Album und Ziel wird Neue Musik verwendet");
  var folder=DropImport.Read(new Project(),new[]{source,a},-1,()=>{});Check(folder.FilesAdded==3&&folder.Project.Folders.Count==2&&!folder.Project.Folders.Any(f=>f.Theme=="Falscher Ordnername"),"Auch Ordnerinhalt wird anhand Album-Tags zugeordnet");
  var existing=new Project{Folders=new List<BoxFolder>{new BoxFolder{Name="000 Rock",Theme="Rock"},new BoxFolder{Name="001 Rock",Theme="Rock"}}};var merged=DropImport.Read(existing,new[]{a,b},-1,()=>{});Check(merged.Project.Folders.Count==3&&merged.Project.Folders[0].Songs.Count==0&&merged.Project.Folders[1].Songs.Count==0&&String.IsNullOrEmpty(merged.Project.Folders[0].Group)&&merged.Project.Folders[2].Songs.Count==2,"Vorhandene gleichnamige Themenordner nicht verändert oder gruppiert");
  string big=Path.Combine(work,"472 Titel");Directory.CreateDirectory(big);var paths=new List<string>();for(int i=471;i>=0;i--){string path=Path.Combine(big,i+".mp3");Tags.WriteCopy(new Song{Source=basis,Artist="Interpret "+i.ToString("D3"),Title="Titel"},"Mein Thema",path,()=>{});paths.Add(path);}var split=DropImport.Read(p,paths.ToArray(),0,()=>{});var group=split.Project.Folders.Where(f=>f.Theme=="Mein Thema").ToList();Check(group.Select(f=>f.Songs.Count).SequenceEqual(new[]{99,99,99,99,76}),"472 Titel desselben Albums automatisch aufgeteilt");Check(group.SelectMany(f=>f.Songs).Select(s=>s.Artist).SequenceEqual(Enumerable.Range(0,472).Select(i=>"Interpret "+i.ToString("D3"))),"Alphabetische Reihenfolge über Album-Folgeordner");
  Check(p.Folders.Count==1&&p.Folders[0].Songs.Count==0&&before.SequenceEqual(File.ReadAllBytes(original)),"Projekt und Originaldateien unverändert erhalten");
  int checks=0;bool cancelled=false;try{DropImport.Read(p,paths.ToArray(),0,()=>{if(++checks==8)throw new OperationCanceledException();});}catch(OperationCanceledException){cancelled=true;}Check(cancelled&&p.Folders[0].Songs.Count==0,"Abbruch der Tag-Zuordnung ohne Teiländerungen");
  var reference=DropImport.Read(new Project(),new[]{input},-1,()=>{});Check(reference.FilesAdded==15&&reference.Project.Folders.Count==5,"Fünf Referenzordner mit und ohne Album-Tags eingelesen");
  var untouched=new Project{Folders=new List<BoxFolder>{new BoxFolder{Name="031 Rock",Theme="Rock",Group="Bestehende Gruppe",Cover="Alt.jpg",SourceDirectory="Alt",Songs=new List<Song>{new Song{Source=original,Artist="Z",Title="Alt",Album="Original-Album"},new Song{Source=original,Artist="A",Title="Nicht sortieren",Album="Unverändert"}}},new BoxFolder{Name="050 Weiteres",Theme="Weiteres"}}};
  var isolated=DropImport.Read(untouched,new[]{a,b},0,()=>{});var kept=isolated.Project.Folders[0];Check(kept.Name=="031 Rock"&&kept.Group=="Bestehende Gruppe"&&kept.Cover=="Alt.jpg"&&kept.SourceDirectory=="Alt"&&kept.Songs[0].Artist=="Z"&&kept.Songs[0].Album=="Original-Album"&&kept.Songs[1].Artist=="A"&&kept.Songs[1].Album=="Unverändert","Vorhandene Ordnerdaten, Tags und Titelreihenfolge vollständig erhalten");Check(isolated.Project.Folders[1].Name=="050 Weiteres"&&isolated.Project.Folders[2].Name=="051 Rock","Neue Nummern schließen hinter höchster bestehender Nummer an");
  Console.WriteLine("BESTANDEN: "+tests+" Tag-Zuordnungsprüfungen");
 }
}
