using System;using System.IO;using System.Linq;using System.Drawing;using System.Collections.Generic;using System.Reflection;using System.Windows.Forms;using Musikbox;using MusikboxTests;
class WindowsUI61 {
 static int checks;static Exception uiError;
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);checks++;}
 static object Field(object target,string name){return target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);}
 static object Call(object target,string name,params object[] args){return target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);}
 static void Pump(){Application.DoEvents();if(uiError!=null)throw uiError;}
 [STAThread]static int Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.ThreadException+=(s,e)=>uiError=e.Exception;string root=Path.Combine(Path.GetTempPath(),"Musikbox-UI61-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
 try{using(var view=new MainView(Path.Combine(root,"layout.json"))){var controller=new Controller(view,Path.Combine(root,"backups"),new Playback(new PlaybackTestDevice()));
  var project=new Project();project.Folders.Add(new BoxFolder{Name="Test",SourceDirectory=root,Songs=Enumerable.Range(0,12).Select(i=>new Song{Artist="Artist "+i,Title="Title "+i,Source=Path.Combine(root,i+".mp3")}).ToList()});Call(controller,"ReplaceProject",project,null,true);
  view.Show();view.WindowState=FormWindowState.Normal;Pump();var rows=view.Songs.Rows.Cast<DataGridViewRow>().ToArray();
  string[] pages={"Bibliothek","Tag-Editor","Cover-Studio","Coversuche","Coverbearbeitung","Covergestaltung","Cover zuweisen / exportieren","Audio-Werkzeuge","Konvertierung","MP3Gain","fre:ac","FFmpeg","Musikerkennung","Export","Statistiken","Wiedergabelisten","Einstellungen"};
  foreach(var size in new[]{new Size(1000,680),new Size(1250,800),new Size(1700,1000)}){view.ClientSize=size;
   foreach(string from in pages)foreach(string to in pages){Call(controller,"OpenStudioModule",from);Call(controller,"OpenStudioModule",to);Pump();bool wide=new[]{"Export","Statistiken","Wiedergabelisten","Einstellungen"}.Contains(to);bool library=to=="Bibliothek"||to=="Tag-Editor";
    var table=(Control)Field(view,"studioLibrary");var center=(Control)Field(view,"studioCenter");var metadata=(Control)Field(view,"studioRight");
    Check(table.Visible==!wide,from+" -> "+to+": Titellistensichtbarkeit");Check(!wide||!metadata.Visible,to+": keine Metadaten");Check(view.ModuleHost.Visible==!library,to+": Modulzustand");
    Check(!wide||view.ModuleHost.Bounds==center.ClientRectangle,to+": voller Arbeitsbereich");Check(view.Songs.Rows.Count==rows.Length&&view.Songs.Rows[0]==rows[0],to+": Zeilen erhalten");
    Check((int)Field(view,"studioSwitchDepth")==0,to+": Transaktion geschlossen");Check(view.ModuleHost.Controls.Count<=1,to+": keine alten Module");
   }
  }
  using(var cover=new CoverSearchView("Glen Campbell","Sunflower",false)){cover.Show(view);Pump();Check(cover.ArtistName.Width>40&&cover.TitleName.Width>40,"Coversuche Eingabefelder");Check(cover.Find.Visible&&cover.Apply.Visible,"Coversuche Buttons");cover.Close();}
  view.Close();}
  Console.WriteLine("BESTANDEN: "+checks+" Windows-UI-Pruefungen bei aktueller System-DPI. Kein Audio-/Mikrofontest, keine Pruefung anderer echter Windows-DPI-Einstellungen.");return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}finally{try{Directory.Delete(root,true);}catch{}}
 }
}
