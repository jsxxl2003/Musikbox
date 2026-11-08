using System;
using System.IO;
using System.Linq;
using Musikbox;
class EncodingTests {
 static int tests;
 static void Check(bool b,string text){if(!b)throw new Exception(text);tests++;Console.WriteLine("OK "+text);}
 static void Main(string[] args) {
  string work=Path.GetFullPath(args[0]);Directory.CreateDirectory(work);string err;
  var p=new Project{FfmpegExecutable="/usr/bin/ffmpeg"};Check(p.Mp3Bitrate==192&&!p.VariableBitrate&&p.VbrQuality==2&&!p.ReencodeMp3,"Standard 192 kbit/s CBR; keine automatische MP3-Neucodierung");
  string old=Path.Combine(work,"alt.json");File.WriteAllText(old,"{\"Folders\":[]}");var legacy=Storage.Load(old);Check(legacy.Mp3Bitrate==192&&!legacy.VariableBitrate&&legacy.VbrQuality==2,"Alte Projektdateien erhalten neue Standardwerte");
  string source=Path.Combine(work,"test.wav");Ffmpeg.Run(p.FfmpegExecutable,"-v error -nostdin -y -f lavfi -i "+Ffmpeg.Quote("sine=frequency=330:duration=6")+" "+Ffmpeg.Quote(source),()=>{},out err);
  var song=new Song{Source=source,Artist="Test",Title="Bitraten"};
  foreach(int b in Ffmpeg.Bitrates){p.Mp3Bitrate=b;string file=Path.Combine(work,"cbr-"+b+".mp3");Ffmpeg.Write(song,"Test",file,p,()=>{});string actual=Ffmpeg.Run(Ffmpeg.ProbePath(p),"-v error -select_streams a:0 -show_entries stream=bit_rate -of default=noprint_wrappers=1:nokey=1 "+Ffmpeg.Quote(file),()=>{},out err);Check(actual.Trim()==(b*1000).ToString(),"CBR-Ausgabe tatsächlich "+b+" kbit/s");}
  p.VariableBitrate=true;
  for(int q=0;q<=5;q++){p.VbrQuality=q;string file=Path.Combine(work,"vbr-"+q+".mp3");Ffmpeg.Write(song,"Test",file,p,()=>{});string sizes=Ffmpeg.Run(Ffmpeg.ProbePath(p),"-v error -select_streams a:0 -show_packets -show_entries packet=size -of csv=p=0 "+Ffmpeg.Quote(file),()=>{},out err);var frames=sizes.Split('\n').Select(line=>line.Trim().Split(',')[0]).Where(x=>x.Length>0).Select(x=>Int32.Parse(x)).ToList();Check(frames.Count>50&&frames.Max()-frames.Min()>10,"VBR V"+q+": wechselnde MP3-Framegrößen bestätigt");}
  p.VbrQuality=3;p.Mp3Bitrate=224;p.ReencodeMp3=true;string saved=Path.Combine(work,"neu.json");Storage.Save(p,saved);var loaded=Storage.Load(saved);Check(loaded.VariableBitrate&&loaded.VbrQuality==3&&loaded.Mp3Bitrate==224&&loaded.ReencodeMp3,"Alle Kodierungseinstellungen gespeichert und geladen");var copy=Rules.Sorted(p);Check(copy.VariableBitrate&&copy.VbrQuality==3&&copy.Mp3Bitrate==224&&copy.ReencodeMp3,"Kodierungseinstellungen bei automatischer Sortierung erhalten");
  var mp3=new Song{Source=Path.Combine(work,"cbr-192.mp3")};p.ReencodeMp3=false;Check(!Ffmpeg.NeedsEncoding(p,mp3),"Vorhandene MP3 ohne ausdrückliche Auswahl nicht neu kodiert");p.ReencodeMp3=true;Check(Ffmpeg.NeedsEncoding(p,mp3),"Ausdrückliche Auswahl aktiviert MP3-Neucodierung");
  p.Mp3Bitrate=144;bool invalid=false;try{Ffmpeg.Validate(p,true);}catch{invalid=true;}Check(invalid,"Ungültige CBR-Bitrate abgefangen");p.Mp3Bitrate=192;p.VbrQuality=9;invalid=false;try{Ffmpeg.Validate(p,true);}catch{invalid=true;}Check(invalid,"Ungültige VBR-Qualität abgefangen");
  Console.WriteLine("BESTANDEN: "+tests+" Bitraten-Prüfungen (Linux-Programme)");
 }
}
