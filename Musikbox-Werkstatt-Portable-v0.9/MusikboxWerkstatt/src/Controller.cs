using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
namespace Musikbox {
 public class Controller {
  readonly MainView v;Project p=new Project();bool refresh,dirty,busy;CancellationTokenSource cancel;
  BoxFolder Current {get{return v.Folders.SelectedItem as BoxFolder;}}
  public Controller(MainView view) {
   v=view;v.CanDrop=()=>!busy;v.PathsDropped+=ImportDropped;
   v.Button("Ordner laden",()=>Run(Import));v.Button("Neuer Ordner",()=>Run(NewFolder));
   v.Button("Ordner bearbeiten",()=>Run(EditFolder));v.Button("↑",()=>Move(-1));v.Button("↓",()=>Move(1));
   v.Button("Entfernen",()=>Run(Remove));v.Button("Titel hinzufügen",()=>Run(AddSongs));v.Button("Titel entfernen",()=>Run(RemoveSongs));v.Button("Cover",()=>Run(SetCover));
   v.Tools.Items.Add(new ToolStripSeparator());
   v.Button("Audio-Einstellungen",()=>Run(GainSettings));v.Button("Sortiervorschau",()=>Run(Preview));v.Button("Prüfen",()=>Run(Check));v.Button("Export",()=>Run(StartExport));
   v.Tools.Items.Add(new ToolStripSeparator());v.Button("Speichern",()=>Run(Save));v.Button("Öffnen",()=>Run(Open));v.Button("Hilfe",Help);
   v.Tools.Items.Add(v.Cancel);v.Cancel.Click+=(s,e)=>{if(cancel!=null)cancel.Cancel();};
   v.Folders.SelectedIndexChanged+=(s,e)=>ShowSongs();
   v.Songs.CellEndEdit+=(s,e)=>{if(refresh||Current==null||e.RowIndex<0)return;var song=(Song)v.Songs.Rows[e.RowIndex].Tag;string value=Convert.ToString(v.Songs.Rows[e.RowIndex].Cells[e.ColumnIndex].Value).Trim();if(e.ColumnIndex==0)song.Artist=value;else if(e.ColumnIndex==1)song.Title=value;dirty=true;UpdateStatus();};
   v.Songs.DataError+=(s,e)=>{e.ThrowException=false;};
   v.FormClosing+=(s,e)=>{if(busy){MessageBox.Show(v,"Bitte zuerst den laufenden Vorgang abbrechen und dessen Ende abwarten.");e.Cancel=true;}else if(!CanDiscard())e.Cancel=true;};
   RefreshFolders(0);
  }
  void Run(Action action){if(busy)return;try{v.Songs.EndEdit();action();}catch(Exception e){MessageBox.Show(v,e.Message,"Vorgang nicht abgeschlossen",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
  bool CanDiscard(){return !dirty||MessageBox.Show(v,"Ungespeicherte Projektänderungen verwerfen?","Projekt",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes;}
  void UpdateStatus(){v.Status.Text=p.Folders.Count+"/99 Ordner · "+p.Folders.Sum(f=>f.Songs.Count)+" Titel"+(dirty?" · nicht gespeichert":"")+(p.Normalize?" · MP3Gain: "+p.TargetDb+" dB":p.FfmpegLoudness?" · FFmpeg: "+p.TargetLufs+" LUFS":" · Lautstärke unverändert")+" · Originaldateien bleiben unverändert";}
  void RefreshFolders(int index){v.Folders.BeginUpdate();v.Folders.Items.Clear();foreach(var f in p.Folders)v.Folders.Items.Add(f);v.Folders.EndUpdate();if(p.Folders.Count>0)v.Folders.SelectedIndex=Math.Max(0,Math.Min(index,p.Folders.Count-1));else ShowSongs();UpdateStatus();}
  void ShowSongs(){refresh=true;v.Songs.Rows.Clear();var f=Current;if(v.Cover.Image!=null){v.Cover.Image.Dispose();v.Cover.Image=null;}if(f!=null){
   v.Details.Text=f.Name+"  ·  Thema: "+f.Theme+"\r\nGruppe: "+(String.IsNullOrWhiteSpace(f.Group)?"einzelner Ordner":f.Group)+" · Album beim Export: "+f.Theme;
   foreach(var s in f.Songs){int row=v.Songs.Rows.Add(s.Artist,s.Title,s.Album,Path.GetFileName(s.Source));v.Songs.Rows[row].Tag=s;if(String.IsNullOrWhiteSpace(s.Artist)||String.IsNullOrWhiteSpace(s.Title)||String.IsNullOrWhiteSpace(s.Album))v.Songs.Rows[row].DefaultCellStyle.BackColor=Color.LemonChiffon;}
   try{if(File.Exists(f.Cover))using(var img=Image.FromFile(f.Cover))v.Cover.Image=new Bitmap(img);}catch{}
  }else v.Details.Text="Dateien oder Ordner aus dem Windows-Explorer hierher ziehen.";
   v.Songs.Columns["Album"].ReadOnly=true;v.Songs.Columns["Album"].ToolTipText="Der Album-Tag wird beim Export aus dem Themennamen gesetzt. Thema über 'Ordner bearbeiten' ändern.";refresh=false;}
  async void ImportDropped(string[] paths,int targetIndex){
   if(busy)return;v.Songs.EndEdit();cancel=new CancellationTokenSource();var token=cancel.Token;SetBusy(true);DropResult completed=null;
   try {
    completed=await Task.Run(()=>DropImport.Read(p,paths,targetIndex,()=>token.ThrowIfCancellationRequested()));
    if(completed.FilesAdded>0){p=completed.Project;dirty=true;RefreshFolders(completed.SelectedIndex);}
    if(completed.Warnings.Count>0)v.Report("Hinweise zum Drag-and-drop-Import",String.Join("\r\n",completed.Warnings));
   }catch(OperationCanceledException){MessageBox.Show(v,"Import abgebrochen. Es wurden keine neuen Dateien oder Ordner übernommen.");}catch(Exception e){MessageBox.Show(v,e.Message,"Drag-and-drop-Import nicht abgeschlossen");}finally{cancel.Dispose();cancel=null;SetBusy(false);if(completed!=null)v.Status.Text+=" · Import: +"+completed.FilesAdded+" Titel, +"+completed.FoldersAdded+" Ordner, "+completed.Skipped+" bereits vorhanden";}
  }
  async void Import(){
   string root;using(var d=new FolderBrowserDialog{Description="Musikordner oder übergeordneten Sammlungsordner wählen"}){if(d.ShowDialog(v)!=DialogResult.OK)return;root=d.SelectedPath;}
   cancel=new CancellationTokenSource();var token=cancel.Token;SetBusy(true);var warnings=new List<string>();
   try{var folders=await Task.Run(()=>Importer.Read(root,warnings,p,()=>token.ThrowIfCancellationRequested()));var fresh=folders.Where(f=>!p.Folders.Any(x=>String.Equals(x.SourceDirectory,f.SourceDirectory,StringComparison.OrdinalIgnoreCase))).ToList();
    if(p.Folders.Count+fresh.Count>99)throw new Exception("Der Import würde mehr als 99 Ordner ergeben. Bitte kleinere Auswahl importieren.");
    foreach(var f in fresh){if(p.Folders.Any(x=>x.Name.Equals(f.Name,StringComparison.OrdinalIgnoreCase)))throw new Exception("Ordnername bereits vorhanden: "+f.Name);}
    var candidate=p.WithFolders(p.Folders.Concat(fresh).OrderBy(x=>x.Name,StringComparer.OrdinalIgnoreCase).ToList());p=Rules.Sorted(candidate);dirty|=fresh.Count>0;RefreshFolders(0);
    if(warnings.Count>0)v.Report("Nicht importierte Dateien",String.Join("\r\n",warnings));
    if(folders.Count==0)MessageBox.Show(v,"Keine unterstützten Audiodateien gefunden. ZIP-Dateien bitte zuvor im Windows-Explorer entpacken.");
   }catch(OperationCanceledException){MessageBox.Show(v,"Import abgebrochen. Es wurden keine neuen Ordner übernommen.");}catch(Exception e){MessageBox.Show(v,e.Message,"Import");}finally{cancel.Dispose();cancel=null;SetBusy(false);}
  }
  void NewFolder(){if(p.Folders.Count>=99)throw new Exception("Bereits 99 Ordner vorhanden.");string name=v.Ask("Neuer Ordner – z. B. 033 Country von damals bis heute","");if(name==null)return;if(String.IsNullOrWhiteSpace(name)||Rules.SafeName(name)!=name)throw new Exception("Bitte gültigen Windows-Ordnernamen eingeben.");if(p.Folders.Any(f=>f.Name.Equals(name,StringComparison.OrdinalIgnoreCase)))throw new Exception("Ordnername bereits vorhanden.");var added=new BoxFolder{Name=name,Theme=Rules.Theme(name),Group=""};p.Folders.Add(added);p.Folders.Sort((a,b)=>StringComparer.OrdinalIgnoreCase.Compare(a.Name,b.Name));dirty=true;RefreshFolders(p.Folders.IndexOf(added));}
  void EditFolder(){var f=Current;if(f==null)return;string name=v.Ask("Ordnername mit Nummer",f.Name);if(name==null)return;string theme=v.Ask("Themenname / Album ohne Nummer",f.Theme);if(theme==null)return;string group=v.Ask("Gemeinsame Sortiergruppe – leer = einzelner Ordner",f.Group);if(group==null)return;
   if(String.IsNullOrWhiteSpace(name)||Rules.SafeName(name)!=name||String.IsNullOrWhiteSpace(theme))throw new Exception("Ordnername oder Thema ungültig.");if(p.Folders.Any(x=>x!=f&&x.Name.Equals(name,StringComparison.OrdinalIgnoreCase)))throw new Exception("Ordnername bereits vorhanden.");f.Name=name;f.Theme=theme.Trim();f.Group=group.Trim();p.Folders.Sort((a,b)=>StringComparer.OrdinalIgnoreCase.Compare(a.Name,b.Name));dirty=true;RefreshFolders(p.Folders.IndexOf(f));}
  void Move(int delta){Run(()=>{int i=v.Folders.SelectedIndex,j=i+delta;if(i<0||j<0||j>=p.Folders.Count)return;Rules.MoveFolder(p,i,j);dirty=true;RefreshFolders(j);});}
  void Remove(){var f=Current;if(f==null)return;if(MessageBox.Show(v,"Ordner aus dem Projekt entfernen? Die Originaldateien bleiben erhalten.","Entfernen",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;int i=v.Folders.SelectedIndex;p.Folders.Remove(f);dirty=true;RefreshFolders(i);}
  void SetCover(){if(Current==null)return;using(var d=new OpenFileDialog{Filter="Bilder|*.jpg;*.jpeg;*.png"})if(d.ShowDialog(v)==DialogResult.OK){using(var img=Image.FromFile(d.FileName)){}Current.Cover=d.FileName;dirty=true;ShowSongs();}}
  async void AddSongs(){var f=Current;if(f==null){MessageBox.Show(v,"Bitte zuerst einen Themenordner auswählen.");return;}string[] files;
   using(var d=new OpenFileDialog{Filter="Audiodateien|*.mp3;*.flac;*.wav;*.m4a;*.aac;*.ogg;*.opus;*.wma;*.aif;*.aiff",Multiselect=true}){if(d.ShowDialog(v)!=DialogResult.OK)return;files=d.FileNames;}
   int selected=v.Folders.SelectedIndex;var warnings=new List<string>();var additions=new List<Song>();
   var existing=new HashSet<string>(p.Folders.Where(x=>x==f||(!String.IsNullOrWhiteSpace(f.Group)&&String.Equals(x.Group,f.Group,StringComparison.OrdinalIgnoreCase))).SelectMany(x=>x.Songs).Select(x=>x.Source),StringComparer.OrdinalIgnoreCase);
   cancel=new CancellationTokenSource();var token=cancel.Token;SetBusy(true);
   try {
    await Task.Run(()=>{foreach(string file in files){token.ThrowIfCancellationRequested();if(!existing.Add(file))continue;try{var added=Ffmpeg.ReadSong(file,p,()=>token.ThrowIfCancellationRequested());added.Album=f.Theme;additions.Add(added);}catch(OperationCanceledException){throw;}catch(Exception e){warnings.Add(Path.GetFileName(file)+": "+e.Message);}}});
    if(additions.Count>0){var candidate=p.WithFolders(new List<BoxFolder>());foreach(var x in p.Folders)candidate.Folders.Add(new BoxFolder{Name=x.Name,Theme=x.Theme,Group=x.Group,SourceDirectory=x.SourceDirectory,Cover=x.Cover,Songs=x.Songs.Select(song=>song.Copy()).ToList()});candidate.Folders[selected].Songs.AddRange(additions);int before=p.Folders.Count;p=Rules.Sorted(candidate);dirty=true;RefreshFolders(selected);if(p.Folders.Count>before)MessageBox.Show(v,(p.Folders.Count-before)+" Folgeordner automatisch angelegt. Alle Titel wurden alphabetisch verteilt. Nachfolgende Ordnernummern wurden bei Bedarf weitergeschoben.","Thema aufgeteilt");}
    if(warnings.Count>0)v.Report("Nicht hinzugefügte Titel",String.Join("\r\n",warnings));
   }catch(OperationCanceledException){MessageBox.Show(v,"Hinzufügen abgebrochen. Es wurden keine Titel übernommen.");}catch(Exception e){MessageBox.Show(v,e.Message,"Hinzufügen nicht abgeschlossen");}finally{cancel.Dispose();cancel=null;SetBusy(false);}
  }
  void RemoveSongs(){var f=Current;if(f==null)return;var songs=v.Songs.SelectedRows.Cast<DataGridViewRow>().Select(r=>r.Tag as Song).Where(s=>s!=null).ToList();if(songs.Count==0)return;if(MessageBox.Show(v,songs.Count+" Titel aus dem Projekt entfernen? Originale bleiben erhalten.","Titel entfernen",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;foreach(var s in songs)f.Songs.Remove(s);dirty=true;RefreshFolders(v.Folders.SelectedIndex);}
  string Summary(Project sorted){var text=new System.Text.StringBuilder();text.AppendLine("Audio: "+(sorted.FfmpegLoudness?"FFmpeg-Lautheitsanpassung, MP3-Neucodierung":sorted.Normalize?"MP3Gain-Lautstärkeanpassung":"Lautstärke unverändert"));text.AppendLine("MP3-Ausgabe bei Neucodierung: "+Ffmpeg.EncodingLabel(sorted));text.AppendLine("Neu kodierte Titel: "+sorted.Folders.SelectMany(f=>f.Songs).Count(song=>Ffmpeg.NeedsEncoding(sorted,song)));text.AppendLine();foreach(var f in sorted.Folders){text.AppendLine(f.Name+" — "+f.Songs.Count+" Titel — Album: "+f.Theme);int i=0;foreach(var s in f.Songs)text.AppendLine("  "+(++i).ToString("D2")+"  "+s.Artist+" | "+s.Title+" | "+f.Theme);text.AppendLine();}return text.ToString();}
  void Preview(){var sorted=Rules.Sorted(p);v.Report("Vorschau – alphabetisch nach Interpret und Titel",Summary(sorted));if(MessageBox.Show(v,"Diese Verteilung ins Projekt übernehmen?","Sortierung",MessageBoxButtons.YesNo)==DialogResult.Yes){p=sorted;dirty=true;RefreshFolders(0);}}
  void Check(){var sorted=Rules.Sorted(p);var errors=Rules.Validate(sorted);var warnings=new List<string>();foreach(var f in sorted.Folders){foreach(var group in f.Songs.GroupBy(s=>(s.Artist+"\0"+s.Title).ToUpperInvariant()).Where(g=>g.Count()>1))warnings.Add("Mögliche Dublette in "+f.Name+": "+group.First().Artist+" – "+group.First().Title);foreach(var s in f.Songs)if(s.Album!=f.Theme)warnings.Add("Album wird angepasst: "+s.Artist+" – "+s.Title);}
   v.Report("Prüfergebnis","FEHLER: "+errors.Count+"\r\n"+String.Join("\r\n",errors)+"\r\n\r\nHINWEISE: "+warnings.Count+"\r\n"+String.Join("\r\n",warnings));}
  void Save(){using(var d=new SaveFileDialog{Filter="Musikbox-Projekt|*.musikbox.json",FileName="Meine-Musikbox.musikbox.json"})if(d.ShowDialog(v)==DialogResult.OK){Storage.Save(p,d.FileName);dirty=false;UpdateStatus();}}
  void Open(){if(!CanDiscard())return;using(var d=new OpenFileDialog{Filter="Musikbox-Projekt|*.json"})if(d.ShowDialog(v)==DialogResult.OK){var loaded=Storage.Load(d.FileName);p=Rules.Sorted(loaded);dirty=true;RefreshFolders(0);}}
  void SetBusy(bool state){busy=state;foreach(ToolStripItem item in v.Tools.Items)item.Enabled=!state;v.Cancel.Enabled=state&&cancel!=null;v.Folders.Enabled=!state;v.Songs.Enabled=!state;v.Progress.Value=0;if(!state)UpdateStatus();else v.Status.Text="Bitte warten …";}
  async void StartExport(){
   try {
    Gain.Validate(p);Ffmpeg.Validate(p,p.Folders.SelectMany(f=>f.Songs).Any(song=>!Ffmpeg.IsMp3(song.Source)));var sorted=Rules.Sorted(p);var errors=Rules.Validate(sorted);if(errors.Count>0)throw new Exception(String.Join("\r\n",errors.Take(25)));
    v.Report("Exportvorschau",Summary(sorted));string target;using(var d=new FolderBrowserDialog{Description="Ziel wählen: neuer Export-Unterordner wird automatisch angelegt"}){if(d.ShowDialog(v)!=DialogResult.OK)return;target=d.SelectedPath;}
    if(MessageBox.Show(v,"Jetzt "+sorted.Folders.Sum(f=>f.Songs.Count)+" Titel als Kopien exportieren?\r\nAlbum-Tags werden auf den Themennamen gesetzt. Vorhandene Exporte bleiben erhalten.\r\n"+(p.Normalize?"Lautstärkeanpassung EIN: "+p.TargetDb+" dB mit Clipping-Schutz.":p.FfmpegLoudness?"FFmpeg-Lautheitsanpassung EIN: "+p.TargetLufs+" LUFS (MP3-Neucodierung).":"Lautstärkeanpassung AUS.")+"","Export starten",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
    cancel=new CancellationTokenSource();SetBusy(true);var token=cancel.Token;
    string path=await Task.Run(()=>Exporter.Export(sorted,target,()=>token.ThrowIfCancellationRequested(),(n,name)=>v.BeginInvoke((Action)(()=>{v.Progress.Value=n;v.Status.Text=n+" % · "+name;}))));
    MessageBox.Show(v,"Export erfolgreich:\r\n"+path+"\r\n\r\nFür die Musikbox die nummerierten Ordner aus diesem Exportordner auf den Datenträger kopieren. Bestehende Musik vorher sichern.","Fertig");
   }catch(OperationCanceledException){MessageBox.Show(v,"Export abgebrochen. Unvollständige Kopien werden nach Möglichkeit entfernt.");}catch(Exception e){MessageBox.Show(v,e.Message,"Export nicht abgeschlossen");}finally{if(cancel!=null){cancel.Dispose();cancel=null;}SetBusy(false);}
  }
  void GainSettings(){
   using(var d=new Form{Text="Audio-Einstellungen",Width=720,Height=640,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}) {
    var mode=new ComboBox{Left=200,Top=16,Width=480,DropDownStyle=ComboBoxStyle.DropDownList};mode.Items.AddRange(new object[]{"Lautstärke unverändert","MP3Gain – ohne MP3-Neucodierung","FFmpeg – Lautheitsanpassung mit MP3-Neucodierung"});mode.SelectedIndex=p.Normalize?1:p.FfmpegLoudness?2:0;
    var modeLabel=new Label{Left=18,Top=20,Width=180,Text="Lautstärke beim Export:"};
    var info=new Label{Left=18,Top=55,Width=660,Height=55,Text="Andere Audioformate werden beim Export immer in MP3 konvertiert.\r\nMP3Gain und FFmpeg-Lautheitsanpassung sind alternativ wählbar. Originale bleiben erhalten."};
    var gainLabel=new Label{Left=18,Top=117,Width=180,Text="MP3Gain-Ziel (dB):"};var gainTarget=new NumericUpDown{Left=200,Top=114,Width=85,Minimum=80,Maximum=95,DecimalPlaces=1,Increment=0.5m,Value=(decimal)(p.TargetDb>=80&&p.TargetDb<=95?p.TargetDb:89)};
    var lufsLabel=new Label{Left=330,Top=117,Width=210,Text="FFmpeg-Ziel (LUFS):"};var lufsTarget=new NumericUpDown{Left=545,Top=114,Width=85,Minimum=-23,Maximum=-10,DecimalPlaces=1,Increment=0.5m,Value=(decimal)(p.TargetLufs>=-23&&p.TargetLufs<=-10?p.TargetLufs:-16)};
    var peak=new Label{Left=18,Top=155,Width=660,Height=35,Text="FFmpeg: True-Peak-Ziel -2 dBTP; MP3-Ausgabe 44,1 kHz, Stereo.\r\nDie dB-Skala von MP3Gain und LUFS sind unterschiedliche Skalen."};
    var encodingLabel=new Label{Left=18,Top=205,Width=180,Text="MP3-Bitratenmodus:"};var encoding=new ComboBox{Left=200,Top=201,Width=480,DropDownStyle=ComboBoxStyle.DropDownList};encoding.Items.AddRange(new object[]{"Feste Bitrate (CBR)","Variable Bitrate (VBR)"});encoding.SelectedIndex=p.VariableBitrate?1:0;
    var bitrateLabel=new Label{Left=18,Top=247,Width=180,Text="CBR-Bitrate (kbit/s):"};var bitrate=new ComboBox{Left=200,Top=243,Width=90,DropDownStyle=ComboBoxStyle.DropDownList};foreach(int b in Ffmpeg.Bitrates)bitrate.Items.Add(b);bitrate.SelectedItem=Ffmpeg.Bitrates.Contains(p.Mp3Bitrate)?p.Mp3Bitrate:192;
    var qualityLabel=new Label{Left=330,Top=247,Width=135,Text="VBR-Qualität:"};var quality=new ComboBox{Left=465,Top=243,Width=215,DropDownStyle=ComboBoxStyle.DropDownList};quality.Items.AddRange(new object[]{"V0 – höchste Qualität","V1","V2 – Standard","V3","V4","V5 – kleinere Dateien"});quality.SelectedIndex=Math.Max(0,Math.Min(5,p.VbrQuality));
    var bitrateHint=new Label{Left=18,Top=283,Width=660,Height=42,Text="Standard: 192 kbit/s CBR. Bei VBR bestimmt die Qualitätsstufe die Bitrate;\r\nes gibt keinen festen kbit/s-Wert. Ausgabe: 44,1 kHz, Stereo."};
    var reencode=new CheckBox{Left=18,Top=490,Width=660,Height=40,Text="Auch vorhandene MP3s auf diese Qualität neu kodieren (optional, Qualitätsverlust möglich)",Checked=p.ReencodeMp3};
    Action updateEncoding=()=>{bitrate.Enabled=encoding.SelectedIndex==0;quality.Enabled=encoding.SelectedIndex==1;};encoding.SelectedIndexChanged+=(sender,e)=>updateEncoding();updateEncoding();
    var gainPath=new TextBox{Left=18,Top=335,Width=315,ReadOnly=true,Text=p.GainExecutable??""};var chooseGain=new Button{Left=340,Top=332,Width=165,Text="mp3gain.exe wählen"};
    var ffPath=new TextBox{Left=18,Top=378,Width=315,ReadOnly=true,Text=p.FfmpegExecutable??""};var chooseFf=new Button{Left=340,Top=375,Width=165,Text="ffmpeg.exe wählen"};
    var downloadGain=new Button{Left=515,Top=332,Width=165,Text="MP3Gain ZIP laden"};var downloadFf=new Button{Left=515,Top=375,Width=165,Text="FFmpeg ZIP laden"};
    var gainPage=new LinkLabel{Left=18,Top=546,Width=175,Height=25,Text="MP3Gain-Downloadseite"};var ffPage=new LinkLabel{Left=200,Top=546,Width=180,Height=25,Text="FFmpeg-Downloadseite"};
    Action<string> openDownload=url=>{try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=url,UseShellExecute=true});}catch(Exception ex){MessageBox.Show(d,"Der Browser konnte nicht geöffnet werden. Bitte diese Adresse im Browser öffnen:\r\n"+url+"\r\n\r\n"+ex.Message,"Download");}};
    downloadGain.Click+=(sender,e)=>openDownload("https://sourceforge.net/projects/mp3gain/files/MP3Gain-Windows%20%28Stable%29/1.2.5/mp3gain-win-1_2_5.zip/download");
    downloadFf.Click+=(sender,e)=>openDownload("https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip");
    gainPage.LinkClicked+=(sender,e)=>openDownload("https://mp3gain.sourceforge.net/download.php");ffPage.LinkClicked+=(sender,e)=>openDownload("https://ffmpeg.org/download.html");
    Action<TextBox,string> choose=(box,name)=>{using(var file=new OpenFileDialog{Filter="Programm|"+name,Title=name+" auswählen"})if(file.ShowDialog(d)==DialogResult.OK){if(!Path.GetFileName(file.FileName).Equals(name,StringComparison.OrdinalIgnoreCase)){MessageBox.Show(d,"Bitte "+name+" auswählen.");return;}box.Text=file.FileName;}};
    chooseGain.Click+=(sender,e)=>choose(gainPath,"mp3gain.exe");chooseFf.Click+=(sender,e)=>choose(ffPath,"ffmpeg.exe");
    var hint=new Label{Left=18,Top=419,Width=660,Height=60,Text="ZIP laden, im Windows-Explorer vollständig entpacken, dann die EXE auswählen.\r\nFFmpeg: ffmpeg.exe und ffprobe.exe befinden sich im entpackten Unterordner bin.\r\nInternet ist nur zum Herunterladen erforderlich; danach arbeiten die Werkzeuge offline."};
    var ok=new Button{Left=410,Top=540,Width=130,Text="Übernehmen"};var no=new Button{Left=550,Top=540,Width=130,Text="Abbrechen",DialogResult=DialogResult.Cancel};
    ok.Click+=(sender,e)=>{try{var candidate=p.WithFolders(p.Folders);candidate.Normalize=mode.SelectedIndex==1;candidate.FfmpegLoudness=mode.SelectedIndex==2;candidate.TargetDb=(double)gainTarget.Value;candidate.TargetLufs=(double)lufsTarget.Value;candidate.Mp3Bitrate=(int)bitrate.SelectedItem;candidate.VariableBitrate=encoding.SelectedIndex==1;candidate.VbrQuality=quality.SelectedIndex;candidate.ReencodeMp3=reencode.Checked;candidate.GainExecutable=gainPath.Text;candidate.FfmpegExecutable=ffPath.Text;Gain.Validate(candidate);Ffmpeg.Validate(candidate,!String.IsNullOrWhiteSpace(ffPath.Text));p=candidate;dirty=true;UpdateStatus();d.DialogResult=DialogResult.OK;}catch(Exception ex){MessageBox.Show(d,ex.Message);}};
    mode.SelectedIndexChanged+=(sender,e)=>{gainTarget.Enabled=mode.SelectedIndex==1;lufsTarget.Enabled=mode.SelectedIndex==2;};gainTarget.Enabled=mode.SelectedIndex==1;lufsTarget.Enabled=mode.SelectedIndex==2;
    d.Controls.AddRange(new Control[]{downloadGain,downloadFf,gainPage,ffPage,encodingLabel,encoding,bitrateLabel,bitrate,qualityLabel,quality,bitrateHint,reencode,mode,modeLabel,info,gainLabel,gainTarget,lufsLabel,lufsTarget,peak,gainPath,chooseGain,ffPath,chooseFf,hint,ok,no});d.AcceptButton=ok;d.CancelButton=no;d.ShowDialog(v);
   }
  }
  void Help(){v.Report("Erste Schritte","1. ZIP-Dateien im Windows-Explorer entpacken.\r\nDateien und Ordner können aus dem Explorer direkt ins Fenster gezogen werden.\r\nDrag-and-drop ordnet nach Album-Tag ausschließlich in NEUE Ordner zu.\r\nBestehende Ordner bleiben unverändert; neue Nummern folgen am Ende.\r\nFehlt Album: Thema des ausgewählten Ordners als Vorlage; Ordnername bei Ordnerimport.\r\nFehlen Album und Themenvorlage, wird Neue Musik angelegt.\r\n2. 'Ordner laden': Themenordner oder übergeordneten Ordner wählen.\r\n3. Interpret und Titel direkt in der Tabelle bearbeiten.\r\n4. 'Ordner bearbeiten': Themenname und optional Sortiergruppe festlegen.\r\n   Zusammengehörige Country-Ordner erhalten dieselbe Sortiergruppe UND dasselbe Thema.\r\n   Verschiedene Chartjahre bleiben ohne gemeinsame Sortiergruppe.\r\n5. Mit ↑ / ↓ Ordnerreihenfolge festlegen.\r\n6. Mehr als 99 Titel werden automatisch auf Folgeordner aufgeteilt.\r\n   Nachfolgende Ordnernummern werden bei Bedarf weitergeschoben.\r\n   Sortiervorschau und Prüfen verwenden, Projekt speichern.\r\n7. Exportieren und die nummerierten Ordner auf den Musikbox-Datenträger kopieren.\r\n\r\nVersion 0.9: MP3 mit ID3v2.3 ohne Sonderflags oder ohne v2-Tag. Andere Varianten werden gemeldet.\r\nAlbum wird beim Export aus dem Thema gesetzt. Andere v2.3-Frames und eingebettete Bilder bleiben erhalten.\r\nID3v1 wird ergänzend neu geschrieben (30 Zeichen, begrenzter Zeichensatz).\r\nDie Dateinamen erhalten 01–99 als Sortierhilfe; die Anzeige verwendet die Tags.\r\nDie tatsächliche Sortierung und Coverauswahl muss an der Musikbox getestet werden.\r\nLeere Themenordner können angelegt werden. Musik wird über vorhandene Musikordner importiert.\r\nProjektdateien enthalten Verweise auf Quellen; Quellen nicht verschieben.\r\n\r\nLautstärke: über Audio-Einstellungen einrichten; wirkt nur auf Exportkopien.\r\nMP3Gain-Kommandozeilenprogramm erforderlich (mp3gain.exe, nicht MP3GainGUI.exe).\r\nFFmpeg konvertiert andere Formate beim Export nach MP3.\r\nFFmpeg-Lautheitsanpassung kodiert auch vorhandene MP3 neu.\r\nMP3-Standard: 192 kbit/s CBR. CBR-Bitraten und VBR-Qualität sind wählbar.\r\nDownload-Schaltflächen in Audio-Einstellungen öffnen die Windows-ZIP-Downloads im Browser.\r\nZIPs vollständig entpacken, dann die EXE auswählen.\r\nDie Fremdprogramme werden nicht automatisch installiert oder ausgeführt.");}
 }
 static class Program {
  [STAThread] static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);var v=new MainView();new Controller(v);Application.Run(v);}
 }
}
