using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Text;
namespace Musikbox {
 // Personal search aliases, not acoustic model training. No recordings are stored.
 public sealed class VoiceCorrections {
  readonly string path;public readonly Dictionary<string,string> Items=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
  public VoiceCorrections(string path){this.path=path;if(File.Exists(path))foreach(string line in File.ReadAllLines(path)){var a=line.Split('\t');if(a.Length==2)try{Items[Encoding.UTF8.GetString(Convert.FromBase64String(a[0]))]=Encoding.UTF8.GetString(Convert.FromBase64String(a[1]));}catch(FormatException){}}}
  public string Apply(string text){string value;return Items.TryGetValue((text??"").Trim(),out value)?value:text;}
  public void Set(string heard,string target){heard=(heard??"").Trim();target=(target??"").Trim();if(heard.Length==0||target.Length==0)throw new ArgumentException("Erkannten Begriff und richtigen Suchtext eingeben.");Items[heard]=target;Save();}
  public void Remove(string heard){Items.Remove(heard);Save();}
  void Save(){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));string temp=path+".tmp";File.WriteAllLines(temp,Items.OrderBy(x=>x.Key).Select(x=>Convert.ToBase64String(Encoding.UTF8.GetBytes(x.Key))+"\t"+Convert.ToBase64String(Encoding.UTF8.GetBytes(x.Value))));File.Move(temp,path,true);}
 }
}
