using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
namespace Musikbox {
 public static class SpeechPhrases {
  [DataContract] class Result {
   [DataMember(Name="text")] public string Text {get;set;}
   [DataMember(Name="partial")] public string Partial {get;set;}
   [DataMember(Name="alternatives")] public Alternative[] Alternatives {get;set;}
  }
  [DataContract] class Alternative {[DataMember(Name="text")] public string Text {get;set;}}
  static Result Read(string json){using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json)))return (Result)new DataContractJsonSerializer(typeof(Result)).ReadObject(stream);}
  public static string[] Parse(string json){var r=Read(json);return (r.Alternatives??new Alternative[0]).Select(a=>a.Text).Concat(new[]{r.Text}).Where(t=>!String.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();}
  public static string Partial(string json){return Read(json).Partial??"";}
  public static double Peak16(byte[] pcm,int count){int peak=0;for(int i=0;i+1<Math.Min(count,pcm.Length);i+=2){int sample=(short)(pcm[i]|pcm[i+1]<<8);peak=Math.Max(peak,Math.Abs(sample));}return peak/32768d;}
  public static string[] Grammar(IEnumerable<Song> songs,bool english){return songs.SelectMany(s=>new[]{s.Artist,s.Title,s.Title+" "+(english?"by":"von")+" "+s.Artist,(english?"play ":"spiele ")+s.Title+" "+s.Artist}).Where(s=>!String.IsNullOrWhiteSpace(s)&&s.Length<180).Distinct(StringComparer.OrdinalIgnoreCase).Take(2500).ToArray();}
 }
}
