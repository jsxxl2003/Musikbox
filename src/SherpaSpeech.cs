using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SherpaOnnx;

namespace Musikbox {
 internal static class SherpaSpeech {
  // Multilingual Whisper ONNX, executed by sherpa-onnx (no browser, no cloud).
  public static string DirectoryPath=>PortablePaths.Tool("sherpa-onnx");
  static string Encoder=>Path.Combine(DirectoryPath,"base-encoder.int8.onnx");
  static string Decoder=>Path.Combine(DirectoryPath,"base-decoder.int8.onnx");
  static string Tokens=>Path.Combine(DirectoryPath,"base-tokens.txt");
  public static bool Available=>File.Exists(Encoder)&&File.Exists(Decoder)&&File.Exists(Tokens);
  public static Task<string> Transcribe(byte[] pcm,string prompt,CancellationToken cancellation){
   if(!Available)throw new FileNotFoundException("Sherpa-ONNX-Sprachmodell fehlt im Ordner Werkzeuge\\sherpa-onnx.");
   return Task.Run(()=>{
    cancellation.ThrowIfCancellationRequested();
    var config=new OfflineRecognizerConfig();
    config.ModelConfig.Whisper.Encoder=Encoder;
    config.ModelConfig.Whisper.Decoder=Decoder;
    config.ModelConfig.Tokens=Tokens;
    config.ModelConfig.NumThreads=Math.Max(1,Math.Min(4,Environment.ProcessorCount));
    config.ModelConfig.Debug=0;
    using(var recognizer=new OfflineRecognizer(config))
    using(var stream=recognizer.CreateStream()){
     var samples=new float[pcm.Length/2];
     for(int i=0;i<samples.Length;i++)samples[i]=BitConverter.ToInt16(pcm,i*2)/32768f;
     cancellation.ThrowIfCancellationRequested();
     stream.AcceptWaveform(16000,samples);
     recognizer.Decode(stream);
     cancellation.ThrowIfCancellationRequested();
     return stream.Result.Text?.Trim()??"";
    }
   },cancellation);
  }
 }
}
