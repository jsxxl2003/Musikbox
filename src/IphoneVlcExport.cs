using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Musikbox {
 public partial class Controller {
  void OpenIphoneVlcExport(){
   var songs=SelectedSongs().Where(s=>s!=null&&!String.IsNullOrWhiteSpace(s.Source)&&File.Exists(s.Source)).ToList();
   if(songs.Count==0){AppMessage.Show(v,"Bitte zuerst die gewünschten Musiktitel in der Titelliste markieren.");return;}
   using(var dialog=new Form{Text="iPhone – VLC-Musikexport",StartPosition=FormStartPosition.CenterParent,Width=700,Height=410,BackColor=Color.FromArgb(26,24,39),ForeColor=Color.White,Font=StudioDrawing.Font(15),FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
    dialog.Text="iPhone – USB-Musikexport";
    dialog.Height=300;
    var help=new Label{Left=20,Top=18,Width=640,Height=75,ForeColor=Color.White,Text="USB: iPhone anschließen und entsperren.\\nCopyTrans Manager öffnen, Titel aus dem Explorer hinzufügen und in CopyTrans auf Aktualisieren klicken."};
    var status=new Label{Left=20,Top=108,Width=640,Height=48,ForeColor=Color.White,Text=songs.Count+" Titel ausgewählt. Originaldateien bleiben unverändert."};
    var usb=MainView.StudioButton("CopyTrans + Musikordner öffnen",()=>{});usb.SetBounds(20,175,420,42);
    var cancel=MainView.StudioButton("Schließen",()=>{});cancel.SetBounds(450,175,210,42);
    dialog.Controls.AddRange(new Control[]{help,status,usb,cancel});
    usb.Click+=(s,e)=>{
     try{
      var candidates=new[]{
       Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"CopyTransManager.exe"),
       Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"CopyTrans Manager","CopyTransManager.exe"),
       Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"CopyTrans Manager","CopyTransManager.exe")
      };
      var installed=candidates.FirstOrDefault(File.Exists);
      if(installed!=null)System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(installed){UseShellExecute=true});
      else AppMessage.Show(dialog,"CopyTrans Manager bitte über die vorhandene Desktop-Verknüpfung starten.");
      var folders=songs.Select(x=>Path.GetDirectoryName(x.Source)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
      foreach(var folder in folders.Take(5))System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe","\\\""+folder+"\\\""){UseShellExecute=true});
      status.Text=songs.Count+" Titel in "+folders.Length+" Ordner(n). In CopyTrans hinzufügen und Aktualisieren drücken.";
      if(folders.Length>5)AppMessage.Show(dialog,"Weitere Musikordner bitte bei Bedarf einzeln im Explorer öffnen.");
     }catch(Exception ex){AppMessage.Show(dialog,ex.Message);}
    };
    cancel.Click+=(s,e)=>dialog.Close();
    dialog.ShowDialog(v);
   }
  }
 }
}