using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
namespace Musikbox {
 public static class TitleCoverChoice {
  public static void Validate(string path){if(String.IsNullOrEmpty(path)||!File.Exists(path))throw new Exception("Das gewählte Cover ist nicht mehr erreichbar.");Tags.ArtworkFrame(path);using(var image=Image.FromFile(path)){} }
  public static bool Apply(Song song,string path){Validate(path);if(song.Artwork==path)return false;song.Artwork=path;return true;}
 }
 public class TitleCoverChoiceView:ModernForm {
  public readonly Button UseSingle=new Button{Text="Single-Cover übernehmen",Dock=DockStyle.Fill,Enabled=false},UseFolder=new Button{Text="Ordnercover als Titelcover",Dock=DockStyle.Fill,Enabled=false};
  public readonly PictureBox SinglePreview=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(241,244,248)},FolderPreview=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(241,244,248)};
  public string SelectedArtwork {get;private set;}
  public bool AppendArtwork{get{return append.Checked;}}
  readonly CheckBox append=new CheckBox{Text="Vorhandene Cover ergänzen",AutoSize=true};
  readonly string folderCover;string singleCover;
  public TitleCoverChoiceView(Song song,string folderCover,string singleCover=null,Func<Song,CoverSearchView> searchFactory=null){
   this.folderCover=folderCover;this.singleCover=singleCover;
   Text="Cover für diesen Titel auswählen";Width=860;Height=590;MinimumSize=new Size(740,500);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
   var panel=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(16),ColumnCount=2,RowCount=6};panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));foreach(int size in new[]{72,28})panel.RowStyles.Add(new RowStyle(SizeType.Absolute,size));panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));foreach(int size in new[]{44,40,44})panel.RowStyles.Add(new RowStyle(SizeType.Absolute,size));Controls.Add(panel);
   var note=new Label{Dock=DockStyle.Fill,AutoEllipsis=true,Text=song.Artist+" – "+song.Title+"\r\nWelches Cover soll dieser einzelne Titel bekommen?\r\nDas Themenordnercover bleibt erhalten. Mit ‚Behalten‘ bleibt das bisherige Titelcover bestehen."};panel.Controls.Add(note,0,0);panel.SetColumnSpan(note,2);
   panel.Controls.Add(new Label{Text="Dein Themenordnercover · Orientierung an der Musikbox",Dock=DockStyle.Fill},0,1);panel.Controls.Add(new Label{Text="Single-Cover aus der Online-Suche",Dock=DockStyle.Fill},1,1);panel.Controls.Add(FolderPreview,0,2);panel.Controls.Add(SinglePreview,1,2);panel.Controls.Add(UseFolder,0,3);panel.Controls.Add(UseSingle,1,3);
   var search=new Button{Text="Single-Cover suchen …",Name="SingleCoverSearch",Dock=DockStyle.Fill};search.Click+=(s,e)=>{using(var d=searchFactory==null?new CoverSearchView(song.Artist,song.Title):searchFactory(song))if(d.ShowDialog(this)==DialogResult.OK)SelectArtwork(d.SelectedArtwork);};panel.Controls.Add(search,1,4);
   var local=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true};var choose=new Button{Text="Bilddatei wählen …",AutoSize=true};choose.Click+=(s,e)=>{using(var d=new AppOpenFileDialog{Filter="Coverbilder|*.jpg;*.jpeg;*.png"})if(d.ShowDialog(this)==DialogResult.OK)SelectArtwork(d.FileName);};local.Controls.Add(choose);local.Controls.Add(append);panel.Controls.Add(local,0,4);var folderNote=new Label();
   var keep=new Button{Text="Bisheriges Titelcover behalten / schließen",Dock=DockStyle.Fill,DialogResult=DialogResult.Cancel};panel.Controls.Add(keep,0,5);panel.SetColumnSpan(keep,2);CancelButton=keep;
   UseSingle.Enabled=LoadPreview(SinglePreview,singleCover);UseFolder.Enabled=LoadPreview(FolderPreview,folderCover);folderNote.Text=UseFolder.Enabled?"Dein Themenbild wird auch für diesen einzelnen Titel verwendet.":"Ordnercover nicht verfügbar: "+FolderPreview.AccessibleDescription;
   UseSingle.Click+=(s,e)=>SelectArtwork(this.singleCover);UseFolder.Click+=(s,e)=>SelectArtwork(this.folderCover);
   FormClosed+=(s,e)=>{if(SinglePreview.Image!=null)SinglePreview.Image.Dispose();if(FolderPreview.Image!=null)FolderPreview.Image.Dispose();};
  }
  bool LoadPreview(PictureBox box,string path){if(box.Image!=null){box.Image.Dispose();box.Image=null;}try{TitleCoverChoice.Validate(path);using(var image=Image.FromFile(path))box.Image=new Bitmap(image);return true;}catch(Exception ex){box.AccessibleDescription=ex.Message;return false;}}
  void SelectArtwork(string path){try{TitleCoverChoice.Validate(path);SelectedArtwork=path;DialogResult=DialogResult.OK;}catch(Exception ex){AppMessage.Show(this,ex.Message,"Cover nicht übernommen");}}
 }
}
