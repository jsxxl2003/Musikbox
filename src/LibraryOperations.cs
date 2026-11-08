using System.Collections.Generic;
using System.Linq;
namespace Musikbox {
 public static class LibraryOperations {
  public static int Remove(Project project,IEnumerable<Song> selected){var songs=new HashSet<Song>(selected);int removed=0;foreach(var folder in project.Folders)removed+=folder.Songs.RemoveAll(song=>songs.Contains(song));return removed;}
 }
}
