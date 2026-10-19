using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
namespace Musikbox {
 public static class Storage {
  public static void Save(Project p,string file) {
   string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
   try {using(var s=File.Create(temp))new DataContractJsonSerializer(typeof(Project)).WriteObject(s,p);
    if(File.Exists(file))File.Replace(temp,file,null);else File.Move(temp,file);
   } finally {if(File.Exists(temp))File.Delete(temp);}
  }
  public static Project Load(string file) {using(var s=File.OpenRead(file)){var p=(Project)new DataContractJsonSerializer(typeof(Project)).ReadObject(s);if(p==null||p.Folders==null||p.Folders.Any(f=>f==null||f.Songs==null||f.Songs.Any(x=>x==null)))throw new Exception("Ungültige Projektdatei.");return p;}}
 }
 public static class Importer {
  public static List<BoxFolder> Read(string root,List<string> warnings,Project project=null,Action checkCancel=null) {
   if(project==null)project=new Project();if(checkCancel==null)checkCancel=()=>{};
   var dirs=new List<string>{root};dirs.AddRange(Directory.GetDirectories(root,"*",SearchOption.AllDirectories));
   var result=new List<BoxFolder>();
   foreach(var dir in dirs.OrderBy(x=>x,StringComparer.OrdinalIgnoreCase)) {
    var paths=Directory.GetFiles(dir).Where(p=>Ffmpeg.Extensions.Contains(Path.GetExtension(p).ToLowerInvariant())).OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).ToList();
    if(paths.Count==0)continue;
    var f=new BoxFolder{Name=Path.GetFileName(dir),Theme=Rules.Theme(Path.GetFileName(dir)),SourceDirectory=dir,Group=""};
    foreach(var path in paths) {
     try {checkCancel();f.Songs.Add(Ffmpeg.ReadSong(path,project,checkCancel));}
     catch(OperationCanceledException){throw;}
     catch(Exception e){warnings.Add(path+": "+e.Message);}
    }
    var pics=Directory.GetFiles(dir).Where(x=>new[]{".jpg",".jpeg",".png"}.Contains(Path.GetExtension(x).ToLowerInvariant())).ToList();
    f.Cover=pics.FirstOrDefault(x=>Path.GetFileName(x).Equals("Folder.jpg",StringComparison.OrdinalIgnoreCase))??pics.FirstOrDefault();
    result.Add(f);
   }
   return result;
  }
 }
 public static class Exporter {
  public static string Export(Project project,string parent,Action checkCancel,Action<int,string> progress) {
   Gain.Validate(project);Ffmpeg.Validate(project,project.Folders.SelectMany(f=>f.Songs).Any(song=>!Ffmpeg.IsMp3(song.Source)));
   Project p=Rules.Sorted(project);var errors=Rules.Validate(p);if(errors.Count>0)throw new Exception(String.Join("\r\n",errors.Take(30)));
   string full=Path.GetFullPath(parent).TrimEnd(new char[]{Path.DirectorySeparatorChar})+Path.DirectorySeparatorChar;
   foreach(var s in p.Folders.SelectMany(f=>f.Songs)) {
    string dir=Path.GetDirectoryName(Path.GetFullPath(s.Source)).TrimEnd(new char[]{Path.DirectorySeparatorChar})+Path.DirectorySeparatorChar;
    if(full.StartsWith(dir,StringComparison.OrdinalIgnoreCase))throw new Exception("Das Exportziel darf nicht innerhalb eines Musik-Quellordners liegen.");
   }
   string id=DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6);
   string stage=Path.Combine(parent,".Musikbox-unvollstaendig-"+id),dest=Path.Combine(parent,"Musikbox-Export-"+id);
   Directory.CreateDirectory(stage);int done=0,total=p.Folders.Sum(f=>f.Songs.Count);
   var report=new StringBuilder("Ordner;Position;Interpret;Titel;Album;Dateiname\r\n");var gainLog=new StringBuilder();
   try {
    foreach(var f in p.Folders) {
     checkCancel();string outdir=Path.Combine(stage,f.Name);Directory.CreateDirectory(outdir);
     if(!String.IsNullOrEmpty(f.SourceDirectory)&&Directory.Exists(f.SourceDirectory))foreach(var pic in Directory.GetFiles(f.SourceDirectory).Where(x=>new[]{".jpg",".jpeg",".png"}.Contains(Path.GetExtension(x).ToLowerInvariant())))File.Copy(pic,Path.Combine(outdir,Path.GetFileName(pic)),false);
     if(!String.IsNullOrEmpty(f.Cover)) {
      // Nur echte JPEG-Dateien werden unter dem kompatiblen Namen Folder.jpg kopiert.
      using(var c=File.OpenRead(f.Cover)){if(c.ReadByte()==255&&c.ReadByte()==216)File.Copy(f.Cover,Path.Combine(outdir,"Folder.jpg"),true);else File.Copy(f.Cover,Path.Combine(outdir,"Cover"+Path.GetExtension(f.Cover)),true);}
     }
     int i=0;
     foreach(var song in f.Songs) {
      checkCancel();i++;string name=i.ToString("D2")+" - "+Rules.SafeName(song.Artist+" - "+song.Title)+".mp3";
      string path=Path.Combine(outdir,name);if(path.Length>=250)throw new Exception("Exportpfad zu lang. Bitte kürzeren Zielpfad oder Ordnernamen wählen.");
      if(Ffmpeg.NeedsEncoding(p,song)){progress(done*100/Math.Max(1,total),"FFmpeg: "+name);gainLog.AppendLine(f.Name+" / "+name);gainLog.AppendLine(Ffmpeg.Write(song,f.Theme,path,p,checkCancel));gainLog.AppendLine();}
      else Tags.WriteCopy(song,f.Theme,path,checkCancel);
      if(p.Normalize){progress(done*100/Math.Max(1,total),"MP3Gain: "+name);gainLog.AppendLine(f.Name+" / "+name);gainLog.AppendLine(Gain.Apply(path,p,checkCancel));gainLog.AppendLine();}
      var verify=Tags.Read(path);
      if(verify.Artist!=song.Artist||verify.Title!=song.Title||verify.Album!=f.Theme)throw new Exception("Tag-Prüfung fehlgeschlagen: "+name);
      report.AppendLine(String.Join(";",new[]{f.Name,i.ToString(),song.Artist,song.Title,f.Theme,name}.Select(x=>"\""+x.Replace("\"","\"\"")+"\"")));
      progress(++done*100/Math.Max(1,total),name);
     }
    }
    checkCancel();Directory.Move(stage,dest);
    // Bericht außerhalb der Musikordner, damit die Musikbox nur Musik und Bilder erhält.
    File.WriteAllText(dest+"-Protokoll.csv",report.ToString(),new UTF8Encoding(true));
    if(gainLog.Length>0)File.WriteAllText(dest+"-Audioverarbeitung.txt",(p.Normalize?"MP3Gain-Zielpegel: "+p.TargetDb+" dB":p.FfmpegLoudness?"FFmpeg-Zielwert: "+p.TargetLufs+" LUFS, True-Peak-Ziel -2 dBTP":"Formatkonvertierung ohne Lautheitsanpassung")+"\r\n\r\n"+gainLog.ToString(),new UTF8Encoding(true));
    return dest;
   } catch {try{if(Directory.Exists(stage))Directory.Delete(stage,true);}catch{}throw;}
  }
 }
}
