using System;
using System.Linq;
using System.Collections.Generic;
namespace Musikbox {
 public static class TagEditing {
  public static string Common(IEnumerable<Song> songs,Func<Song,string> value){var values=songs.Select(s=>value(s)??"").Distinct(StringComparer.Ordinal).Take(2).ToList();return values.Count==1?values[0]:"";}
  public static int Apply(IEnumerable<Song> songs,bool setArtist,string artist,bool setTitle,string title,bool setAlbum=false,string album=null){
   string a=(artist??"").Trim(),t=(title??"").Trim(),b=(album??"").Trim();int changed=0;
   foreach(var song in songs){bool change=(setArtist&&song.Artist!=a)||(setTitle&&song.Title!=t)||(setAlbum&&(!song.AlbumOverride||song.Album!=b));if(song.PendingTags==null)song.PendingTags=new Dictionary<string,string>();if(setArtist){song.Artist=a;song.PendingTags["artist"]=a;}if(setTitle){song.Title=t;song.PendingTags["title"]=t;}if(setAlbum){song.Album=b;song.AlbumOverride=true;song.PendingTags["album"]=b;}if(change)changed++;}
   return changed;
  }
 }
}
