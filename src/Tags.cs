using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
namespace Musikbox {
 // Bewusst begrenzter ID3v2.3-Reader/Writer. Unbekannte Tag-Varianten werden gesperrt.
 public sealed class Tags {
  public string Artist="",Title="",Album="";
  public long AudioStart,AudioEnd;
  public List<byte[]> Other=new List<byte[]>();
  static int Sync(byte[] b,int p) { int n=0;for(int i=0;i<4;i++){if(b[p+i]>127)throw new Exception("Ungültige ID3-Länge.");n=(n<<7)|b[p+i];}return n; }
  static int Big(byte[] b,int p) { long n=0;for(int i=0;i<4;i++)n=(n<<8)|b[p+i];if(n>Int32.MaxValue)throw new Exception("Ungültige Frame-Länge.");return (int)n; }
  static string Text(byte[] b) {
   if(b.Length==0)return "";
   Encoding e;
   switch(b[0]) {case 0:e=Encoding.GetEncoding(28591);break;case 1:e=(b.Length>2&&b[1]==254&&b[2]==255)?Encoding.BigEndianUnicode:Encoding.Unicode;break;case 2:e=Encoding.BigEndianUnicode;break;case 3:e=Encoding.UTF8;break;default:throw new Exception("Unbekannte Textkodierung.");}
   return e.GetString(b,1,b.Length-1).Trim('\0','\uFEFF');
  }
  public static Tags Read(string path) {
   var t=new Tags();
   using(var f=File.OpenRead(path)) {
    if(f.Length<128)throw new Exception("Datei zu kurz für eine MP3-Datei.");
    t.AudioEnd=f.Length;var h=new byte[10];f.Read(h,0,10);
    if(h[0]=='I'&&h[1]=='D'&&h[2]=='3') {
     if(h[3]!=3||h[4]!=0||h[5]!=0){var song=MediaMetadata.Read(path);t.Artist=song.Artist;t.Title=song.Title;t.Album=song.Album;t.AudioStart=10+Sync(h,6)+(h[3]==4&&(h[5]&16)!=0?10:0);t.AudioEnd=f.Length;return t;}
     int size=Sync(h,6);if(size>32*1024*1024||size>f.Length-10)throw new Exception("Ungültige oder zu große ID3-Tags.");
     byte[] b=new byte[size];int got=0;while(got<size){int n=f.Read(b,got,size-got);if(n==0)throw new EndOfStreamException();got+=n;}t.AudioStart=10+size;
     int p=0;
     while(p+10<=size&&b[p]!=0) {
      string key=Encoding.ASCII.GetString(b,p,4);if(!System.Text.RegularExpressions.Regex.IsMatch(key,"^[A-Z0-9]{4}$"))throw new Exception("Ungültiger ID3-Frame.");
      int n=Big(b,p+4);if(n>size-p-10||n<0)throw new Exception("Beschädigter ID3-Frame.");
      if(b[p+8]!=0||b[p+9]!=0)throw new Exception("Geschützte oder komprimierte ID3-Frames werden noch nicht unterstützt.");
      var d=new byte[n];Buffer.BlockCopy(b,p+10,d,0,n);
      if(key=="TPE1")t.Artist=Text(d);else if(key=="TIT2")t.Title=Text(d);else if(key=="TALB")t.Album=Text(d);
      else {var raw=new byte[n+10];Buffer.BlockCopy(b,p,raw,0,n+10);t.Other.Add(raw);}
      p+=10+n;
     }
    }
    f.Position=f.Length-128;var tail=new byte[128];f.Read(tail,0,128);
    if(tail[0]=='T'&&tail[1]=='A'&&tail[2]=='G') {
     t.AudioEnd-=128;
     var e=Encoding.GetEncoding(28591);
     if(t.Title.Length==0)t.Title=e.GetString(tail,3,30).TrimEnd('\0',' ');
     if(t.Artist.Length==0)t.Artist=e.GetString(tail,33,30).TrimEnd('\0',' ');
     if(t.Album.Length==0)t.Album=e.GetString(tail,63,30).TrimEnd('\0',' ');
    }
    if(t.AudioEnd<=t.AudioStart)throw new Exception("Keine Audiodaten vorhanden.");
   }
   return t;
  }
  static void Frame(Stream s,string key,string text) {
   byte[] data=Encoding.Unicode.GetBytes(text+"\0");int size=data.Length+3;
   byte[] header=new byte[10];Encoding.ASCII.GetBytes(key).CopyTo(header,0);
   for(int i=0;i<4;i++)header[4+i]=(byte)(size>>(24-i*8));s.Write(header,0,10);
   s.WriteByte(1);s.WriteByte(255);s.WriteByte(254);s.Write(data,0,data.Length);
  }
  static void WriteComment(Stream output,string text){byte[] content=Encoding.Unicode.GetBytes(text+"\0");byte[] payload=new byte[10+content.Length];payload[0]=1;payload[1]=(byte)'d';payload[2]=(byte)'e';payload[3]=(byte)'u';payload[4]=255;payload[5]=254;payload[8]=255;payload[9]=254;Buffer.BlockCopy(content,0,payload,10,content.Length);byte[] header=new byte[10];Encoding.ASCII.GetBytes("COMM").CopyTo(header,0);for(int i=0;i<4;i++)header[4+i]=(byte)(payload.Length>>(24-i*8));output.Write(header,0,10);output.Write(payload,0,payload.Length);}
  public static byte[] ArtworkFrame(string path){
   byte[] image=File.ReadAllBytes(path);if(image.Length>8*1024*1024)throw new Exception("Titelcover darf höchstens 8 MB groß sein.");string mime=image.Length>2&&image[0]==255&&image[1]==216?"image/jpeg":image.Length>4&&image[0]==137&&image[1]==80&&image[2]==78&&image[3]==71?"image/png":null;if(mime==null)throw new Exception("Titelcover muss ein JPEG- oder PNG-Bild sein.");
   using(var stream=new MemoryStream()){stream.WriteByte(0);byte[] type=Encoding.ASCII.GetBytes(mime);stream.Write(type,0,type.Length);stream.WriteByte(0);stream.WriteByte(3);stream.WriteByte(0);stream.Write(image,0,image.Length);byte[] payload=stream.ToArray(),frame=new byte[payload.Length+10];Encoding.ASCII.GetBytes("APIC").CopyTo(frame,0);for(int i=0;i<4;i++)frame[4+i]=(byte)(payload.Length>>(24-i*8));payload.CopyTo(frame,10);return frame;}
  }
  public static void WriteCopy(Song song,string album,string target,Action checkCancel,List<byte[]> preservedFrames=null) {
   var head=new byte[10];using(var check=File.OpenRead(song.Source))check.Read(head,0,10);
   if(head[0]=='I'&&head[1]=='D'&&head[2]=='3'&&(head[3]!=3||head[5]!=0)){MediaMetadata.WriteCopy(song,album,target,checkCancel);return;}
   var t=Read(song.Source);
   using(var source=File.OpenRead(song.Source)) using(var output=new FileStream(target,FileMode.CreateNew,FileAccess.Write)) {
    using(var tags=new MemoryStream()) {
     Frame(tags,"TPE1",song.Artist);Frame(tags,"TIT2",song.Title);Frame(tags,"TALB",album);if(!String.IsNullOrEmpty(song.Year))Frame(tags,"TYER",song.Year);if(!String.IsNullOrEmpty(song.Genre))Frame(tags,"TCON",song.Genre);if(!String.IsNullOrEmpty(song.Comment))WriteComment(tags,song.Comment);
     foreach(var frame in preservedFrames??t.Other){if(new[]{"TYER","TDRC","TCON","COMM"}.Contains(Encoding.ASCII.GetString(frame,0,4)))continue;if(!String.IsNullOrEmpty(song.Artwork)&&Encoding.ASCII.GetString(frame,0,4)=="APIC")continue;tags.Write(frame,0,frame.Length);}
     if(!String.IsNullOrEmpty(song.Artwork)){byte[] art=ArtworkFrame(song.Artwork);tags.Write(art,0,art.Length);}
     tags.Write(new byte[1024],0,1024);int len=(int)tags.Length;byte[] h={73,68,51,3,0,0,0,0,0,0};
     for(int i=0;i<4;i++)h[6+i]=(byte)((len>>(21-7*i))&127);
     output.Write(h,0,10);tags.Position=0;tags.CopyTo(output);
    }
    source.Position=t.AudioStart;long remaining=t.AudioEnd-t.AudioStart;var buffer=new byte[65536];
    while(remaining>0){checkCancel();int n=source.Read(buffer,0,(int)Math.Min(buffer.Length,remaining));if(n==0)throw new EndOfStreamException();output.Write(buffer,0,n);remaining-=n;}
    // ID3v1 ist eine ergänzende, auf 30 Bytes begrenzte Kompatibilitätskopie.
    byte[] v1=new byte[128];Encoding.ASCII.GetBytes("TAG").CopyTo(v1,0);
    var enc=Encoding.GetEncoding(28591);
    foreach(var pair in new[]{Tuple.Create(song.Title,3),Tuple.Create(song.Artist,33),Tuple.Create(album,63)}) {
     byte[] b=enc.GetBytes(pair.Item1);Buffer.BlockCopy(b,0,v1,pair.Item2,Math.Min(30,b.Length));
    }
    v1[127]=255;output.Write(v1,0,v1.Length);
   }

  }
 }
}
