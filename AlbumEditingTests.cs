using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Musikbox;
class AlbumEditingTests {
 static int count;static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Console.WriteLine("OK "+message);}
 static void Main(string[] args){string root=args[0];Directory.CreateDirectory(root);string input=Path.Combine(root,"input"),output=Path.Combine(root,"output");Directory.CreateDirectory(input);Directory.CreateDirectory(output);string basis=Path.Combine(input,"basis.mp3");File.WriteAllBytes(basis,new byte[512]);string source=Path.Combine(input,"source.mp3");Tags.WriteCopy(new Song{Source=basis,Artist="Interpret",Title="Titel"},"Quellalbum",source,()=>{});byte[] original=File.ReadAllBytes(source);
  Song a=new Song{Source=source,Artist="Interpret",Title="Titel",Album="Thema"},b=a.Copy();var songs=new[]{a,b};
  Check(TagEditing.Apply(songs,false,"",false,"",true,"  Eigenes Album ä  ")==2&&songs.All(s=>s.Album=="Eigenes Album ä"&&s.AlbumOverride),"Album unabhängig für mehrere ausgewählte Titel bearbeitbar und getrimmt");
  Check(a.Artist=="Interpret"&&a.Title=="Titel"&&a.Source==source,"Albumänderung erhält Interpret, Titel und Quelldateipfad");
  Check(TagEditing.Apply(songs,false,"",false,"",true,"Eigenes Album ä")==0,"Bereits gesetztes gleiches Album zählt nicht erneut als Änderung");
  Check(TagEditing.Apply(songs,false,"",false,"",false,"Ignorieren")==0&&a.Album=="Eigenes Album ä","Nicht angehaktes Album bleibt unverändert");
  var normal=new Song{Source=source,Artist="Interpret",Title="Normal",Album="Vorher"};var p=new Project();p.Folders.Add(new BoxFolder{Name="000 Thema",Theme="Thema",Songs=new List<Song>{a,normal}});var sorted=Rules.Sorted(p);
  Check(sorted.Folders[0].Songs.Single(s=>s.Title=="Titel").Album=="Eigenes Album ä"&&sorted.Folders[0].Songs.Single(s=>s.Title=="Normal").Album=="Thema","Sortierung erhält eigenes Album und verwendet Thema als Standard für unberührte Titel");
  sorted.Folders[0].Theme="Neues Thema";var themed=Rules.Sorted(sorted);Check(themed.Folders[0].Songs.Single(s=>s.Title=="Titel").Album=="Eigenes Album ä"&&themed.Folders[0].Songs.Single(s=>s.Title=="Normal").Album=="Neues Thema","Themenänderung überschreibt kein ausdrücklich bearbeitetes Album");
  var loaded=Storage.FromBytes(Storage.Bytes(p));Check(loaded.Folders[0].Songs[0].AlbumOverride&&loaded.Folders[0].Songs[0].Album==a.Album&&a.Copy().AlbumOverride,"Projekt-Speicherung und Kopieren erhalten Albumwert und ausdrückliche Zuordnung");
  var legacy=Storage.FromBytes(Encoding.UTF8.GetBytes("{\"Folders\":[{\"Name\":\"000 Alt\",\"Theme\":\"Alt\",\"Songs\":[{\"Source\":\"alt.mp3\",\"Album\":\"Vorher\"}]}]}"));Check(!legacy.Folders[0].Songs[0].AlbumOverride&&Rules.Sorted(legacy).Folders[0].Songs[0].Album=="Alt","Ältere Projekte ohne neue Kennzeichnung behalten bisheriges Themenalbum-Verhalten");
  var history=new ProjectHistory();history.Reset(p,true);TagEditing.Apply(new[]{a},false,"",false,"",true,"Anderes Album");history.Record(p,"Album ändern");Check(history.Undo().Folders[0].Songs[0].Album=="Eigenes Album ä"&&history.Redo().Folders[0].Songs[0].Album=="Anderes Album","Rückgängig und Wiederholen erhalten individuelle Albumänderungen");
  string outdir=Exporter.Export(p,output,()=>{},(n,t)=>{});var written=Directory.GetFiles(outdir,"*.mp3",SearchOption.AllDirectories).Select(Tags.Read).ToList();Check(written.Any(t=>t.Title=="Titel"&&t.Album=="Anderes Album")&&written.Any(t=>t.Title=="Normal"&&t.Album=="Thema"),"MP3-Export schreibt individuelles Album und Themenstandard korrekt");
  Check(File.ReadAllText(outdir+"-Protokoll.csv").Contains("\"Anderes Album\"")&&p.Folders[0].Theme=="Thema"&&p.Folders[0].Name=="000 Thema","Exportbericht nennt tatsächliches Album, Ordnername und Thema bleiben erhalten");
  TagEditing.Apply(new[]{a},false,"",false,"",true,"");Check(Rules.Sorted(p).Folders[0].Songs.Single(s=>s.Title=="Titel").Album==""&&a.AlbumOverride,"Ausdrücklich geleertes Album wird nicht durch Thema ersetzt");
  string cleared=Exporter.Export(p,output,()=>{},(n,t)=>{});Check(Directory.GetFiles(cleared,"*.mp3",SearchOption.AllDirectories).Select(Tags.Read).Single(t=>t.Title=="Titel").Album=="","Export übernimmt ausdrücklich leeres Album");
  var prepared=new Project{Normalize=true,GainExecutable="nicht-aufzurufen.exe"};var ready=a.Copy();ready.Album="Nach Import bearbeitet";ready.PreparedSource=source;ready.PreparedProfile=ImportAudio.Profile(prepared,ready);prepared.Folders.Add(new BoxFolder{Name="000 Thema",Theme="Thema",Songs=new List<Song>{ready}});string cached=Exporter.Export(prepared,output,()=>{},(n,t)=>{});Check(Tags.Read(Directory.GetFiles(cached,"*.mp3",SearchOption.AllDirectories).Single()).Album=="Nach Import bearbeitet","Passende Audio-Arbeitskopie wird ohne erneute Normalisierung mit neuem Album exportiert");
  string wave=Path.Combine(input,"quelle.wav"),err;Ffmpeg.Run("/usr/bin/ffmpeg","-hide_banner -v error -f lavfi -i sine=frequency=440:duration=2 -y "+Ffmpeg.Quote(wave),()=>{},out err);byte[] wav=File.ReadAllBytes(wave);var encoded=new Project{FfmpegExecutable="/usr/bin/ffmpeg"};encoded.Folders.Add(new BoxFolder{Name="000 Thema",Theme="Thema",Songs=new List<Song>{new Song{Source=wave,Artist="Künstler",Title="WAV",Album="Eigenes WAV-Album",AlbumOverride=true}}});string converted=Exporter.Export(encoded,output,()=>{},(n,t)=>{});Check(Tags.Read(Directory.GetFiles(converted,"*.mp3",SearchOption.AllDirectories).Single()).Album=="Eigenes WAV-Album","Echte FFmpeg-Konvertierung erhält individuell bearbeitetes Album");
  Check(original.SequenceEqual(File.ReadAllBytes(source))&&wav.SequenceEqual(File.ReadAllBytes(wave)),"Originale nach allen Album-Exportwegen bytegleich");
  Console.WriteLine("BESTANDEN: "+count+" Album-Bearbeitungs-/Exportprüfungen; synthetische Medien, echtes FFmpeg unter Linux");
 }
}
