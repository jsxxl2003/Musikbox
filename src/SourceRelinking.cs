using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace Musikbox {
 public class RelinkEntry {
  public string Kind,OldPath,NewPath,Status;public bool Found;public Action<Project,string> Set;
 }
 public class RelinkPlan {
  public readonly List<RelinkEntry> Entries=new List<RelinkEntry>();
  public byte[] Baseline;
  public int Found {get{return Entries.Count(e=>e.Found);}}
 }
 public static class SourceRelinking {
  static bool WindowsPath(string p){return p!=null&&(System.Text.RegularExpressions.Regex.IsMatch(p,@"^[a-zA-Z]:[\\/]")||p.StartsWith(@"\\"));}
  static string Normal(string path){
   if(String.IsNullOrWhiteSpace(path))throw new Exception("Bitte einen absoluten Ordnerpfad angeben.");
   if(WindowsPath(path)){path=path.Replace('/','\\');if(path.Split('\\').Any(s=>s==".."||s=="."))throw new Exception("Bitte den vollständigen Ordnerpfad ohne . oder .. angeben.");if(path.StartsWith(@"\\")&&path.Split(new[]{'\\'},StringSplitOptions.RemoveEmptyEntries).Length<2)throw new Exception("Ein Netzwerkpfad benötigt Server und Freigabe.");return path;}
   if(!Path.IsPathRooted(path))throw new Exception("Bitte einen absoluten Ordnerpfad angeben.");return Path.GetFullPath(path);
  }
  public static string Root(string path){path=Normal(path);char sep=WindowsPath(path)?'\\':Path.DirectorySeparatorChar;return path.TrimEnd('\\','/')+sep;}
  public static string Map(string source,string oldRoot,string newRoot){
   string old=Root(oldRoot),target=Root(newRoot),path;try{path=Normal(source);}catch{return null;}
   var comparison=WindowsPath(old)?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal;
   if(String.Equals(path.TrimEnd('\\','/'),old.TrimEnd('\\','/'),comparison))return target;
   if(!path.StartsWith(old,comparison))return null;string relative=path.Substring(old.Length);char sep=WindowsPath(target)?'\\':Path.DirectorySeparatorChar;
   return target+relative.Replace('\\',sep).Replace('/',sep);
  }
  public static List<string> SuggestedRoots(Project p,Action check=null){
   if(check==null)check=()=>{};
   var paths=p.Folders.SelectMany(f=>f.Songs).Where(s=>{check();return !File.Exists(s.Source);}).Select(s=>s.Source).Where(s=>!String.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
   var parents=paths.Select(s=>{int n=s.LastIndexOfAny(new[]{'\\','/'});return n<0?null:s.Substring(0,n+1);}).Where(s=>s!=null).ToList();var roots=new List<string>();
   if(parents.Count>0){string common=Root(parents[0]);while(!parents.All(s=>s.StartsWith(common,WindowsPath(common)?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal))){var trimmed=common.TrimEnd('\\','/');int n=trimmed.LastIndexOfAny(new[]{'\\','/'});if(n<0){common=null;break;}string next=trimmed.Substring(0,n+1);if(next==common){common=null;break;}common=next;}if(common!=null)roots.Add(common);}
   roots.AddRange(p.Folders.Where(f=>{check();return !String.IsNullOrEmpty(f.SourceDirectory)&&!Directory.Exists(f.SourceDirectory);}).Select(f=>f.SourceDirectory));roots.AddRange(parents);
   return roots.Distinct(StringComparer.OrdinalIgnoreCase).Take(200).ToList();
  }
  public static RelinkPlan Plan(Project p,string oldRoot,string newRoot,Action checkCancel){
   Root(oldRoot);Root(newRoot);if(!Directory.Exists(newRoot))throw new Exception("Der neue Speicherort ist nicht erreichbar.");
   var plan=new RelinkPlan{Baseline=Storage.Bytes(p)};
   Action<string,string,bool,Action<Project,string>> add=(kind,source,directory,set)=>{
    checkCancel();if(String.IsNullOrEmpty(source)||(directory?Directory.Exists(source):File.Exists(source)))return;
    string target=Map(source,oldRoot,newRoot);bool found=target!=null&&(directory?Directory.Exists(target):File.Exists(target));
    plan.Entries.Add(new RelinkEntry{Kind=kind,OldPath=source,NewPath=target,Found=found,Status=target==null?"Außerhalb des alten Speicherorts":found?"Gefunden – bitte Zuordnung prüfen":"Am neuen Ort nicht gefunden",Set=set});
   };
   for(int i=0;i<p.Folders.Count;i++){checkCancel();int f=i;var folder=p.Folders[i];
    add("Quellordner",folder.SourceDirectory,true,(q,v)=>q.Folders[f].SourceDirectory=v);
    add("Ordnercover",folder.Cover,false,(q,v)=>q.Folders[f].Cover=v);
    if(folder.CoverDesign!=null)add("Cover-Hintergrund",folder.CoverDesign.ImagePath,false,(q,v)=>q.Folders[f].CoverDesign.ImagePath=v);
    for(int j=0;j<folder.Songs.Count;j++){int s=j;var song=folder.Songs[j];
     add("Musiktitel",song.Source,false,(q,v)=>{q.Folders[f].Songs[s].Source=v;q.Folders[f].Songs[s].PreparedSource=null;q.Folders[f].Songs[s].PreparedProfile=null;});
     add("Titelcover",song.Artwork,false,(q,v)=>q.Folders[f].Songs[s].Artwork=v);
    }
   }checkCancel();return plan;
  }
  public static Project Apply(Project current,RelinkPlan plan){
   if(!Storage.Bytes(current).SequenceEqual(plan.Baseline))throw new Exception("Das Projekt hat sich geändert. Bitte die Zuordnung erneut prüfen.");
   var candidate=Storage.FromBytes(plan.Baseline);
   foreach(var entry in plan.Entries.Where(e=>e.Found)){
    if(!(entry.Kind=="Quellordner"?Directory.Exists(entry.NewPath):File.Exists(entry.NewPath)))throw new Exception("Der neue Speicherort ist inzwischen nicht mehr erreichbar. Es wurde nichts übernommen.");
    entry.Set(candidate,entry.NewPath);
   }return candidate;
  }
 }
}
