using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
namespace Musikbox {
 [DataContract] public class CoverText {
  [DataMember] public string Text="{Thema}";
  [DataMember] public string Font="Segoe UI";
  [DataMember] public float Size=90,X=100,Y=400;
  [DataMember] public int Color=System.Drawing.Color.White.ToArgb();
  [DataMember] public bool Bold=true;
  [DataMember] public int Alignment;
 }
 [DataContract] public class CoverDesign {
  [DataMember] public int Background=Color.FromArgb(12,35,92).ToArgb(),EndColor=Color.FromArgb(14,95,112).ToArgb();
  [DataMember] public bool Gradient=true,Border=true;
  [DataMember] public string ImagePath;
  [DataMember] public List<CoverText> Texts=new List<CoverText>();
  public static CoverDesign Default(){return new CoverDesign{Texts=new List<CoverText>{new CoverText{Text="Meine Musikbox",Size=70,Y=100,X=50,Alignment=1},new CoverText{Text="{Thema}",Size=85,Y=430,X=50,Alignment=1},new CoverText{Text="{Jahr}",Size=170,Y=800,X=50,Alignment=1}}};}
 }
 public static class CoverArt {
  public static CoverDesign Copy(CoverDesign source){using(var stream=new MemoryStream()){var json=new DataContractJsonSerializer(typeof(CoverDesign));json.WriteObject(stream,source);stream.Position=0;return (CoverDesign)json.ReadObject(stream);}}
  public static string Expand(string text,string name,string theme){var year=Regex.Match(theme??"",@"\b(?:19|20)\d{2}\b");var number=Regex.Match(name??"",@"^\d{3}");return (text??"").Replace("{Thema}",theme??"").Replace("{Jahr}",year.Value).Replace("{Nummer}",number.Value);}
  public static Bitmap Render(CoverDesign design,string name,string theme,int size=1200){
   var bitmap=new Bitmap(size,size);try{using(var g=Graphics.FromImage(bitmap)){
    g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;g.ScaleTransform(size/1200f,size/1200f);
    if(design.Gradient)using(var brush=new LinearGradientBrush(new Rectangle(0,0,1200,1200),Color.FromArgb(design.Background),Color.FromArgb(design.EndColor),45))g.FillRectangle(brush,0,0,1200,1200);else g.Clear(Color.FromArgb(design.Background));
    if(!String.IsNullOrEmpty(design.ImagePath))using(var image=Image.FromFile(design.ImagePath)){float scale=Math.Max(1200f/image.Width,1200f/image.Height);float w=image.Width*scale,h=image.Height*scale;g.DrawImage(image,(1200-w)/2,(1200-h)/2,w,h);}
    if(design.Border)using(var pen=new Pen(Color.FromArgb(225,255,255,255),5))g.DrawRectangle(pen,25,25,1150,1150);
    foreach(var t in design.Texts){if(t.Size<10||t.Size>350)throw new Exception("Schriftgröße muss zwischen 10 und 350 liegen.");using(var font=new Font(t.Font,t.Size,t.Bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))using(var brush=new SolidBrush(Color.FromArgb(t.Color)))using(var format=new StringFormat{Alignment=(StringAlignment)Math.Max(0,Math.Min(2,t.Alignment))})g.DrawString(Expand(t.Text,name,theme),font,brush,new RectangleF(t.X,t.Y,1100,1200-t.Y),format);}
   }return bitmap;}catch{bitmap.Dispose();throw;}
  }
  public static string Save(CoverDesign design,string name,string theme){string root=Path.Combine(PortablePaths.Data,"Cover");Directory.CreateDirectory(root);string path=Path.Combine(root,Guid.NewGuid().ToString("N")+".jpg");using(var image=Render(design,name,theme))image.Save(path,ImageFormat.Jpeg);return path;}
 }
}
