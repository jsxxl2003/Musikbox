using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using Musikbox;
class FingerprintTests {
 static int tests;
 static void Check(bool value,string message){if(!value)throw new Exception(message);tests++;Console.WriteLine("OK "+message);}
 static bool Fails(Action action){try{action();return false;}catch{return true;}}
 static void Main(string[] args){
  var work=Path.GetFullPath(args[0]);Directory.CreateDirectory(work);string exe=Path.GetFullPath(args[1]);
  var matches=AcoustId.Parse(Encoding.UTF8.GetBytes("{\"status\":\"ok\",\"results\":[{\"score\":0.7,\"recordings\":[{\"title\":\"Titel\",\"artists\":[{\"name\":\"Interpret\"}]}]},{\"score\":0.98,\"recordings\":[{\"title\":\"Titel\",\"artists\":[{\"name\":\"Interpret\"}]},{\"title\":\"Andere Aufnahme\",\"artists\":[{\"name\":\"A\",\"joinphrase\":\" feat. \"},{\"name\":\"B\"}]}]},{\"score\":1,\"recordings\":[null,{\"title\":\"Unvollständig\"}]},{\"score\":2,\"recordings\":[{\"title\":\"Ungültig\",\"artists\":[{\"name\":\"X\"}]}]}]}"));
  Check(matches.Count==2,"Mehrdeutige Treffer erhalten, Dubletten und unvollständige Metadaten entfernt");
  Check(matches[0].Score==0.98&&matches[0].Title=="Titel","Höchster Trefferwert für doppelte Aufnahme verwendet");
  Check(matches[1].Artist=="A feat. B","Mitwirkende mit Verknüpfung übernommen");
  Check(AcoustId.Parse(Encoding.UTF8.GetBytes("{\"status\":\"ok\",\"results\":[]}")).Count==0,"Keine Treffer ohne erfundene Angaben");
  Check(AcoustId.Parse(Encoding.UTF8.GetBytes("{\"status\":\"ok\"}")).Count==0,"Leere gültige Antwort behandelt");
  Check(Fails(()=>AcoustId.Parse(Encoding.UTF8.GetBytes("{}"))),"Unvollständige Dienstantwort abgewiesen");
  bool key=false;try{AcoustId.Parse(Encoding.UTF8.GetBytes("{\"status\":\"error\",\"error\":{\"code\":4,\"message\":\"Invalid API key\"}}"));}catch(Exception e){key=e.Message.Contains("Anwendungsschlüssel");}Check(key,"Ungültiger Anwendungsschlüssel verständlich gemeldet");
  Check(Fails(()=>AcoustId.Parse(Encoding.UTF8.GetBytes("not json"))),"Beschädigte Antwort abgewiesen");
  Check(Fails(()=>AcoustId.ParsePrint(Encoding.UTF8.GetBytes("{\"duration\":0,\"fingerprint\":\"ABC\"}"))),"Ungültige Dauer abgewiesen");
  Check(Fails(()=>AcoustId.ParsePrint(Encoding.UTF8.GetBytes("{\"duration\":10,\"fingerprint\":\"\"}"))),"Leerer Fingerabdruck abgewiesen");
  Check(Fails(()=>AcoustId.ParsePrint(Encoding.UTF8.GetBytes("{\"duration\":10,\"fingerprint\":\"bad&key=secret\"}"))),"Ungültige Fingerabdruckzeichen abgewiesen");
  Check(Fails(()=>new AcoustIdSettings{Executable=exe,ClientKey=" "}.Validate()),"Fehlender Schlüssel vor Netzzugriff gemeldet");
  Check(Fails(()=>new AcoustIdSettings{Executable=Path.Combine(work,"missing.exe"),ClientKey="x"}.Validate()),"Fehlendes Werkzeug vor Netzzugriff gemeldet");
  string source=Path.Combine(work,"unbekannt ä (1).wav"),err;
  Ffmpeg.Run("/usr/bin/ffmpeg","-hide_banner -v error -f lavfi -i "+Ffmpeg.Quote("sine=frequency=440:duration=150")+" -y "+Ffmpeg.Quote(source),()=>{},out err);
  byte[] original=File.ReadAllBytes(source);var p=AcoustId.Calculate(exe,source,CancellationToken.None);
  Check(p.Duration>=149.9&&p.Duration<=150.1,"Gesamtdauer 150 Sekunden trotz 120 Sekunden Fingerabdruck gemeldet");
  Check(p.Fingerprint.Length>20,"Echter Chromaprint-1.6.2-Fingerabdruck für generiertes Audiosignal");
  string renamed=Path.Combine(work,"Falscher Interpret - Falscher Titel.wav");File.Copy(source,renamed);
  Check(AcoustId.Calculate(exe,renamed,CancellationToken.None).Fingerprint==p.Fingerprint,"Falscher Dateiname beeinflusst Fingerabdruck nicht");
  string tagged=Path.Combine(work,"falsche-tags.wav");Ffmpeg.Run("/usr/bin/ffmpeg","-hide_banner -v error -i "+Ffmpeg.Quote(source)+" -c copy -metadata artist=Falsch -metadata title=AuchFalsch "+Ffmpeg.Quote(tagged),()=>{},out err);
  Check(AcoustId.Calculate(exe,tagged,CancellationToken.None).Fingerprint==p.Fingerprint,"Falsche Interpret- und Titel-Tags beeinflussen Fingerabdruck nicht");
  Check(original.SequenceEqual(File.ReadAllBytes(source)),"Original nach Fingerabdruck-Berechnung bytegleich");
  string body=AcoustId.Body(p,"test&key");Check(body.StartsWith("client=test%26key&duration=150&fingerprint=")&&body.Contains("meta=recordings+compress"),"POST-Felder kodiert und Gesamtdauer übermittelt");
  Check(!body.Contains("unbekannt")&&!body.Contains("Falscher")&&!body.Contains("source="),"Dateinamen und Musik-Tags nicht im Anfragekörper");
  string bad=Path.Combine(work,"defekt.wav");File.WriteAllText(bad,"kein Audio");Check(Fails(()=>AcoustId.Calculate(exe,bad,CancellationToken.None)),"Defekte Audiodatei ohne Treffer abgewiesen");
  using(var cancel=new CancellationTokenSource()){cancel.Cancel();bool canceled=false;try{AcoustId.Calculate(exe,source,cancel.Token);}catch(OperationCanceledException){canceled=true;}Check(canceled,"Abbruch vor Werkzeugstart respektiert");}
  var song=new Song{Source=source,Artist="Falsch",Title="Unbekannt",Album="Charts 1990",Artwork="eigenes.jpg",PreparedSource="arbeitskopie.mp3"};TagEditing.Apply(new[]{song},true,matches[0].Artist,true,matches[0].Title);
  Check(song.Artist=="Interpret"&&song.Title=="Titel"&&song.Album=="Charts 1990"&&song.Source==source&&song.Artwork=="eigenes.jpg"&&song.PreparedSource=="arbeitskopie.mp3","Übernahme ändert nur Interpret und Titel, erhält Thema, Quellen und Cover");
  Console.WriteLine("BESTANDEN: "+tests+" Fingerabdruck-Prüfungen; API-Antworten offline, kein Live-Zugang verwendet");
 }
}
