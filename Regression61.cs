using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Threading;using Musikbox;
class Regression61 {
 static int count;static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static void Main(){var glen=new Song{Artist="Glen Campbell",Title="Sunflower",Album="Country",Source="glen.mp3",Year="1977",Genre="Country"};var ac=new Song{Artist="AC/DC",Title="Thunderstruck",Source="acdc.mp3"};
 foreach(string text in new[]{"Glen Sun","Campbell Glen","campb sun","GLEN SUN"})Check(LibrarySearch.VoiceMatches(glen,text),text);
 foreach(string text in new[]{"ACDC","AC DC","AC/DC"})Check(LibrarySearch.VoiceMatches(ac,text),text);
 Check(!LibrarySearch.VoiceMatches(glen,"unbekannt Campbell"),"Kein Treffer nur wegen letztem Kuenstlerwort");
 Check(LibrarySearch.Matches(glen,"","1977 country"),"Jahr und Genre");Check(!LibrarySearch.Matches(glen,"","1978"),"Falsches Jahr ausgeschlossen");
 Check(LibrarySearch.VoiceMatches(glen,"Glenn Campbel"),"Phonetische Schreibvarianten");
 var folder=new BoxFolder{Name="Country"};var index=new LibrarySearchIndex(new[]{new LibrarySearchIndex.Entry(folder,ac),new LibrarySearchIndex.Entry(folder,glen)});
 Check(index.Filter("Glen Sun",true,CancellationToken.None).Single().Song==glen,"Sprachsuchindex");
 Check(index.Filter("country 1977",false,CancellationToken.None).Single().Song==glen,"Metadaten-Suchindex");
 using(var cancel=new CancellationTokenSource()){cancel.Cancel();bool cancelled=false;try{index.Filter("glen",true,cancel.Token);}catch(OperationCanceledException){cancelled=true;}Check(cancelled,"Suchabbruch");}
 var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".tsv");try{var aliases=new VoiceCorrections(path);aliases.Set("Kämpel","Glen Campbell");Check(new VoiceCorrections(path).Apply("kämpel")=="Glen Campbell","Korrektur nach Neustart");aliases.Set("Kämpel","Campbell");Check(new VoiceCorrections(path).Apply("Kämpel")=="Campbell","Korrektur bearbeiten");aliases.Remove("Kämpel");Check(new VoiceCorrections(path).Apply("Kämpel")=="Kämpel","Korrektur loeschen");}finally{File.Delete(path);}
 var songs=new[]{glen,ac};Check(TagEditing.Apply(songs,true,"Test",false,"")==2,"Mehrfachbearbeitung");Check(songs.All(s=>s.Artist=="Test")&&glen.Title=="Sunflower","Nicht angehakte Titel erhalten");Check(TagEditing.Apply(songs,false,"",false,"",true,"Eigener Titel")==2&&songs.All(s=>s.AlbumOverride),"Individuelle Album-Tags");
 glen.Source=Path.GetFullPath("a/glen.mp3");ac.Source=Path.GetFullPath("b/ac.mp3");var third=new Song{Source=Path.GetFullPath("a/third.mp3")};var plan=TrackNumbering.Plan(new[]{glen,ac,third},3,true,true,true);Check(String.Join(",",plan.Select(p=>p.Fields["track"]))=="03/2,03/1,04/2","Tracknummern je Ordner, Start, Nullen, Gesamt");Check(glen.Track==null,"Trackvorschau schreibt nicht");
 Check(SourceRelinking.Map(@"C:\Alt\Musik\song.mp3",@"C:\Alt",@"D:\Neu")==@"D:\Neu\Musik\song.mp3","Pfadzuordnung");Check(SourceRelinking.Map(@"C:\Andere\song.mp3",@"C:\Alt",@"D:\Neu")==null,"Fremde Pfade ausgeschlossen");
 using(var capture=new SpeechCapture()){
  var quiet=new byte[3200];var loud=new byte[3200];for(int i=0;i<loud.Length;i+=2){loud[i]=0x00;loud[i+1]=0x10;}
  bool silenceOnly=true;for(int i=0;i<10;i++)silenceOnly&=capture.Feed(quiet,quiet.Length)==null;Check(silenceOnly,"Stille erzeugt keine Suchanfrage");
  Check(capture.Feed(loud,loud.Length)==null,"Aufnahme wartet auf Sprechpause");capture.Feed(loud,loud.Length);capture.Feed(loud,loud.Length);byte[] clip=null;for(int i=0;i<8;i++)clip=capture.Feed(quiet,quiet.Length)??clip;Check(clip!=null&&clip.Length>9600,"PCM-Segment nach Sprechpause");Check(capture.Finish()==null,"Kein altes Segment nach Abschluss");
  capture.Feed(loud,loud.Length);capture.Feed(loud,loud.Length);Check(capture.Finish()!=null,"Erneute Aufnahme ohne Neustart");
 }
 Console.WriteLine("BESTANDEN: "+count+" Tests gegen den vollstaendigen aktualisierten Anwendungscode; kein Windows-/Mikrofontest.");
 }
}
