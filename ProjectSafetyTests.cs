using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Musikbox;
class ProjectSafetyTests {
 static int tests;
 static void Check(bool value,string text){if(!value)throw new Exception(text);tests++;Console.WriteLine("OK "+text);}
 static bool Fails(Action action){try{action();return false;}catch{return true;}}
 static Project Example(string source){return new Project{FfmpegExecutable="ffmpeg.exe",Mp3Bitrate=192,Folders=new List<BoxFolder>{new BoxFolder{Name="000 Charts 1990",Theme="Charts 1990",Group="Charts",SourceDirectory=Path.GetDirectoryName(source),CoverDesign=CoverDesign.Default(),Songs=new List<Song>{new Song{Source=source,Artist="Interpret",Title="Titel",Album="Charts 1990",PreparedSource="Arbeitskopie.mp3",PreparedProfile="Profil"}}}}};}
 static void Main(string[] args){string work=Path.GetFullPath(args[0]);Directory.CreateDirectory(work);var p=Example(Path.Combine(work,"alt","Musik","song.mp3"));var history=new ProjectHistory();history.Reset(p,true);
  Check(!history.CanUndo&&!history.CanRedo&&!history.IsDirty,"Leerer Verlauf beginnt am gespeicherten Stand");
  Check(!history.Record(p,"Keine Änderung")&&!history.CanUndo,"Unveränderter Stand erzeugt keinen Rückgängig-Schritt");
  p.Folders[0].Songs[0].Artist="Neu";history.Record(p,"Interpret");Check(history.CanUndo&&history.IsDirty&&history.UndoLabel=="Interpret","Tagänderung rückgängig und beschriftet");
  p.Folders[0].CoverDesign.Texts[0].Text="Geändert";p.Folders[0].Cover="cover.jpg";p.Normalize=true;history.Record(p,"Cover und Einstellungen");
  p=history.Undo();Check(p.Folders[0].Cover==null&&p.Folders[0].CoverDesign.Texts[0].Text=="Meine Musikbox"&&!p.Normalize&&p.Folders[0].Songs[0].Artist=="Neu","Verschachtelte Coverentwürfe, Audioeinstellungen und Tags unabhängig zurückgesetzt");
  p=history.Undo();Check(p.Folders[0].Songs[0].Artist=="Interpret"&&!history.IsDirty&&history.CanRedo,"Rückgängig erreicht gespeicherten Stand");
  p=history.Redo();Check(p.Folders[0].Songs[0].Artist=="Neu"&&history.IsDirty,"Wiederholen stellt Änderung wieder her");history.MarkSaved();Check(!history.IsDirty,"Manuelles Speichern setzt gespeicherten Stand");p=history.Undo();Check(history.IsDirty,"Rückgängig vor den neuen Speicherstand markiert ungespeichert");p=history.Redo();Check(!history.IsDirty,"Wiederholen bis Speicherstand markiert gespeichert");
  p=history.Undo();p.Folders[0].Theme="Neues Thema";history.Record(p,"Thema");Check(!history.CanRedo,"Neue Änderung nach Rückgängig verwirft veralteten Wiederholpfad");
  p.Folders.Clear();history.Record(p,"Ordner entfernen");p=history.Undo();Check(p.Folders.Count==1&&p.Folders[0].Songs.Count==1,"Entfernter Ordner samt Titeln wiederhergestellt");
  history.Reset(Example("anderes.mp3"),true);Check(!history.CanUndo&&!history.CanRedo,"Projektwechsel trennt Verläufe");
  var bounded=new ProjectHistory(2);p=Example("a.mp3");bounded.Reset(p,true);for(int i=0;i<4;i++){p.TargetDb=85+i;bounded.Record(p,"Pegel");}int count=0;while(bounded.CanUndo){p=bounded.Undo();count++;}Check(count==2,"Schrittzahl des Verlaufs begrenzt");
  var memory=new ProjectHistory(30,1);p=Example("a.mp3");memory.Reset(p,true);for(int i=0;i<4;i++){p.TargetDb=85+i;memory.Record(p,"Pegel");}count=0;while(memory.CanUndo){p=memory.Undo();count++;}Check(count==1,"Speichergrenze behält mindestens den letzten Rückgängig-Schritt");
  string root=Path.Combine(work,"backups");var backups=new ProjectBackups(Path.Combine(work,"Mein Projekt.musikbox.json"),root,3);p=Example("a.mp3");var bytes=Storage.Bytes(p);string first=backups.Save(bytes,1);
  Check(File.Exists(first)&&Storage.Load(first).Folders[0].Songs[0].Artist=="Interpret","Automatische Sicherung als ladbare Projektdatei geschrieben");
  Check(backups.Save(bytes,2)==null&&ProjectBackups.List(root).Count==1,"Identische Sicherungen nicht doppelt geschrieben");
  for(int i=3;i<8;i++){p.TargetDb=80+i;backups.Save(Storage.Bytes(p),i);}Check(ProjectBackups.List(root).Count==3,"Nur die letzten Sicherungsstände je Projekt behalten");
  Check(!File.Exists(first),"Ältester Stand erst nach erfolgreicher neuer Sicherung entfernt");
  var before=ProjectBackups.List(root).Select(e=>e.File).ToArray();Check(Fails(()=>backups.Save(new byte[]{1,2,3},8))&&ProjectBackups.List(root).Select(e=>e.File).SequenceEqual(before),"Ungültiger neuer Stand zerstört keine gültige Sicherung");
  p.TargetDb=95;string newer=backups.Save(Storage.Bytes(p),10);p.TargetDb=80;Check(backups.Save(Storage.Bytes(p),9)==null&&Storage.Load(newer).TargetDb==95,"Verspätete alte Hintergrundsicherung kann neueren Stand nicht ersetzen");
  string dir=Path.GetDirectoryName(newer);File.WriteAllText(Path.Combine(dir,"abgebrochen.musikbox.json.tmp"),"defekt");Check(ProjectBackups.List(root).Count==3,"Unvollständige temporäre Dateien nicht als Wiederherstellung angeboten");
  var other=new ProjectBackups(Path.Combine(work,"anderer Ort","Mein Projekt.musikbox.json"),root,3);other.Save(bytes,1);Check(ProjectBackups.List(root).Count==4,"Gleichnamige Projekte an anderen Orten getrennt gesichert");
  var unnamed1=new ProjectBackups(null,root);var unnamed2=new ProjectBackups(null,root);unnamed1.Save(bytes,1);unnamed2.Save(bytes,1);Check(ProjectBackups.List(root).Count==6,"Zwei ungespeicherte Sitzungen überschreiben sich nicht");
  Check(!Directory.GetFiles(root,"*.mp3",SearchOption.AllDirectories).Any(),"Sicherungen enthalten keine kopierten Musikdateien");
  string blocked=Path.Combine(work,"kein Ordner");File.WriteAllText(blocked,"blockiert");Check(Fails(()=>new ProjectBackups(null,blocked).Save(bytes,1)),"Schreibfehler als Fehler gemeldet");
  Check(SourceRelinking.Map(@"E:\Musik\000 Rock\Titel.mp3",@"E:\Musik",@"F:\Musik")==@"F:\Musik\000 Rock\Titel.mp3","Windows-Laufwerkswechsel mit Unterordnern abgebildet");
  Check(SourceRelinking.Map(@"e:\musik\Titel.mp3",@"E:\Musik",@"F:\Musik")==@"F:\Musik\Titel.mp3","Windows-Pfade unabhängig von Großschreibung zugeordnet");
  Check(SourceRelinking.Map(@"E:\Musikarchiv\Titel.mp3",@"E:\Musik",@"F:\Musik")==null,"Ähnlicher Ordnername nicht versehentlich als Unterordner behandelt");
  Check(SourceRelinking.Map(@"E:\Musik\Titel.mp3",@"E:\",@"F:\")==@"F:\Musik\Titel.mp3","Wechsel des ganzen Laufwerks unterstützt");
  Check(SourceRelinking.Map(@"E:\",@"E:\",@"F:\")==@"F:\","Laufwerkswurzel bleibt absolut");
  Check(SourceRelinking.Map(@"\\alt\freigabe\Musik\a.mp3",@"\\alt\freigabe",@"\\neu\freigabe")==@"\\neu\freigabe\Musik\a.mp3","Netzwerkfreigaben mit derselben Struktur zugeordnet");
  Check(Fails(()=>SourceRelinking.Root(@"E:\Musik\..\Andere")),"Mehrdeutige Windows-Elternpfade abgewiesen");
  Check(Fails(()=>SourceRelinking.Root("relativer-pfad")),"Relative alte Speicherorte abgewiesen");
  string oldRoot=Path.Combine(work,"alt"),newRoot=Path.Combine(work,"neu");Directory.CreateDirectory(Path.Combine(newRoot,"Musik"));string newSong=Path.Combine(newRoot,"Musik","song.mp3");File.WriteAllBytes(newSong,new byte[]{1,4,7});string picture=Path.Combine(newRoot,"Musik","Folder.jpg");File.WriteAllText(picture,"Bildfixture");string existing=Path.Combine(work,"erreichbar.mp3");File.WriteAllText(existing,"erhalten");
  p=Example(Path.Combine(oldRoot,"Musik","song.mp3"));p.Folders[0].Cover=Path.Combine(oldRoot,"Musik","Folder.jpg");p.Folders[0].CoverDesign.ImagePath=p.Folders[0].Cover;p.Folders[0].Songs[0].Artwork=p.Folders[0].Cover;p.Folders[0].Songs.Add(new Song{Source=Path.Combine(oldRoot,"Musik","nicht-da.mp3"),Artist="Fehlend",Title="Nicht gefunden",Album="Charts 1990"});p.Folders[0].Songs.Add(new Song{Source=existing,Artist="Bestehend",Title="Erhalten",Album="Charts 1990"});var original=Storage.Bytes(p);
  var plan=SourceRelinking.Plan(p,oldRoot,newRoot,()=>{});Check(plan.Found==5&&plan.Entries.Count==6,"Musik, Quellordner und drei Coververweise geprüft; fehlender Titel bleibt ungelöst");
  Check(Storage.Bytes(p).SequenceEqual(original),"Vorschau verändert das Projekt nicht");
  var linked=SourceRelinking.Apply(p,plan);var song=linked.Folders[0].Songs[0];Check(song.Source==newSong&&song.PreparedSource==null&&song.PreparedProfile==null,"Neue Quelle übernommen und veraltetes Audio-Profil verworfen");
  Check(song.Artist=="Interpret"&&song.Title=="Titel"&&song.Album=="Charts 1990"&&linked.Folders[0].Theme=="Charts 1990","Interpret, Titel und Themenalbum beim Wiederverbinden erhalten");
  Check(linked.Folders[0].Cover==picture&&song.Artwork==picture&&linked.Folders[0].CoverDesign.ImagePath==picture,"Ordnercover, Titelcover und bearbeitbarer Coverhintergrund wieder verbunden");
  Check(linked.Folders[0].Songs[1].Source==p.Folders[0].Songs[1].Source&&linked.Folders[0].Songs[2].Source==existing,"Nicht gefundene und bereits erreichbare Quellen bleiben unverändert");
  Check(Storage.Bytes(p).SequenceEqual(original)&&File.ReadAllBytes(newSong).SequenceEqual(new byte[]{1,4,7}),"Ausgangsprojekt und Musikdatei nach Übernahme unverändert");
  var h=new ProjectHistory();h.Reset(p,true);h.Record(linked,"Wiederverbinden");Check(Storage.Bytes(h.Undo()).SequenceEqual(original),"Wiederverbinden vollständig rückgängig möglich");
  p.Folders[0].Songs[0].Artist="Inzwischen geändert";Check(Fails(()=>SourceRelinking.Apply(p,plan)),"Veraltete Zuordnungsvorschau nach Projektänderung abgewiesen");p=Storage.FromBytes(original);
  File.Delete(newSong);Check(Fails(()=>SourceRelinking.Apply(p,plan))&&Storage.Bytes(p).SequenceEqual(original),"Entfernter Datenträger vor Übernahme ohne Teiländerung erkannt");
  int n=0;bool canceled=false;try{SourceRelinking.Plan(p,oldRoot,newRoot,()=>{if(++n>3)throw new OperationCanceledException();});}catch(OperationCanceledException){canceled=true;}Check(canceled&&Storage.Bytes(p).SequenceEqual(original),"Abgebrochene Dateisuche verändert kein Projekt");
  Check(SourceRelinking.SuggestedRoots(p).Any(s=>s.Contains("Musik")),"Alter Speicherort aus fehlenden Quellen vorgeschlagen");
  var large=new Project();int titles=0;for(int f=0;f<88;f++){var folder=new BoxFolder{Name=f.ToString("D3")+" Thema",Theme="Thema"};while(folder.Songs.Count<(f==87?94:92)){folder.Songs.Add(new Song{Source="USB/"+f+"/"+titles+".mp3",Artist="Interpret "+titles,Title="Titel "+titles,Album="Thema"});titles++;}large.Folders.Add(folder);}var largeHistory=new ProjectHistory();largeHistory.Reset(large,true);large.Folders[87].Songs.Last().Title="Gezielte Änderung";largeHistory.Record(large,"Ein Titel");var largeUndo=largeHistory.Undo();Check(largeUndo.Folders.Count==88&&largeUndo.Folders.Sum(f=>f.Songs.Count)==8098&&largeUndo.Folders[87].Songs.Last().Title!="Gezielte Änderung","Rückgängig in Sammlung mit 88 Ordnern und 8.098 Titeln erhält Struktur und alle Titel");var largeRedo=largeHistory.Redo();Check(largeRedo.Folders[87].Songs.Last().Title=="Gezielte Änderung","Einzelne Änderung in großer Sammlung wiederholt");string largeBackup=new ProjectBackups(null,Path.Combine(work,"large-backup")).Save(Storage.Bytes(largeRedo),1);Check(Storage.Load(largeBackup).Folders.Sum(f=>f.Songs.Count)==8098,"Automatische Sicherung großer Sammlung vollständig wieder ladbar");
  Console.WriteLine("BESTANDEN: "+tests+" Projektschutz-Prüfungen unter Linux/Mono, Windows-Pfade zusätzlich rein logisch geprüft");
 }
}
