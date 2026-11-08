using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Musikbox;

// Regression for Windows ButtonBase's opaque paint buffer. Render onto deliberately
// dirty surfaces: every repaint must be independent of the previous frame/sibling.
class StudioPaintTests {
 static int count;
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);count++;Console.WriteLine("OK "+label);}
 static void Paint(Control c,Graphics g){
  var args=new PaintEventArgs(g,c.ClientRectangle);
  if(!(c is StudioActionButton))typeof(Control).GetMethod("OnPaintBackground",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{args});
  typeof(Control).GetMethod("OnPaint",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{args});
 }
 static Bitmap Render(Control c,Color dirty){var b=new Bitmap(c.Width,c.Height);using(var g=Graphics.FromImage(b)){g.Clear(dirty);Paint(c,g);}return b;}
 static bool Same(Bitmap a,Bitmap b){for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++)if(a.GetPixel(x,y)!=b.GetPixel(x,y))return false;return true;}
 static void Stable(Control c,string label){using(var a=Render(c,Color.Magenta))using(var b=Render(c,Color.Lime))Check(Same(a,b),label+": kompletter Hintergrund unabhaengig vom alten Puffer");}
 [STAThread]static void Main(){
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  using(var host=new Form{BackColor=ModernTheme.Background,ClientSize=new Size(650,650)})
  using(var panel=new StudioPanel{Bounds=new Rectangle(15,15,610,600),Fill=Color.FromArgb(31,25,46),End=Color.FromArgb(20,19,33)}){
   host.Controls.Add(panel);host.Show();Application.DoEvents();
   var button=new StudioActionButton{Navigation=true,Text="Bibliothek",Symbol="♫",Active=true,Bounds=new Rectangle(12,15,240,50)};panel.Controls.Add(button);
   Stable(button,"Aktive Navigation");
   using(var old=Render(button,Color.Magenta)){
    button.Active=false;button.Text="Tag-Editor";button.Symbol="◇";
    using(var g=Graphics.FromImage(old))Paint(button,g);
    using(var fresh=Render(button,Color.Lime))Check(Same(old,fresh),"Auswahl- und Textwechsel hinterlassen keine alte Bibliothek-Beschriftung");
   }
   Stable(button,"Inaktive Navigation");
   using(var nav=Render(button,Color.Magenta))using(var full=Render(panel,Color.Lime))Check(nav.GetPixel(5,5)==full.GetPixel(button.Left+5,button.Top+5),"Navigation setzt den Verlauf an der richtigen Elternposition fort");
   string[] icons={"Search","Clear","Mic","Previous","Next","Volume"};
   foreach(string icon in icons){button.Navigation=false;button.Text="";button.Symbol=icon;button.Transparent=true;Stable(button,icon);}
   button.Primary=true;button.Symbol="Play";Stable(button,"Runder Playerknopf");
   button.Primary=false;button.Transparent=false;button.Text="Speichern";button.Symbol=null;Stable(button,"Metadaten speichern");
   button.Enabled=false;Stable(button,"Deaktivierter Knopf");
   button.Enabled=true;
   using(var before=Render(button,Color.Lime)){
    typeof(Control).GetMethod("OnMouseEnter",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(button,new object[]{EventArgs.Empty});
    using(var g=Graphics.FromImage(before))Paint(button,g);
    typeof(Control).GetMethod("OnMouseLeave",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(button,new object[]{EventArgs.Empty});
    using(var g=Graphics.FromImage(before))Paint(button,g);
    using(var fresh=Render(button,Color.Magenta))Check(Same(before,fresh),"Hover und Verlassen hinterlassen keine Farb- oder Textreste");
   }
   var nested=new StudioPanel{Bounds=new Rectangle(270,30,300,140),Gradient=false,Fill=Color.FromArgb(34,32,53)};panel.Controls.Add(nested);
   var search=new StudioActionButton{Bounds=new Rectangle(10,10,40,40),Transparent=true,Symbol="Search"};nested.Controls.Add(search);Stable(search,"Verschachtelte Suchleiste");
   var art=new StudioArt{Bounds=new Rectangle(20,200,80,80),Disc=true};panel.Controls.Add(art);Stable(art,"Cover- und Logoecken");
   var slider=new StudioSlider{Bounds=new Rectangle(20,300,250,30),Value=3800};panel.Controls.Add(slider);Stable(slider,"Player-Regler");
   using(var bitmap=new Bitmap(100,100))using(var g=Graphics.FromImage(bitmap)){
    g.Clear(Color.Magenta);g.SetClip(new Rectangle(20,20,20,20));Paint(art,g);
    Check(bitmap.GetPixel(10,10).ToArgb()==Color.Magenta.ToArgb(),"Cover wahrt den vorgegebenen Ausschnitt bei Teil-Neuzeichnung");
   }
   for(int i=0;i<20;i++){button.Text=i%2==0?"Bibliothek":"Audio-Werkzeuge";button.Active=i%2==0;button.SetBounds(12,15,230+i,50);button.Refresh();Application.DoEvents();}
   Stable(button,"Wiederholtes Umschalten und Groessenaenderung");
   host.Close();
  }
  Console.WriteLine("BESTANDEN: "+count+" Zeichenpruefungen; keine echte Windows-Abnahme.");
 }
}
