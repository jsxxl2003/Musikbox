using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Musikbox;
class FolderCoverUiTests {
 static void Mouse(Control control,int x,int y){typeof(Control).GetMethod("OnMouseDown",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(control,new object[]{new MouseEventArgs(MouseButtons.Left,1,x,y,0)});}
 static int count;static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Console.WriteLine("OK "+message);}
 static void Call(object c,string method,params object[] args){c.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,args);}
 [STAThread]static void Main(string[] args){string root=args[0];Directory.CreateDirectory(root);Application.EnableVisualStyles();string dir=Path.Combine(root,"Thema");Directory.CreateDirectory(dir);string cover=Path.Combine(dir,"Folder.PNG");using(var bitmap=new Bitmap(80,80)){using(var g=Graphics.FromImage(bitmap))g.Clear(Color.Red);bitmap.Save(cover,System.Drawing.Imaging.ImageFormat.Png);}
  File.WriteAllText(Path.Combine(dir,"aaa.jpg"),"kein Bild");Check(FolderArtwork.Find(dir)==cover,"Folder.PNG hat Vorrang vor beliebigem Bildnamen");
  using(var view=new MainView()){var c=new Controller(view,Path.Combine(root,"backups"));var p=new Project();p.Folders.Add(new BoxFolder{Name="000 Thema",Theme="Thema",SourceDirectory=dir,Songs=new System.Collections.Generic.List<Song>{new Song{Source="a.mp3",Artist="Vorher",Title="Titel",Album="Thema"}}});p.Folders.Add(new BoxFolder{Name="001 Ohne",Theme="Ohne"});p.Folders.Add(new BoxFolder{Name="002 Defekt",Theme="Defekt",Cover=Path.Combine(dir,"aaa.jpg")});Call(c,"ReplaceProject",p,null,true);view.Show();Application.DoEvents();
   Check(view.Cover.Image!=null&&((Bitmap)view.Cover.Image).GetPixel(20,20).R>200,"Ordnerklick zeigt nachträglich gefundenes Ordnerbild auch in älterem Projekt");
   Check(p.Folders[0].Cover==null,"Anzeige ergänzt keine Projektzuordnung ohne ausdrückliche Auswahl");
   view.CoverTabs.SelectedIndex=1;view.Folders.SelectedIndex=1;Application.DoEvents();Check(view.CoverTabs.SelectedIndex==0&&view.Cover.Image==null&&view.CoverStatus.Text.Contains("Kein Ordnercover"),"Ordnerwechsel aktiviert Ordnercover und entfernt altes Bild mit verständlichem Hinweis");
   view.Folders.SelectedIndex=2;Application.DoEvents();Check(view.Cover.Image==null&&view.CoverStatus.Text.Contains("nicht lesbar"),"Beschädigtes Bild wird sichtbar gemeldet");
   view.Folders.SelectedIndex=0;view.Size=new Size(960,600);Application.DoEvents();Rectangle rect=view.RectangleToClient(view.Cover.RectangleToScreen(view.Cover.ClientRectangle));using(var shot=new Bitmap(view.Width,view.Height)){view.DrawToBitmap(shot,new Rectangle(Point.Empty,shot.Size));shot.Save(Path.Combine(root,"folder-before-check.png"));}Check(rect.Top>0&&rect.Bottom<view.ClientSize.Height&&rect.Height>=70,"Ordnercover bei Mindestfenstergröße vor dem Tag-Editor sichtbar: "+rect+" / Split "+view.SidebarSplit.SplitterDistance+" / Höhe "+view.SidebarSplit.Height);
   Check(!view.Controls.OfType<Label>().Any(label=>label.Text=="Musikbox Werkstatt"),"Doppelte Programmüberschrift entfernt; Name bleibt im Fensterrand");
   Check(view.Tools.Parent!=null&&view.Tools.FindForm()==view&&view.OnlineCover.Owner is ToolStripDropDown,"Eine gemeinsame Befehlszeile; Coveroption im Titelmenü erhalten");
   int width=view.WorkspaceSplit.SplitterDistance;view.WorkspaceSplit.SplitterDistance=width+30;Application.DoEvents();Check(view.WorkspaceSplit.SplitterDistance==width+30&&!view.WorkspaceSplit.IsSplitterFixed&&view.WorkspaceSplit.SplitterWidth==8,"Senkrechte Trennleiste macht linken Bereich breiter und schmaler");
   int before=view.SidebarSplit.SplitterDistance;view.SidebarSplit.SplitterDistance=before-25;Application.DoEvents();Check(view.SidebarSplit.SplitterDistance==before-25&&view.SidebarSplit.Orientation==Orientation.Horizontal&&!view.SidebarSplit.IsSplitterFixed,"Linke Trennleiste vergrößert Coverbereich und Tag-Editor nach Wunsch");
   view.TagEditor.Artist.Text="Nachher";Mouse(view.Counts,2,2);Application.DoEvents();Check(view.Songs.SelectedRows.Count==0&&view.Songs.CurrentCell==null&&p.Folders[0].Songs[0].Artist=="Nachher","Freiflächenklick beendet seitliche Bearbeitung, übernimmt Eingabe und hebt Auswahl auf");
   view.Songs.CurrentCell=view.Songs.Rows[0].Cells[0];view.Songs.Rows[0].Selected=true;view.Songs.BeginEdit(true);((TextBox)view.Songs.EditingControl).Text="Tabelle";Mouse(view.Songs,view.Songs.Width-20,view.Songs.Height-20);Application.DoEvents();Check(!view.Songs.IsCurrentCellInEditMode&&view.Songs.SelectedRows.Count==0&&p.Folders[0].Songs[0].Artist=="Tabelle","Freie Tabellenfläche beendet Zellenbearbeitung ohne Verlust");
   view.Songs.CurrentCell=view.Songs.Rows[0].Cells[0];view.Songs.Rows[0].Selected=true;Mouse(view.TagEditor.Artist,2,2);Check(view.Songs.SelectedRows.Count==1,"Klick ins Tag-Eingabefeld erhält die zu bearbeitende Titelauswahl");
   Call(c,"ReplaceProject",p,null,true);
   using(var bitmap=new Bitmap(view.Width,view.Height)){using(var g=Graphics.FromImage(bitmap))g.CopyFromScreen(view.Location,Point.Empty,bitmap.Size);bitmap.Save(Path.Combine(root,"folder-cover-visible.png"));}view.Close();
  }
  Console.WriteLine("BESTANDEN: "+count+" Ordnercover-Oberflächenprüfungen unter Mono");
 }
}
