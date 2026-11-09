using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Musikbox {
 // Browser-based Google speech recognition, with a one-time localhost callback.
 // Chrome owns the microphone; recognized text returns directly to the Studio.
 internal sealed class ChromeSpeechBridge : IDisposable {
  HttpListener listener; CancellationTokenSource cancel; bool active;
  public bool Active=>active;
  public event Action<string> Recognized;
  public event Action<string> Failed;
  public event Action<string> Status;
  public void Start(){
   Stop();
   int port;using(var tcp=new TcpListener(IPAddress.Loopback,0)){tcp.Start();port=((IPEndPoint)tcp.LocalEndpoint).Port;}
   var server=new HttpListener();string prefix="http://127.0.0.1:"+port+"/";server.Prefixes.Add(prefix);
   try{server.Start();}catch(Exception ex){server.Close();Failed?.Invoke("Lokaler Chrome-Sprachtest kann nicht gestartet werden: "+ex.Message);return;}
   listener=server;cancel=new CancellationTokenSource();active=true;
   string token=Guid.NewGuid().ToString("N");
   _=Serve(server,token,cancel.Token);
   try{
    var psi=new ProcessStartInfo{FileName="chrome.exe",UseShellExecute=true,Arguments="--new-window \""+prefix+"?key="+token+"\""};
    Process.Start(psi);
    Status?.Invoke("Google-Spracherkennung in Chrome geöffnet · bitte sprechen");
   }catch(Exception ex){Stop();Failed?.Invoke("Google Chrome konnte nicht geöffnet werden: "+ex.Message);}
  }
  async Task Serve(HttpListener server,string token,CancellationToken ct){
   try{
    while(!ct.IsCancellationRequested&&server.IsListening){
     HttpListenerContext ctx;
     try{ctx=await server.GetContextAsync().ConfigureAwait(false);}catch{break;}
     var req=ctx.Request;var res=ctx.Response;
     try{
      if(req.Url.AbsolutePath=="/"&&req.HttpMethod=="GET"&&req.QueryString["key"]==token){
       var data=Encoding.UTF8.GetBytes(Page(token));res.ContentType="text/html; charset=utf-8";res.ContentLength64=data.Length;await res.OutputStream.WriteAsync(data,0,data.Length).ConfigureAwait(false);
      }else if(req.Url.AbsolutePath=="/result"&&req.HttpMethod=="POST"&&req.QueryString["key"]==token&&req.ContentLength64>=0&&req.ContentLength64<4096){
       string result;using(var reader=new StreamReader(req.InputStream,Encoding.UTF8))result=await reader.ReadToEndAsync().ConfigureAwait(false);
       var data=Encoding.UTF8.GetBytes("OK");res.ContentLength64=data.Length;await res.OutputStream.WriteAsync(data,0,data.Length).ConfigureAwait(false);
       if(!String.IsNullOrWhiteSpace(result)){Recognized?.Invoke(result.Trim());Stop();return;}
      }else{res.StatusCode=403;}
     }catch(Exception ex){Debug.WriteLine(ex);}finally{try{res.Close();}catch{}}
    }
   }finally{if(active&&ReferenceEquals(listener,server))Stop();}
  }
  static string Page(string key)=>@"<!doctype html><html lang='de'><meta charset='utf-8'><title>Musikbox Sprachsuche</title>
<style>body{font:18px system-ui;background:#191823;color:#eee;padding:25px}button{background:#8c3dd7;color:white;border:0;padding:14px;border-radius:8px;font:inherit}</style>
<h2>Musikbox · Google-Spracherkennung</h2><p id='status'>Mikrofon wird gestartet …</p><p id='words'></p><button id='stop'>Abbrechen</button>
<script>
const Speech=window.SpeechRecognition||window.webkitSpeechRecognition;
const status=document.getElementById('status'),words=document.getElementById('words');
let recognition;
document.getElementById('stop').onclick=()=>{if(recognition)recognition.abort();status.textContent='Abgebrochen';window.close()};
if(!Speech){status.textContent='Chrome unterstützt hier keine Web-Spracherkennung.'}
else{
 recognition=new Speech();recognition.lang='de-DE';recognition.continuous=false;recognition.interimResults=true;recognition.maxAlternatives=5;
 recognition.onresult=async e=>{const item=e.results[e.results.length-1];words.textContent=item[0].transcript;
  if(item.isFinal){recognition.stop();status.textContent='Erkannt – Übertragung an Musikbox …';
   try{await fetch('/result?key="+key+@"',{method:'POST',body:item[0].transcript});status.textContent='Fertig. Fenster kann geschlossen werden.';window.close()}catch(err){status.textContent='Übertragung fehlgeschlagen: '+err.message}}
 };
 recognition.onerror=e=>status.textContent='Spracherkennung: '+e.error;
 recognition.onend=()=>{if(!words.textContent)status.textContent='Keine Sprache erkannt – erneut starten';};
 try{recognition.start();status.textContent='Bitte jetzt sprechen …'}catch(e){status.textContent=e.message}
}
</script></html>";
  public void Stop(){active=false;try{cancel?.Cancel();}catch{}try{listener?.Stop();listener?.Close();}catch{}listener=null;cancel?.Dispose();cancel=null;}
  public void Dispose()=>Stop();
 }
}
