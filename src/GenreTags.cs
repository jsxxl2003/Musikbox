using System;using System.Linq;namespace Musikbox {public static class GenreTags {
 public static string[] Normalize(string[] genres){return genres.Select(x=>(x??"").Trim()).Where(x=>x.Length>0).Select(x=>TagLib.Genres.IndexToAudio(x)??x).ToArray();}
 public static void Write(TagLib.File file,string[] genres){var values=Normalize(genres);TagLib.Id3v2.Tag.UseNumericGenres=false;file.Tag.Genres=values;var tag=file.GetTag(TagLib.TagTypes.Id3v2,file.Name.EndsWith(".mp3",StringComparison.OrdinalIgnoreCase)) as TagLib.Id3v2.Tag;if(tag!=null){if(values.Length==0)tag.RemoveFrames("TCON");else TagLib.Id3v2.TextInformationFrame.Get(tag,"TCON",true).Text=values;}}
}}
