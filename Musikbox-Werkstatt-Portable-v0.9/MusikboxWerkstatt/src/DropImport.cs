using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace Musikbox {
 public class DropResult {
  public Project Project;
  public int FilesAdded,FoldersAdded,Skipped;
  public int SelectedIndex;
  public List<string> Warnings=new List<string>();
 }
 public static class DropImport {
  static BoxFolder Copy(BoxFolder f){return new BoxFolder{Name=f.Name,Theme=f.Theme,Group=f.Group,SourceDirectory=f.SourceDirectory,Cover=f.Cover,Songs=f.Songs.Select(s=>s.Copy()).ToList()};}
  static string ThemeKey(string value){return (value??"").Trim().Normalize();}
  static bool AddByTags(Project candidate,Song song,BoxFolder fallback,BoxFolder sourceFolder) {
   string theme=ThemeKey(song.Album);bool hasAlbum=theme.Length>0;if(!hasAlbum)theme=fallback==null?"Neue Musik":fallback.Theme;
   List<BoxFolder> matches;
   if(!hasAlbum&&sourceFolder!=null)matches=candidate.Folders.Where(f=>String.Equals(f.SourceDirectory,sourceFolder.SourceDirectory,StringComparison.OrdinalIgnoreCase)).ToList();
   else if(!hasAlbum&&fallback!=null)matches=candidate.Folders.Where(f=>String.Equals(ThemeKey(f.Theme),ThemeKey(theme),StringComparison.OrdinalIgnoreCase)).ToList();
   else matches=candidate.Folders.Where(f=>String.Equals(ThemeKey(f.Theme),ThemeKey(theme),StringComparison.OrdinalIgnoreCase)).ToList();
   BoxFolder destination;
   if(matches.Count==0) {
    string name=sourceFolder!=null&&String.Equals(ThemeKey(sourceFolder.Theme),ThemeKey(theme),StringComparison.OrdinalIgnoreCase)?sourceFolder.Name:Rules.SafeName(theme);
    string baseName=name;int n=1;while(candidate.Folders.Any(f=>String.Equals(f.Name,name,StringComparison.OrdinalIgnoreCase)))name=baseName+" ("+(++n)+")";
    destination=new BoxFolder{Name=name,Theme=theme,Group="",SourceDirectory=sourceFolder==null?null:sourceFolder.SourceDirectory,Cover=sourceFolder==null?null:sourceFolder.Cover};candidate.Folders.Add(destination);matches.Add(destination);
   }else destination=matches[0];
   if(matches.Count>1){string group="Thema: "+destination.Theme+" ("+Guid.NewGuid().ToString("N").Substring(0,6)+")";foreach(var f in matches)f.Group=group;}
   if(matches.SelectMany(f=>f.Songs).Any(s=>String.Equals(s.Source,song.Source,StringComparison.OrdinalIgnoreCase)))return false;
   if(String.IsNullOrEmpty(destination.Cover)&&sourceFolder!=null)destination.Cover=sourceFolder.Cover;
   song.Album=destination.Theme;destination.Songs.Add(song);return true;
  }
  public static DropResult Read(Project original,string[] paths,int targetIndex,Action checkCancel) {
   var result=new DropResult();var candidate=original.WithFolders(new List<BoxFolder>());
   BoxFolder target=targetIndex>=0&&targetIndex<original.Folders.Count?original.Folders[targetIndex]:null;
   var importedDirectories=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   var unique=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var loose=new List<string>();var coveredFiles=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   string firstAdded=null;
   foreach(string raw in paths) {
    checkCancel();string path;
    try{path=Path.GetFullPath(raw);}catch(Exception e){result.Warnings.Add(raw+": "+e.Message);continue;}
    if(!unique.Add(path.TrimEnd(new char[]{Path.DirectorySeparatorChar}))){result.Skipped++;continue;}
    if(Directory.Exists(path)) {
     if(importedDirectories.Contains(path.TrimEnd(new char[]{Path.DirectorySeparatorChar}))){result.Skipped++;continue;}
     List<BoxFolder> imported;
     try{imported=Importer.Read(path,result.Warnings,candidate,checkCancel);}catch(OperationCanceledException){throw;}catch(Exception e){result.Warnings.Add(path+": "+e.Message);continue;}
     foreach(var folder in imported) {
      if(!importedDirectories.Add(Path.GetFullPath(folder.SourceDirectory).TrimEnd(new char[]{Path.DirectorySeparatorChar}))){result.Skipped++;continue;}
      foreach(var song in folder.Songs){checkCancel();coveredFiles.Add(song.Source);if(AddByTags(candidate,song,folder,folder)){result.FilesAdded++;if(firstAdded==null)firstAdded=song.Source;}else result.Skipped++;}
     }
    }else if(File.Exists(path)&&Ffmpeg.Extensions.Contains(Path.GetExtension(path).ToLowerInvariant()))loose.Add(path);
    else result.Warnings.Add(Path.GetFileName(path)+": keine unterstützte Audiodatei oder kein erreichbarer Ordner. ZIP-Dateien bitte zuerst entpacken.");
   }
   foreach(string file in loose) {
    checkCancel();if(coveredFiles.Contains(file)){result.Skipped++;continue;}
    Song song;
    try{song=Ffmpeg.ReadSong(file,candidate,checkCancel);}catch(OperationCanceledException){throw;}catch(Exception e){result.Warnings.Add(Path.GetFileName(file)+": "+e.Message);continue;}
    if(AddByTags(candidate,song,target,null)){result.FilesAdded++;if(firstAdded==null)firstAdded=song.Source;}else result.Skipped++;
   }
   if(result.FilesAdded==0){result.Project=original;result.SelectedIndex=targetIndex;return result;}
   checkCancel();
   // Nur die neu importierte Sammlung sortieren und aufteilen. Bestehende
   // Ordner und Nummern sind vollständig außerhalb dieser Verarbeitung.
   foreach(var folder in candidate.Folders)folder.Name=Rules.SafeName(Rules.Theme(folder.Name));
   string targetSource=firstAdded;
   var sorted=Rules.Sorted(candidate);checkCancel();
   if(original.Folders.Count+sorted.Folders.Count>99)throw new Exception("Für diesen Import werden "+sorted.Folders.Count+" neue Ordner benötigt. Zusammen mit den vorhandenen Ordnern wären es mehr als 99. Es wurde nichts übernommen.");
   int last=-1;
   foreach(var folder in original.Folders){var m=System.Text.RegularExpressions.Regex.Match(folder.Name??"",@"^(\d{3})\s");if(m.Success)last=Math.Max(last,Int32.Parse(m.Groups[1].Value));}
   if(last+sorted.Folders.Count>999)throw new Exception("Keine fortlaufenden dreistelligen Nummern hinter den vorhandenen Ordnern verfügbar. Es wurde nichts übernommen.");
   foreach(var folder in sorted.Folders)folder.Name=(++last).ToString("D3")+" "+Rules.Theme(folder.Name);
   result.Project=original.WithFolders(original.Folders.Select(Copy).Concat(sorted.Folders).ToList());result.FoldersAdded=sorted.Folders.Count;
   result.SelectedIndex=targetSource==null?original.Folders.Count:result.Project.Folders.FindIndex(f=>f.Songs.Any(s=>s.Source==targetSource)&&result.Project.Folders.IndexOf(f)>=original.Folders.Count);
   return result;
  }
 }
}
