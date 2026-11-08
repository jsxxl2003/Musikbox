using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Musikbox;
class ModularUiTests {
 static int count;static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Console.WriteLine("OK "+message);}
 static ToolStripMenuItem Command(MainView view,string text){return view.Tools.Items.OfType<ToolStripDropDownButton>().Single(menu=>menu.Text=="Ansicht").DropDownItems.OfType<ToolStripMenuItem>().Single(item=>item.Text==text);}
 [STAThread]static void Main(string[] args){Directory.CreateDirectory(args[0]);Application.EnableVisualStyles();string layout=Path.Combine(args[0],"layout.json");
  Check(WorkspaceLayout.Load(layout)==null,"Fehlende Anordnung startet ohne erfundene Einstellungen");
  var outside=WorkspaceLayout.VisibleBounds(new Rectangle(50000,-50000,4000,4000),new[]{new Rectangle(0,0,1280,720)});Check(outside==new Rectangle(0,0,1280,720),"Nicht mehr vorhandener Bildschirm wird auf erreichbare Fläche begrenzt");
  var negative=WorkspaceLayout.VisibleBounds(new Rectangle(-1200,10,600,300),new[]{new Rectangle(0,0,1280,720),new Rectangle(-1280,0,1280,720)});Check(negative.X==-1200&&negative.Width==600,"Monitor links vom Hauptbildschirm mit negativen Koordinaten bleibt nutzbar");
  var tiny=WorkspaceLayout.VisibleBounds(new Rectangle(10,10,-5,-5),new[]{new Rectangle(0,0,1280,720)});Check(tiny.Width==320&&tiny.Height==220,"Ungültige gespeicherte Größen werden auf bedienbare Mindestwerte begrenzt");
  using(var view=new MainView(layout)){new Controller(view,Path.Combine(args[0],"backups"));view.Show();Application.DoEvents();Check(view.Panes.Length==5&&view.PlayerPane.IsHidden,"Player ist eigener Baustein und beginnt platzsparend ausgeblendet");
   view.QuietPaneLayout();Application.DoEvents();Check(!view.FolderPane.IsHidden&&!view.SongPane.IsHidden&&view.TagPane.IsHidden&&view.ArtworkPane.IsHidden&&view.PlayerPane.IsHidden,"Ruhige Ansicht zeigt ausschließlich Ordner und Titel");
   Command(view,"Tag-Bearbeitung").PerformClick();Check(!view.TagPane.IsHidden&&Command(view,"Tag-Bearbeitung").Checked,"Fenstermenü blendet einen Baustein ein und zeigt seinen Zustand");Command(view,"Tag-Bearbeitung").PerformClick();Check(view.TagPane.IsHidden&&!Command(view,"Tag-Bearbeitung").Checked,"Dasselbe Menü blendet den Baustein wieder aus");
   view.ArtworkPane.Float(view);view.ArtworkPane.FloatingWindow.Bounds=new Rectangle(400,120,400,320);view.ArtworkPane.HidePane();view.PlayerPane.Float(view);view.PlayerPane.FloatingWindow.Bounds=new Rectangle(20,30,430,290);view.SavePaneLayout();var saved=WorkspaceLayout.Load(layout);Check(saved.Panes.Count==5&&saved.Panes.Single(p=>p.Name=="Cover").Hidden&&saved.Panes.Single(p=>p.Name=="Player").Floating,"Speichern hält Fensterpositionen, Andockzustand und Sichtbarkeit getrennt fest");
   view.ResetPaneLayout();Check(view.Panes.All(p=>!p.IsFloating)&&!view.TagPane.IsHidden&&!view.ArtworkPane.IsHidden&&view.PlayerPane.IsHidden,"Standardansicht holt die Bearbeitungsbereiche zurück und blendet unbenötigten Player aus");view.LoadPaneLayout();Application.DoEvents();Check(view.ArtworkPane.IsHidden&&view.ArtworkPane.IsFloating&&view.PlayerPane.IsFloating&&!view.PlayerPane.IsHidden&&view.PlayerPane.FloatingWindow.Location==new Point(20,30),"Geladene Anordnung stellt sichtbare und versteckte Fenster am gespeicherten Ort wieder her");
   view.ResetPaneLayout();view.SavePaneLayout();Check(!WorkspaceLayout.Load(layout).Panes.Any(p=>p.Floating),"Erneutes Speichern ersetzt dieselbe Anordnung atomar");
   string json=File.ReadAllText(layout);Check(!json.Contains("Source")&&!json.Contains("ClientKey")&&!json.Contains("Fingerprint"),"Fensteranordnung enthält weder Musikpfade noch Erkennungszugänge");
   File.WriteAllText(layout,"defekt");view.LoadPaneLayout();Check(view.Panes.All(p=>!p.IsFloating)&&view.Status.Text.Contains("Standardansicht"),"Beschädigte Anordnung blockiert das Programm nicht und stellt Standardansicht wieder her");
   File.WriteAllText(layout,new string('x',70000));bool rejected=false;try{WorkspaceLayout.Load(layout);}catch{rejected=true;}Check(rejected,"Zu große Einstellungsdatei wird begrenzt");view.Close();
  }
  Console.WriteLine("BESTANDEN: "+count+" modulare Fenster-/Anordnungsprüfungen unter Mono");
 }
}
