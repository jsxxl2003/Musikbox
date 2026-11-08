using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Text.RegularExpressions;using System.Threading;
namespace Musikbox {
 // Immutable metadata snapshots: filtering never opens music files or reads tags.
 public sealed class LibrarySearchIndex {
  public sealed class Entry {
   public readonly BoxFolder Folder;public readonly Song Song;readonly string[] fields;string[] normal,compact,words;
   public Entry(BoxFolder folder,Song song){Folder=folder;Song=song;fields=new[]{song.Artist,song.Title,song.Album,song.Year,song.Genre,song.Comment,Path.GetFileName(song.Source),folder.Name};}
   void Prepare(){if(normal!=null)return;compact=fields.Select(LibrarySearch.Compact).ToArray();words=fields.SelectMany(f=>Regex.Split(LibrarySearch.Normalize(f),"[^a-z0-9]+")).Where(w=>w.Length>0).Select(LibrarySearch.Sound).ToArray();normal=fields.Select(LibrarySearch.Normalize).ToArray();}
   internal bool Matches(Query query){Prepare();for(int i=0;i<query.Terms.Length;i++){string term=query.Terms[i],needle=query.Compact[i];if(needle.Length==0)continue;if(normal.Any(f=>f.Contains(term))||compact.Any(f=>f.Contains(needle)))continue;if(needle.Length<4||needle.All(Char.IsDigit))return false;string sound=query.Sounds[i];if(!words.Any(w=>w.Length>=3&&(w==sound||LibrarySearch.Near(w,sound,needle.Length>=8?2:1))))return false;}return true;}
  }
  internal sealed class Query {public readonly string[] Terms,Compact,Sounds;public Query(string text){Terms=Regex.Split(LibrarySearch.Normalize(LibrarySearch.Correct(text)),@"[\s.,!?;:]+");Compact=Terms.Select(LibrarySearch.Compact).ToArray();Sounds=Compact.Select(LibrarySearch.Sound).ToArray();}}
  readonly Entry[] entries;readonly object gate=new object();public LibrarySearchIndex(IEnumerable<Entry> source){entries=source.ToArray();}
  public Entry[] Filter(string text,bool voice,CancellationToken token){var query=new Query(text);var found=new List<Entry>();lock(gate){foreach(var entry in entries){token.ThrowIfCancellationRequested();if(entry.Matches(query)||(voice&&LibrarySearch.VoiceMatches(entry.Song,text)))found.Add(entry);}}token.ThrowIfCancellationRequested();if(!String.IsNullOrWhiteSpace(text))return found.Select(e=>new{Entry=e,Rank=SearchText.Relevance(text,e.Song.Artist+" "+e.Song.Title+" "+e.Song.Album)}).OrderByDescending(x=>x.Rank).Select(x=>x.Entry).ToArray();return found.ToArray();}
 }
}
