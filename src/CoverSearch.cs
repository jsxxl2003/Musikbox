using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
namespace Musikbox {
 public enum CoverNameMode {Automatic,Artist,Title,FreeName}
 public class CoverMatch {
  public string Id,GroupId,Release,Recording,Artist,Date,Type;public bool IsGroup,GroupCoverUsed;
  public override string ToString(){return (String.IsNullOrEmpty(Artist)?"":Artist+" · ")+Release+(String.IsNullOrEmpty(Type)?"":" ["+Type+"]")+" "+(Date??"")+(String.IsNullOrEmpty(Recording)?"":" · "+Recording);}
 }
 public class CoverMissingException:Exception {public CoverMissingException():base("Für diese Veröffentlichung ist kein Frontcover hinterlegt. Bitte einen anderen Treffer wählen oder die Bildersuche verwenden."){} }
 public static class CoverSearch {
  [DataContract] public class Document {[DataMember(Name="recordings")]public Recording[] Recordings;[DataMember(Name="release-groups")]public Group[] Groups;}
  [DataContract] public class Recording {[DataMember(Name="title")]public string Title;[DataMember(Name="releases")]public Release[] Releases;[DataMember(Name="artist-credit")]public Credit[] Credits;}
  [DataContract] public class Credit {[DataMember(Name="name")]public string Name;[DataMember(Name="joinphrase")]public string Join;[DataMember(Name="artist")]public Artist Artist;}
  [DataContract] public class Artist {[DataMember(Name="name")]public string Name;}
  [DataContract] public class Release {[DataMember(Name="id")]public string Id;[DataMember(Name="title")]public string Title;[DataMember(Name="date")]public string Date;[DataMember(Name="release-group")]public Group Group;[DataMember(Name="artist-credit")]public Credit[] Credits;}
  [DataContract] public class Group {[DataMember(Name="id")]public string Id;[DataMember(Name="title")]public string Title;[DataMember(Name="first-release-date")]public string Date;[DataMember(Name="primary-type")]public string Type;[DataMember(Name="artist-credit")]public Credit[] Credits;}
  static readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);static DateTime last=DateTime.MinValue;
  public static T Json<T>(byte[] data){using(var stream=new MemoryStream(data))return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);}
  static string Clean(string value){return SearchText.Clean(value).Replace('’','\'');}
  static string Words(string value){value=Clean(value);if(value.Length==0||value.Length>250)throw new Exception("Bitte einen Suchnamen mit 1 bis 250 Zeichen eingeben.");var words=SearchText.QueryTerms(value);if(words.Length==0)words=new[]{value};return "("+String.Join(" AND ",words.Select(Fragment))+")";}
  static string Fragment(string word){
   string literal="\""+word.Replace("\\","\\\\").Replace("\"","\\\"")+"\"";
   // MusicBrainz indexes words: literal punctuation names remain exact;
   // letter/number fragments additionally match inside each indexed word.
   if(!System.Text.RegularExpressions.Regex.IsMatch(word,@"^[\p{L}\p{N}]+$"))return literal;
   return "("+literal+" OR /.*"+LibrarySearch.Normalize(word)+".*/)";
  }
  public static string Query(string artist,string title){artist=Clean(artist);title=Clean(title);if(artist.Length==0&&title.Length==0)return Words("");return (artist.Length==0?"":"artist:"+Words(artist))+(artist.Length>0&&title.Length>0?" AND ":"")+(title.Length==0?"":"recording:"+Words(title));}
  public static string NameText(string artist,string title,CoverNameMode mode,string name=null){string text=mode==CoverNameMode.Artist?Clean(artist):mode==CoverNameMode.Title?Clean(title):mode==CoverNameMode.FreeName?Clean(name):Clean(artist+" "+title);Words(text);return text;}
  public static string SearchUrl(string artist,string title,CoverNameMode mode=CoverNameMode.Automatic,string name=null){
   string query;bool groups=mode==CoverNameMode.Artist||(mode==CoverNameMode.Automatic&&!String.IsNullOrWhiteSpace(artist)&&String.IsNullOrWhiteSpace(title));
   if(mode==CoverNameMode.Artist)query="artist:"+Words(artist);else if(mode==CoverNameMode.Title)query="recording:"+Words(title);else if(mode==CoverNameMode.FreeName){string[] words=Clean(NameText(artist,title,mode,name)).Split(' ');query=String.Join(" AND ",words.Select(w=>"(artist:"+Words(w)+" OR recording:"+Words(w)+" OR release:"+Words(w)+")"));}else query=Query(artist,title);
   return "https://musicbrainz.org/ws/2/"+(groups?"release-group":"recording")+"/?query="+Uri.EscapeDataString(query)+"&fmt=json&limit=50";
  }
  public static string BrowserUrl(string artist,string title,CoverNameMode mode,string name=null){return "https://www.bing.com/images/search?q="+Uri.EscapeDataString(NameText(artist,title,mode,name)+" cover");}
  static string ArtistName(Credit[] credits){var text=new StringBuilder();foreach(var c in credits??new Credit[0]){if(c==null)continue;string name=String.IsNullOrWhiteSpace(c.Name)?c.Artist==null?null:c.Artist.Name:c.Name;if(String.IsNullOrWhiteSpace(name))continue;if(text.Length>0&&text[text.Length-1]!=' ')text.Append(" / ");text.Append(name);text.Append(c.Join??"");}return text.ToString().Trim();}
  static string Id(string text){Guid id;return Guid.TryParse(text,out id)?id.ToString():null;}
  public static List<CoverMatch> Parse(byte[] bytes){var d=Json<Document>(bytes);if(d==null)throw new SerializationException("Leere Suchantwort");var found=new List<CoverMatch>();var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach(var r in d.Recordings??new Recording[0]){if(r==null)continue;foreach(var release in r.Releases??new Release[0]){if(release==null)continue;string id=Id(release.Id),group=release.Group==null?null:Id(release.Group.Id);if(id==null||!ids.Add(group==null?"release:"+id:"group:"+group))continue;string artist=ArtistName(r.Credits);if(artist.Length==0)artist=ArtistName(release.Credits);found.Add(new CoverMatch{Id=id,GroupId=group,Release=release.Title,Recording=r.Title,Artist=artist,Date=release.Date,Type=release.Group==null?null:release.Group.Type});}}
   foreach(var g in d.Groups??new Group[0]){if(g==null)continue;string id=Id(g.Id);if(id==null||!ids.Add("group:"+id))continue;found.Add(new CoverMatch{Id=id,GroupId=id,IsGroup=true,Release=g.Title,Artist=ArtistName(g.Credits),Date=g.Date,Type=g.Type});}return found.Take(50).ToList();
  }
  public static string[] SearchUrls(string artist,string title,CoverNameMode mode=CoverNameMode.Automatic,string name=null){var urls=new List<string>{SearchUrl(artist,title,mode,name)};if(mode==CoverNameMode.Artist||mode==CoverNameMode.Automatic)foreach(string person in SearchText.Artists(artist))urls.Add(SearchUrl(person,title,mode,name));return urls.Distinct().Take(6).ToArray();}
  public static List<CoverMatch> Search(string artist,string title,CancellationToken token,CoverNameMode mode=CoverNameMode.Automatic,string name=null,Func<string,CancellationToken,int,byte[]> fetch=null){var found=new List<CoverMatch>();foreach(string url in SearchUrls(artist,title,mode,name)){gate.Wait(token);try{int delay=(int)(last.AddMilliseconds(1100)-DateTime.UtcNow).TotalMilliseconds;if(delay>0&&token.WaitHandle.WaitOne(delay))token.ThrowIfCancellationRequested();token.ThrowIfCancellationRequested();last=DateTime.UtcNow;try{found.AddRange(Parse((fetch??Get)(url,token,2*1024*1024)));}catch(SerializationException){throw new Exception("Die Musikdatenbank hat eine ungültige Antwort geliefert.");}}finally{gate.Release();}}string q=mode==CoverNameMode.Artist?artist:mode==CoverNameMode.Title?title:mode==CoverNameMode.FreeName?name:artist+" "+title;return found.GroupBy(m=>m.GroupId??m.Id).Select(g=>g.OrderByDescending(m=>SearchText.Relevance(q,m.Artist+" "+m.Recording+" "+m.Release)).First()).OrderByDescending(m=>SearchText.Relevance(q,m.Artist+" "+m.Recording+" "+m.Release)).Take(100).ToList();}
  public static Uri Redirect(Uri current,string location){Uri next;if(String.IsNullOrWhiteSpace(location)||!Uri.TryCreate(current,location,out next)||(next.Scheme!="https"&&next.Scheme!="http"))throw new Exception("Ungültige Weiterleitung des Coveranbieters.");if(next.Scheme=="http"){var secure=new UriBuilder(next){Scheme="https",Port=next.IsDefaultPort?-1:next.Port};next=secure.Uri;}return next;}
  public static byte[] Get(string url,CancellationToken token,int maxBytes){
   token.ThrowIfCancellationRequested();ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;Uri current=new Uri(url);if(current.Scheme!="https")throw new Exception("Für die Coversuche ist HTTPS erforderlich.");
   for(int redirects=0;redirects<6;redirects++){
    var request=(HttpWebRequest)WebRequest.Create(current);request.UserAgent="MusikboxWerkstatt/0.17 (https://github.com/jsxxl2003)";request.Timeout=20000;request.ReadWriteTimeout=20000;request.AllowAutoRedirect=false;request.AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate;
    using(token.Register(()=>request.Abort()))try{using(var response=(HttpWebResponse)request.GetResponse()){
     int code=(int)response.StatusCode;if(code==301||code==302||code==303||code==307||code==308){current=Redirect(current,response.Headers["Location"]);continue;}
     using(var input=response.GetResponseStream())using(var output=new MemoryStream()){var buffer=new byte[32768];int n;while((n=input.Read(buffer,0,buffer.Length))>0){token.ThrowIfCancellationRequested();if(output.Length+n>maxBytes)throw new Exception("Antwortdatei ist zu groß.");output.Write(buffer,0,n);}token.ThrowIfCancellationRequested();return output.ToArray();}
    }}catch(WebException e){token.ThrowIfCancellationRequested();var response=e.Response as HttpWebResponse;int code=response==null?0:(int)response.StatusCode;if(response!=null)response.Dispose();if(code==404)throw new CoverMissingException();if(code==429||code==503)throw new Exception("Der Coverdienst ist ausgelastet. Bitte kurz warten oder die Bildersuche im Browser nutzen.");throw new Exception("Online-Coverdienst nicht erreichbar"+(code==0?"":" (HTTP "+code+")")+". Bitte Internet prüfen oder die Bildersuche im Browser nutzen.",e);}
   }throw new Exception("Zu viele Weiterleitungen beim Coverabruf. Bitte später erneut versuchen.");
  }
  public static string Download(CoverMatch match,CancellationToken token,Func<string,CancellationToken,int,byte[]> fetch=null){if(match==null||Id(match.Id)==null)throw new Exception("Ungültige Veröffentlichungs-ID.");var get=fetch??Get;match.GroupCoverUsed=match.IsGroup;byte[] bytes;
   try{bytes=get("https://coverartarchive.org/"+(match.IsGroup?"release-group":"release")+"/"+Id(match.Id)+"/front-500",token,8*1024*1024);}
   catch(CoverMissingException){string group=Id(match.GroupId);if(match.IsGroup||group==null)throw;bytes=get("https://coverartarchive.org/release-group/"+group+"/front-500",token,8*1024*1024);match.GroupCoverUsed=true;}
   token.ThrowIfCancellationRequested();return SaveImage(bytes);
  }
  public static string ImportImage(string path){if(new FileInfo(path).Length>8*1024*1024)throw new Exception("Das Bild darf höchstens 8 MB groß sein.");return SaveImage(File.ReadAllBytes(path));}
  public static string SaveImage(byte[] bytes){try{using(var stream=new MemoryStream(bytes))using(var image=Image.FromStream(stream)){if(image.Width>8000||image.Height>8000)throw new Exception("Coverbild ist zu groß.");string root=Path.Combine(PortablePaths.Data,"Titelcover");Directory.CreateDirectory(root);string path=Path.Combine(root,Guid.NewGuid().ToString("N")+".jpg");using(var bitmap=new Bitmap(image))bitmap.Save(path,ImageFormat.Jpeg);return path;}}catch(ArgumentException){throw new Exception("Der Anbieter hat kein lesbares Coverbild geliefert. Bitte einen anderen Treffer oder eine Bilddatei auswählen.");}}
 }
}
