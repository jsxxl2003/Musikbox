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
     if(h[3]!=3||h[4]!=0||h[5]!=0)throw new Exception("Dieses Tag-Format wird in Version 0.9 noch nicht unterstützt (benötigt ID3v2.3 ohne Sonderflags).");
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
  public static void WriteCopy(Song song,string album,string target,Action checkCancel,List<byte[]> preservedFrames=null) {
   var t=Read(song.Source);
   using(var source=File.OpenRead(song.Source)) using(var output=new FileStream(target,FileMode.CreateNew,FileAccess.Write)) {
    using(var tags=new MemoryStream()) {
     Frame(tags,"TPE1",song.Artist);Frame(tags,"TIT2",song.Title);Frame(tags,"TALB",album);
     foreach(var frame in preservedFrames??t.Other)tags.Write(frame,0,frame.Length);
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
