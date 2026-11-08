using System;using System.IO;using System.Linq;using System.Text;using System.Threading;using System.Threading.Tasks;using System.Collections.Generic;using System.Diagnostics;
#if NET8_0_OR_GREATER
using NAudio.Wave;
#endif
namespace Musikbox {
 public interface IVoiceInput:IDisposable{event Action<byte[],int> DataAvailable;event Action<Exception> Stopped;void Start();}
 public sealed class VoiceService:IDisposable {
  public event Action<string[]> Recognized;public event Action<string> StateChanged,Failed;
  public event Action<int,string[]> RecognitionCompleted;public event Action<int,bool,string> StatusUpdated;public event Action<int,string> ErrorUpdated;
  public string Language="auto";readonly object gate=new object();readonly Func<IVoiceInput> inputFactory;readonly Func<byte[],string,CancellationToken,Task<string>> transcriber;readonly bool validateFiles;int generation;bool disposed;Session current;
  sealed class Session{public readonly CancellationTokenSource Cancel=new CancellationTokenSource();public readonly SpeechCapture Capture=new SpeechCapture();public IVoiceInput Input;public Task Starting=Task.CompletedTask,Worker=Task.CompletedTask;public bool Processing;public string Prompt;public int Generation;public DateTime LastLevel;}
  public VoiceService(Func<IVoiceInput> inputFactory=null,Func<byte[],string,CancellationToken,Task<string>> transcriber=null,bool validateFiles=true){this.inputFactory=inputFactory??(()=>new WindowsVoiceInput());this.transcriber=transcriber??TranscribeAsync;this.validateFiles=validateFiles;}
  public bool Active{get{lock(gate)return current!=null&&!disposed;}}public int Generation{get{return Volatile.Read(ref generation);}}
  bool IsCurrent(Session s){lock(gate)return !disposed&&current==s&&!s.Cancel.IsCancellationRequested;}
  public Task Start(IEnumerable<Song> songs){Stop();Session session;lock(gate){if(disposed)return Task.CompletedTask;session=new Session{Generation=++generation,LastLevel=DateTime.UtcNow};session.Prompt=String.Join(", ",songs.Select(s=>SearchText.Clean(s.Artist)).Where(s=>s.Length>0).Distinct().Take(24));if(session.Prompt.Length>500)session.Prompt=session.Prompt.Substring(0,500);current=session;
    session.Starting=Task.Run(()=>{session.Cancel.Token.ThrowIfCancellationRequested();if(validateFiles&&(!File.Exists(ModelPath)||!File.Exists(ExecutablePath)))throw new IOException("Whisper-Programm oder Sprachmodell fehlt. Bitte das vollständige portable Paket entpacken.");var input=inputFactory();session.Input=input;session.Cancel.Token.ThrowIfCancellationRequested();input.DataAvailable+=(bytes,count)=>Receive(session,bytes,count);input.Stopped+=error=>InputStopped(session,error);input.Start();session.Cancel.Token.ThrowIfCancellationRequested();});}
   Notify(session.Generation,true,"Mikrofon wird gestartet …");return CompleteStart(session);
  }
  async Task CompleteStart(Session s){try{await s.Starting.ConfigureAwait(false);if(IsCurrent(s))Notify(s.Generation,true,"Mikrofon aktiv · bitte sprechen · X beendet die Aufnahme");}catch(OperationCanceledException){}catch(Exception ex){Fail(s,ex.Message);}}
  void Receive(Session s,byte[] bytes,int count){byte[] clip;lock(gate){if(disposed||current!=s||s.Processing)return;clip=s.Capture.Feed(bytes,count);if(clip!=null){s.Processing=true;s.Worker=Task.Run(()=>Recognize(s,clip));return;}}
   if((DateTime.UtcNow-s.LastLevel).TotalSeconds>3){s.LastLevel=DateTime.UtcNow;Notify(s.Generation,true,"Mikrofon aktiv · bitte sprechen · X beendet die Aufnahme");}
  }
  async Task Recognize(Session s,byte[] clip){try{s.Cancel.Token.ThrowIfCancellationRequested();Notify(s.Generation,true,"Sprache wird lokal erkannt …");string text=await transcriber(clip,s.Prompt,s.Cancel.Token).ConfigureAwait(false);if(!IsCurrent(s))return;if(!String.IsNullOrWhiteSpace(text)){RecognitionCompleted?.Invoke(s.Generation,new[]{text});if(IsCurrent(s))Recognized?.Invoke(new[]{text});Notify(s.Generation,true,"Erkannt: "+text+" · erneut sprechen oder X zum Beenden");}else Notify(s.Generation,true,"Nicht verstanden · bitte erneut sprechen");}
   catch(OperationCanceledException){}catch(Exception ex){Fail(s,ex.Message);}finally{lock(gate){if(current==s)s.Processing=false;}}
  }
  void InputStopped(Session s,Exception error){if(IsCurrent(s))Fail(s,error==null?"Die Aufnahme wurde beendet. Mikrofon bitte erneut starten.":error.Message);}
  void Fail(Session s,string message){int g=End(s);if(g<0||disposed||g!=Generation)return;Notify(g,false,message);ErrorUpdated?.Invoke(g,message);if(g==Generation)Failed?.Invoke(message);}
  void Notify(int g,bool active,string message){if(g!=Generation||disposed)return;StatusUpdated?.Invoke(g,active,message);StateChanged?.Invoke(message);}
  public void Stop(){int g=End(null);if(!disposed)Notify(g,false,"Mikrofon ausgeschaltet · Suchtext bleibt erhalten");}
  int End(Session expected){Session old;int closedGeneration;lock(gate){if(expected!=null&&current!=expected)return -1;old=current;current=null;closedGeneration=++generation;}if(old!=null){old.Cancel.Cancel();_ = Cleanup(old);}return closedGeneration;}
  static async Task Cleanup(Session s){try{try{await s.Starting.ConfigureAwait(false);}catch{}if(s.Input!=null)await Task.Run(()=>s.Input.Dispose()).ConfigureAwait(false);try{await s.Worker.ConfigureAwait(false);}catch{}}catch{}finally{s.Capture.Dispose();s.Cancel.Dispose();}}
  public void Dispose(){if(disposed)return;disposed=true;End(null);}
#if NET8_0_OR_GREATER
  public static string[] InputDevices(){return Enumerable.Range(0,WaveIn.DeviceCount).Select(i=>WaveIn.GetCapabilities(i).ProductName).ToArray();}
  public static string ExecutablePath{get{return PortablePaths.Tool(Path.Combine("whisper",System.Runtime.Intrinsics.X86.Avx2.IsSupported?"whisper-cli.exe":"whisper-cli-compatible.exe"));}}
  public static string ModelPath{get{string preference=Path.Combine(PortablePaths.Data,"Sprachmodell.txt");string mode=File.Exists(preference)?File.ReadAllText(preference).Trim():"auto";string small=PortablePaths.Tool(Path.Combine("whisper","ggml-small.bin"));return mode=="small"&&File.Exists(small)?small:PortablePaths.Tool(Path.Combine("whisper","ggml-base.bin"));}}
  public static string Transcribe(byte[] pcm,string prompt,CancellationToken token){return TranscribeAsync(pcm,prompt,token).GetAwaiter().GetResult();}
  static async Task<string> TranscribeAsync(byte[] pcm,string prompt,CancellationToken token){string root=Path.Combine(Path.GetTempPath(),"Musikbox-Sprache-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);try{string wav=Path.Combine(root,"aufnahme.wav"),output=Path.Combine(root,"erkannt");using(var writer=new WaveFileWriter(wav,new WaveFormat(16000,16,1)))writer.Write(pcm,0,pcm.Length);var start=new ProcessStartInfo{FileName=ExecutablePath,WorkingDirectory=Path.GetDirectoryName(ExecutablePath)};string[] args={"-m",ModelPath,"-f",wav,"-l","auto","-t",Math.Max(1,Math.Min(4,Environment.ProcessorCount-1)).ToString(),"-bs","1","-bo","1","-nf","-nt","-otxt","-of",output,"-ng","--prompt",prompt??""};foreach(string arg in args)start.ArgumentList.Add(arg);await CancellableProcess.RunAsync(start,token,TimeSpan.FromSeconds(45)).ConfigureAwait(false);token.ThrowIfCancellationRequested();return File.Exists(output+".txt")?System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(output+".txt",Encoding.UTF8),@"\[[^\]]*\]"," ").Trim():"";}finally{try{Directory.Delete(root,true);}catch{}}}
#else
  public static string[] InputDevices(){return new string[0];}public static string ExecutablePath{get{return "";}}public static string ModelPath{get{return "";}}static Task<string> TranscribeAsync(byte[] p,string q,CancellationToken t){throw new NotSupportedException();}
#endif
 }
 internal sealed class WindowsVoiceInput:IVoiceInput {
  public event Action<byte[],int> DataAvailable;public event Action<Exception> Stopped;
#if NET8_0_OR_GREATER
  WaveInEvent mic;bool disposed;
  public void Start(){if(WaveIn.DeviceCount==0)throw new IOException("Kein Mikrofon verfügbar. Eingabegerät und Mikrofonberechtigung prüfen.");string preference=Path.Combine(PortablePaths.Data,"Mikrofon.txt"),name=File.Exists(preference)?File.ReadAllText(preference):"";int preferred=-1;for(int i=0;i<WaveIn.DeviceCount;i++)if(WaveIn.GetCapabilities(i).ProductName==name)preferred=i;Exception last=null;foreach(int device in new[]{preferred,-1}.Concat(Enumerable.Range(0,WaveIn.DeviceCount)).Distinct()){var input=new WaveInEvent{DeviceNumber=device,WaveFormat=new WaveFormat(16000,16,1),BufferMilliseconds=100};try{mic=input;input.DataAvailable+=(s,e)=>{if(!disposed&&mic==input)DataAvailable?.Invoke(e.Buffer,e.BytesRecorded);};input.RecordingStopped+=(s,e)=>{if(!disposed&&mic==input)Stopped?.Invoke(e.Exception);};input.StartRecording();return;}catch(Exception ex){mic=null;last=ex;try{input.Dispose();}catch{}}}throw new IOException("Mikrofon konnte nicht geöffnet werden. Windows-Mikrofonzugriff und Eingabegerät prüfen. "+last?.Message);}
  public void Dispose(){disposed=true;var old=mic;mic=null;if(old!=null){try{old.StopRecording();}catch{}old.Dispose();}}
#else
  public void Start(){throw new NotSupportedException();}public void Dispose(){}
#endif
 }
}
