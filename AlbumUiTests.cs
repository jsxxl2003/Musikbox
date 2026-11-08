using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using Musikbox;
class AlbumUiTests {
 static int count;static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Console.WriteLine("OK "+message);}
 static object Call(object c,string method,params object[] args){return c.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,args);}
 static Project Current(Controller c){return (Project)typeof(Controller).GetField("p",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c);}
 static ToolStripMenuItem Command(MainView v,string text){return v.Tools.Items.OfType<ToolStripDropDownButton>().SelectMany(menu=>menu.DropDownItems.OfType<ToolStripMenuItem>()).Single(item=>item.Text==text);}
 static IEnumerable<Control> All(Control c){foreach(Control child in c.Controls){yield return child;foreach(var nested in All(child))yield return nested;}}
 [STAThread]static void Main(string[] args){Directory.CreateDirectory(args[0]);Application.EnableVisualStyles();using(var v=new MainView()){var c=new Controller(v,args[0]);var p=new Project();p.Folders.Add(new BoxFolder{Name="000 Thema",Theme="Thema",Cover="Themenbild.jpg",Songs=Enumerable.Range(0,2).Select(i=>new Song{Source=i+".mp3",Artist="Interpret "+i,Title="Titel "+i,Album="Thema"}).ToList()});Call(c,"ReplaceProject",p,null,true);v.Show();Application.DoEvents();
  Check(v.Songs.AllowUserToOrderColumns&&v.Songs.AllowUserToResizeColumns&&v.Songs.AutoSizeColumnsMode==DataGridViewAutoSizeColumnsMode.None,"Spalten lassen sich unabhängig verschieben und verbreitern");
  var header=v.Songs.Columns["Titel"].HeaderCell.ContextMenuStrip;Check(header!=null&&header.Items.Count==10,"Rechtsklick auf Kopfzeile bietet alle zehn Spalten");
  var bitrate=header.Items.OfType<ToolStripMenuItem>().Single(x=>x.Text=="Bitrate");bitrate.PerformClick();Check(v.Songs.Columns["Bitrate"].Visible&&v.Songs.Columns["Bitrate"].ReadOnly,"Bitrate einblendbar und schreibgeschützt");bitrate.PerformClick();Check(!v.Songs.Columns["Bitrate"].Visible,"Zusatzspalte wieder ausblendbar");
  v.Songs.Columns["Album"].DisplayIndex=0;Check(v.Songs.Columns["Album"].Index==2,"Verschieben verändert die Tag-Zuordnung nicht");v.Songs.Columns["Album"].DisplayIndex=2;
  string sample=Path.Combine(args[0],"Spalteninfo.mp3");File.WriteAllBytes(sample,new byte[1048576]);var details=TrackColumns.Read(sample);Check(details[3].Contains("1")&&details[4]=="MP3"&&details[1]!="—"&&details[2]!="—","Dateigröße, Format und Zeitangaben aus Quelldatei gelesen");Check(TrackColumns.Read(sample+".fehlt")[0]=="—","Fehlende Quelle zeigt keinen erfundenen Bitratenwert");
  Check(!v.Songs.Columns["Album"].ReadOnly&&!v.TagEditor.Album.ReadOnly,"Album in Tabelle und Seiteneditor bearbeitbar");
  v.TagEditor.Album.Text="Einzelalbum";Check(v.TagEditor.ChangeAlbum.Checked&&v.TagEditor.Pending,"Seitliche Albumeingabe aktiviert gezielt die Albumübernahme");Command(v,"Markierung aufheben").PerformClick();Check(p.Folders[0].Songs[0].Album=="Einzelalbum"&&p.Folders[0].Songs[0].AlbumOverride&&p.Folders[0].Songs[1].Album=="Thema"&&v.Songs.SelectedRows.Count==0,"Markierung aufheben übernimmt Album ausschließlich für bisherigen Titel");
  Check(p.Folders[0].Theme=="Thema"&&p.Folders[0].Cover=="Themenbild.jpg","Albumänderung verändert weder Themenname noch Ordnercover");
  Command(v,"Rückgängig").PerformClick();Check(Current(c).Folders[0].Songs[0].Album=="Thema"&&!Current(c).Folders[0].Songs[0].AlbumOverride,"Rückgängig entfernt auch die individuelle Albumzuordnung");Command(v,"Wiederholen").PerformClick();Check(Current(c).Folders[0].Songs[0].Album=="Einzelalbum"&&Current(c).Folders[0].Songs[0].AlbumOverride,"Wiederholen stellt individuelles Album wieder her");
  v.Songs.CurrentCell=v.Songs.Rows[1].Cells[2];v.Songs.BeginEdit(true);((TextBox)v.Songs.EditingControl).Text="Tabellenalbum";v.Songs.EndEdit();Check(Current(c).Folders[0].Songs[1].Album=="Tabellenalbum"&&Current(c).Folders[0].Songs[1].AlbumOverride,"Direkte Tabellenbearbeitung speichert eigenes Album");
  Command(v,"Alle auswählen").PerformClick();Check(v.TagEditor.Album.Text==""&&!v.TagEditor.ChangeAlbum.Checked,"Unterschiedliche Alben werden bei Mehrfachauswahl nicht versehentlich geleert");v.TagEditor.Album.Text="Sammelalbum";All(v.TagEditor).OfType<Button>().Single(b=>b.Text=="Tags übernehmen").PerformClick();Check(Current(c).Folders[0].Songs.All(s=>s.Album=="Sammelalbum"&&s.AlbumOverride),"Stapelbearbeitung über Seiteneditor übernimmt Album für alle markierten Titel");
  using(var timer=new System.Windows.Forms.Timer{Interval=60}){timer.Tick+=(sender,e)=>{var dialog=Application.OpenForms.OfType<TagEditorView>().FirstOrDefault();if(dialog==null)return;timer.Stop();All(dialog).OfType<CheckBox>().Single(box=>box.Text=="Album ändern").Checked=true;((TextBox)typeof(TagEditorView).GetField("album",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(dialog)).Text="Dialogalbum";All(dialog).OfType<Button>().Single(b=>b.Text=="Übernehmen").PerformClick();};timer.Start();Command(v,"Tags bearbeiten").PerformClick();}
  Check(Current(c).Folders[0].Songs.All(s=>s.Album=="Dialogalbum"&&s.AlbumOverride),"Vollständiger Tag-Dialog übergibt die Albumänderung an den Controller");
  var sorted=Rules.Sorted(Current(c));Check(sorted.Folders[0].Theme=="Thema"&&sorted.Folders[0].Cover=="Themenbild.jpg"&&sorted.Folders[0].Songs.All(s=>s.Album=="Dialogalbum"),"Sortiervorschau erhält bearbeitete Alben und Themenbild");Call(c,"ReplaceProject",new Project(),null,true);v.Close();}
  Console.WriteLine("BESTANDEN: "+count+" Album-Oberflächenprüfungen unter Mono");
 }
}
