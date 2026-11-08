using System;
using System.IO;
namespace Musikbox {
 public static class BundledFpcalc {
  public const string Version="1.6.2";
  public static string PathIn(string directory){return PortablePaths.ToolIn(directory,Path.Combine("fpcalc","fpcalc.exe"));}
  public static string Resolve(string configured,string directory=null){
   string bundled=PathIn(directory??PortablePaths.Root);
   if(File.Exists(bundled))return bundled;
   // Alte Installationen dürfen ihr bisheriges Werkzeug weiter nutzen.
   return !String.IsNullOrWhiteSpace(configured)&&File.Exists(configured)?configured:null;
  }
 }
}
