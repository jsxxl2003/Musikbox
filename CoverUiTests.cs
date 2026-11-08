using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;
using Musikbox;
class CoverUiTests {
 static int tests;
 static void Check(bool yes,string text){if(!yes)throw new Exception(text);tests++;Console.WriteLine("OK "+text);}
 static void Wait(Func<bool> ready){var clock=System.Diagnostics.Stopwatch.StartNew();while(!ready()){Application.DoEvents();if(clock.ElapsedMilliseconds>5000)throw new Exception("UI-Zeitlimit");Thread.Sleep(15);}Application.DoEvents();}
 [STAThread]static void Main(string[] args){string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  string oldImage=Path.Combine(root,"alt.jpg"),newImage=Path.Combine(root,"neu.jpg");using(var bitmap=new Bitmap(80,80)){using(var g=Graphics.FromImage(bitmap))g.Clear(Color.DarkRed);bitmap.Save(oldImage,System.Drawing.Imaging.ImageFormat.Jpeg);using(var g=Graphics.FromImage(bitmap))g.Clear(Color.Teal);bitmap.Save(newImage,System.Drawing.Imaging.ImageFormat.Jpeg);}
  var old=new CoverMatch{Id="11111111-1111-1111-1111-111111111111",Artist="Alt",Release="Alter Treffer"};var current=new CoverMatch{Id="22222222-2222-2222-2222-222222222222",Artist="ABBA",Release="Waterloo",Type="Single"};CoverNameMode received=CoverNameMode.Automatic;
  using(var entered=new ManualResetEventSlim())using(var release=new ManualResetEventSlim())using(var returned=new ManualResetEventSlim())using(var view=new CoverSearchView("ABBA","old",false,(a,t,m,n,token)=>{received=m;if(a=="offline")throw new Exception("Testdienst nicht erreichbar");if(t=="zero")return new List<CoverMatch>();return new List<CoverMatch>{t=="old"?old:current};},(match,token)=>{if(match==old){entered.Set();release.Wait(5000);returned.Set();return oldImage;}return newImage;})){
   try{view.Show();Application.DoEvents();Check(view.Mode.Items.Count==4&&!view.FreeName.Enabled,"Vier Namens-Sucharten in Oberfläche vorhanden");view.Find.PerformClick();Wait(()=>view.Results.Items.Count==1);Check(view.Results.SelectedIndex==-1&&!view.Apply.Enabled&&view.PreviewImage.Image==null,"Neue Treffer erfordern bewusste Auswahl, keine automatische Coverübernahme");
    view.Results.SelectedIndex=0;Wait(()=>entered.IsSet);view.TitleName.Text="new";Check(view.Results.Items.Count==0&&view.SelectedArtwork==null&&!view.Apply.Enabled,"Geänderte Namen verwerfen alte Treffer und laufende Vorschau");view.Find.PerformClick();Wait(()=>view.Results.Items.Count==1);view.Results.SelectedIndex=0;Wait(()=>view.SelectedArtwork==newImage);Check(view.Apply.Enabled&&view.PreviewImage.Image!=null,"Neuer Suchtreffer lädt passende Vorschau und aktiviert Auswahl");release.Set();Wait(()=>returned.IsSet);for(int i=0;i<10;i++){Application.DoEvents();Thread.Sleep(15);}Check(view.SelectedArtwork==newImage,"Verspätetes altes Cover überschreibt neue Vorschau nicht");
    view.Mode.SelectedIndex=1;Check(view.ArtistName.Enabled&&!view.TitleName.Enabled&&!view.FreeName.Enabled&&!view.Apply.Enabled,"Nur-Interpret-Modus ignoriert Titelfeld und entfernt vorherige Vorschau");view.Find.PerformClick();Wait(()=>view.Results.Items.Count==1);Check(received==CoverNameMode.Artist,"Ausgewählte Namensart an Suchdienst weitergegeben");view.Mode.SelectedIndex=2;Check(!view.ArtistName.Enabled&&view.TitleName.Enabled,"Nur-Titel-Modus verwendet ausschließlich Titelnamen");view.Mode.SelectedIndex=3;Check(!view.ArtistName.Enabled&&!view.TitleName.Enabled&&view.FreeName.Enabled,"Freie Namenssuche besitzt unabhängiges Eingabefeld");
    view.FreeName.Text="ABBA Waterloo";view.Find.PerformClick();Wait(()=>view.Results.Items.Count==1);Check(received==CoverNameMode.FreeName,"Freie Namenssuche gestartet");view.Mode.SelectedIndex=0;view.TitleName.Text="zero";view.Find.PerformClick();Wait(()=>view.State.Text.Contains("Keine Veröffentlichung"));Check(view.Results.Items.Count==0&&!view.Apply.Enabled,"Keine Treffer zeigt Erklärung und keine veraltete Auswahl");view.ArtistName.Text="offline";view.Find.PerformClick();Wait(()=>view.State.Text.Contains("Testdienst"));Check(view.Results.Items.Count==0&&view.PreviewImage.Image==null&&!view.Apply.Enabled,"Dienstfehler lässt kein altes Cover zur Übernahme stehen");
    view.ArtistName.Text="ABBA";view.TitleName.Text="Waterloo";view.Mode.SelectedIndex=1;view.Find.PerformClick();Wait(()=>view.Results.Items.Count==1);view.Results.SelectedIndex=0;Wait(()=>view.SelectedArtwork==newImage);view.Size=new Size(860,590);Application.DoEvents();Check(view.Find.Visible&&view.PreviewImage.Width>250&&view.Results.Width>250,"Suchfelder und Covervorschau bei Mindestgröße erreichbar");using(var image=new Bitmap(view.Width,view.Height)){using(var graphics=Graphics.FromImage(image))graphics.CopyFromScreen(view.Location,Point.Empty,image.Size);image.Save(Path.Combine(root,"cover-name-search.png"));}view.Close();
   }finally{release.Set();}
  }
  Console.WriteLine("BESTANDEN: "+tests+" Coversuche-UI-Prüfungen mit Offline-Dienst, kein Windows-Nachweis");
 }
}
