using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Musikbox;
class ModernUiTests {
 static int count;static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Console.WriteLine("OK "+message);}
 static Control Find(Control c,Type type){if(type.IsInstanceOfType(c))return c;foreach(Control child in c.Controls){var found=Find(child,type);if(found!=null)return found;}return null;}
 static void Mouse(Control c,string name,int x,int y){typeof(ResizeGrip).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{new MouseEventArgs(MouseButtons.Left,1,x,y,0)});}
 [STAThread]static void Main(string[] args){Directory.CreateDirectory(args[0]);Application.EnableVisualStyles();
  var a=new Rectangle(100,100,300,300);var b=new Rectangle(400,110,200,200);var screen=new Rectangle(0,0,1400,1000);
  Check(MagnetGeometry.Joined(a,b),"Berührende Fenster gelten als verbunden");Check(!MagnetGeometry.Joined(a,new Rectangle(402,399,200,200)),"Bloße Eckberührung verbindet keine Gruppe");
  Check(MagnetGeometry.Snap(new Rectangle(103,105,290,280),new[]{b},screen)==new Point(7,5),"Nachbarfenster rastet horizontal und vertikal ein");
  Check(MagnetGeometry.Snap(new Rectangle(7,8,300,300),new Rectangle[0],screen)==new Point(-7,-8),"Bildschirmrand rastet ein");Check(MagnetGeometry.Snap(a,new Rectangle[0],screen)==Point.Empty,"Entfernte Kanten verursachen keinen Sprung");
  using(var view=new MainView(Path.Combine(args[0],"layout.json"))){new Controller(view,Path.Combine(args[0],"backups"));
   using(var one=new ModernForm{StartPosition=FormStartPosition.Manual,Bounds=a})using(var two=new ModernForm{StartPosition=FormStartPosition.Manual,Bounds=b})using(var far=new ModernForm{StartPosition=FormStartPosition.Manual,Bounds=new Rectangle(900,700,200,200)}){
    view.Magnets.Register(one);view.Magnets.Register(two);view.Magnets.Register(far);one.Show();two.Show();far.Show();Application.DoEvents();view.Magnets.Begin(one,false);view.Magnets.Move(one,new Point(150,160));view.Magnets.End();Check(one.Location==new Point(150,160)&&two.Location==new Point(450,170),"Verbundene Fenster bewegen sich mit unverändertem Abstand");Check(far.Location==new Point(900,700),"Unverbundene Fenster bleiben stehen");
    view.Magnets.Begin(one,true);view.Magnets.Move(one,new Point(200,230));view.Magnets.End();Check(two.Location==new Point(450,170)&&one.Location==new Point(200,230),"Umschalt-Ziehen löst das einzelne Fenster");
    one.Bounds=a;two.Bounds=b;view.Magnets.Enabled=false;view.Magnets.Begin(one,false);view.Magnets.Move(one,new Point(130,150));view.Magnets.End();Check(two.Bounds==b&&one.Location==new Point(130,150),"Abgeschaltete Magnetfunktion bewegt nur das gewählte Fenster");
   }
   view.Show();Application.DoEvents();
   var menu=view.Tools.Items.OfType<ToolStripDropDownButton>().Single(m=>m.Text=="Ansicht");var toggles=menu.DropDownItems.OfType<ToolStripMenuItem>().Take(5).ToArray();Check(toggles.Length==5&&toggles.Select(t=>t.Text).SequenceEqual(view.Panes.Select(p=>p.Caption)),"Ansicht bietet alle fünf Module in einer gemeinsamen Übersicht");
   Check(toggles.Take(4).All(t=>t.Checked)&&!toggles[4].Checked,"Häkchen zeigen die anfangs eingeblendeten Module");toggles[0].PerformClick();Check(view.FolderPane.IsHidden&&!toggles[0].Checked,"Klick schaltet Fenster aus und entfernt sein Häkchen");toggles[0].PerformClick();Check(!view.FolderPane.IsHidden&&toggles[0].Checked,"Erneuter Klick schaltet Fenster samt Häkchen ein");
   view.HandleHistoryKey(Keys.Alt|Keys.D5,view);Check(!view.PlayerPane.IsHidden&&toggles[4].Checked,"Alt+5 schaltet Player ein");view.PlayerPane.HidePane();Check(!toggles[4].Checked,"Schließen am Modul aktualisiert auch Ansicht-Häkchen");
   Check(view.Songs.BackgroundColor==ModernTheme.Navy&&view.Songs.DefaultCellStyle.ForeColor==Color.White,"Titelliste hat dunkelblauen Hintergrund und lesbare helle Schrift");
   view.TagPane.Float(view);Application.DoEvents();var floating=view.TagPane.FloatingWindow;var grip=Find(floating,typeof(ResizeGrip));Check(grip!=null&&grip.Visible&&grip.Cursor==Cursors.SizeNWSE,"Sichtbarer diagonaler Ziehgriff im freien Modul");
   Check(grip.PointToScreen(Point.Empty).X>floating.Right-35&&grip.PointToScreen(Point.Empty).Y>floating.Bottom-35,"Ziehgriff liegt unten rechts im Rahmen");
   Size original=floating.Size;Point location=floating.Location;Mouse(grip,"OnMouseDown",8,8);Mouse(grip,"OnMouseMove",88,58);Mouse(grip,"OnMouseUp",8,8);Check(floating.Size==new Size(original.Width+80,original.Height+50)&&floating.Location==location,"Ziehen vergrößert Breite und Höhe gleichzeitig ohne Verschieben");
   Mouse(grip,"OnMouseDown",8,8);Mouse(grip,"OnMouseMove",-2000,-2000);Mouse(grip,"OnMouseUp",8,8);Check(floating.Size==floating.MinimumSize,"Ziehen respektiert die bedienbare Mindestgröße");
   Size stopped=floating.Size;Mouse(grip,"OnMouseMove",100,100);Check(floating.Size==stopped,"Nach Loslassen endet die Größenänderung");
   floating.Size=new Size(460,520);view.ArtworkPane.Float(view);view.ArtworkPane.FloatingWindow.Bounds=new Rectangle(850,120,400,400);floating.Location=new Point(370,120);view.Magnets.Enabled=false;view.SavePaneLayout();Check(!WorkspaceLayout.Load(Path.Combine(args[0],"layout.json")).Magnetic,"Magnet-Einstellung wird gespeichert");Application.DoEvents();view.Magnets.Enabled=true;view.LoadPaneLayout();Application.DoEvents();Check(!view.Magnets.Enabled,"Magnet-Einstellung wird wiederhergestellt");
   menu.HideDropDown();Application.DoEvents();
   using(var shot=new Bitmap(1400,1000)){using(var graphics=Graphics.FromImage(shot))graphics.CopyFromScreen(0,0,0,0,shot.Size);shot.Save(Path.Combine(args[0],"modern-ui.png"));}
   view.ResetPaneLayout();view.Close();
  }
  Console.WriteLine("BESTANDEN: "+count+" Modern-Oberflächen-/Magnet-/Ziehgriffprüfungen unter Mono");
 }
}
