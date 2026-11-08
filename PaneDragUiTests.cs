using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Musikbox;
class PaneDragUiTests {
 static int count;static void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;Console.WriteLine("OK "+text);}
 static void Mouse(Control control,string method,MouseButtons button,Point screen){var local=control.PointToClient(screen);typeof(Control).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(control,new object[]{new MouseEventArgs(button,1,local.X,local.Y,0)});}
 static Control Title(Control parent){return parent.Controls.OfType<MetallicHeader>().Single().Controls.OfType<Label>().Single();}
 static void Call(Controller controller,string method,params object[] args){typeof(Controller).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,args);}
 [STAThread]static void Main(string[] args){Directory.CreateDirectory(args[0]);Application.EnableVisualStyles();using(var view=new MainView(Path.Combine(args[0],"layout.json"))){var controller=new Controller(view,Path.Combine(args[0],"backups"));var project=new Project();var folder=new BoxFolder{Name="000 Thema",Theme="Thema"};folder.Songs.Add(new Song{Source="nicht-vorhanden.mp3",Artist="Interpret",Title="Vorher",Album="Thema"});project.Folders.Add(folder);Call(controller,"ReplaceProject",project,null,true);view.Show();Application.DoEvents();view.Magnets.Enabled=false;
  Check(view.Panes.All(p=>!p.Controls.OfType<MetallicHeader>().Single().Controls.OfType<Button>().Any(b=>b.Text=="Fenster"||b.Text=="Zeigen")),"Alle Modulkopfzeilen kommen ohne Fenster-/Zeigen-Schaltfläche aus");
  Check(Title(view.FolderPane).Cursor==Cursors.Default,"Kopfzeile zeigt normalen Mauszeiger");
  Point edgePoint=view.FolderPane.PointToScreen(new Point(1,30));Mouse(view.FolderPane,"OnMouseMove",MouseButtons.None,edgePoint);Check(view.FolderPane.Cursor==Cursors.SizeWE,"Größenzeiger nur am Rand");
  typeof(Control).GetMethod("OnMouseLeave",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view.FolderPane,new object[]{EventArgs.Empty});Check(view.FolderPane.Cursor==Cursors.Default,"Rand verlassen stellt normalen Mauszeiger wieder her");
  foreach(int edge in new[]{1,2,4,8,10}){
   var pane=view.FolderPane;int w=pane.Width,h=pane.Height;
   Point local=new Point(edge==1?1:edge==2||edge==10?pane.Width-2:pane.Width/2,edge==4?1:edge==8||edge==10?pane.Height-2:pane.Height/2);
   Point from=pane.PointToScreen(local);Point to=new Point(from.X+(edge==1?-15:edge==2||edge==10?15:0),from.Y+(edge==4?-12:edge==8||edge==10?12:0));
   Mouse(pane,"OnMouseDown",MouseButtons.Left,from);Mouse(pane,"OnMouseMove",MouseButtons.Left,to);Mouse(pane,"OnMouseUp",MouseButtons.Left,to);Application.DoEvents();
   Check(!pane.IsFloating&&((edge&3)==0||pane.Width>w)&&((edge&12)==0||pane.Height>h),"Angedockter Rand/Ecke "+edge+" ändert Größe ohne Abtrennen");
  }
  view.PlayerPane.ShowPane();Application.DoEvents();int ph=view.PlayerPane.Height;Point pp=view.PlayerPane.PointToScreen(new Point(50,1));Mouse(view.PlayerPane,"OnMouseDown",MouseButtons.Left,pp);Mouse(view.PlayerPane,"OnMouseMove",MouseButtons.Left,new Point(pp.X,pp.Y-20));Mouse(view.PlayerPane,"OnMouseUp",MouseButtons.Left,new Point(pp.X,pp.Y-20));Check(view.PlayerPane.Height>ph,"Player lässt sich am oberen Rand vergrößern");int resized=view.PlayerPane.Height;view.PlayerPane.HidePane();view.PlayerPane.ShowPane();Check(view.PlayerPane.Height==resized,"Playergröße bleibt beim Aus-/Einblenden erhalten");
  var title=Title(view.TagPane);Point start=title.PointToScreen(new Point(30,8));Cursor.Position=start;Mouse(title,"OnMouseDown",MouseButtons.Left,start);Mouse(title,"OnMouseUp",MouseButtons.Left,start);Check(!view.TagPane.IsFloating,"Ein einfacher Linksklick löst das Modul noch nicht");
  Mouse(title,"OnMouseDown",MouseButtons.Right,start);Cursor.Position=new Point(start.X+100,start.Y+50);Mouse(title,"OnMouseMove",MouseButtons.Right,Cursor.Position);Mouse(title,"OnMouseUp",MouseButtons.Right,Cursor.Position);Check(!view.TagPane.IsFloating,"Rechtsziehen löst kein Modul");
  Cursor.Position=start;Mouse(title,"OnMouseDown",MouseButtons.Left,start);title.Capture=false;Cursor.Position=new Point(start.X+100,start.Y+50);Mouse(title,"OnMouseMove",MouseButtons.None,Cursor.Position);Check(!view.TagPane.IsFloating,"Verlorene Mauserfassung bricht einen noch nicht begonnenen Zug ab");
  view.TagEditor.Title.Text="Beim Ziehen übernommen";
  foreach(var pane in new[]{view.TagPane,view.FolderPane,view.SongPane,view.ArtworkPane,view.PlayerPane}){
   pane.ShowPane();Application.DoEvents();var handle=pane==view.FolderPane?(Control)pane.Controls.OfType<MetallicHeader>().Single():Title(pane);Point pressed=handle.PointToScreen(new Point(Math.Max(30,handle.Width-45),8));Cursor.Position=pressed;Mouse(handle,"OnMouseDown",MouseButtons.Left,pressed);Cursor.Position=new Point(pressed.X+100,pressed.Y+60);Mouse(handle,"OnMouseMove",MouseButtons.Left,Cursor.Position);Check(pane.IsFloating&&pane.FloatingWindow.Visible&&Title(pane.FloatingWindow).Capture,"Links halten und ziehen löst "+pane.Caption+" und übergibt den laufenden Zug");
   var window=pane.FloatingWindow;var free=Title(window);Point origin=window.Location;Cursor.Position=new Point(Cursor.Position.X-50,Cursor.Position.Y-40);Mouse(free,"OnMouseMove",MouseButtons.Left,Cursor.Position);Check(window.Location==new Point(origin.X-50,origin.Y-40),"Weiterziehen verschiebt "+pane.Caption+" ohne erneuten Mausklick");Mouse(free,"OnMouseUp",MouseButtons.Left,Cursor.Position);Point stopped=window.Location;Cursor.Position=new Point(Cursor.Position.X+30,Cursor.Position.Y+20);Mouse(free,"OnMouseMove",MouseButtons.None,Cursor.Position);Check(window.Location==stopped&&!free.Capture,"Loslassen beendet den Zug für "+pane.Caption);pane.DockBack();Application.DoEvents();
  }
  Check(project.Folders[0].Songs[0].Title=="Beim Ziehen übernommen","Abtrennen übernimmt die offene Tag-Eingabe ohne Datenverlust");
  Check(view.Folders.FindForm()==view&&view.TagEditor.FindForm()==view,"Andocken setzt dieselben Bearbeitungsfelder zurück");Call(controller,"ReplaceProject",new Project(),null,true);view.Close();
 }Console.WriteLine("BESTANDEN: "+count+" direkte Modulziehprüfungen unter Mono");}
}
