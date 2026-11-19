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
    var help=new Label{Left=20,Top=18,Width=640,Height=95,ForeColor=Color.White,Text="WLAN: PC und iPhone mit demselben Netzwerk verbinden.\nAuf dem iPhone VLC → Entdecken → Über WLAN teilen einschalten.\nDie dort angezeigte HTTP-Adresse unten eintragen."};
    var address=new StudioTextBox{Left=20,Top=120,Width=640,Text=StudioPreferences.Get("IphoneVlcUrl","http://")};
    var status=new Label{Left=20,Top=165,Width=640,Height=50,ForeColor=Color.White,Text=songs.Count+" Titel ausgewählt. Originaldateien bleiben unverändert."};
    var send=MainView.StudioButton("Über WLAN übertragen",()=>{});send.SetBounds(20,230,205,40);
    var browser=MainView.StudioButton("VLC im Browser öffnen",()=>{});browser.SetBounds(235,230,205,40);
    var cancel=MainView.StudioButton("Schließen",()=>{});cancel.SetBounds(450,230,210,40);
    dialog.Controls.AddRange(new Control[]{help,address,status,send,browser,cancel});
    var usb=MainView.StudioButton("USB: Dateien und Anleitung öffnen",()=>{});usb.SetBounds(20,285,640,42);
    dialog.Height=390;
    dialog.Controls.Add(usb);
    usb.Click+=(s,e)=>{
     try{
      // USB app file sharing is controlled by Apple Devices; do not pretend the
      // Windows app can write into the iOS VLC sandbox directly.
      var first=songs[0].Source;
      System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe","/select,\\\""+first+"\\\""){UseShellExecute=true});
      var folders=songs.Select(x=>Path.GetDirectoryName(x.Source)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
      AppMessage.Show(dialog,"USB-Übertragung mit Apple-Geräte (Windows):\n\n1. iPhone per USB verbinden, entsperren und diesem PC vertrauen.\n2. Apple-Geräte öffnen → iPhone → Dateien → VLC.\n3. Die ausgewählten Musikdateien über die VLC-Dateifreigabe hinzufügen.\n\nKeinen iPhone-Systemordner auswählen!\n\n"+songs.Count+" Titel ausgewählt, in "+folders+" Quellordner(n). Bei mehreren Ordnern die Dateien ordnerweise hinzufügen.\n\nEs werden keine zusätzlichen Dateien auf C: angelegt.");
     }catch(Exception ex){AppMessage.Show(dialog,ex.Message);}
    };

    cancel.Click+=(s,e)=>dialog.Close();
    bool Valid(out Uri uri){uri=null;return Uri.TryCreate(address.Text.Trim(),UriKind.Absolute,out uri)&&uri.Scheme==Uri.UriSchemeHttp&&uri.UserInfo.Length==0&&uri.HostNameType!=UriHostNameType.Unknown;}
    browser.Click+=(s,e)=>{if(!Valid(out var uri)){AppMessage.Show(dialog,"Bitte die in VLC angezeigte HTTP-Adresse eingeben.");return;}StudioPreferences.Set("IphoneVlcUrl",uri.GetLeftPart(UriPartial.Authority));try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri){UseShellExecute=true});}catch(Exception ex){AppMessage.Show(dialog,ex.Message);}};
    send.Click+=async(s,e)=>{
     if(!Valid(out var uri)){AppMessage.Show(dialog,"Bitte die in VLC angezeigte HTTP-Adresse eingeben.");return;}
     var endpoint=new Uri(uri.GetLeftPart(UriPartial.Authority).TrimEnd('/')+"/upload");
     StudioPreferences.Set("IphoneVlcUrl",uri.GetLeftPart(UriPartial.Authority));
     send.Enabled=false;browser.Enabled=false;int done=0;
     try{
      using(var client=new HttpClient{Timeout=TimeSpan.FromMinutes(20)}){
       foreach(var song in songs){
        status.Text="Übertrage "+(++done)+" / "+songs.Count+": "+Path.GetFileName(song.Source);status.Refresh();
        using(var content=new MultipartFormDataContent())
        using(var stream=new FileStream(song.Source,FileMode.Open,FileAccess.Read,FileShare.Read)){
         var part=new StreamContent(stream);part.Headers.ContentType=new MediaTypeHeaderValue("application/octet-stream");
         content.Add(part,"files[]",Path.GetFileName(song.Source));
         using(var response=await client.PostAsync(endpoint,content)){if(!response.IsSuccessStatusCode)throw new IOException("VLC antwortet mit HTTP "+(int)response.StatusCode+". Bitte WLAN-Freigabe und Adresse prüfen.");}
        }
       }
      }
      status.Text="Fertig: "+done+" Titel an VLC gesendet.";
     }catch(Exception ex){status.Text="Übertragung nach "+(done-1)+" Titeln abgebrochen.";AppMessage.Show(dialog,ex.Message+"\nAlternativ „VLC im Browser öffnen“ verwenden.");}
     finally{if(!dialog.IsDisposed){send.Enabled=true;browser.Enabled=true;}}
    };
    dialog.ShowDialog(v);
   }
  }
 }
}