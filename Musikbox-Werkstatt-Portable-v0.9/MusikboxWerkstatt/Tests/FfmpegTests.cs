using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Musikbox;
class FfmpegTests {
 static int tests;
 static void Check(bool b,string text){if(!b)throw new Exception(text);tests++;Console.WriteLine("OK "+text);}
 static void Main(string[] args) {
  string work=Path.GetFullPath(args[0]);Directory.CreateDirectory(work);string folder=Path.Combine(work,"000 Grüße & Musik");Directory.CreateDirectory(folder);
  var p=new Project{FfmpegExecutable="/usr/bin/ffmpeg",TargetLufs=-16};string err;
  foreach(string ext in new[]{"flac","wav","m4a","ogg"}) {
   string path=Path.Combine(folder,"Ärztin & Band – Grüße."+ext);
   Ffmpeg.Run(p.FfmpegExecutable,"-v error -nostdin -y -f lavfi -i "+Ffmpeg.Quote("sine=frequency=440:duration=6")+" -metadata artist="+Ffmpeg.Quote("Ärztin & Band")+" -metadata title="+Ffmpeg.Quote("Grüße – Straße")+" -metadata album=Alt "+Ffmpeg.Quote(path),()=>{},out err);
   var song=Ffmpeg.ReadSong(path,p,()=>{});Check(song.Artist=="Ärztin & Band"&&song.Title=="Grüße – Straße"&&song.Album=="Alt","Unicode-Metadaten aus "+ext+" eingelesen");
  }
  var warnings=new List<string>();p.Folders=Importer.Read(folder,warnings,p);Check(warnings.Count==0&&p.Folders.Count==1&&p.Folders[0].Songs.Count==4,"Gemischte Audioformate gemeinsam importiert");
  var originals=p.Folders[0].Songs.ToDictionary(s=>s.Source,s=>File.ReadAllBytes(s.Source));
  string dest=Exporter.Export(p,work,()=>{},(n,s)=>{});var files=Directory.GetFiles(dest,"*.mp3",SearchOption.AllDirectories);Check(files.Length==4,"Vier unterschiedliche Formate als MP3 exportiert");
  foreach(string file in files){var tags=Tags.Read(file);Check(tags.Artist=="Ärztin & Band"&&tags.Title=="Grüße – Straße"&&tags.Album=="Grüße & Musik","MP3-Tags nach Konvertierung vollständig");
   string audio=Ffmpeg.Run(Ffmpeg.ProbePath(p),"-v error -select_streams a:0 -show_entries stream=codec_name,sample_rate,channels,bit_rate -of default=noprint_wrappers=1 "+Ffmpeg.Quote(file),()=>{},out err);Check(audio.Contains("codec_name=mp3")&&audio.Contains("sample_rate=44100")&&audio.Contains("channels=2")&&audio.Contains("bit_rate=192000"),"MP3-Ausgabe 192 kbit/s, 44,1 kHz, Stereo geprüft");}
  Check(originals.All(x=>x.Value.SequenceEqual(File.ReadAllBytes(x.Key))),"Originaldateien nach Konvertierung bytegleich");
  p.FfmpegLoudness=true;Song source=Ffmpeg.ReadSong(originals.Keys.First(),p,()=>{});string normal=Path.Combine(work,"normalisiert.mp3");string log=Ffmpeg.Write(source,"Neues Thema",normal,p,()=>{});Check(log.Contains("Messung")&&log.Contains("Verarbeitung"),"Zwei-Pass-Lautheitsanalyse protokolliert");
  Ffmpeg.Run(p.FfmpegExecutable,"-hide_banner -nostdin -nostats -i "+Ffmpeg.Quote(normal)+" -af loudnorm=I=-16:TP=-2:LRA=11:print_format=json -f null -",()=>{},out err);var measured=Ffmpeg.Measurement(err);double lufs=Double.Parse(measured["input_i"],CultureInfo.InvariantCulture),peak=Double.Parse(measured["input_tp"],CultureInfo.InvariantCulture);Check(Math.Abs(lufs+16)<0.6&&peak< -1,"Kodierte Ausgabe nachgemessen: "+lufs+" LUFS, "+peak+" dBTP");
  string saved=Path.Combine(work,"ffmpeg-project.json");Storage.Save(p,saved);var loaded=Storage.Load(saved);Check(loaded.FfmpegExecutable==p.FfmpegExecutable&&loaded.FfmpegLoudness&&loaded.TargetLufs== -16,"FFmpeg-Einstellungen gespeichert und geladen");Check(Rules.Sorted(p).FfmpegLoudness,"FFmpeg-Einstellung nach automatischer Aufteilung erhalten");
  p.Normalize=true;bool both=false;try{Ffmpeg.Validate(p,true);}catch{both=true;}Check(both,"Gleichzeitige MP3Gain- und FFmpeg-Anpassung verhindert");p.Normalize=false;
  bool cancelled=false;string cancel=Path.Combine(work,"abbruch.mp3");int calls=0;try{Ffmpeg.Write(source,"Thema",cancel,p,()=>{if(++calls>=3)throw new OperationCanceledException();});}catch(OperationCanceledException){cancelled=true;}Check(cancelled&&!File.Exists(cancel),"Abbruch erzeugt keine fertige MP3");
  string silent=Path.Combine(work,"Stille.wav");Ffmpeg.Run(p.FfmpegExecutable,"-v error -nostdin -y -f lavfi -i anullsrc=r=44100:cl=stereo -t 4 "+Ffmpeg.Quote(silent),()=>{},out err);bool silence=false;try{Ffmpeg.Write(new Song{Source=silent,Artist="Test",Title="Stille"},"Thema",Path.Combine(work,"still.mp3"),p,()=>{});}catch(Exception e){silence=e.Message.Contains("messbar");}Check(silence,"Nicht messbare Stille mit verständlicher Meldung abgefangen");
  string mp3=Directory.GetFiles(Path.GetFullPath(args[1]),"*.mp3",SearchOption.AllDirectories).First();var original=Tags.Read(mp3);string recoded=Path.Combine(work,"mp3-normalisiert.mp3");Ffmpeg.Write(new Song{Source=mp3,Artist=original.Artist,Title=original.Title},"Thema",recoded,p,()=>{});Check(Tags.Read(recoded).Other.SelectMany(x=>x).SequenceEqual(original.Other.SelectMany(x=>x)),"Eingebettete Cover und weitere ID3-Frames aus MP3 erhalten");
  Console.WriteLine("BESTANDEN: "+tests+" FFmpeg-Prüfungen (Linux-Programme)");
 }
}
