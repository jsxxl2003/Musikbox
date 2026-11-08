using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
namespace Musikbox {
 [DataContract] public class AcoustIdSettings {
  [DataMember] public string Executable,ClientKey;
  static string PathName {get{return Path.Combine(PortablePaths.Data,"AcoustID.dat");}}
  public static AcoustIdSettings Load(){var settings=File.Exists(PathName)?CoverSearch.Json<AcoustIdSettings>(ProtectedData.Unprotect(File.ReadAllBytes(PathName),null,DataProtectionScope.CurrentUser)):new AcoustIdSettings();settings.Executable=BundledFpcalc.Resolve(settings.Executable);return settings;}
  public void Validate(){Executable=BundledFpcalc.Resolve(Executable);if(String.IsNullOrWhiteSpace(Executable))throw new Exception("fpcalc.exe fehlt im Programmpaket. Bitte die portable ZIP vollständig entpacken und den Ordner tools neben der Musikbox-Werkstatt.exe belassen.");if(String.IsNullOrWhiteSpace(ClientKey))throw new Exception("Der AcoustID-Anwendungsschlüssel (Client/API Key) fehlt. Ein Benutzer-Schlüssel für Einsendungen ist dafür nicht geeignet.");}
  public void Save(){Validate();using(var s=new MemoryStream()){new DataContractJsonSerializer(typeof(AcoustIdSettings)).WriteObject(s,this);byte[] encrypted=ProtectedData.Protect(s.ToArray(),null,DataProtectionScope.CurrentUser);Directory.CreateDirectory(Path.GetDirectoryName(PathName));string temp=PathName+".tmp";File.WriteAllBytes(temp,encrypted);if(File.Exists(PathName))File.Replace(temp,PathName,null);else File.Move(temp,PathName);}}
 }
 public class FingerprintMatch {
  public string Artist,Title,RecordingId;public double Score;
  public override string ToString(){return Artist+" – "+Title+" (Trefferwert "+Score.ToString("0.000",CultureInfo.CurrentCulture)+")";}
 }
 public class FingerprintResult {
  public Song Song;public List<FingerprintMatch> Matches=new List<FingerprintMatch>();public string Error;
 }
 public static class AcoustId {
  public const string Endpoint="https://api.acoustid.org/v2/lookup";
  static readonly SemaphoreSlim Gate=new SemaphoreSlim(1,1);
  static DateTime lastRequest=DateTime.MinValue;
  [DataContract] public class Print {
   [DataMember(Name="duration")]public double Duration;
   [DataMember(Name="fingerprint")]public string Fingerprint;
  }
  [DataContract] public class Response {
   [DataMember(Name="status")]public string Status;
   [DataMember(Name="error")]public Error Error;
   [DataMember(Name="results")]public Result[] Results;
  }
  [DataContract] public class Error {[DataMember(Name="code")]public int Code;}
  [DataContract] public class Result {
   [DataMember(Name="score")]public double Score;
   [DataMember(Name="recordings")]public Recording[] Recordings;
  }
  [DataContract] public class Recording {
   [DataMember(Name="id")]public string Id;
   [DataMember(Name="title")]public string Title;
   [DataMember(Name="artists")]public Artist[] Artists;
  }
  [DataContract] public class Artist {
   [DataMember(Name="name")]public string Name;
   [DataMember(Name="joinphrase")]public string Join;
  }
  public static Print ParsePrint(byte[] bytes){
   var p=CoverSearch.Json<Print>(bytes);
   if(p==null||Double.IsNaN(p.Duration)||Double.IsInfinity(p.Duration)||p.Duration<1||p.Duration>Int32.MaxValue||String.IsNullOrWhiteSpace(p.Fingerprint)||p.Fingerprint.Length>200000||!System.Text.RegularExpressions.Regex.IsMatch(p.Fingerprint,@"^[A-Za-z0-9_-]+$"))throw new Exception("fpcalc hat keinen gültigen Audio-Fingerabdruck geliefert. Die Datei ist möglicherweise zu kurz oder nicht lesbar.");
   return p;
  }
  public static Print Calculate(string executable,string source,CancellationToken token){
   token.ThrowIfCancellationRequested();if(!File.Exists(source))throw new Exception("Die Originaldatei wurde nicht gefunden.");
   // Without -length, fpcalc fingerprints the first 120 seconds and reports full duration.
   var start=new ProcessStartInfo{FileName=Path.GetFullPath(executable),Arguments="-json "+Ffmpeg.Quote(Path.GetFullPath(source)),UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,RedirectStandardInput=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};
   using(var process=new Process{StartInfo=start}){
    process.Start();process.StandardInput.Close();var output=process.StandardOutput.ReadToEndAsync();var errors=process.StandardError.ReadToEndAsync();var watch=Stopwatch.StartNew();
    try{while(!process.WaitForExit(100)){token.ThrowIfCancellationRequested();if(watch.Elapsed.TotalMinutes>5)throw new Exception("Zeitlimit bei der Fingerabdruck-Berechnung überschritten.");}token.ThrowIfCancellationRequested();}
    catch{try{if(!process.HasExited){process.Kill();process.WaitForExit();}}catch{}throw;}
    Task.WaitAll(output,errors);
    // Some FFmpeg builds signal end of input (exit 3) even after a complete print.
    if(process.ExitCode!=0&&(process.ExitCode!=3||errors.Result.IndexOf("End of file",StringComparison.OrdinalIgnoreCase)<0))throw new Exception("fpcalc konnte die Audiodatei nicht lesen (Exitcode "+process.ExitCode+").");
    if(output.Result.Length>256000)throw new Exception("Ungültige fpcalc-Ausgabe.");
    return ParsePrint(Encoding.UTF8.GetBytes(output.Result));
   }
  }
  public static List<FingerprintMatch> Parse(byte[] bytes){
   var d=CoverSearch.Json<Response>(bytes);
   if(d==null||d.Status!="ok")throw new Exception(d!=null&&d.Error!=null&&d.Error.Code==4?"AcoustID lehnt den Anwendungsschlüssel ab. Bitte den Client/API Key prüfen.":"AcoustID hat die Anfrage abgelehnt. Zugang und Dienststatus prüfen.");
   var matches=new List<FingerprintMatch>();
   foreach(var result in d.Results??new Result[0]){
    if(result==null||Double.IsNaN(result.Score)||Double.IsInfinity(result.Score)||result.Score<0||result.Score>1)continue;
    foreach(var r in result.Recordings??new Recording[0]){
     if(r==null||String.IsNullOrWhiteSpace(r.Title))continue;
     var artists=(r.Artists??new Artist[0]).Where(a=>a!=null&&!String.IsNullOrWhiteSpace(a.Name)).ToArray();if(artists.Length==0)continue;
     var artist=new StringBuilder();for(int i=0;i<artists.Length;i++){artist.Append(artists[i].Name.Trim());if(i+1<artists.Length)artist.Append(String.IsNullOrEmpty(artists[i].Join)?" / ":artists[i].Join);}
     matches.Add(new FingerprintMatch{Artist=artist.ToString(),Title=r.Title.Trim(),RecordingId=r.Id,Score=result.Score});
    }
   }
   // Different releases of the same artist/title need only one suggestion.
   return matches.OrderByDescending(m=>m.Score).GroupBy(m=>m.Artist+"\0"+m.Title,StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).ToList();
  }
  public static string Body(Print p,string key){return "client="+Uri.EscapeDataString(key)+"&duration="+((int)p.Duration).ToString(CultureInfo.InvariantCulture)+"&fingerprint="+Uri.EscapeDataString(p.Fingerprint)+"&meta=recordings+compress&format=json";}
  public static List<FingerprintMatch> Identify(Song song,AcoustIdSettings settings,CancellationToken token){
   settings.Validate();var print=Calculate(settings.Executable,song.Source,token);
   Gate.Wait(token);try{
    int wait=(int)(lastRequest.AddMilliseconds(500)-DateTime.UtcNow).TotalMilliseconds;if(wait>0&&token.WaitHandle.WaitOne(wait))token.ThrowIfCancellationRequested();token.ThrowIfCancellationRequested();
    ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
    var request=(HttpWebRequest)WebRequest.Create(Endpoint);request.AllowAutoRedirect=false;request.Method="POST";request.Timeout=25000;request.ReadWriteTimeout=25000;request.ContentType="application/x-www-form-urlencoded";request.UserAgent="MusikboxWerkstatt/0.17";byte[] body=Encoding.UTF8.GetBytes(Body(print,settings.ClientKey));request.ContentLength=body.Length;
    lastRequest=DateTime.UtcNow;
    using(token.Register(()=>request.Abort()))try{
     using(var output=request.GetRequestStream())output.Write(body,0,body.Length);
     using(var response=request.GetResponse())using(var input=response.GetResponseStream())using(var bytes=new MemoryStream()){
      var buffer=new byte[8192];int count;while((count=input.Read(buffer,0,buffer.Length))>0){token.ThrowIfCancellationRequested();if(bytes.Length+count>2*1024*1024)throw new Exception("AcoustID-Antwort zu groß.");bytes.Write(buffer,0,count);}token.ThrowIfCancellationRequested();return Parse(bytes.ToArray());
     }
    }catch(WebException e){token.ThrowIfCancellationRequested();if(e.Response!=null)e.Response.Dispose();throw new Exception("AcoustID nicht erreichbar oder Anfrage abgelehnt. Anwendungsschlüssel, Internet und Anbieterzugang prüfen.");}
   }finally{Gate.Release();}
  }
 }
}
