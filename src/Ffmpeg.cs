using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
namespace Musikbox {
 public static class Ffmpeg {
  public static readonly string[] Extensions={".mp3",".flac",".wav",".m4a",".aac",".ogg",".opus",".wma",".aif",".aiff",".mp4",".mkv",".mov",".avi",".webm",".mpg",".mpeg",".ts",".m2ts"};
  public static bool IsMp3(string path){return Path.GetExtension(path).Equals(".mp3",StringComparison.OrdinalIgnoreCase);}
  public static bool NeedsEncoding(Project p,Song song){return !IsMp3(song.Source)||p.FfmpegLoudness||p.ReencodeMp3;}
  public static readonly int[] Bitrates={128,160,192,224,256,320};
  public static string EncodingArguments(Project p){return p.VariableBitrate?"-q:a "+p.VbrQuality:"-b:a "+p.Mp3Bitrate+"k";}
  public static string EncodingLabel(Project p){return p.VariableBitrate?"Variable Bitrate (VBR), Qualität V"+p.VbrQuality:p.Mp3Bitrate+" kbit/s, feste Bitrate (CBR)";}
  public static string ProbePath(Project p){return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(p.FfmpegExecutable)),"ffprobe"+Path.GetExtension(p.FfmpegExecutable));}
  public static void Validate(Project p,bool required) {
   if(p.Normalize&&p.FfmpegLoudness)throw new Exception("Bitte nur ein Lautstärkeverfahren wählen: MP3Gain oder FFmpeg.");
   if(!required&&!p.FfmpegLoudness&&!p.ReencodeMp3)return;
   if(!Bitrates.Contains(p.Mp3Bitrate)||p.VbrQuality<0||p.VbrQuality>5)throw new Exception("Ungültige MP3-Bitrate oder VBR-Qualitätsstufe.");
   if(String.IsNullOrWhiteSpace(p.FfmpegExecutable)||!File.Exists(p.FfmpegExecutable))throw new Exception("Bitte über 'Audio-Einstellungen' ffmpeg.exe auswählen.");
   if(!File.Exists(ProbePath(p)))throw new Exception("ffprobe.exe muss im selben Ordner wie ffmpeg.exe liegen.");
   if(p.FfmpegLoudness&&(Double.IsNaN(p.TargetLufs)||Double.IsInfinity(p.TargetLufs)||p.TargetLufs < -23||p.TargetLufs > -10))throw new Exception("Der FFmpeg-Zielwert muss zwischen -23 und -10 LUFS liegen.");
  }
  public static string Quote(string value) {
   // Windows CreateProcess-Quoting; auch eingebettete Anführungszeichen und
   // abschließende Backslashes werden korrekt behandelt. Keine Shell beteiligt.
   var b=new StringBuilder("\"");int slash=0;
   foreach(char c in value){if(c=='\\'){slash++;continue;}if(c=='"'){b.Append('\\',slash*2+1);b.Append(c);}else{b.Append('\\',slash);b.Append(c);}slash=0;}
   b.Append('\\',slash*2);return b.Append('"').ToString();
  }
  public static string Run(string exe,string args,Action checkCancel,out string stderr) {
   checkCancel();var start=new ProcessStartInfo{FileName=Path.GetFullPath(exe),Arguments=args,WorkingDirectory=Path.GetDirectoryName(Path.GetFullPath(exe)),UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,RedirectStandardInput=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};
   using(var process=new Process{StartInfo=start}) {
    process.Start();process.StandardInput.Close();var stdout=process.StandardOutput.ReadToEndAsync();var err=process.StandardError.ReadToEndAsync();var clock=Stopwatch.StartNew();
    try{while(!process.WaitForExit(100)){checkCancel();if(clock.Elapsed.TotalMinutes>20)throw new Exception("FFmpeg-Zeitlimit pro Verarbeitungsschritt überschritten.");}checkCancel();}
    catch{try{if(!process.HasExited){process.Kill();process.WaitForExit();}}catch{}throw;}
    Task.WaitAll(stdout,err);stderr=err.Result;
    if(process.ExitCode!=0)throw new Exception("Audioverarbeitung fehlgeschlagen:\r\n"+(stderr.Length>1800?stderr.Substring(stderr.Length-1800):stderr));
    return stdout.Result;
   }
  }
  [DataContract] public class ProbeDocument {
   [DataMember(Name="format")]public ProbeFormat Format;
   [DataMember(Name="streams")]public ProbeStream[] Streams;
  }
  [DataContract] public class ProbeFormat {[DataMember(Name="tags")]public Dictionary<string,string> Tags;}
  [DataContract] public class ProbeStream {[DataMember(Name="codec_type")]public string Type;[DataMember(Name="tags")]public Dictionary<string,string> Tags;}
  static T Json<T>(string json){using(var s=new MemoryStream(Encoding.UTF8.GetBytes(json)))return (T)new DataContractJsonSerializer(typeof(T),new DataContractJsonSerializerSettings{UseSimpleDictionaryFormat=true}).ReadObject(s);}
  static string Tag(Dictionary<string,string> tags,string key){if(tags==null)return "";foreach(var pair in tags)if(pair.Key.Equals(key,StringComparison.OrdinalIgnoreCase))return pair.Value??"";return "";}
  public static Song ReadSong(string path,Project p,Action checkCancel) {
   checkCancel();if(MediaProbe57.Extensions.Contains(Path.GetExtension(path).ToLowerInvariant()))return MediaProbe57.Read(path,p.FfmpegExecutable,checkCancel);var song=MediaMetadata.Read(path);if(new[]{".wav",".aif",".aiff",".wma"}.Contains(Path.GetExtension(path).ToLowerInvariant())&&!String.IsNullOrEmpty(p.FfmpegExecutable)){string folder=Path.GetDirectoryName(Path.GetFullPath(p.FfmpegExecutable)),probe=Path.Combine(folder,"ffprobe.exe");if(!File.Exists(probe))probe=Path.Combine(folder,"ffprobe");if(File.Exists(probe))try{string error;var doc=Json<ProbeDocument>(Run(probe,"-v error -show_entries format_tags:stream_tags -of json "+Quote(Path.GetFullPath(path)),checkCancel,out error));var tags=doc.Format==null?null:doc.Format.Tags;Action<string,Action<string>> use=(key,set)=>{string value=Tag(tags,key);if(value.Length==0)foreach(var stream in doc.Streams??new ProbeStream[0]){value=Tag(stream.Tags,key);if(value.Length>0)break;}if(value.Length>0)set(value);};use("artist",value=>song.Artist=value);use("title",value=>song.Title=value);use("album",value=>song.Album=value);use("genre",value=>song.Genre=value);use("date",value=>song.Year=value.Length>=4?value.Substring(0,4):value);use("comment",value=>song.Comment=value);}catch(OperationCanceledException){throw;}catch{} }return song;
  }

  static string Number(double n){return n.ToString("0.00",CultureInfo.InvariantCulture);}
  public static Dictionary<string,string> Measurement(string log) {
   int start=log.LastIndexOf('{'),end=log.LastIndexOf('}');if(start<0||end<=start)throw new Exception("FFmpeg hat keine Lautheitsmessung geliefert.");
   var result=Json<Dictionary<string,string>>(log.Substring(start,end-start+1));
   foreach(string key in new[]{"input_i","input_tp","input_lra","input_thresh","target_offset"}){double value;if(!result.ContainsKey(key)||!Double.TryParse(result[key],NumberStyles.Float,CultureInfo.InvariantCulture,out value)||Double.IsNaN(value)||Double.IsInfinity(value))throw new Exception("Lautheit nicht messbar (möglicherweise stumm oder zu kurz).");}
   return result;
  }
  public static string Write(Song song,string album,string target,Project p,Action checkCancel) {
   Validate(p,true);checkCancel();string work=Path.Combine(Path.GetTempPath(),"MusikboxFFmpeg-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(work);
   try {
    string err,filter="",log="",input=Quote(Path.GetFullPath(song.Source));
    if(p.FfmpegLoudness) {
     string first="aformat=sample_rates=44100:channel_layouts=stereo,loudnorm=I="+Number(p.TargetLufs)+":TP=-2.0:LRA=11:print_format=json";
     Run(p.FfmpegExecutable,"-hide_banner -nostdin -nostats -i "+input+" -map 0:a:0 -af "+Quote(first)+" -f null -",checkCancel,out err);
     var m=Measurement(err);log="Messung vor Verarbeitung:\r\n"+err.Substring(err.LastIndexOf('{'));
     filter="aformat=sample_rates=44100:channel_layouts=stereo,loudnorm=I="+Number(p.TargetLufs)+":TP=-2.0:LRA=11:measured_I="+m["input_i"]+":measured_TP="+m["input_tp"]+":measured_LRA="+m["input_lra"]+":measured_thresh="+m["input_thresh"]+":offset="+m["target_offset"]+":linear=true:print_format=json";
    }
    string converted=Path.Combine(work,"converted.mp3");
    Run(p.FfmpegExecutable,"-hide_banner -nostdin -nostats -y -i "+input+" -map 0:a:0 -vn -map_metadata -1 "+(filter.Length==0?"":"-af "+Quote(filter)+" ")+"-c:a libmp3lame "+EncodingArguments(p)+" -ar 44100 -ac 2 -id3v2_version 3 "+Quote(converted),checkCancel,out err);
    if(p.FfmpegLoudness)log+="\r\n\r\nVerarbeitung:\r\n"+err.Substring(err.LastIndexOf('{'));
    Song output=song.Copy();output.Source=converted;
    var frames=IsMp3(song.Source)?Tags.Read(song.Source).Other:null;
    Tags.WriteCopy(output,album,target,checkCancel,frames);MediaMetadata.PreserveArtwork(song.Source,target,song);
    return "MP3: "+EncodingLabel(p)+", 44,1 kHz, Stereo.\r\n"+log;
   }finally{foreach(string file in Directory.GetFiles(work))File.Delete(file);Directory.Delete(work);}
  }
 }
}
