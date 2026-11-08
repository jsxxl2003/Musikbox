using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace Musikbox {
 public enum FolderImportMode { PreserveStructure, ByAlbumTags }
 public class DropImportException:Exception {
  public readonly string Report;
  public DropImportException(string message,string report):base(message){Report=message+"\r\n\r\n"+report;}
 }
 public class DropResult {
  public Project Project;
  public string Report;
  public int FilesAdded,FoldersAdded,Skipped;
  public int SelectedIndex;
  public List<string> Warnings=new List<string>();
 }
 public static class DropImport {
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
  static string Describe(Project original,Project candidate,string[] paths,FolderImportMode mode,DropResult result) {
   var b=new System.Text.StringBuilder("Importbericht · Studio "+AppIdentity.Version+"\r\n");
   b.AppendLine("Modus: "+(mode==FolderImportMode.PreserveStructure?"Ordnerstruktur übernehmen":"Nach Album-Tags neu ordnen"));
   b.AppendLine("Ausgewählte Einträge: "+paths.Length);
   b.AppendLine("Bisherige Bibliothek bleibt erhalten: "+original.Folders.Count+" Ordner");
   b.AppendLine("Neu eingelesen: "+candidate.Folders.Count+" Ordner / "+result.FilesAdded+" Titel");
   int needed=ImportPacking.Prepare(candidate,()=>{}).Folders.Count;
   b.AppendLine("Benötigte neue Ordner nach themengetrenntem Auffüllen: "+needed);
   b.AppendLine("Importbestand: "+needed+" von maximal 99 Ordnern");
   b.AppendLine("Doppelte/überlappende Auswahl übersprungen: "+result.Skipped);
   b.AppendLine("Hinweise beim Lesen: "+result.Warnings.Count);
   b.AppendLine();b.AppendLine("Titel | Einzelbedarf vor Zusammenfassung | Quellordner / Thema");
   foreach(var f in candidate.Folders){
    b.AppendLine(f.Songs.Count+" | "+Math.Max(1,(f.Songs.Count+98)/99)+" | "+f.Name);
    b.AppendLine("  Quelle: "+(f.SourceDirectory??"Einzeldateien nach Album-Tags"));
    b.AppendLine("  Formate: "+String.Join(", ",f.Songs.GroupBy(x=>Path.GetExtension(x.Source).ToLowerInvariant()).Select(g=>g.Key+": "+g.Count())));
   }
   if(result.Warnings.Count>0){b.AppendLine();b.AppendLine("LESEHINWEISE");foreach(string w in result.Warnings)b.AppendLine(w);}
   return b.ToString();
  }
  public static DropResult Read(Project original,string[] paths,int targetIndex,Action checkCancel,FolderImportMode mode=FolderImportMode.PreserveStructure,Action<int,int> progress=null,bool recursive=true,Action<Song> songRead=null) {
   var result=new DropResult();var candidate=original.WithFolders(new List<BoxFolder>());
   // A new batch never inherits an old folder or its numbering.
   BoxFolder target=null;
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
     try{imported=Importer.Read(path,result.Warnings,candidate,checkCancel,progress==null?(Action<int,int>)null:(folders,songs)=>progress(importedDirectories.Count+folders,result.FilesAdded+songs),recursive,songRead);}catch(OperationCanceledException){throw;}catch(Exception e){result.Warnings.Add(path+": "+e.Message);continue;}
     foreach(var folder in imported) {
      if(!importedDirectories.Add(Path.GetFullPath(folder.SourceDirectory).TrimEnd(new char[]{Path.DirectorySeparatorChar}))){result.Skipped++;continue;}
      if(mode==FolderImportMode.PreserveStructure) {
       if(folder.Songs.Count==0)continue;
       folder.Group="";candidate.Folders.Add(folder);
       foreach(var song in folder.Songs){checkCancel();coveredFiles.Add(song.Source);result.FilesAdded++;if(firstAdded==null)firstAdded=song.Source;}
      }else foreach(var song in folder.Songs){checkCancel();coveredFiles.Add(song.Source);if(AddByTags(candidate,song,folder,folder)){result.FilesAdded++;if(firstAdded==null)firstAdded=song.Source;}else result.Skipped++;}
     }
    }else if(File.Exists(path)&&Ffmpeg.Extensions.Contains(Path.GetExtension(path).ToLowerInvariant()))loose.Add(path);
    else result.Warnings.Add(Path.GetFileName(path)+": keine unterstützte Audiodatei oder kein erreichbarer Ordner. ZIP-Dateien bitte zuerst entpacken.");
   }
   var looseCandidate=mode==FolderImportMode.PreserveStructure?original.WithFolders(new List<BoxFolder>()):candidate;
   foreach(string file in loose) {
    checkCancel();if(coveredFiles.Contains(file)){result.Skipped++;continue;}
    Song song;
    try{song=Ffmpeg.ReadSong(file,candidate,checkCancel);}catch(OperationCanceledException){throw;}catch(Exception e){result.Warnings.Add(Path.GetFileName(file)+": "+e.Message);continue;}
    if(AddByTags(looseCandidate,song,target,null)){if(songRead!=null)songRead(song);result.FilesAdded++;if(firstAdded==null)firstAdded=song.Source;}else result.Skipped++;
   }
   if(mode==FolderImportMode.PreserveStructure)candidate.Folders.AddRange(looseCandidate.Folders);
   if(progress!=null)progress(candidate.Folders.Count,result.FilesAdded);
   result.Report=Describe(original,candidate,paths,mode,result);
   if(result.FilesAdded==0){result.Project=original;result.SelectedIndex=targetIndex;return result;}
   checkCancel();
   // Build the replacement in isolation; the current collection is untouched
   // until the entire batch (including audio preparation) succeeds.
   if(mode==FolderImportMode.ByAlbumTags)foreach(var folder in candidate.Folders)folder.Name=Rules.SafeName(Rules.Theme(folder.Name));
   else candidate.Folders=candidate.Folders.OrderBy(f=>f.Name,StringComparer.OrdinalIgnoreCase).ToList();
   string targetSource=firstAdded;
   Project sorted;
   try{sorted=Rules.Sorted(ImportPacking.Prepare(candidate,checkCancel));}catch(OperationCanceledException){throw;}catch(Exception e){throw new DropImportException(e.Message,result.Report);}
   checkCancel();
   if(sorted.Folders.Count>99)throw new DropImportException("Für diesen Import werden mehr als 99 Ordner benötigt. Es wurde nichts übernommen.",result.Report);
   result.Project=sorted;result.FoldersAdded=sorted.Folders.Count;
   result.SelectedIndex=targetSource==null?0:sorted.Folders.FindIndex(f=>f.Songs.Any(song=>song.Source==targetSource));
   return result;
  }
 }
}
