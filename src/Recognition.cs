using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Threading;
namespace Musikbox {
 [DataContract] public class RecognitionSettings {
  [DataMember] public string Host="identify-eu-west-1.acrcloud.com",Key,Secret;
  static string PathName {get{return Path.Combine(PortablePaths.Data,"Erkennung.dat");}}
  public static RecognitionSettings Load(){if(!File.Exists(PathName))return new RecognitionSettings();byte[] bytes=ProtectedData.Unprotect(File.ReadAllBytes(PathName),null,DataProtectionScope.CurrentUser);return CoverSearch.Json<RecognitionSettings>(bytes);}
  public void Validate(){if(!System.Text.RegularExpressions.Regex.IsMatch(Host??"",@"^identify-[a-z0-9-]+\.acrcloud\.com$"))throw new Exception("Bitte den ACRCloud-Identify-Host deines Projekts eingeben.");if(String.IsNullOrWhiteSpace(Key)||String.IsNullOrWhiteSpace(Secret))throw new Exception("Access Key und Access Secret des eigenen ACRCloud-Projekts fehlen.");}
  public void Save(){Validate();using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(RecognitionSettings)).WriteObject(stream,this);byte[] encrypted=ProtectedData.Protect(stream.ToArray(),null,DataProtectionScope.CurrentUser);Directory.CreateDirectory(Path.GetDirectoryName(PathName));string temp=PathName+".tmp";File.WriteAllBytes(temp,encrypted);if(File.Exists(PathName))File.Replace(temp,PathName,null);else File.Move(temp,PathName);}}
 }
 public class RecognizedSong {public string Artist,Title,Album;public int Score;}
 public static class Recognition {
  [DataContract] public class Response {[DataMember(Name="status")]public Status Status;[DataMember(Name="metadata")]public Metadata Metadata;}
  [DataContract] public class Status {[DataMember(Name="code")]public int Code;[DataMember(Name="msg")]public string Message;}
  [DataContract] public class Metadata {[DataMember(Name="music")]public Music[] Music;}
  [DataContract] public class Music {[DataMember(Name="title")]public string Title;[DataMember(Name="artists")]public Name[] Artists;[DataMember(Name="album")]public Name Album;[DataMember(Name="score")]public int Score;}
  [DataContract] public class Name {[DataMember(Name="name")]public string Value;}
  public static string Signature(string key,string secret,string timestamp){string text="POST\n/v1/identify\n"+key+"\naudio\n1\n"+timestamp;using(var hmac=new HMACSHA1(Encoding.UTF8.GetBytes(secret)))return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(text)));}
  public static RecognizedSong Parse(byte[] data){var d=CoverSearch.Json<Response>(data);if(d.Status==null||d.Status.Code!=0)throw new Exception("Kein Erkennungstreffer: "+(d.Status==null?"Ungültige Antwort":d.Status.Message));var match=(d.Metadata==null?null:d.Metadata.Music??new Music[0]);if(match==null||match.Length==0)throw new Exception("Kein Musiktitel erkannt.");var m=match[0];return new RecognizedSong{Title=m.Title,Artist=String.Join(" / ",(m.Artists??new Name[0]).Select(x=>x.Value)),Album=m.Album==null?"":m.Album.Value,Score=m.Score};}
  public static RecognizedSong Identify(Song song,Project p,RecognitionSettings settings,CancellationToken token){
   settings.Validate();Ffmpeg.Validate(p,true);string temp=Path.Combine(Path.GetTempPath(),"MusikboxProbe-"+Guid.NewGuid().ToString("N")+".wav");
   try{string err;Ffmpeg.Run(p.FfmpegExecutable,"-hide_banner -v error -nostdin -y -i "+Ffmpeg.Quote(song.Source)+" -t 12 -map 0:a:0 -vn -ac 1 -ar 16000 -c:a pcm_s16le "+Ffmpeg.Quote(temp),()=>token.ThrowIfCancellationRequested(),out err);
    byte[] sample=File.ReadAllBytes(temp);string timestamp=((long)(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds).ToString(),boundary="musikbox"+Guid.NewGuid().ToString("N");byte[] body;
    using(var stream=new MemoryStream()){Action<string> write=value=>{byte[] bytes=Encoding.UTF8.GetBytes(value);stream.Write(bytes,0,bytes.Length);};var fields=new Dictionary<string,string>{{"access_key",settings.Key},{"sample_bytes",sample.Length.ToString()},{"timestamp",timestamp},{"signature",Signature(settings.Key,settings.Secret,timestamp)},{"data_type","audio"},{"signature_version","1"}};
     foreach(var pair in fields)write("--"+boundary+"\r\nContent-Disposition: form-data; name=\""+pair.Key+"\"\r\n\r\n"+pair.Value+"\r\n");write("--"+boundary+"\r\nContent-Disposition: form-data; name=\"sample\"; filename=\"probe.wav\"\r\nContent-Type: audio/wav\r\n\r\n");stream.Write(sample,0,sample.Length);write("\r\n--"+boundary+"--\r\n");body=stream.ToArray();}
    ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;var request=(HttpWebRequest)WebRequest.Create("https://"+settings.Host+"/v1/identify");request.AllowAutoRedirect=false;request.Method="POST";request.ContentType="multipart/form-data; boundary="+boundary;request.ContentLength=body.Length;request.Timeout=25000;request.ReadWriteTimeout=25000;
    using(token.Register(()=>request.Abort()))try{using(var output=request.GetRequestStream())output.Write(body,0,body.Length);using(var response=request.GetResponse())using(var input=response.GetResponseStream())using(var bytes=new MemoryStream()){var buffer=new byte[8192];int n;while((n=input.Read(buffer,0,buffer.Length))>0){token.ThrowIfCancellationRequested();if(bytes.Length+n>1024*1024)throw new Exception("Erkennungsantwort zu groß.");bytes.Write(buffer,0,n);}return Parse(bytes.ToArray());}}catch(WebException){token.ThrowIfCancellationRequested();throw new Exception("Musikerkennung nicht erreichbar. Zugangsdaten, Guthaben und Internetverbindung prüfen.");}
   }finally{if(File.Exists(temp))File.Delete(temp);}
  }
 }
}
