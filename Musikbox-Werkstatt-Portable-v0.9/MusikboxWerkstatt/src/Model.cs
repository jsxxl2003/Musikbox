using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

namespace Musikbox {
 [DataContract] public class Song {
  [DataMember] public string Source;
  [DataMember] public string Artist;
  [DataMember] public string Title;
  [DataMember] public string Album;
  public Song Copy() { return (Song)MemberwiseClone(); }
 }
 [DataContract] public class BoxFolder {
  [DataMember] public string Name;
  [DataMember] public string Theme;
  [DataMember] public string Group;
  [DataMember] public string SourceDirectory;
  [DataMember] public string Cover;
  [DataMember] public List<Song> Songs = new List<Song>();
  public override string ToString() { return Name + "   [" + Songs.Count + "/99]"; }
 }
 [DataContract] public class Project {
  [DataMember] public List<BoxFolder> Folders = new List<BoxFolder>();
  [DataMember] public bool Normalize;
  [DataMember] public string GainExecutable;
  [DataMember] public double TargetDb=89;
  [DataMember] public string FfmpegExecutable;
  [DataMember] public bool FfmpegLoudness;
  [DataMember] public double TargetLufs=-16;
  [DataMember] public int Mp3Bitrate=192;
  [DataMember] public bool VariableBitrate;
  [DataMember] public int VbrQuality=2;
  [DataMember] public bool ReencodeMp3;
  [OnDeserializing] void Defaults(StreamingContext context){Mp3Bitrate=192;VbrQuality=2;TargetDb=89;TargetLufs=-16;}
  public Project WithFolders(List<BoxFolder> folders) {return new Project{Folders=folders,Normalize=Normalize,GainExecutable=GainExecutable,TargetDb=TargetDb,FfmpegExecutable=FfmpegExecutable,FfmpegLoudness=FfmpegLoudness,TargetLufs=TargetLufs,Mp3Bitrate=Mp3Bitrate,VariableBitrate=VariableBitrate,VbrQuality=VbrQuality,ReencodeMp3=ReencodeMp3};}
 }
 public static class Rules {
  public static string Theme(string folder) { return System.Text.RegularExpressions.Regex.Replace(folder, @"^\d{3}\s+", ""); }
  public static string SafeName(string s) {
   var bad = "<>:\"/\\|?*";
   string r = new string(s.Normalize().Select(c => c < 32 || bad.IndexOf(c)>=0 ? '_' : c).ToArray()).Trim().TrimEnd('.',' ');
   if (r.Length>120) r=r.Substring(0,120).TrimEnd('.',' ');
   if (r.Length==0) r="Unbenannt";
   if (System.Text.RegularExpressions.Regex.IsMatch(r,@"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\.|$)",System.Text.RegularExpressions.RegexOptions.IgnoreCase)) r="_"+r;
   return r;
  }
  static readonly CompareInfo German=CultureInfo.GetCultureInfo("de-DE").CompareInfo;
  public static void MoveFolder(Project p,int i,int j) {
   var a=p.Folders[i];var b=p.Folders[j];
   var re=new System.Text.RegularExpressions.Regex(@"^\d{3}\s+");
   if(!re.IsMatch(a.Name)||!re.IsMatch(b.Name))throw new Exception("Zum Verschieben bitte beide Ordner mit dreistelliger Nummer benennen, z. B. 031 Country.");
   string an=a.Name.Substring(0,3),bn=b.Name.Substring(0,3);a.Name=bn+a.Name.Substring(3);b.Name=an+b.Name.Substring(3);
   p.Folders.RemoveAt(i);p.Folders.Insert(j,a);
  }
  public static int Compare(Song a, Song b) {
   int n=German.Compare(a.Artist,b.Artist,CompareOptions.IgnoreCase);
   if(n==0)n=German.Compare(a.Title,b.Title,CompareOptions.IgnoreCase);
   if(n==0)n=StringComparer.OrdinalIgnoreCase.Compare(a.Source,b.Source);
   return n;
  }
  public static Project Sorted(Project p) {
   var result=p.WithFolders(new List<BoxFolder>());
   foreach(var f in p.Folders) result.Folders.Add(new BoxFolder {Name=f.Name,Theme=f.Theme,Group=f.Group,SourceDirectory=f.SourceDirectory,Cover=f.Cover,Songs=f.Songs.Select(s=>s.Copy()).ToList()});
   var processed=new HashSet<string>();
   foreach(var f in result.Folders.ToList()) {
    string group=(f.Group??"").Trim();
    var folders=group.Length==0 ? new List<BoxFolder>{f} : result.Folders.Where(x=>String.Equals((x.Group??"").Trim(),group,StringComparison.OrdinalIgnoreCase)).ToList();
    if(group.Length>0&&!processed.Add(group.ToUpperInvariant()))continue;
    if(folders.Select(x=>x.Theme).Distinct(StringComparer.OrdinalIgnoreCase).Count()>1)throw new Exception("Zusammengehörige Ordner müssen denselben Themennamen haben: "+group);
    var songs=folders.SelectMany(x=>x.Songs).ToList();songs.Sort(Compare);
    int required=(songs.Count+98)/99;
    int extra=Math.Max(0,required-folders.Count);
    if(result.Folders.Count+extra>99)throw new Exception("Die automatische Aufteilung würde mehr als 99 Ordner benötigen. Es wurde nichts übernommen.");
    if(extra>0) {
     if(group.Length==0){group="Thema: "+f.Theme+" ("+Guid.NewGuid().ToString("N").Substring(0,6)+")";foreach(var folder in folders)folder.Group=group;processed.Add(group.ToUpperInvariant());}
     int insert=result.Folders.IndexOf(folders.Last())+1;
     for(int j=0;j<extra;j++) {
      var added=new BoxFolder{Name=f.Theme,Theme=f.Theme,Group=group,Cover=f.Cover,SourceDirectory=f.SourceDirectory};
      result.Folders.Insert(insert+j,added);folders.Add(added);
     }
    }
    for(int i=0;i<folders.Count;i++) {folders[i].Songs=songs.Skip(i*99).Take(99).ToList();foreach(var song in folders[i].Songs)song.Album=folders[i].Theme;}
   }
   int previous=-1;
   foreach(var folder in result.Folders) {
    var match=System.Text.RegularExpressions.Regex.Match(folder.Name??"",@"^(\d{3})\s+(.+)$");
    int number=match.Success?Int32.Parse(match.Groups[1].Value):previous+1;
    number=Math.Max(previous+1,number);
    if(number>999)throw new Exception("Keine fortlaufende dreistellige Ordnernummer verfügbar. Bitte mit einer niedrigeren Startnummer beginnen.");
    string suffix=match.Success?match.Groups[2].Value:folder.Name;
    folder.Name=number.ToString("D3")+" "+suffix;previous=number;
   }
   return result;
  }
  public static List<string> Validate(Project p) {
   var errors=new List<string>();
   if(p.Folders.Count==0)errors.Add("Keine Ordner vorhanden.");
   if(p.Folders.Count>99)errors.Add("Höchstens 99 Ordner erlaubt.");
   var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var numbers=new HashSet<string>();string previous="";
   foreach(var f in p.Folders) {
    if(String.IsNullOrWhiteSpace(f.Name)||SafeName(f.Name)!=f.Name)errors.Add("Ungültiger Ordnername: "+f.Name);
    if(!names.Add(f.Name))errors.Add("Doppelter Ordnername: "+f.Name);
    if(!System.Text.RegularExpressions.Regex.IsMatch(f.Name??"",@"^\d{3}\s+\S"))errors.Add("Dreistellige Ordnernummer fehlt: "+f.Name);
    else {string number=f.Name.Substring(0,3);if(!numbers.Add(number))errors.Add("Ordnernummer mehrfach vergeben: "+number);if(String.CompareOrdinal(previous,number)>0)errors.Add("Ordnernummern passen nicht zur angezeigten Reihenfolge: "+f.Name);previous=number;}
    if(String.IsNullOrWhiteSpace(f.Theme))errors.Add("Themenname fehlt: "+f.Name);
    if(f.Songs.Count>99)errors.Add(f.Name+": mehr als 99 Titel.");
    if(!String.IsNullOrEmpty(f.Cover)&&!File.Exists(f.Cover))errors.Add("Cover fehlt: "+f.Cover);
    foreach(var s in f.Songs) {
     if(!File.Exists(s.Source))errors.Add("Quelldatei fehlt: "+s.Source);
     if(String.IsNullOrWhiteSpace(s.Artist)||String.IsNullOrWhiteSpace(s.Title))errors.Add("Interpret oder Titel fehlt: "+Path.GetFileName(s.Source));
    }
   }
   return errors;
  }
 }
}
