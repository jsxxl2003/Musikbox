using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;

namespace Musikbox {
 // Keeps the Whisper model loaded between microphone searches.
 internal static class PersistentWhisper {
  static readonly SemaphoreSlim mutex=new SemaphoreSlim(1,1);
  static WhisperFactory factory;
  static string loadedPath;
  public static async Task<string> Transcribe(byte[] pcm,string prompt,CancellationToken token){
   await mutex.WaitAsync(token).ConfigureAwait(false);
   try{
    string model=VoiceService.ModelPath;
    if(factory==null||!String.Equals(loadedPath,model,StringComparison.OrdinalIgnoreCase)){
     factory?.Dispose();
     factory=WhisperFactory.FromPath(model);
     loadedPath=model;
    }
    using(var processor=factory.CreateBuilder().WithLanguage("de").Build())
    using(var wav=new MemoryStream()){
     using(var writer=new NAudio.Wave.WaveFileWriter(new NonClosingStream(wav),new NAudio.Wave.WaveFormat(16000,16,1))){
      writer.Write(pcm,0,pcm.Length);
     }
     wav.Position=0;
     var result=new StringBuilder();
     await foreach(var segment in processor.ProcessAsync(wav,token).ConfigureAwait(false))
      result.Append(segment.Text).Append(' ');
     return result.ToString().Trim();
    }
   }finally{mutex.Release();}
  }
  sealed class NonClosingStream:Stream {
   readonly Stream inner;public NonClosingStream(Stream s){inner=s;}
   public override bool CanRead=>inner.CanRead;public override bool CanSeek=>inner.CanSeek;public override bool CanWrite=>inner.CanWrite;
   public override long Length=>inner.Length;public override long Position{get=>inner.Position;set=>inner.Position=value;}
   public override void Flush()=>inner.Flush();public override int Read(byte[] b,int o,int c)=>inner.Read(b,o,c);
   public override long Seek(long o,SeekOrigin origin)=>inner.Seek(o,origin);public override void SetLength(long v)=>inner.SetLength(v);
   public override void Write(byte[] b,int o,int c)=>inner.Write(b,o,c);
  }
 }
}
