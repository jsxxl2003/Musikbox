using System;using System.IO;using System.Linq;using System.Collections.Generic;
namespace Musikbox {
 public sealed class LibraryCountSnapshot {public readonly int Titles,Albums,Artists;public LibraryCountSnapshot(int titles,int albums,int artists){Titles=titles;Albums=albums;Artists=artists;}}
 // Unique source paths, independent of import callback order and overlapping drops.
 public sealed class LibraryCounter {
  readonly object gate=new object();readonly HashSet<string> files=new HashSet<string>(StringComparer.OrdinalIgnoreCase),albums=new HashSet<string>(StringComparer.OrdinalIgnoreCase),artists=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  public bool Add(Song song){if(song==null)return false;string path=String.IsNullOrWhiteSpace(song.Source)?null:Path.GetFullPath(song.Source);lock(gate){if(path==null||!files.Add(path))return false;if(!String.IsNullOrWhiteSpace(song.Album))albums.Add(song.Album.Trim());if(!String.IsNullOrWhiteSpace(song.Artist))artists.Add(song.Artist.Trim());return true;}}
  public LibraryCountSnapshot Snapshot(){lock(gate)return new LibraryCountSnapshot(files.Count,albums.Count,artists.Count);}
  public static LibraryCountSnapshot FromSongs(IEnumerable<Song> songs){var counter=new LibraryCounter();foreach(var song in songs)counter.Add(song);return counter.Snapshot();}
 }
}
