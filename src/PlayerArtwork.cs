using System;using System.IO;using System.Drawing;using System.Threading.Tasks;
namespace Musikbox {public partial class Controller {
 int playerArtworkGeneration;
 async void RefreshPlayerArtwork(){int generation=++playerArtworkGeneration;var song=playerSong;v.Player.SetArtwork(null);if(song==null)return;string source=song.Source,art=song.Artwork;try{var bytes=await Task.Run(()=>!String.IsNullOrEmpty(art)&&File.Exists(art)?File.ReadAllBytes(art):File.Exists(source)?MediaMetadata.Artwork(source):null);if(v.IsDisposed||generation!=playerArtworkGeneration||playerSong!=song||song.Source!=source||bytes==null)return;using(var stream=new MemoryStream(bytes))using(var image=Image.FromStream(stream))v.Player.SetArtwork(new Bitmap(image));}catch{}}
}}
