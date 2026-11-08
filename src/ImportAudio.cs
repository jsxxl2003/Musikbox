using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
namespace Musikbox {
 public static class ImportAudio {
  public static string CacheRoot {get{return Path.Combine(PortablePaths.Data,"Arbeitskopien");}}
  public static bool Required(Project p,Song s){return p.Normalize||Ffmpeg.NeedsEncoding(p,s);}
  public static string Profile(Project p,Song s){
   var f=new FileInfo(s.Source);if(!f.Exists)return "";
   string value=String.Join("|",new[]{"1",Path.GetFullPath(s.Source),p.Normalize.ToString(),p.FfmpegLoudness.ToString(),p.ReencodeMp3.ToString(),p.TargetDb.ToString("R",CultureInfo.InvariantCulture),p.TargetLufs.ToString("R",CultureInfo.InvariantCulture),p.Mp3Bitrate.ToString(),p.VariableBitrate.ToString(),p.VbrQuality.ToString(),p.GainExecutable??"",p.FfmpegExecutable??"",f.Length.ToString(),f.LastWriteTimeUtc.Ticks.ToString()});
   using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","");
  }
  public static bool Ready(Project p,Song s){return Required(p,s)&&!String.IsNullOrEmpty(s.PreparedSource)&&File.Exists(s.PreparedSource)&&s.PreparedProfile==Profile(p,s);}
  public static void ValidatePending(Project p,IEnumerable<Song> songs){var pending=songs.Where(s=>!Ready(p,s)).ToList();if(pending.Count==0)return;Gain.Validate(p);Ffmpeg.Validate(p,pending.Any(s=>!Ffmpeg.IsMp3(s.Source)));}
  public static void Prepare(Project p,IEnumerable<Song> input,Action checkCancel,Action<int,int,string> progress,string cacheRoot=null){
   var songs=input.Where(s=>Required(p,s)&&!Ready(p,s)).ToList();if(songs.Count==0)return;
   ValidatePending(p,songs);checkCancel();string root=cacheRoot??CacheRoot;Directory.CreateDirectory(root);
   string id=Guid.NewGuid().ToString("N"),stage=Path.Combine(root,".unvollstaendig-"+id),final=Path.Combine(root,id);Directory.CreateDirectory(stage);
   var records=new List<Tuple<Song,string,string>>();
   try{
    for(int i=0;i<songs.Count;i++){
     checkCancel();var s=songs[i];string fingerprint=Profile(p,s);string name=i.ToString("D5")+".mp3",target=Path.Combine(stage,name);
     string tool=p.FfmpegLoudness?"FFmpeg-Lautheit":p.Normalize?"MP3Gain":"FFmpeg-Konvertierung";
     progress(i,songs.Count,tool+": "+s.Artist+" – "+s.Title);
     try{
      if(Ffmpeg.NeedsEncoding(p,s))Ffmpeg.Write(s,s.Album??"",target,p,checkCancel);else Tags.WriteCopy(s,s.Album??"",target,checkCancel);
      if(p.Normalize)Gain.Apply(target,p,checkCancel);
      var verify=Tags.Read(target);if(verify.Artist!=s.Artist||verify.Title!=s.Title||verify.Album!=(s.Album??""))throw new Exception("Tags der Arbeitskopie stimmen nicht überein.");
      if(fingerprint!=Profile(p,s))throw new Exception("Quelldatei wurde während der Verarbeitung verändert.");
      records.Add(Tuple.Create(s,name,fingerprint));progress(i+1,songs.Count,tool+": fertig");
     }catch(OperationCanceledException){throw;}catch(Exception e){throw new Exception("Automatische Audioverarbeitung fehlgeschlagen bei "+Path.GetFileName(s.Source)+". Es wurde nichts übernommen.\r\n"+e.Message,e);}
    }
    checkCancel();Directory.Move(stage,final);
    foreach(var record in records){record.Item1.PreparedSource=Path.Combine(final,record.Item2);record.Item1.PreparedProfile=record.Item3;}
   }catch{try{if(Directory.Exists(stage))Directory.Delete(stage,true);}catch{}throw;}
  }
 }
}
