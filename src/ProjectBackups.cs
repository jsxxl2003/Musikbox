using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Globalization;
namespace Musikbox {
 public class ProjectBackupEntry {
  public string File,ProjectName;public DateTime Time;
 }
 public class ProjectBackups {
  public static string DefaultRoot {get{return Path.Combine(PortablePaths.Data,"Projektsicherungen");}}
  readonly object gate=new object();readonly string directory;readonly int keep;long lastRevision=-1,lastTicks;byte[] lastBytes;
  public ProjectBackups(string projectFile,string root=null,int keep=20){
   if(keep<1)throw new ArgumentOutOfRangeException("keep");this.keep=keep;
   string identity=String.IsNullOrEmpty(projectFile)?Guid.NewGuid().ToString("N"):Path.GetFullPath(projectFile).ToUpperInvariant();string key;
   using(var hash=SHA256.Create())key=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(identity))).Replace("-","").Substring(0,16);
   string name=String.IsNullOrEmpty(projectFile)?"Ungespeichertes Projekt":Path.GetFileNameWithoutExtension(projectFile);if(name.EndsWith(".musikbox",StringComparison.OrdinalIgnoreCase))name=name.Substring(0,name.Length-9);if(name.Length>60)name=name.Substring(0,60);
   directory=Path.Combine(root??DefaultRoot,Rules.SafeName(name)+"-"+key);
  }
  public string Save(byte[] bytes,long revision){lock(gate){
   if(revision<=lastRevision)return null;
   // Validate before writing or pruning a last known good backup.
   Storage.FromBytes(bytes);
   if(lastBytes!=null&&lastBytes.SequenceEqual(bytes)){lastRevision=revision;return null;}
   Directory.CreateDirectory(directory);lastTicks=Math.Max(lastTicks,Directory.GetFiles(directory,"*.musikbox.json").Select(f=>TimeOf(f).Ticks).DefaultIfEmpty(0).Max());lastTicks=Math.Max(DateTime.UtcNow.Ticks,lastTicks+1);string target=Path.Combine(directory,new DateTime(lastTicks,DateTimeKind.Utc).ToString("yyyyMMdd-HHmmss-fffffff")+"-"+Guid.NewGuid().ToString("N").Substring(0,8)+".musikbox.json");string temp=target+".tmp";
   try{using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}File.Move(temp,target);}finally{if(File.Exists(temp))File.Delete(temp);}
   lastRevision=revision;lastBytes=(byte[])bytes.Clone();
   foreach(var old in Directory.GetFiles(directory,"*.musikbox.json").OrderByDescending(f=>Path.GetFileName(f),StringComparer.Ordinal).Skip(keep))File.Delete(old);
   return target;
  }}
  static DateTime TimeOf(string file){string name=Path.GetFileName(file);DateTime time;return name.Length>=23&&DateTime.TryParseExact(name.Substring(0,23),"yyyyMMdd-HHmmss-fffffff",CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal,out time)?time:File.GetLastWriteTimeUtc(file);}
  public static List<ProjectBackupEntry> List(string root=null){root=root??DefaultRoot;var result=new List<ProjectBackupEntry>();if(!Directory.Exists(root))return result;
   foreach(string dir in Directory.GetDirectories(root))foreach(string file in Directory.GetFiles(dir,"*.musikbox.json"))result.Add(new ProjectBackupEntry{File=file,ProjectName=System.Text.RegularExpressions.Regex.Replace(Path.GetFileName(dir),@"-[A-F0-9]{16}$",""),Time=TimeOf(file)});
   return result.OrderByDescending(e=>e.Time).ThenByDescending(e=>e.File,StringComparer.Ordinal).ToList();
  }
 }
}
