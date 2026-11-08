using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using Musikbox;
class WorkspaceUiTests {
 static int count;static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Console.WriteLine("OK "+message);}
 static object Call(object c,string method,params object[] args){return c.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,args);}
 static Project Current(Controller c){return (Project)typeof(Controller).GetField("p",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c);}
 static IEnumerable<Control> All(Control c){foreach(Control child in c.Controls){yield return child;foreach(var nested in All(child))yield return nested;}}
 [STAThread]static void Main(string[] args){Directory.CreateDirectory(args[0]);Application.EnableVisualStyles();string cover=Path.Combine(args[0],"thema.png");using(var image=new Bitmap(40,40)){using(var g=Graphics.FromImage(image))g.Clear(Color.Blue);image.Save(cover,System.Drawing.Imaging.ImageFormat.Png);}
  using(var v=new MainView()){var c=new Controller(v,Path.Combine(args[0],"backups"));var p=new Project();for(int i=0;i<2;i++)p.Folders.Add(new BoxFolder{Name=i.ToString("D3")+" Thema "+i,Theme="Thema "+i,Cover=cover,Songs=new List<Song>{new Song{Source=i+".mp3",Artist="Interpret "+i,Title="Titel "+i,Album="Thema "+i}}});Call(c,"ReplaceProject",p,null,true);v.Show();Application.DoEvents();
   Check(v.Counts.Text=="Ordner: 2 · Titel: 2","Untere Basisinformation zählt ausschließlich Projektordner und Titel");
   Check(!v.Status.Visible&&!v.BackupStatus.Visible&&!v.Progress.Visible&&v.Counts.Visible,"Untere Leiste enthält keine Bearbeitungs-, Sicherungs- oder Fortschrittsanzeige");
   Call(c,"SetBusy",true);Call(c,"ReadProgress",7,600);Call(c,"AudioProgress",3,99,"Bearbeitung läuft");Application.DoEvents();Check(v.Counts.Text=="Ordner: 2 · Titel: 2","Laufende Import- und Audioarbeit überschreibt keine Bestandszahlen");Call(c,"SetBusy",false);
   Check(v.Panes.Length==5&&v.Panes.All(pane=>!pane.IsFloating),"Fünf eigenständige Arbeitsbereiche starten angedockt");
   Check(v.Tools.Items.OfType<ToolStripDropDownButton>().Any(menu=>menu.Text=="Ansicht"),"Fenstermenü bietet Öffnen, Anordnen und Standardaufteilung");
   v.TagEditor.Album.Text="Vor Fensterwechsel";v.TagPane.Float(v);Application.DoEvents();Check(p.Folders[0].Songs[0].Album=="Vor Fensterwechsel"&&v.TagEditor.FindForm()==v.TagPane.FloatingWindow&&v.TagEditor.Album.Text=="Vor Fensterwechsel","Abtrennen des Editors übernimmt offene Eingabe und erhält den Bearbeitungskontext");
   v.FolderPane.Float(v);v.ArtworkPane.Float(v);v.SongPane.Float(v);v.PlayerPane.Float(v);Application.DoEvents();Check(v.Panes.All(pane=>pane.IsFloating&&pane.FloatingWindow.Owner==v)&&v.OwnedForms.Length==5,"Alle fünf Fenster sind dem Hauptprogramm zugeordnet");
   Check(v.WorkspaceSplit.Panel1Collapsed&&v.Folders.AllowDrop&&v.Songs.AllowDrop,"Ausgelagerte linke Bereiche geben Platz frei und behalten Drag-and-drop");
   v.Folders.SelectedIndex=1;Application.DoEvents();Check(v.Songs.Rows.Count==1&&Convert.ToString(v.Songs.Rows[0].Cells[0].Value)=="Interpret 1"&&v.TagEditor.Album.Text=="Thema 1"&&v.Cover.Image!=null,"Ordnerauswahl im eigenen Fenster aktualisiert Titelliste, Editor und Cover gemeinsam");
   v.TagEditor.Album.Text="Zweites Fensteralbum";All(v.TagEditor).OfType<Button>().Single(b=>b.Text=="Tags übernehmen").PerformClick();Check(p.Folders[1].Songs[0].Album=="Zweites Fensteralbum"&&p.Folders[0].Songs[0].Album=="Vor Fensterwechsel","Schwebender Tag-Editor bearbeitet ausschließlich den aktuell gewählten Titel");
   v.TagPane.FloatingWindow.ActiveControl=null;v.HandleHistoryKey(Keys.Control|Keys.Z,v.TagPane.FloatingWindow);Application.DoEvents();Check(Current(c).Folders[1].Songs[0].Album=="Thema 1","Projekt-Rückgängig funktioniert auch aus einem eigenen Arbeitsfenster");
   v.TilePanes();Application.DoEvents();var bounds=v.Panes.Select(pane=>pane.FloatingWindow.Bounds).ToList();Check(bounds.Select(b=>b.Location).Distinct().Count()==5&&bounds.All(b=>Screen.PrimaryScreen.WorkingArea.IntersectsWith(b)),"Fenster nebeneinander verteilt fünf sichtbare Bereiche ohne identische Position");
   v.FolderPane.FloatingWindow.Close();Application.DoEvents();Check(v.FolderPane.IsFloating&&v.FolderPane.IsHidden&&!v.Folders.IsDisposed,"Schließen eines Arbeitsfensters blendet es ohne Datenverlust aus");v.FolderPane.ShowPane();Check(v.FolderPane.FloatingWindow.Visible&&!v.FolderPane.IsHidden,"Ausgeblendetes Fenster lässt sich mit derselben Auswahl wieder anzeigen");
   v.ResetPaneLayout();Application.DoEvents();Check(v.Panes.All(pane=>!pane.IsFloating)&&v.OwnedForms.Length==0&&!v.WorkspaceSplit.Panel1Collapsed&&v.Songs.FindForm()==v,"Standardaufteilung dockt alle Arbeitsbereiche vollständig zurück");
   Check(Current(c).Folders.Count==2&&Current(c).Folders.Sum(f=>f.Songs.Count)==2,"Abtrennen, Anordnen und Andocken erzeugt keine doppelten Projektdaten");
   Call(c,"ReplaceProject",new Project(),null,true);Check(v.Counts.Text=="Ordner: 0 · Titel: 0","Projektwechsel aktualisiert die Bestandszahlen");v.TagPane.Float(v);v.SongPane.Float(v);v.Close();Application.DoEvents();Check(v.Panes.All(pane=>!pane.IsFloating),"Schließen des Hauptprogramms beendet auch seine Arbeitsfenster");
  }
  Console.WriteLine("BESTANDEN: "+count+" koordinierte Arbeitsfenster-Prüfungen unter Mono");
 }
}
