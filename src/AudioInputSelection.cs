using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace Musikbox {
 // A tool selection is independent of the numbered jukebox collection.
 public sealed class AudioInputSelection {
  readonly bool mp3Only;
  public AudioInputSelection(bool mp3Only=false){this.mp3Only=mp3Only;}
  readonly List<Song> songs=new List<Song>();
  public List<Song> Songs {get{return songs.ToList();}}
  public void Clear(){songs.Clear();}
  public void Add(IEnumerable<string> paths,Action check){
   var pending=new List<Song>();var seen=new HashSet<string>(songs.Select(s=>Path.GetFullPath(s.Source)),StringComparer.OrdinalIgnoreCase);
   foreach(string path in paths)foreach(string file in Files(Path.GetFullPath(path),check)){
    check();if(!seen.Add(file))continue;
    if(mp3Only?!Ffmpeg.IsMp3(file):!Ffmpeg.Extensions.Contains(Path.GetExtension(file).ToLowerInvariant()))throw new ArgumentException((mp3Only?"MP3Gain benötigt MP3-Dateien: ":"Nicht unterstützte Audiodatei: ")+Path.GetFileName(file));
    pending.Add(MediaMetadata.Read(file));
   }
   check();if(pending.Count==0&&songs.Count==0)throw new ArgumentException(mp3Only?"Keine MP3-Dateien gefunden.":"Keine unterstützten Audiodateien gefunden.");
   songs.AddRange(pending);
  }
  IEnumerable<string> Files(string path,Action check){
   check();if(File.Exists(path)){yield return path;yield break;}
   if(!Directory.Exists(path))throw new FileNotFoundException("Datei oder Ordner nicht erreichbar.",path);
   foreach(string f in Directory.GetFiles(path).Where(f=>mp3Only?Ffmpeg.IsMp3(f):Ffmpeg.Extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))){check();yield return Path.GetFullPath(f);}
   foreach(string d in Directory.GetDirectories(path))if((File.GetAttributes(d)&FileAttributes.ReparsePoint)==0)foreach(string f in Files(d,check))yield return f;
  }
 }
}
