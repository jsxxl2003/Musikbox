using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Musikbox;
class StructureDropTests {
 static int tests;
 static void Check(bool b,string text){if(!b)throw new Exception(text);tests++;Console.WriteLine("OK "+text);}
 static void Main(string[] args){
  string work=Path.GetFullPath(args[0]);Directory.CreateDirectory(work);
  string basis=Path.Combine(work,"basis.mp3");File.Copy(Path.GetFullPath(args[1]),basis);
  string collection=Path.Combine(work,"Sammlung");Directory.CreateDirectory(collection);
  var dirs=new List<string>();int total=0;
  for(int i=0;i<88;i++){
   string dir=Path.Combine(collection,i.ToString("D3")+" "+(i<2?"Country":"Thema "+i));Directory.CreateDirectory(dir);dirs.Add(dir);
   for(int j=0;j<(i<2?93:92);j++){
    string file=Path.Combine(dir,j.ToString("D2")+".mp3");
    Tags.WriteCopy(new Song{Source=basis,Artist="Interpret "+(99-j).ToString("D3"),Title="Titel "+j},"Album "+i+"-"+(j%2),file,()=>{});total++;
   }
  }
  File.WriteAllBytes(Path.Combine(dirs[0],"Folder.jpg"),new byte[]{255,216,255,217});
  var original=new Project();var imported=DropImport.Read(original,dirs.AsEnumerable().Reverse().ToArray(),-1,()=>{});
  Check(total==8098&&imported.FilesAdded==8098&&imported.FoldersAdded==88,"88 Ordner mit 8098 synthetischen MP3-Testdateien importiert");
  Check(imported.Project.Folders.Select(f=>f.Name).SequenceEqual(dirs.Select(Path.GetFileName)),"Nummern 000 bis 087 und Themen trotz umgekehrter Auswahl erhalten");
  Check(imported.Project.Folders.All(f=>f.Songs.Count<=99),"Kein Zielordner hat mehr als 99 Titel");
  Check(imported.Project.Folders.Take(2).All(f=>f.Songs.Count==93)&&imported.Project.Folders.Take(2).All(f=>String.IsNullOrEmpty(f.Group)),"Gleichnamige Country-Ordner nicht zusammengelegt oder gruppiert");
  Check(imported.Project.Folders.All(f=>f.Songs.Select(s=>s.Artist).SequenceEqual(f.Songs.Select(s=>s.Artist).OrderBy(x=>x,StringComparer.Ordinal))),"Titel je Ordner alphabetisch nach Interpret");
  Check(imported.Project.Folders.All(f=>f.Songs.All(s=>s.Album==f.Theme)),"Album im Projekt entspricht dem Ordner-Thema");
  Check(imported.Project.Folders[0].Cover==Path.Combine(dirs[0],"Folder.jpg"),"Ordnercover übernommen");
  Check(Tags.Read(Path.Combine(dirs[0],"00.mp3")).Album=="Album 0-0"&&original.Folders.Count==0,"Quell-Tags und Eingangsprojekt unverändert");
  var parent=DropImport.Read(new Project(),new[]{collection,dirs[0],Path.Combine(dirs[0],"00.mp3")},-1,()=>{});
  Check(parent.FilesAdded==8098&&parent.FoldersAdded==88,"Sammlungsordner und überlappende Auswahl ohne doppelte Titel");
  bool rejected=false;try{DropImport.Read(new Project(),dirs.ToArray(),-1,()=>{},FolderImportMode.ByAlbumTags);}catch(Exception e){rejected=e.Message.Contains("99");}
  Check(rejected,"Alternative Album-Zuordnung überschreitet hier bewusst die Ordnergrenze");
  var existing=new Project{Folders=new List<BoxFolder>{new BoxFolder{Name="050 Alt",Theme="Alt",Songs=new List<Song>{new Song{Source=basis,Artist="Z",Title="Alt",Album="Unverändert"}}}}};
  var added=DropImport.Read(existing,new[]{dirs[0]},0,()=>{});
  Check(existing.Folders[0].Name=="050 Alt"&&existing.Folders[0].Songs[0].Album=="Unverändert"&&added.Project.Folders.Count==1,"Bestehende Ordner bleiben unverändert");
  Check(added.Project.Folders[0].Name=="000 Country"&&added.Warnings.Count==0,"Neue Bibliothek verwendet ihre Quellnummer ohne alte Nummerierungskollision");
  var gap=DropImport.Read(new Project(),new[]{dirs[31],dirs[32]},-1,()=>{});
  Check(gap.Project.Folders[0].Name.StartsWith("031 ")&&gap.Project.Folders[1].Name.StartsWith("032 "),"Freie dreistellige Nummern mit Lücken bleiben erhalten");
  string large=Path.Combine(work,"090 Groß");Directory.CreateDirectory(large);
  for(int i=0;i<472;i++)Tags.WriteCopy(new Song{Source=basis,Artist="Interpret "+(471-i).ToString("D3"),Title="Titel"},"Abweichendes Album",Path.Combine(large,i+".mp3"),()=>{});
  var split=DropImport.Read(new Project(),new[]{large},-1,()=>{});
  Check(split.Project.Folders.Select(f=>f.Songs.Count).SequenceEqual(new[]{99,99,99,99,76}),"Übervoller Quellordner mit 472 Titeln in fünf Ordner aufgeteilt");
  Check(split.Project.Folders.Select(f=>f.Name.Substring(0,3)).SequenceEqual(new[]{"090","091","092","093","094"})&&split.Project.Folders.All(f=>f.Theme=="Groß"),"Folgeordner mit gleichem Thema und fortlaufenden Nummern");
  var mixed=DropImport.Read(new Project(),new[]{dirs[0],Path.Combine(dirs[1],"00.mp3")},-1,()=>{});
  Check(mixed.FoldersAdded==2&&mixed.Project.Folders.First(f=>f.Theme=="Country").Songs.Count==93,"Gemischter Drop verteilt Einzeldateien getrennt von erhaltenen Ordnern");
  int checks=0;bool cancelled=false;try{DropImport.Read(existing,dirs.ToArray(),0,()=>{if(++checks==25)throw new OperationCanceledException();});}catch(OperationCanceledException){cancelled=true;}
  Check(cancelled&&existing.Folders.Count==1&&existing.Folders[0].Songs.Count==1,"Abbruch ohne Teiländerungen");
  var maxed=new Project();for(int i=0;i<12;i++)maxed.Folders.Add(new BoxFolder{Name=i.ToString("D3")+" Alt",Theme="Alt"});
  var replaced=DropImport.Read(maxed,dirs.ToArray(),-1,()=>{});Check(replaced.Project.Folders.Count==88&&maxed.Folders.Count==12,"88 neue Ordner ersetzen zwölf alte statt sich mit ihnen zu addieren");
  Check(imported.Report.Contains("Neu eingelesen: 88 Ordner / 8098 Titel")&&imported.Report.Contains("Neue Bibliothek: 88 von maximal 99 Ordnern"),"Importbericht zeigt die tatsächlich eingelesenen Mengen");
  string uneven=Path.Combine(work,"Ungleich verteilt");Directory.CreateDirectory(uneven);int unevenCount=0;
  for(int i=0;i<88;i++){
   string d=Path.Combine(uneven,i.ToString("D3")+" Thema");Directory.CreateDirectory(d);
   int count=i<13?100:i<61?91:90;
   for(int j=0;j<count;j++){File.Copy(Path.Combine(dirs[0],"00.mp3"),Path.Combine(d,j+".mp3"));unevenCount++;}
  }
  var packed=DropImport.Read(new Project(),new[]{uneven},-1,()=>{});
  Check(unevenCount==8098&&packed.FoldersAdded==82&&packed.Project.Folders.Take(81).All(f=>f.Songs.Count==99)&&packed.Project.Folders.Last().Songs.Count==79,"88 gleichnamige Themenordner mit 8098 Titeln auf 82 volle Folgeordner einschließlich Rest verteilt");
  Check(packed.Report.Contains("Neu eingelesen: 88 Ordner / 8098 Titel")&&packed.Report.Contains("Auffüllen: 82"),"Importbericht berücksichtigt gemeinsame Themen statt getrennter Restordner");
  for(int i=0;i<12;i++){string d=Path.Combine(dirs[i],"Unterordner");Directory.CreateDirectory(d);File.Copy(Path.Combine(dirs[0],"00.mp3"),Path.Combine(d,"Titel.mp3"));}
  var nested=DropImport.Read(new Project(),new[]{collection},-1,()=>{});
  Check(nested.Report.Contains("Neu eingelesen: 100 Ordner")&&nested.Report.Contains(Path.Combine(dirs[0],"Unterordner"))&&nested.FilesAdded==8110&&nested.FoldersAdded==89,"Unterordner vollständig erfasst und gleiches Unterordner-Thema platzsparend aufgefüllt");
  var nonrecursive=DropImport.Read(new Project(),new[]{dirs[0]},-1,()=>{},FolderImportMode.PreserveStructure,null,false);
  Check(nonrecursive.FilesAdded==93&&nonrecursive.FoldersAdded==1,"Bereits einzeln erfasste Quellordner werden ohne erneuten Unterordnerimport verarbeitet");
  Console.WriteLine("BESTANDEN: "+tests+" Strukturimport-Prüfungen. Synthetische Tags; kein Windows-Oberflächentest.");
 }
}
