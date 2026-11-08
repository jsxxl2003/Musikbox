using System;using System.Diagnostics;using System.IO;using System.Text;using System.Threading;using System.Threading.Tasks;
namespace Musikbox {
 public static class CancellableProcess {
  public static async Task<string> RunAsync(ProcessStartInfo start,CancellationToken token,TimeSpan timeout){
   using(var limit=CancellationTokenSource.CreateLinkedTokenSource(token)){limit.CancelAfter(timeout);var ct=limit.Token;ct.ThrowIfCancellationRequested();start.UseShellExecute=false;start.CreateNoWindow=true;start.RedirectStandardOutput=start.RedirectStandardError=true;start.RedirectStandardInput=true;start.StandardOutputEncoding=start.StandardErrorEncoding=Encoding.UTF8;
    using(var process=new Process{StartInfo=start}){process.Start();process.StandardInput.Close();using(var registration=ct.Register(()=>Kill(process))){
     try{var output=process.StandardOutput.ReadToEndAsync(ct);var error=process.StandardError.ReadToEndAsync(ct);await process.WaitForExitAsync(ct).ConfigureAwait(false);await Task.WhenAll(output,error).ConfigureAwait(false);ct.ThrowIfCancellationRequested();if(process.ExitCode!=0)throw new IOException("Spracherkennung fehlgeschlagen:\r\n"+Tail(error.Result+"\r\n"+output.Result));return output.Result;}
     catch(OperationCanceledException){Kill(process);if(token.IsCancellationRequested)throw new OperationCanceledException(token);throw new TimeoutException("Die Spracherkennung antwortet nicht. Bitte erneut starten oder das schnelle Sprachmodell wählen.");}
     finally{Kill(process);}
    }}
   }
  }
  static string Tail(string text){return text.Length>3000?text.Substring(text.Length-3000):text;}
  static void Kill(Process p){try{if(!p.HasExited)p.Kill(true);}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}catch(NotSupportedException){try{p.Kill();}catch{}}}
 }
}
