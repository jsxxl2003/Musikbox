using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Runtime.InteropServices;
namespace Musikbox {
 public interface IPlaybackCommands {string Execute(string command);}
 public sealed class WindowsPlaybackCommands:IPlaybackCommands {
  [DllImport("winmm.dll",CharSet=CharSet.Unicode)]static extern int mciSendString(string command,StringBuilder buffer,int length,IntPtr callback);
  public string Execute(string command){if(Environment.OSVersion.Platform!=PlatformID.Win32NT)throw new PlatformNotSupportedException("Die Audiowiedergabe benötigt Windows. Bitte auf deinem Windows-Rechner testen.");var buffer=new StringBuilder(256);int error=mciSendString(command,buffer,buffer.Capacity,IntPtr.Zero);if(error!=0)throw new Exception("Windows-Wiedergabefehler ("+error+"). Bei nicht unterstützten Formaten bitte eine MP3-Arbeitskopie vorbereiten.");return buffer.ToString().Trim();}
 }
 public enum PlaybackMode {Stopped,Playing,Paused,Finished}
 public class PlaybackInfo {public PlaybackMode Mode;public string File;public int Position,Duration;public bool Open;}
 public class Playback:IDisposable {
  readonly IPlaybackCommands commands;readonly string alias="musikbox"+Guid.NewGuid().ToString("N");
  double volume=.8;string file;bool open,disposed;int duration,position;PlaybackMode mode;
  public Playback(IPlaybackCommands commands=null){this.commands=commands??new WindowsPlaybackCommands();}
  string Send(string command){return commands.Execute(command.Replace("{device}",alias));}
  void Alive(){if(disposed)throw new ObjectDisposedException("Playback");}
  static int Milliseconds(string text){int value;if(!Int32.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out value)||value<0)throw new Exception("Windows hat eine ungültige Wiedergabezeit geliefert.");return value;}
  public PlaybackInfo Read(){Alive();if(open){string status=Send("status {device} mode");position=Math.Min(duration,Milliseconds(Send("status {device} position")));if(status=="playing")mode=PlaybackMode.Playing;else if(status=="paused")mode=PlaybackMode.Paused;else if(status=="stopped"){if(mode!=PlaybackMode.Paused)mode=duration>0&&position>=duration-1?PlaybackMode.Finished:PlaybackMode.Stopped;}else throw new Exception("Der Windows-Player ist nicht bereit ("+status+").");}return new PlaybackInfo{File=file,Open=open,Duration=duration,Position=position,Mode=mode};}
  public void Play(string path){
   Alive();if(String.IsNullOrWhiteSpace(path)||path.IndexOf('"')>=0||path.IndexOf('\r')>=0||path.IndexOf('\n')>=0)throw new Exception("Ungültiger Dateiname.");string full=Path.GetFullPath(path);if(!System.IO.File.Exists(full))throw new Exception("Die Audiodatei fehlt. Bitte über Projekt → Dateien wiederfinden neu verbinden.");
   if(open&&String.Equals(file,full,StringComparison.OrdinalIgnoreCase)){var state=Read();if(state.Mode==PlaybackMode.Playing)return;if(state.Mode==PlaybackMode.Finished){Send("seek {device} to start");position=0;}Send("play {device}");mode=PlaybackMode.Playing;return;}
   Stop();file=full;duration=position=0;
   try{commands.Execute("open \""+full+"\" type mpegvideo alias "+alias);open=true;Send("set {device} time format milliseconds");duration=Milliseconds(Send("status {device} length"));SetVolume(volume);Send("play {device}");mode=PlaybackMode.Playing;}
   catch{try{Stop();}catch{}file=null;duration=position=0;throw;}
  }
  public void SetVolume(double value){volume=Math.Max(0,Math.Min(1,value));if(open)Send("setaudio {device} volume to "+((int)(volume*1000)).ToString(CultureInfo.InvariantCulture));}
  public void Pause(){Alive();if(!open||Read().Mode!=PlaybackMode.Playing)return;Send("pause {device}");mode=PlaybackMode.Paused;}
  public void Seek(double fraction){Alive();if(Double.IsNaN(fraction)||Double.IsInfinity(fraction))throw new ArgumentOutOfRangeException("fraction");if(!open||duration<=0)return;var state=Read();int target=(int)(Math.Max(0,Math.Min(1,fraction))*Math.Max(0,duration-1));Send("seek {device} to "+target.ToString(CultureInfo.InvariantCulture));position=target;if(state.Mode==PlaybackMode.Playing){Send("play {device}");mode=PlaybackMode.Playing;}else mode=PlaybackMode.Paused;}
  public void Stop(){if(!open){position=0;mode=PlaybackMode.Stopped;return;}try{Send("stop {device}");}finally{try{Send("close {device}");}finally{open=false;position=0;mode=PlaybackMode.Stopped;}}}
  public void Clear(){try{Stop();}finally{file=null;duration=position=0;}}
  public void Dispose(){if(disposed)return;try{Clear();}catch{}disposed=true;}
 }
}
