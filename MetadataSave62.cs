using System;using System.IO;using System.Linq;using System.Collections.Generic;using Musikbox;
class MetadataSave62 {
 static int count;static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static void Main(string[] args){string root=Path.GetFullPath(args[0]);var paths=new[]{Path.Combine(root,"glen.mp3"),Path.Combine(root,"wilson.mp3")};var songs=paths.Select(MediaMetadata.Read).ToArray();var original=paths.Select(File.ReadAllBytes).ToArray();var titles=songs.Select(x=>x.Title).ToArray();
 var plan=MetadataFilePlan.Create(songs,new Dictionary<string,string>{{"artist","Björk"},{"comment","Grüße aus Köln"},{"year","2026"},{"genre","Country"}});Check(plan.Count==2&&!plan[0].Fields.ContainsKey("title")&&!plan[1].Fields.ContainsKey("title"),"Mehrfachauswahl behaelt individuelle Titel");Check(songs[0].Artist!="Björk","Planung veraendert weder Datei noch Modell");
 var action=FileTagAction.Apply(plan,Path.Combine(root,"undo"));for(int i=0;i<paths.Length;i++){var tags=TagFields.Read(paths[i]);Check(tags["artist"]=="Björk"&&tags["comment"]=="Grüße aus Köln"&&tags["year"]=="2026"&&tags["genre"]=="Country"&&tags["title"]==titles[i],"Tatsaechliche Dateitags mit Umlauten gespeichert "+i);}
 action.Restore(false);Check(paths.Select((p,i)=>File.ReadAllBytes(p).SequenceEqual(original[i])).All(x=>x),"Datei-Ruecknahme stellt Originalbytes wieder her");action.Restore(true);Check(paths.All(p=>TagFields.Read(p)["artist"]=="Björk"),"Datei-Wiederholung");
 var before=paths.Select(File.ReadAllBytes).ToArray();bool rejected=false;try{FileTagAction.Apply(MetadataFilePlan.Create(songs,new Dictionary<string,string>{{"year","ungueltig"}}),Path.Combine(root,"undo"));}catch(ArgumentException){rejected=true;}Check(rejected&&paths.Select((p,i)=>File.ReadAllBytes(p).SequenceEqual(before[i])).All(x=>x),"Ungueltiges Jahr schreibt keine Quelldatei");
 Check(MetadataFilePlan.Create(new[]{songs[0],songs[0]},new Dictionary<string,string>()).Count==1,"Dateipfad wird nur einmal gespeichert");
 var history=new ProjectHistory();var project=new Project();history.Reset(project,true);Check(history.Record(project,"Dateiaktion:test",true)&&history.CanUndo,"Dateiaktion bleibt auch ohne neue Projektdaten ruecknehmbar");
 action.Restore(false);
 Console.WriteLine("BESTANDEN: "+count+" Dateispeicher-Tests auf echten synthetischen MP3-Dateien. Windows-Explorer-Anzeige nicht hier pruefbar.");}
}
