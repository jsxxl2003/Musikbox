using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace Musikbox {
 public enum CollectionIssueKind { MissingTags, PossibleDuplicate, MissingCover, Capacity }
 public class CollectionIssue {
  public CollectionIssueKind Kind;
  public int FolderIndex,SongIndex=-1;
  public string FolderName,Artist,Title,Message;
 }
 public class CollectionFolderInfo {
  public int FolderIndex,Songs,FreeSlots;
  public string Name,Theme,CoverStatus;
 }
 public class CollectionSummary {
  public int FolderCount,SongCount,FreeFolders,FreeInExistingFolders,FreeOverall;
  public int MissingTagSongs,DuplicateGroups,DuplicateSongs,MissingCovers;
  public List<CollectionIssue> Issues=new List<CollectionIssue>();
  public List<CollectionFolderInfo> Folders=new List<CollectionFolderInfo>();
 }
 public static class CollectionOverview {
  class Entry {public int Folder,Index;public Song Song;}
  static string Key(string value){return Regex.Replace((value??"").Trim().Normalize(),@"\s+"," ").ToUpperInvariant();}
  public static CollectionSummary Analyze(Project project,Func<string,bool> coverExists=null){
   if(coverExists==null)coverExists=File.Exists;
   var r=new CollectionSummary();var entries=new List<Entry>();
   r.FolderCount=project.Folders.Count;r.SongCount=project.Folders.Sum(f=>f.Songs.Count);
   r.FreeFolders=Math.Max(0,99-r.FolderCount);r.FreeOverall=Math.Max(0,9801-r.SongCount);
   for(int i=0;i<project.Folders.Count;i++){
    var f=project.Folders[i];int free=Math.Max(0,99-f.Songs.Count);r.FreeInExistingFolders+=free;
    bool hasCover=!String.IsNullOrWhiteSpace(f.Cover)&&coverExists(f.Cover);
    r.Folders.Add(new CollectionFolderInfo{FolderIndex=i,Name=f.Name,Theme=f.Theme,Songs=f.Songs.Count,FreeSlots=free,CoverStatus=hasCover?"Ordnercover vorhanden":"Kein erreichbares Ordnercover"});
    if(!hasCover){r.MissingCovers++;r.Issues.Add(new CollectionIssue{Kind=CollectionIssueKind.MissingCover,FolderIndex=i,FolderName=f.Name,Message=String.IsNullOrWhiteSpace(f.Cover)?"Kein Ordnercover ausgewählt. Eingebettete MP3-Bilder werden hier nicht geprüft.":"Ausgewählte Coverdatei nicht erreichbar: "+f.Cover});}
    if(f.Songs.Count>99)r.Issues.Add(new CollectionIssue{Kind=CollectionIssueKind.Capacity,FolderIndex=i,FolderName=f.Name,Message=f.Songs.Count+" Titel; mehr als 99 in diesem Ordner."});
    for(int j=0;j<f.Songs.Count;j++){
     var s=f.Songs[j];bool artist=String.IsNullOrWhiteSpace(s.Artist),title=String.IsNullOrWhiteSpace(s.Title);
     if(artist||title){r.MissingTagSongs++;r.Issues.Add(new CollectionIssue{Kind=CollectionIssueKind.MissingTags,FolderIndex=i,SongIndex=j,FolderName=f.Name,Artist=s.Artist,Title=s.Title,Message=artist&&title?"Interpret und Titel fehlen.":artist?"Interpret fehlt.":"Titel fehlt."});}
     else entries.Add(new Entry{Folder=i,Index=j,Song=s});
    }
   }
   foreach(var group in entries.GroupBy(x=>new{Artist=Key(x.Song.Artist),Title=Key(x.Song.Title)}).Where(g=>g.Count()>1)){
    r.DuplicateGroups++;r.DuplicateSongs+=group.Count();
    foreach(var e in group)r.Issues.Add(new CollectionIssue{Kind=CollectionIssueKind.PossibleDuplicate,FolderIndex=e.Folder,SongIndex=e.Index,FolderName=project.Folders[e.Folder].Name,Artist=e.Song.Artist,Title=e.Song.Title,Message=group.Count()+" Einträge mit gleichem Interpret und Titel. Audiodaten wurden nicht verglichen."});
   }
   return r;
  }
 }
}
