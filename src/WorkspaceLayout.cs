using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
namespace Musikbox {
 [DataContract] public class PaneLayout {
  [DataMember] public string Name;
  [DataMember] public bool Floating,Hidden;
  [DataMember] public int X,Y,Width,Height;
 }
 [DataContract] public class WorkspaceLayout {
  [DataMember] public int Version=2;
  [DataMember] public bool Magnetic=true;
  [DataMember] public int SidebarWidth=320,SidebarTop=310,FolderHeight=120;
  [DataMember] public List<PaneLayout> Panes=new List<PaneLayout>();
  public static WorkspaceLayout Load(string path){if(!File.Exists(path))return null;if(new FileInfo(path).Length>65536)throw new Exception("Fensterdatei zu groß.");using(var stream=File.OpenRead(path)){var result=(WorkspaceLayout)new DataContractJsonSerializer(typeof(WorkspaceLayout)).ReadObject(stream);if(result==null||result.Panes==null||result.Panes.Count>20||result.Panes.Any(p=>p==null))throw new Exception("Ungültige Fensteranordnung.");return result;}}
  public static void Save(string path,WorkspaceLayout value){path=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{using(var stream=File.Create(temp))new DataContractJsonSerializer(typeof(WorkspaceLayout)).WriteObject(stream,value);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
  public static Rectangle VisibleBounds(Rectangle desired,Rectangle[] screens){if(screens==null||screens.Length==0)return new Rectangle(0,0,390,400);var area=screens.FirstOrDefault(s=>s.IntersectsWith(desired));if(area.Width==0)area=screens[0];int width=Math.Max(320,Math.Min(desired.Width,area.Width)),height=Math.Max(220,Math.Min(desired.Height,area.Height));return new Rectangle(Math.Max(area.Left,Math.Min(desired.X,area.Right-width)),Math.Max(area.Top,Math.Min(desired.Y,area.Bottom-height)),width,height);}
 }
}
