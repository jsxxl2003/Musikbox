using System;
using System.IO;
using System.Linq;
using Musikbox;
using MusikboxTests;
class PlaybackTests {
 static int tests;static void Check(bool yes,string text){if(!yes)throw new Exception(text);tests++;Console.WriteLine("OK "+text);}
 static bool Fails(Action action){try{action();return false;}catch{return true;}}
 static void Main(string[] args){string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);string file=Path.Combine(root,"Titel ä {device}.mp3"),other=Path.Combine(root,"anderer Titel.mp3");byte[] bytes=new byte[]{1,2,3};File.WriteAllBytes(file,bytes);File.WriteAllBytes(other,bytes);var device=new PlaybackTestDevice();
  using(var player=new Playback(device)){
   Check(player.Read().Mode==PlaybackMode.Stopped&&!player.Read().Open,"Player startet ohne offenes Gerät");player.Stop();Check(device.Commands.Count==0,"Stopp ohne geladenes Gerät ruft kein natives Windows auf");player.Play(file);var state=player.Read();Check(state.Mode==PlaybackMode.Playing&&state.Duration==240000&&state.File==file,"Start öffnet Titel und liest Gesamtdauer");
   Check(device.Commands[0].Contains("\""+file+"\"")&&device.Commands.Any(c=>c.Contains("time format milliseconds")),"Pfad mit Umlaut, Leerzeichen und Platzhalter bleibt unverändert; Zeitformat gesetzt");
   device.Position=45000;player.Pause();Check(player.Read().Mode==PlaybackMode.Paused&&player.Read().Position==45000,"Pause erhält die Titelposition");int opens=device.OpenCount;player.Play(file);Check(device.OpenCount==opens&&player.Read().Mode==PlaybackMode.Playing&&device.Position==45000,"Fortsetzen spielt denselben Titel ohne Neuöffnung weiter");
   player.Play(file);Check(device.OpenCount==opens&&device.Position==45000,"Erneutes Start bei laufender Wiedergabe beginnt nicht ungewollt von vorn");
   player.Seek(0.5);Check(device.Position==119999&&player.Read().Mode==PlaybackMode.Playing,"Positionswechsel bei Wiedergabe setzt Abspielen fort");player.Pause();int plays=device.Commands.Count(c=>c.StartsWith("play "));player.Seek(0.25);Check(player.Read().Mode==PlaybackMode.Paused&&device.Commands.Count(c=>c.StartsWith("play "))==plays,"Positionswechsel in Pause bleibt lautlos pausiert");player.Play(file);Check(player.Read().Mode==PlaybackMode.Playing&&device.Position==59999,"Start nach pausiertem Positionswechsel setzt an neuer Position fort");
   player.Seek(2);Check(device.Position==239999,"Seek am Ende bleibt im gültigen Zeitbereich");player.Seek(-1);Check(device.Position==0,"Negative Positionswahl wird auf Anfang begrenzt");Check(Fails(()=>player.Seek(Double.NaN)),"Ungültiger Seek-Wert wird abgewiesen");
   device.Position=240000;device.Mode="stopped";Check(player.Read().Mode==PlaybackMode.Finished,"Natürliches Titelende erkannt");player.Play(file);Check(device.Position==0&&player.Read().Mode==PlaybackMode.Playing,"Start nach Titelende spielt wieder vom Anfang");
   Check(Fails(()=>player.Play(Path.Combine(root,"fehlt.mp3")))&&player.Read().Mode==PlaybackMode.Playing,"Fehlende neue Datei zerstört laufende Wiedergabe nicht");Check(Fails(()=>player.Play("bad\"name.mp3"))&&player.Read().Mode==PlaybackMode.Playing,"Ungültiger Dateiname vor Gerätewechsel abgewiesen");
   player.Play(other);Check(device.OpenCount==opens+1&&device.Commands.Any(c=>c.StartsWith("close "))&&player.Read().File==other,"Titelwechsel schließt altes Gerät vor neuer Wiedergabe");player.Stop();state=player.Read();Check(!state.Open&&state.Mode==PlaybackMode.Stopped&&state.Position==0&&state.Duration==240000,"Stopp gibt Datei frei und setzt Position auf Anfang");player.Play(other);Check(player.Read().Position==0&&device.OpenCount==opens+2,"Nach Stopp lässt sich der geladene Titel neu starten");player.Clear();Check(player.Read().File==null&&player.Read().Duration==0&&!player.Read().Open,"Projektwechsel kann geladenen Titel vollständig freigeben");
  }
  var failing=new PlaybackTestDevice{Length="ungueltig"};using(var player=new Playback(failing)){Check(Fails(()=>player.Play(file))&&!player.Read().Open&&failing.Commands.Any(c=>c.StartsWith("close ")),"Ungültige Zeitantwort schließt geöffnetes Gerät");}
  failing=new PlaybackTestDevice{FailContains="time format"};using(var player=new Playback(failing)){Check(Fails(()=>player.Play(file))&&!player.Read().Open&&failing.Commands.Any(c=>c.StartsWith("close ")),"Initialisierungsfehler gibt Dateihandle wieder frei");}
  failing=new PlaybackTestDevice();var disposed=new Playback(failing);disposed.Play(file);disposed.Dispose();disposed.Dispose();Check(failing.Commands.Count(c=>c.StartsWith("close "))==1&&Fails(()=>disposed.Read()),"Schließen ist wiederholbar und verhindert weitere Wiedergabeabfragen");
  Check(File.ReadAllBytes(file).SequenceEqual(bytes)&&File.ReadAllBytes(other).SequenceEqual(bytes),"Player verändert keine Musikdatei");Check(PlayerView.Format(3661000)=="61:01"&&PlayerView.Format(-1)=="0:00","Zeitanzeige unterstützt lange Titel und Anfang");
  Console.WriteLine("BESTANDEN: "+tests+" Player-Prüfungen mit simuliertem Windows-Gerät; kein realer Audiotest");
 }
}
