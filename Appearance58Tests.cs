using System;using System.Drawing;using System.IO;using System.Linq;using System.Reflection;using System.Windows.Forms;using Musikbox;
class Appearance58Tests {
 static int count;static void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);count++;}
 static object Field(object value,string field){return value.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(value);}
 static void Pump(){for(int i=0;i<5;i++){Application.DoEvents();System.Threading.Thread.Sleep(20);}}
 static void Save(Control control,string name){using(var image=new Bitmap(control.ClientSize.Width,control.ClientSize.Height)){control.BringToFront();Pump();using(var graphics=Graphics.FromImage(image))graphics.CopyFromScreen(control.PointToScreen(Point.Empty),Point.Empty,image.Size);image.Save("verify/"+name+".png");}}
 [STAThread]static void Main(){Application.EnableVisualStyles();Directory.CreateDirectory("verify");
  using(var view=new MainView(Path.GetFullPath("verify/appearance58-layout.json"))){
   view.Show();view.WindowState=FormWindowState.Normal;view.ClientSize=new Size(1700,1120);view.Location=Point.Empty;Pump();
   var nav=(Panel)Field(view,"studioNavigation");var buttons=nav.Controls.OfType<Button>().ToArray();
   foreach(string parent in new[]{"Cover-Studio","Audio-Werkzeuge"})buttons.Single(b=>b.Text==parent).PerformClick();Pump();
   foreach(string child in new[]{"Coversuche","Coverbearbeitung","Covergestaltung","Cover zuweisen / exportieren","Konvertierung","MP3Gain","fre:ac","FFmpeg"}){var button=buttons.Single(b=>b.Text==child);Check(button.Visible,"Unterprogramm sichtbar: "+child);Check(button.Left>buttons.Single(b=>b.Text=="Cover-Studio").Left+42,"Unterprogramm rechts vom Haupttext: "+child);Check(button.Right<=nav.ClientSize.Width,"Unterprogramm innerhalb der Navigation: "+child);}
   Check(!nav.HorizontalScroll.Visible,"Keine waagerechte Navigationsleiste");Check(((ToolTip)Field(view,"studioTips")).OwnerDraw,"Studio-Tooltips verwenden eigene Darstellung");Save(view,"studio-v0.58");
   view.ClientSize=new Size(1200,650);Pump();Check(nav.VerticalScroll.Visible,"Navigation bei geringer Höhe weiterhin scrollbar");Check(!nav.HorizontalScroll.Visible,"Auch kleines Fenster ohne waagerechte Navigationsleiste");
   buttons.Single(b=>b.Text=="Cover-Studio").PerformClick();Pump();Check(!buttons.Single(b=>b.Text=="Coversuche").Visible,"Unterprogramme einklappbar");view.Close();
  }
  using(var message=new MessageView("Diese Information bleibt in der Studio-Oberfläche lesbar.\nEin längerer Hinweis mit Umlauten: Ä Ö Ü ß.","Hinweis",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)){
   message.Show();message.Location=new Point(20,20);Pump();Check(message.Controls.OfType<StudioNoticeIcon>().Any(),"Hinweissymbol im Studio-Design");Check(message.AcceptButton is StudioActionButton&&((StudioActionButton)message.AcceptButton).Primary,"Primäre Bestätigung violett");Check(((Button)message.CancelButton).DialogResult==DialogResult.No,"Nein-/Abbruch-Semantik bleibt erhalten");Save(message,"hinweis-v0.58");message.Close();Check(message.DialogResult==DialogResult.No,"Schließen bestätigt keine Änderung");
  }
  using(var grid=new DataGridView())using(var form=new ModernForm()){
   grid.Columns.Add("Test","Test");grid.Rows.Add("Längerer Informationstext");form.Controls.Add(grid);ModernTheme.Apply(form);form.Show();Pump();Check(!grid.ShowCellToolTips,"Tabellen nutzen Studio-Tooltips");
   var dynamicPanel=new Panel{AutoScroll=true};form.Controls.Add(dynamicPanel);Check(dynamicPanel.Parent==form,"Nachträgliche Ansichten werden angehängt");form.Close();
  }
  var render=typeof(StudioScrollbars).GetMethod("DrawRail",BindingFlags.Static|BindingFlags.NonPublic);
  using(var overview=new Bitmap(400,260))using(var graphics=Graphics.FromImage(overview)){
   graphics.Clear(ModernTheme.Background);int column=20;
   foreach(float scale in new[]{1f,1.5f,2f}){int thickness=(int)(17*scale);var bounds=new Rectangle(column,10,thickness,180);render.Invoke(null,new object[]{graphics,bounds,true,(int)(17*scale),45,95,true,scale});
    graphics.Flush();int purple=0;for(int x=bounds.Left;x<bounds.Right;x++)if(overview.GetPixel(x,65).ToArgb()==ModernTheme.Accent.ToArgb())purple++;
    Check(purple>0&&purple<=(int)Math.Round(5*scale)+1,"Schmaler violetter Schieber bei Skalierung "+scale);
    Check(overview.GetPixel(bounds.Left,65).ToArgb()==ModernTheme.Navy.ToArgb(),"Keine weiße Scrollspur bei Skalierung "+scale);
    var horizontal=new Rectangle(column,210,100,thickness);render.Invoke(null,new object[]{graphics,horizontal,false,17,35,65,true,scale});Check(overview.GetPixel(column+50,210).ToArgb()==ModernTheme.Navy.ToArgb(),"Horizontale Leiste dunkel bei Skalierung "+scale);column+=100;
   }
   overview.Save("verify/scrollleisten-v0.58.png");
  }
  using(var image=new Bitmap(430,100))using(var graphics=Graphics.FromImage(image)){typeof(StudioToolTip).GetMethod("DrawSurface",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{graphics,new Rectangle(0,0,430,100),"Cover zuweisen / exportieren\nHinweise im Studio-Farbschema",1f});Check(image.GetPixel(5,5).ToArgb()==ModernTheme.Navy.ToArgb(),"Tooltip-Hintergrund dunkel");image.Save("verify/tooltip-v0.58.png");}
  Console.WriteLine("BESTANDEN "+count+" Darstellungsprüfungen (Mono/Xvfb; native Windows-Scrollbar-Nachrichten nicht ausgeführt)");
 }
}
