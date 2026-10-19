using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
namespace Musikbox {
 public static class Gain {
  public static void Validate(Project p) {
   if(!p.Normalize)return;
   if(String.IsNullOrWhiteSpace(p.GainExecutable)||!File.Exists(p.GainExecutable))throw new Exception("Bitte über 'Lautstärke' zuerst das Kommandozeilenprogramm mp3gain.exe auswählen.");
   if(Double.IsNaN(p.TargetDb)||Double.IsInfinity(p.TargetDb)||p.TargetDb<80||p.TargetDb>95)throw new Exception("Der MP3Gain-Zielpegel muss zwischen 80 und 95 dB liegen.");
  }
  public static string Arguments(double target) {return "-r -k -o -s r -d "+(target-89).ToString("0.0",CultureInfo.InvariantCulture)+" input.mp3";}
  public static string Apply(string file,Project project,Action checkCancel) {
   Validate(project);checkCancel();
   // Der alte Windows-Prozess erhält nur ASCII-Dateinamen. Der Arbeitsordner
   // wird über die Unicode-API gesetzt; auch Quellnamen mit Umlauten bleiben nutzbar.
   string work=Path.Combine(Path.GetTempPath(),"MusikboxGain-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(work);
   string input=Path.Combine(work,"input.mp3");File.Copy(file,input);
   try {
    var start=new ProcessStartInfo{FileName=Path.GetFullPath(project.GainExecutable),Arguments=Arguments(project.TargetDb),WorkingDirectory=work,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,RedirectStandardInput=true};
    using(var process=new Process{StartInfo=start}) {
     process.Start();process.StandardInput.Close();var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();var clock=Stopwatch.StartNew();
     try {while(!process.WaitForExit(100)){checkCancel();if(clock.Elapsed.TotalMinutes>10)throw new Exception("MP3Gain-Zeitlimit für einen Titel überschritten.");}checkCancel();}
     catch {try{if(!process.HasExited){process.Kill();process.WaitForExit();}}catch{}throw;}
     Task.WaitAll(stdout,stderr);string output=stdout.Result,err=stderr.Result;
     // Ältere MP3Gain-Versionen haben abweichende Exit-Codes. Deshalb ist eine
     // auswertbare Track-Ergebniszeile zusätzlich erforderlich.
     bool result=false;
     foreach(string line in output.Split(new char[]{'\n'})) {
      var fields=line.Trim().Split(new char[]{'\t'});double gain;
      if(fields.Length>=4&&fields[0].Trim(new char[]{'"'}).Equals("input.mp3",StringComparison.OrdinalIgnoreCase)&&Double.TryParse(fields[2],NumberStyles.Float,CultureInfo.InvariantCulture,out gain))result=true;
     }
     bool error=System.Text.RegularExpressions.Regex.IsMatch(output+"\n"+err,@"\b(error|can't|cannot|could not|failed|not enough samples)\b",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
     if(process.ExitCode<0||process.ExitCode>1||!result||error)throw new Exception("MP3Gain konnte diesen Titel nicht verarbeiten.\r\n"+Limit(output+"\r\n"+err));
     var old=Tags.Read(file);var updated=Tags.Read(input);
     if(old.Artist!=updated.Artist||old.Title!=updated.Title||old.Album!=updated.Album)throw new Exception("MP3Gain hat unerwartet die Titel-Tags verändert.");
     checkCancel();File.Copy(input,file,true);
     return output.Trim();
    }
   } finally {foreach(var temp in Directory.GetFiles(work))File.Delete(temp);Directory.Delete(work);}
  }
  static string Limit(string text){return text.Length>1800?text.Substring(text.Length-1800):text;}
 }
}
