using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Text.RegularExpressions;
namespace Musikbox {
 public static class StudioPreferences {
 static string FilePath(string key){if(!Regex.IsMatch(key,@"^[A-Za-z0-9_-]+$"))throw new ArgumentException("Ungültiger Einstellungsschlüssel.");return Path.Combine(PortablePaths.Data,key+".txt");}
 public static string Get(string key,string fallback=""){try{string p=FilePath(key);return File.Exists(p)?File.ReadAllText(p):fallback;}catch{return fallback;}}
 public static void Set(string key,string value){Directory.CreateDirectory(PortablePaths.Data);string p=FilePath(key),temp=p+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllText(temp,value??"");File.Move(temp,p,true);}finally{if(File.Exists(temp))File.Delete(temp);}}
 public static string Folder(string key){string p=Get("Ziel-"+key);if(Directory.Exists(p))return p;p=Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);return Directory.Exists(p)?p:PortablePaths.Data;}
 public static void SaveFolder(string key,string path){if(!Directory.Exists(path))throw new DirectoryNotFoundException(path);Set("Ziel-"+key,Path.GetFullPath(path));}
 }
 public static class OutputFormatSettings {
 public static readonly string[] Available={"artist","title","album","genre","track","year","_directory","acoustid_id","ordner"};
 public static string Pattern{get{return StudioPreferences.Get("Tag-Ausgabeformat","%artist% - %title%");}set{StudioPreferences.Set("Tag-Ausgabeformat",value);}}
 public static string[] Keys(string pattern){return Regex.Matches(pattern??"",@"%([^%]+)%").Cast<Match>().Select(m=>m.Groups[1].Value.ToLowerInvariant()).Where(Available.Contains).Distinct().ToArray();}
 }
}