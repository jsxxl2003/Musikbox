using System;using System.IO;using System.Text;
namespace Musikbox {
 public static class ProgramErrors {
  public static void Record(Exception error){try{Directory.CreateDirectory(PortablePaths.Data);string file=Path.Combine(PortablePaths.Data,"Programmfehler.log");if(File.Exists(file)&&new FileInfo(file).Length>1024*1024){string previous=File.ReadAllText(file);File.WriteAllText(file,previous.Substring(Math.Max(0,previous.Length-256*1024)),Encoding.UTF8);}File.AppendAllText(file,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+" · "+AppIdentity.Version+Environment.NewLine+error+Environment.NewLine+Environment.NewLine,Encoding.UTF8);}catch{}}
 }
}
