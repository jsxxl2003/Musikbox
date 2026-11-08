using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
namespace Musikbox {
 // Ausschließlich neue Importordner. Themen und unterschiedliche Cover nicht mischen.
 public static class ImportPacking {
  static string CoverKey(BoxFolder folder) {
   if(String.IsNullOrEmpty(folder.Cover))return "";
   try{using(var sha=SHA256.Create())using(var stream=File.OpenRead(folder.Cover))return Convert.ToBase64String(sha.ComputeHash(stream));}
   catch{return "Pfad:"+folder.Cover.ToUpperInvariant();}
  }
  public static Project Prepare(Project candidate,Action checkCancel) {
   var result=candidate.WithFolders(new List<BoxFolder>());
   var groups=new Dictionary<string,List<BoxFolder>>(StringComparer.OrdinalIgnoreCase);
   foreach(var folder in candidate.Folders){checkCancel();string key=(folder.Theme??"").Trim().Normalize()+"\0"+CoverKey(folder);List<BoxFolder> group;if(!groups.TryGetValue(key,out group)){group=new List<BoxFolder>();groups.Add(key,group);}group.Add(folder);}
   var visited=new HashSet<List<BoxFolder>>();
   foreach(var folder in candidate.Folders){checkCancel();var group=groups.Values.First(g=>g.Contains(folder));if(!visited.Add(group))continue;
    var songs=group.SelectMany(f=>f.Songs).Select(s=>s.Copy()).ToList();songs.Sort(Rules.Compare);
    int needed=Math.Max(1,(songs.Count+98)/99);string id=needed>1?"Import: "+Guid.NewGuid().ToString("N"):"";
    for(int i=0;i<needed;i++){checkCancel();var source=group[Math.Min(i,group.Count-1)];result.Folders.Add(new BoxFolder{Name=i<group.Count?source.Name:folder.Theme,Theme=folder.Theme,Group=id,SourceDirectory=source.SourceDirectory,Cover=folder.Cover,CoverDesign=folder.CoverDesign,Songs=songs.Skip(i*99).Take(99).ToList()});}
   }
   return result;
  }
 }
}
