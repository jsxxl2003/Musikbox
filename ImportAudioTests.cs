using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Musikbox;
class ImportAudioTests {
 static int tests;
 static void Check(bool value,string message){if(!value)throw new Exception(message);tests++;Console.WriteLine("OK "+message);}
 static void Main(string[] args){
  string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);string err;
  var p=new Project{FfmpegExecutable="/usr/bin/ffmpeg",FfmpegLoudness=true,TargetLufs=-16,Mp3Bitrate=192};
  string sources=Path.Combine(root,"Quellen");Directory.CreateDirectory(sources);string source=Path.Combine(sources,"Quelle.mp3");
  Ffmpeg.Run(p.FfmpegExecutable,"-v error -nostdin -y -f lavfi -i "+Ffmpeg.Quote("sine=frequency=440:duration=6")+" -c:a libmp3lame -id3v2_version 3 "+Ffmpeg.Quote(source),()=>{},out err);
  var song=new Song{Source=source,Artist="Test",Title="Lautheit",Album="Thema"};byte[] original=File.ReadAllBytes(source);string cache=Path.Combine(root,"cache");int done=0;
  ImportAudio.Prepare(p,new[]{song},()=>{},(n,total,text)=>done=n,cache);
  Check(done==1&&ImportAudio.Ready(p,song),"FFmpeg-Lautheit beim Import automatisch vorbereitet");
  Check(song.Source==source&&original.SequenceEqual(File.ReadAllBytes(source)),"Quelle unverändert und Quellverweis erhalten");
  Check(File.Exists(song.PreparedSource)&&Tags.Read(song.PreparedSource).Title=="Lautheit","Arbeitskopie mit korrekten Tags gespeichert");
  string before=song.PreparedSource;int calls=0;ImportAudio.Prepare(p,new[]{song},()=>{},(n,total,text)=>calls++,cache);
  Check(before==song.PreparedSource&&calls==0,"Identische Einstellungen verwenden vorhandene Kopie ohne erneute Verarbeitung");
  p.TargetLufs=-18;Check(!ImportAudio.Ready(p,song),"Anderer Zielpegel macht alte Arbeitskopie unpassend");p.TargetLufs=-16;
  p.Mp3Bitrate=128;Check(!ImportAudio.Ready(p,song),"Andere Bitrate wird berücksichtigt");p.Mp3Bitrate=192;
  p.VariableBitrate=true;Check(!ImportAudio.Ready(p,song),"CBR-/VBR-Wechsel wird berücksichtigt");p.VariableBitrate=false;
  p.Folders.Add(new BoxFolder{Name="000 Thema",Theme="Thema",Songs=new List<Song>{song}});
  song.Artist="Bearbeitet";song.Title="Neuer Titel";
  string exported=Exporter.Export(p,root,()=>{},(n,text)=>{});string copy=Directory.GetFiles(exported,"*.mp3",SearchOption.AllDirectories).Single();
  var old=Tags.Read(song.PreparedSource);var output=Tags.Read(copy);
  byte[] preparedAudio=File.ReadAllBytes(song.PreparedSource).Skip((int)old.AudioStart).Take((int)(old.AudioEnd-old.AudioStart)).ToArray();
  byte[] exportAudio=File.ReadAllBytes(copy).Skip((int)output.AudioStart).Take((int)(output.AudioEnd-output.AudioStart)).ToArray();
  Check(preparedAudio.SequenceEqual(exportAudio),"Export übernimmt vorbereitete Audiodaten ohne doppelte Lautheitsanpassung");
  Check(output.Artist=="Bearbeitet"&&output.Title=="Neuer Titel","Spätere Tag-Bearbeitung auch mit Arbeitskopie korrekt exportiert");
  string saved=Path.Combine(root,"projekt.json");Storage.Save(p,saved);var loaded=Storage.Load(saved);
  Check(ImportAudio.Ready(loaded,loaded.Folders[0].Songs[0]),"Arbeitskopie und Profil über Projektspeicherung erhalten");
  DateTime stamp=File.GetLastWriteTimeUtc(source);File.SetLastWriteTimeUtc(source,stamp.AddSeconds(2));Check(!ImportAudio.Ready(p,song),"Geänderte Quelle verwirft zwischengespeichertes Profil");File.SetLastWriteTimeUtc(source,stamp);
  File.Delete(song.PreparedSource);Check(!ImportAudio.Ready(p,song),"Gelöschte Arbeitskopie führt zur erneuten Verarbeitung der Originalquelle");
  var cancelSong=new Song{Source=source,Artist="Abbruch",Title="Test",Album="Thema"};bool cancelled=false;
  try{ImportAudio.Prepare(p,new[]{cancelSong},()=>{},(n,total,text)=>{throw new OperationCanceledException();},cache);}catch(OperationCanceledException){cancelled=true;}
  Check(cancelled&&cancelSong.PreparedSource==null&&!Directory.GetDirectories(cache,".unvollstaendig-*").Any(),"Abbruch entfernt unvollständige Dateien und verändert Song nicht");
  var unconfigured=new Project{Normalize=true};bool blocked=false;try{ImportAudio.Prepare(unconfigured,new[]{cancelSong},()=>{},(n,total,text)=>{},cache);}catch{blocked=true;}
  Check(blocked&&cancelSong.PreparedSource==null,"Fehlendes priorisiertes MP3Gain wird klar gemeldet, kein stiller Wechsel");
  var unchanged=new Project();ImportAudio.Prepare(unchanged,new[]{cancelSong},()=>{},(n,total,text)=>{throw new Exception("unerwartet");},cache);
  Check(cancelSong.PreparedSource==null,"Lautstärke unverändert bleibt ohne Bearbeitung");
  var invalid=song.Copy();invalid.PreparedSource=null;invalid.Source=Path.Combine(root,"defekt.mp3");File.WriteAllBytes(invalid.Source,new byte[256]);
  var valid=new Song{Source=source,Artist="Test",Title="Erster",Album="Thema"};bool failed=false;
  try{ImportAudio.Prepare(p,new[]{valid,invalid},()=>{},(n,total,text)=>{},cache);}catch{failed=true;}
  Check(failed&&valid.PreparedSource==null&&invalid.PreparedSource==null&&!Directory.GetDirectories(cache,".unvollstaendig-*").Any(),"Fehler nach erstem Titel verwirft gesamte vorbereitete Teilmenge");
  Check(original.SequenceEqual(File.ReadAllBytes(source)),"Original nach Verarbeitung und Fehlerfällen bytegleich");
  string stub=Path.Combine(root,"mp3gain-test.sh"),arguments=Path.Combine(root,"gain-args.txt");
  File.WriteAllText(stub,"#!/bin/sh\nprintf '%s' \"$*\" > '"+arguments+"'\nprintf 'input.mp3\\t0\\t2.0\\t100\\n'\n");
  Ffmpeg.Run("/bin/chmod","+x "+Ffmpeg.Quote(stub),()=>{},out err);
  var gainProject=new Project{Normalize=true,GainExecutable=stub,TargetDb=91};var gainSong=new Song{Source=source,Artist="Gain",Title="Test",Album="Thema"};
  ImportAudio.Prepare(gainProject,new[]{gainSong},()=>{},(n,total,text)=>{},cache);
  Check(ImportAudio.Ready(gainProject,gainSong)&&File.ReadAllText(arguments).Contains("-r -k -o -s r -d 2.0 input.mp3"),"MP3Gain-Aufruf übernimmt Zielpegel und Clipping-Schutz (Testprogramm)");
  gainProject.TargetDb=89;Check(!ImportAudio.Ready(gainProject,gainSong),"Geänderter MP3Gain-Zielpegel verwirft altes Profil");
  gainProject.FfmpegLoudness=true;bool dualBlocked=false;try{ImportAudio.Prepare(gainProject,new[]{gainSong},()=>{},(n,total,text)=>{},cache);}catch{dualBlocked=true;}
  Check(dualBlocked,"Zwei gleichzeitige Lautstärkeverfahren werden abgelehnt");
  Console.WriteLine("BESTANDEN: "+tests+" Import-Audioprüfungen (echtes FFmpeg unter Linux; MP3Gain-Testprogramm, keine echte Gain-Messung)");
 }
}
