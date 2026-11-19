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
   using(var dialog=new Form{Text="iPhone – VLC-Musikexport",StartPosition=FormStartPosition.CenterParent,Width=620,Height=330,BackColor=Color.FromArgb(26,24,39),ForeColor=Color.White,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
    var help=new Label{Left=20,Top=18,Width=560,Height=70,Text="1. iPhone und PC mit demselben WLAN verbinden.\\n2. VLC auf dem iPhone öffnen → Netzwerk → Über WLAN teilen einschalten.\\n3. Die dort angezeigte http://-Adresse hier eingeben."};
    var address=new TextBox{Left=20,Top=100,Width=550,Text=StudioPreferences.Get("IphoneVlcUrl","http://")};
    var status=new Label{Left=20,Top=148,Width=550,Height=50,Text=songs.Count+" Titel ausgewählt. Originaldateien bleiben unverändert."};
    var send=new Button{Left=20,Top=215,Width=190,Height=38,Text="An VLC übertragen"};
    var browser=new Button{Left=225,Top=215,Width=190,Height=38,Text="VLC im Browser öffnen"};
    var cancel=new Button{Left=430,Top=215,Width=140,Height=38,Text="Schließen"};
    dialog.Controls.AddRange(new Control[]{help,address,status,send,browser,cancel});
    var usb=new Button{Left=20,Top=260,Width=550,Height=32,Text="USB: Dateien für Apple-Geräte-Dateifreigabe öffnen"};
    dialog.Height=355;
    dialog.Controls.Add(usb);
    usb.Click+=(s,e)=>{
     try{
      // USB app file sharing is controlled by Apple Devices; do not pretend the
      // Windows app can write into the iOS VLC sandbox directly.
      var first=songs[0].Source;
      System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe","/select,\\\""+first+"\\\""){UseShellExecute=true});
      var folders=songs.Select(x=>Path.GetDirectoryName(x.Source)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
      AppMessage.Show(dialog,"USB-Übertragung mit Apple-Geräte (Windows):\\n\\n1. iPhone per USB verbinden, entsperren und diesem PC vertrauen.\\n2. Apple-Geräte öffnen → iPhone → Dateien → VLC.\\n3. Im geöffneten Explorer die gewünschten Originaldateien auswählen und in die VLC-Dateifreigabe ziehen (oder über 'Datei hinzufügen' auswählen).\\n\\n"+songs.Count+" Titel ausgewählt, in "+folders+" Quellordner(n). Bei mehreren Ordnern die Dateien ordnerweise hinzufügen.\\n\\nEs werden keine zusätzlichen Dateien auf C: angelegt.");
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
     }catch(Exception ex){status.Text="Übertragung nach "+(done-1)+" Titeln abgebrochen.";AppMessage.Show(dialog,ex.Message+"\\nAlternativ „VLC im Browser öffnen“ verwenden.");}
     finally{if(!dialog.IsDisposed){send.Enabled=true;browser.Enabled=true;}}
    };
    dialog.ShowDialog(v);
   }
  }
 }
}