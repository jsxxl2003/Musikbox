using System;
using System.IO;
namespace Musikbox {
 public static class PortablePaths {
  public static string Root {get{return RootFor(AppDomain.CurrentDomain.BaseDirectory);}}
  public static string RootFor(string applicationDirectory){
   string path=Path.GetFullPath(applicationDirectory).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
   string parent=Path.GetDirectoryName(path);
   return String.Equals(Path.GetFileName(path),"Programm",StringComparison.OrdinalIgnoreCase)&&parent!=null&&File.Exists(Path.Combine(parent,"Musikbox-Werkstatt-Studio.exe"))?parent:path;
  }
  public static string Data {get{return Path.Combine(Root,"Daten");}}
  public static string Tool(string name){return ToolIn(Root,name);}
  public static string ToolIn(string root,string name){string folder=Directory.Exists(Path.Combine(root,"Werkzeuge"))?"Werkzeuge":"tools";return Path.Combine(root,folder,name);}
  public static string Document(string name){string folder=Path.Combine(Root,"Dokumentation");return Path.Combine(Directory.Exists(folder)?folder:Root,name);}
  public static void Initialize(){Directory.CreateDirectory(Data);string test=Path.Combine(Data,".schreibtest");File.WriteAllText(test,"");File.Delete(test);}
  public static void Configure(Project project){string ff=Tool(Path.Combine("ffmpeg","ffmpeg.exe")),gain=Tool(Path.Combine("mp3gain","mp3gain.exe"));if(String.IsNullOrWhiteSpace(project.FfmpegExecutable)||!File.Exists(project.FfmpegExecutable))project.FfmpegExecutable=ff;if(String.IsNullOrWhiteSpace(project.GainExecutable)||!File.Exists(project.GainExecutable))project.GainExecutable=gain;}
 }
}
