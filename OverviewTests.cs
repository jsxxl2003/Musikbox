using System;
using System.Linq;
using System.Collections.Generic;
using Musikbox;
class OverviewTests {
 static int count;
 static void Check(bool value,string text){if(!value)throw new Exception(text);count++;Console.WriteLine("OK "+text);}
 static void Main(){
  var empty=CollectionOverview.Analyze(new Project(),x=>false);
  Check(empty.FolderCount==0&&empty.SongCount==0&&empty.Issues.Count==0,"Leeres Projekt ohne falsche Warnungen");
  Check(empty.FreeFolders==99&&empty.FreeOverall==9801&&empty.FreeInExistingFolders==0,"Freie Ordner und Titelplätze eines leeren Projekts korrekt");
  var p=new Project{Folders=new List<BoxFolder>{
   new BoxFolder{Name="031 Rock",Theme="Rock",Cover="vorhanden.jpg",Songs=new List<Song>{
    new Song{Source="a.mp3",Artist=" ABBA ",Title="Dancing   Queen",Album="Alt"},
    new Song{Source="b.mp3",Artist="",Title="Titel"},new Song{Source="c.mp3",Artist="Interpret",Title=null},new Song{Source="d.mp3",Artist=" ",Title=""}}},
   new BoxFolder{Name="032 Country",Theme="Country",Cover="fehlt.jpg",Songs=new List<Song>{new Song{Source="e.mp3",Artist="abba",Title="dancing queen"},new Song{Source="f.mp3",Artist="ABBA",Title="Anderer Titel"}}},
   new BoxFolder{Name="033 Leer",Theme="Leer"}
  }};
  var r=CollectionOverview.Analyze(p,x=>x=="vorhanden.jpg");
  Check(r.FolderCount==3&&r.SongCount==6,"Ordner- und Titelzahl ohne Umverteilung");
  Check(r.FreeFolders==96&&r.FreeInExistingFolders==291&&r.FreeOverall==9795,"Freie Plätze vorhandener Ordner getrennt von freien Ordnerplätzen");
  Check(r.MissingTagSongs==3&&r.Issues.Count(i=>i.Kind==CollectionIssueKind.MissingTags)==3,"Fehlende Angaben je Titel statt je leerem Feld gezählt");
  Check(r.Issues.Any(i=>i.Message=="Interpret und Titel fehlen."&&i.FolderIndex==0&&i.SongIndex==3),"Hinweis mit beiden fehlenden Feldern und richtigem Sprungziel");
  Check(r.DuplicateGroups==1&&r.DuplicateSongs==2,"Groß-/Kleinschreibung und Leerzeichen bei Dubletten normalisiert");
  var duplicates=r.Issues.Where(i=>i.Kind==CollectionIssueKind.PossibleDuplicate).ToList();
  Check(duplicates.Any(i=>i.FolderIndex==0&&i.SongIndex==0)&&duplicates.Any(i=>i.FolderIndex==1&&i.SongIndex==0),"Ordnerübergreifende Dubletten mit zwei korrekten Sprungzielen");
  Check(!duplicates.Any(i=>i.Title=="Anderer Titel"),"Andere Titel desselben Interpreten nicht als Dublette markiert");
  Check(!duplicates.Any(i=>String.IsNullOrWhiteSpace(i.Artist)||String.IsNullOrWhiteSpace(i.Title)),"Fehlende Angaben erzeugen keine falschen Dublettengruppen");
  Check(r.MissingCovers==2&&r.Issues.Count(i=>i.Kind==CollectionIssueKind.MissingCover)==2,"Nicht ausgewähltes und nicht erreichbares Ordnercover erkannt");
  Check(r.Issues.Where(i=>i.Kind==CollectionIssueKind.MissingCover).All(i=>i.SongIndex==-1),"Coverhinweise springen zum Ordner statt zu einem Titel");
  Check(r.Folders[0].CoverStatus=="Ordnercover vorhanden"&&r.Folders[1].CoverStatus=="Kein erreichbares Ordnercover","Vorhandene und fehlende Cover unterscheidbar");
  Check(p.Folders[0].Songs[0].Artist==" ABBA "&&p.Folders[0].Songs[0].Title=="Dancing   Queen"&&p.Folders[0].Songs[0].Album=="Alt"&&p.Folders[0].Name=="031 Rock","Analyse verändert weder Tags noch Namen oder Reihenfolge");
  var accents=new Project{Folders=new List<BoxFolder>{new BoxFolder{Name="000 Test",Theme="Test",Songs=new List<Song>{new Song{Artist="Ärzte",Title="Grüße"},new Song{Artist="A\u0308rzte",Title="Grüße"}}}}};
  Check(CollectionOverview.Analyze(accents,x=>false).DuplicateSongs==2,"Unicode-Schreibweisen desselben Namens zusammengeführt");
  var full=new Project();for(int i=0;i<99;i++)full.Folders.Add(new BoxFolder{Name=i.ToString("D3")+" Thema",Theme="Thema",Cover="ok",Songs=Enumerable.Range(0,99).Select(j=>new Song{Artist="Interpret "+i,Title="Titel "+j}).ToList()});
  var maximum=CollectionOverview.Analyze(full,x=>true);
  Check(maximum.SongCount==9801&&maximum.FreeFolders==0&&maximum.FreeOverall==0&&maximum.FreeInExistingFolders==0,"Voll belegte Sammlung mit 9801 Titeln korrekt gezählt");
  Check(maximum.Issues.Count==0,"Große vollständige Sammlung erzeugt keine falschen Hinweise");
  full.Folders[0].Songs.Add(new Song{Artist="Zusatz",Title="Titel"});var overflow=CollectionOverview.Analyze(full,x=>true);
  Check(overflow.FreeOverall==0&&overflow.Folders[0].FreeSlots==0&&overflow.Issues.Any(i=>i.Kind==CollectionIssueKind.Capacity&&i.FolderIndex==0),"Übervoller Ordner ohne negative freie Plätze gemeldet");
  Check(r.Folders.Select(f=>f.FolderIndex).SequenceEqual(new[]{0,1,2})&&r.Folders[2].Songs==0&&r.Folders[2].FreeSlots==99,"Alle Ordner einschließlich leerer Ordner mit stabilen Sprungzielen gelistet");
  Console.WriteLine("BESTANDEN: "+count+" Sammlungsübersicht-Prüfungen");
 }
}
