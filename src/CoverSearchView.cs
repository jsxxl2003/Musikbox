using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
namespace Musikbox {
 public class CoverSearchView:ModernForm {
  public string SelectedArtwork;
  public readonly TextBox ArtistName=new StudioTextBox{Dock=DockStyle.Fill},TitleName=new StudioTextBox{Dock=DockStyle.Fill},FreeName=new StudioTextBox{Dock=DockStyle.Fill};
  public readonly ComboBox Mode=new StudioComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList};
  public readonly ListBox Results=new StudioListBox{Dock=DockStyle.Fill,HorizontalScrollbar=true};
  public readonly PictureBox PreviewImage=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=ModernTheme.Silver};
  public readonly Label State=new Label{Dock=DockStyle.Bottom,Height=68,Padding=new Padding(10)};
  public readonly Button Apply=new StudioActionButton{Text="Cover übernehmen",Primary=true,Width=190,Height=38,Enabled=false},Find=new StudioActionButton{Text="Suchen",Width=110,Height=38};
  readonly Func<string,string,CoverNameMode,string,CancellationToken,List<CoverMatch>> lookup;
  readonly Func<CoverMatch,CancellationToken,string> fetch;
  CancellationTokenSource request,download;bool closing,loadingResults;
  CoverNameMode SearchMode {get{return (CoverNameMode)Mode.SelectedIndex;}}
  public CoverSearchView(string artist,string title,bool autoStart=true,Func<string,string,CoverNameMode,string,CancellationToken,List<CoverMatch>> search=null,Func<CoverMatch,CancellationToken,string> downloadImage=null){
   lookup=search??((a,t,m,n,token)=>CoverSearch.Search(a,t,token,m,n));fetch=downloadImage??((match,token)=>CoverSearch.Download(match,token));
   Text="Titelcover nach Namen suchen";Width=1050;Height=700;MinimumSize=new Size(860,590);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);BackColor=ModernTheme.Navy;ArtistName.Text=artist??"";TitleName.Text=title??"";
   var top=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(8),ColumnCount=4,RowCount=4};top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,90));top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,95));top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));for(int i=0;i<4;i++)top.RowStyles.Add(new RowStyle(SizeType.AutoSize));top.GrowStyle=TableLayoutPanelGrowStyle.FixedSize;
   var note=new Label{Dock=DockStyle.Fill,AutoSize=true,Text="Wortteile kombinieren, z. B. „par ans“ unter Freie Namenssuche. Reihenfolge egal.\r\nSucht in Interpreten-, Titel- und Veröffentlichungsnamen. Cover erst nach Auswahl übernehmen."};top.Controls.Add(note,0,0);top.SetColumnSpan(note,4);Action wrapNote=()=>{note.MaximumSize=new Size(Math.Max(100,top.ClientSize.Width-top.Padding.Horizontal-8),0);};top.SizeChanged+=(s,e)=>wrapNote();wrapNote();
   top.Controls.Add(new Label{Text="Interpret",AutoSize=true,Anchor=AnchorStyles.Left},0,1);top.Controls.Add(ArtistName,1,1);top.Controls.Add(new Label{Text="Titel",AutoSize=true,Anchor=AnchorStyles.Left},2,1);top.Controls.Add(TitleName,3,1);
   Mode.Items.AddRange(new object[]{"Interpret und/oder Titel","Nur Interpret","Nur Titel","Freie Namenssuche"});top.Controls.Add(new Label{Text="Suchart",AutoSize=true,Anchor=AnchorStyles.Left},0,2);top.Controls.Add(Mode,1,2);top.Controls.Add(new Label{Text="Freier Name",AutoSize=true,Anchor=AnchorStyles.Left},2,2);top.Controls.Add(FreeName,3,2);Mode.SelectedIndexChanged+=(s,e)=>{ArtistName.Enabled=SearchMode==CoverNameMode.Automatic||SearchMode==CoverNameMode.Artist;TitleName.Enabled=SearchMode==CoverNameMode.Automatic||SearchMode==CoverNameMode.Title;FreeName.Enabled=SearchMode==CoverNameMode.FreeName;};Mode.SelectedIndex=0;
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=false};var browser=new StudioActionButton{Text="Bildersuche im Browser",Width=220,Height=38};var local=new StudioActionButton{Text="Bilddatei auswählen",Width=190,Height=38};buttons.Controls.Add(Find);buttons.Controls.Add(local);top.Controls.Add(buttons,0,3);top.SetColumnSpan(buttons,4);Find.Click+=(s,e)=>Search();browser.Click+=(s,e)=>{try{string url=CoverSearch.BrowserUrl(ArtistName.Text,TitleName.Text,SearchMode,FreeName.Text);State.Text=url;State.Text="Bildersuche geöffnet. Ein passendes Bild speichern und hier über ‚Bilddatei auswählen‘ laden.";}catch(Exception ex){State.Text=ex.Message;}};
   local.Click+=(s,e)=>{using(var d=new AppOpenFileDialog{Filter="Coverbild|*.jpg;*.jpeg;*.png",Title="Ein eigenes oder gespeichertes Coverbild auswählen"})if(d.ShowDialog(this)==DialogResult.OK)try{string path=CoverSearch.ImportImage(d.FileName);CancelRequests();Results.DataSource=null;Display(path);State.Text="Bilddatei geladen. Bitte prüfen und den Vorschlag ausdrücklich auswählen.";}catch(Exception ex){State.Text=ex.Message;}};
   var split=new SplitContainer{Dock=DockStyle.Fill,Width=1000,SplitterDistance=520};split.Panel1.Controls.Add(Results);split.Panel2.Controls.Add(PreviewImage);
   var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(8)};bottom.Controls.Add(Apply);var close=new StudioActionButton{Text="Schließen",Width=130,Height=38,DialogResult=DialogResult.Cancel};bottom.Controls.Add(close);Apply.Click+=(s,e)=>ConfirmArtwork();
   Controls.Add(split);Controls.Add(State);Controls.Add(bottom);Controls.Add(top);AcceptButton=Find;CancelButton=close;Results.SelectedIndexChanged+=(s,e)=>{if(!loadingResults)Preview();};FormClosing+=(s,e)=>{closing=true;CancelRequests();};FormClosed+=(s,e)=>{DisposePreview();if(DialogResult!=DialogResult.OK)SelectedArtwork=null;};Results.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter&&Apply.Enabled){ConfirmArtwork();e.Handled=true;e.SuppressKeyPress=true;}};Action changed=()=>{CancelRequests();loadingResults=true;Results.DataSource=null;loadingResults=false;ClearPreview();State.Text="Suchnamen geändert. Mit Suchen neue Vorschläge laden.";};ArtistName.TextChanged+=(s,e)=>changed();TitleName.TextChanged+=(s,e)=>changed();FreeName.TextChanged+=(s,e)=>changed();Mode.SelectedIndexChanged+=(s,e)=>changed();ModernTheme.Apply(this);if(autoStart)Shown+=(s,e)=>Search();
  }
  void CancelRequests(){if(request!=null){request.Cancel();request=null;}if(download!=null){download.Cancel();download=null;}}
  void ClearPreview(){SelectedArtwork=null;Apply.Enabled=false;AcceptButton=Find;DisposePreview();}
  void DisposePreview(){if(PreviewImage.Image!=null){PreviewImage.Image.Dispose();PreviewImage.Image=null;}}
  void ConfirmArtwork(){if(Apply.Enabled&&!String.IsNullOrEmpty(SelectedArtwork)){DialogResult=DialogResult.OK;Close();}}
  protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(keyData==Keys.Enter&&Apply.Enabled){ConfirmArtwork();return true;}return base.ProcessCmdKey(ref msg,keyData);}
  void Display(string path){using(var image=Image.FromFile(path)){var previous=PreviewImage.Image;PreviewImage.Image=new Bitmap(image);if(previous!=null)previous.Dispose();}SelectedArtwork=path;Apply.Enabled=true;AcceptButton=Apply;}
  async void Search(){CancelRequests();ClearPreview();loadingResults=true;Results.DataSource=null;loadingResults=false;var source=request=new CancellationTokenSource();State.Text="Suche nach Namen im Internet …";
   try{string a=ArtistName.Text,t=TitleName.Text,n=FreeName.Text;var mode=SearchMode;var matches=await Task.Run(()=>lookup(a,t,mode,n,source.Token));if(closing||source!=request||source.IsCancellationRequested)return;loadingResults=true;try{Results.DataSource=matches;Results.SelectedIndex=-1;}finally{loadingResults=false;}State.Text=matches.Count==0?"Keine Veröffentlichung zu diesen Namen gefunden. Bitte nur Interpret, nur Titel oder die freie Namenssuche versuchen. Alternativ: Bildersuche im Browser.":matches.Count+" Veröffentlichungen gefunden. Einen Eintrag auswählen, um dessen Cover zu laden. Nicht jede Ausgabe besitzt ein Cover.";
   }catch(OperationCanceledException){}catch(Exception ex){if(!closing&&source==request)State.Text=ex.Message;}finally{source.Dispose();if(request==source)request=null;}
  }
  async void Preview(){if(download!=null){download.Cancel();download=null;}ClearPreview();var match=Results.SelectedItem as CoverMatch;if(match==null)return;var source=download=new CancellationTokenSource();State.Text="Lade Cover zu "+match.Release+" …";
   try{string path=await Task.Run(()=>fetch(match,source.Token));if(closing||source!=download||source.IsCancellationRequested)return;Display(path);State.Text=(match.GroupCoverUsed?"Cover der Album-/Single-Gruppe; es kann von einer anderen Ausgabe stammen. ":"")+"Bild prüfen und auswählen. Mit Cover übernehmen oder Enter endgültig bestätigen.";}
   catch(OperationCanceledException){}catch(Exception ex){if(!closing&&source==download){ClearPreview();State.Text=ex.Message;}}finally{source.Dispose();if(download==source)download=null;}
  }
 }
}
