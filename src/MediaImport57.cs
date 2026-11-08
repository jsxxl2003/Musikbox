using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Globalization;using System.Threading;using System.Threading.Tasks;using System.Windows.Forms;
namespace Musikbox {
 public static partial class MediaProbe57 {
  [System.Runtime.Serialization.DataContract]public class Info{[System.Runtime.Serialization.DataMember(Name="format")]public Format Format;[System.Runtime.Serialization.DataMember(Name="streams")]public Stream[] Streams;}
  [System.Runtime.Serialization.DataContract]public class Format{[System.Runtime.Serialization.DataMember(Name="duration")]public string Duration;[System.Runtime.Serialization.DataMember(Name="tags")]public Dictionary<string,string> Tags;}
  [System.Runtime.Serialization.DataContract]public class Stream{[System.Runtime.Serialization.DataMember(Name="codec_type")]public string Type;}
  public static readonly string[] Extensions={".mp4",".mkv",".mov",".avi",".webm",".mpg",".mpeg",".ts",".m2ts"};
  public static Song Read(string path,string ffmpeg,Action check){string probe=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(ffmpeg)),"ffprobe"+Path.GetExtension(ffmpeg)),error;string json=Ffmpeg.Run(probe,"-v error -show_entries format=duration:format_tags:stream=codec_type -of json "+Ffmpeg.Quote(Path.GetFullPath(path)),check,out error);Info info;using(var stream=new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))info=(Info)new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Info),new System.Runtime.Serialization.Json.DataContractJsonSerializerSettings{UseSimpleDictionaryFormat=true}).ReadObject(stream);if(info.Streams==null||!info.Streams.Any(s=>s.Type=="audio"))throw new Exception("Diese Mediendatei enthält keine Audiospur: "+Path.GetFileName(path));var tags=info.Format==null?null:info.Format.Tags;Func<string,string> tag=key=>tags==null?"":tags.Where(kv=>kv.Key.Equals(key,StringComparison.OrdinalIgnoreCase)).Select(kv=>kv.Value).FirstOrDefault()??"";double duration;Double.TryParse(info.Format==null?"":info.Format.Duration,NumberStyles.Float,CultureInfo.InvariantCulture,out duration);return new Song{Source=Path.GetFullPath(path),Title=String.IsNullOrEmpty(tag("title"))?Path.GetFileNameWithoutExtension(path):tag("title"),Artist=tag("artist"),Album=tag("album"),Genre=tag("genre"),Comment=tag("comment"),Year=tag("date"),Duration=duration};}
 }
 public partial class Controller {
  void ImportMediaFiles(string[] paths){ImportDropped(paths,-1,false,false);}
 }
}
