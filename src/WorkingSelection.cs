using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Windows.Forms;
namespace Musikbox {public partial class Controller {
 void SelectWorkingPaths(string[] paths){var known=AllSongs.Where(song=>paths.Any(path=>String.Equals(Path.GetFullPath(song.Source),Path.GetFullPath(path),StringComparison.OrdinalIgnoreCase)||(Directory.Exists(path)&&Path.GetFullPath(song.Source).StartsWith(Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)))).ToList();if(known.Count==0||paths.Any(path=>File.Exists(path)&&!known.Any(song=>String.Equals(Path.GetFullPath(song.Source),Path.GetFullPath(path),StringComparison.OrdinalIgnoreCase)))){ImportDropped(paths,-1,true);return;}refresh=true;try{v.Songs.ClearSelection();foreach(DataGridViewRow row in v.Songs.Rows)row.Selected=known.Contains(row.Tag as Song);}finally{refresh=false;}SyncSideTags();}
 }
}
