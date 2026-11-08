using System;
using System.IO;
using System.Linq;
namespace Musikbox {
 public static class FolderArtwork {
  public static string Find(string directory) {
   if(!Directory.Exists(directory))return null;
   var files=Directory.GetFiles(directory).Where(p=>new[]{".jpg",".jpeg",".png",".bmp",".gif"}.Contains(Path.GetExtension(p).ToLowerInvariant())).OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).ToList();
   foreach(string name in new[]{"folder","cover","front","album","albumart"}){var match=files.FirstOrDefault(p=>Path.GetFileNameWithoutExtension(p).Equals(name,StringComparison.OrdinalIgnoreCase));if(match!=null)return match;}
   return files.FirstOrDefault();
  }
 }
}
