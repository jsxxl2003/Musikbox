using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using Musikbox;
class SelectionUiTests {
 static int count;static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Console.WriteLine("OK "+message);}
 static void Call(object c,string method,params object[] args){c.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,args);}
 static ToolStripMenuItem Command(MainView v,string text){return v.Tools.Items.OfType<ToolStripDropDownButton>().Single(menu=>menu.Text=="Titel").DropDownItems.OfType<ToolStripMenuItem>().Single(item=>item.Text==text);}
 [STAThread]static void Main(string[] args){Directory.CreateDirectory(args[0]);Application.EnableVisualStyles();using(var v=new MainView()){var c=new Controller(v,args[0]);var p=new Project();p.Folders.Add(new BoxFolder{Name="000 Test",Theme="Test",Songs=Enumerable.Range(0,3).Select(i=>new Song{Source=i+".mp3",Artist="Interpret "+i,Title="Titel "+i,Album="Test"}).ToList()});p.Folders.Add(new BoxFolder{Name="001 Zweiter",Theme="Zweiter",Songs=new List<Song>{new Song{Source="anderer.mp3",Artist="Anderer",Title="Titel",Album="Zweiter"}}});Call(c,"ReplaceProject",p,null,true);v.Show();Application.DoEvents();
  Check(Command(v,"Alle auswählen")!=null&&Command(v,"Markierung umkehren")!=null&&Command(v,"Markierung aufheben")!=null,"Alle drei Befehle stehen im oberen Titel-Menü");
  Command(v,"Alle auswählen").PerformClick();Check(v.Songs.SelectedRows.Count==3,"Alle auswählen markiert die gesamte geöffnete Titelliste");
  Command(v,"Markierung umkehren").PerformClick();Check(v.Songs.SelectedRows.Count==0&&v.Songs.CurrentCell==null&&!v.TagEditor.Enabled,"Umkehren einer vollständigen Auswahl hebt Auswahl und Bearbeitungsbezug auf");
  Command(v,"Markierung umkehren").PerformClick();Check(v.Songs.SelectedRows.Count==3,"Umkehren einer leeren Auswahl wählt alle Titel");
  Command(v,"Markierung aufheben").PerformClick();Check(v.Songs.SelectedRows.Count==0&&v.Songs.CurrentCell==null,"Markierung aufheben entfernt auch die aktive Zelle");
  v.Songs.CurrentCell=v.Songs.Rows[1].Cells[0];v.Songs.ClearSelection();v.Songs.Rows[1].Selected=true;Command(v,"Markierung umkehren").PerformClick();Check(v.Songs.Rows[0].Selected&&!v.Songs.Rows[1].Selected&&v.Songs.Rows[2].Selected&&v.Songs.CurrentRow.Selected,"Teilweise Auswahl wird exakt umgekehrt, aktiver Titel gehört zur neuen Auswahl");
  Check(p.Folders[1].Songs.Single().Artist=="Anderer"&&p.Folders.SelectMany(f=>f.Songs).Count()==4,"Auswahlbefehle ändern keine Titel oder anderen Ordner");
  Check(!((ProjectHistory)typeof(Controller).GetField("history",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c)).CanUndo,"Reine Auswahlbefehle erzeugen keine Projektänderung");
  Command(v,"Markierung aufheben").PerformClick();v.Songs.CurrentCell=v.Songs.Rows[0].Cells[0];v.Songs.Rows[0].Selected=true;v.TagEditor.Artist.Text="Bearbeitet";Command(v,"Alle auswählen").PerformClick();Check(p.Folders[0].Songs[0].Artist=="Bearbeitet"&&p.Folders[0].Songs[1].Artist=="Interpret 1"&&v.Songs.SelectedRows.Count==3,"Offene Tag-Eingabe wird vor Auswahlwechsel ausschließlich beim bisherigen Titel übernommen");
  Call(c,"ReplaceProject",new Project(),null,true);Command(v,"Alle auswählen").PerformClick();Command(v,"Markierung umkehren").PerformClick();Command(v,"Markierung aufheben").PerformClick();Check(v.Songs.Rows.Count==0&&v.Songs.CurrentCell==null,"Alle drei Befehle sind bei leerer Titelliste sicher");v.Close();}
  Console.WriteLine("BESTANDEN: "+count+" Titel-Auswahlprüfungen unter Mono");
 }
}
