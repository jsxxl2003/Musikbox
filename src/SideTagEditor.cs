using System;using System.Drawing;using System.Linq;using System.Collections.Generic;using System.Windows.Forms;
namespace Musikbox {
 public class SideTagEditor:UserControl {
  public event Action ApplyRequested,WriteFilesRequested,TrackNumbersRequested;
  public readonly TextBox Artist=Field(),Title=Field(),Album=Field(),Year=Field(),Genre=Field(),Comment=Field(),Track=Field(),Acoustid=Field(),DirectoryName=Field(),FolderName=Field();
  public readonly CheckBox ChangeArtist=Flag("Interpret"),ChangeTitle=Flag("Titel"),ChangeAlbum=Flag("Album"),ChangeYear=Flag("Jahr"),ChangeGenre=Flag("Genre"),ChangeComment=Flag("Kommentar"),ChangeTrack=Flag("Track"),ChangeAcoustid=Flag("AcoustID");
  readonly StudioPanel card=new StudioPanel();readonly Label heading;readonly StudioActionButton apply;readonly Dictionary<string,StudioPanel> boxes=new Dictionary<string,StudioPanel>();readonly Dictionary<string,Label> labels=new Dictionary<string,Label>();readonly Dictionary<string,TextBox> fields;bool loading;
  static TextBox Field(){return new StudioTextBox{BorderStyle=BorderStyle.None,Font=StudioDrawing.Font(12),BackColor=StudioDrawing.Hex("#272537"),ForeColor=Color.White,Tag="StudioExact"};}static CheckBox Flag(string s){return new CheckBox{Text=s,Visible=false};}
  public bool Pending{get{return new[]{ChangeArtist,ChangeTitle,ChangeAlbum,ChangeYear,ChangeGenre,ChangeComment,ChangeTrack,ChangeAcoustid}.Any(f=>f.Checked);}}
  public SideTagEditor(){Tag="StudioExact";BackColor=Color.Transparent;Dock=DockStyle.Fill;Height=410;card.Dock=DockStyle.Fill;Controls.Add(card);heading=new StudioLabel{Text="Metadaten",TextPixels=17,Bold=true,Font=StudioDrawing.Font(17,true),ForeColor=ModernTheme.Ink,BackColor=Color.Transparent,Tag="StudioExact"};card.Controls.Add(heading);apply=new StudioActionButton{Text="Speichern",Primary=true};apply.Click+=(s,e)=>{if(ApplyRequested!=null)ApplyRequested();};card.Controls.Add(apply);
   fields=new Dictionary<string,TextBox>{{"artist",Artist},{"title",Title},{"album",Album},{"year",Year},{"genre",Genre},{"track",Track},{"acoustid_id",Acoustid},{"_directory",DirectoryName},{"ordner",FolderName}};
   string[] names={"Interpret","Titel","Album","Jahr","Genre","Track","AcoustID","Quellordner","Ordner"};var flags=new[]{ChangeArtist,ChangeTitle,ChangeAlbum,ChangeYear,ChangeGenre,ChangeTrack,ChangeAcoustid};int i=0;
   foreach(var pair in fields){string key=pair.Key;var label=new StudioLabel{Text=names[i],TextPixels=12,Font=StudioDrawing.Font(12),TextAlign=ContentAlignment.MiddleLeft,ForeColor=ModernTheme.Muted,BackColor=Color.Transparent,Tag="StudioExact"};labels.Add(key,label);card.Controls.Add(label);var box=new StudioPanel{Radius=7,Fill=StudioDrawing.Hex("#272537"),Gradient=false,Border=ModernTheme.Edge};box.Controls.Add(pair.Value);boxes.Add(key,box);card.Controls.Add(box);if(i<flags.Length){var flag=flags[i];pair.Value.TextChanged+=(s,e)=>{if(!loading)flag.Checked=true;};}i++;}
   DirectoryName.ReadOnly=FolderName.ReadOnly=true;
   var menu=new ContextMenuStrip{BackColor=ModernTheme.Silver,ForeColor=ModernTheme.Ink};
   menu.Items.Add("Metadatenfelder auswählen …",null,(sender,e)=>ChooseFields());
   ModernTheme.Apply(menu);
   card.ContextMenuStrip=menu;
   var choose=new StudioActionButton{Text="Felder auswählen",Width=136,Height=34};
   choose.Click+=(sender,e)=>ChooseFields();
   card.Controls.Add(choose);
   Resize+=(sender,e)=>{Arrange();choose.SetBounds(Math.Max(8,Width-152),Math.Max(8,Height-46),136,34);};
   Arrange();choose.SetBounds(Math.Max(8,Width-152),Math.Max(8,Height-46),136,34);
  }
  void ChooseFields(){using(var dialog=new OutputFormatView(OutputFormatSettings.Pattern))if(dialog.ShowDialog(FindForm())==DialogResult.OK){OutputFormatSettings.Pattern=dialog.Pattern;ApplyFormat();}}\n  public void ApplyFormat(){Arrange();}
  void Arrange(){if(fields==null)return;float scale=1;
#if NET8_0_OR_GREATER
   scale=DeviceDpi/96f;
#endif
   Func<float,int> p=x=>(int)Math.Round(x*scale);heading.SetBounds(p(16),p(23),p(130),p(25));apply.SetBounds(Math.Max(p(154),Width-p(118)),p(16),p(102),p(42));foreach(var key in fields.Keys){boxes[key].Visible=false;labels[key].Visible=false;}int n=0;foreach(string key in OutputFormatSettings.Keys(OutputFormatSettings.Pattern)){if(!fields.ContainsKey(key))continue;var box=boxes[key];var label=labels[key];label.Visible=box.Visible=true;label.SetBounds(p(16),p(66+n*44),p(82),p(36));box.SetBounds(p(102),p(66+n*44),Math.Max(p(70),Width-p(118)),p(36));var field=fields[key];field.Font=StudioDrawing.Font(12*scale);field.SetBounds(p(10),p(9),Math.Max(20,box.Width-p(20)),p(20));n++;}MinimumSize=new Size(0,p(Math.Max(180,120+n*44)));}
  public void ShowSelection(List<Song> songs,string theme){loading=true;try{heading.Text=songs.Count>1?"Metadaten ("+songs.Count+")":"Metadaten";Artist.Text=TagEditing.Common(songs,s=>s.Artist);Title.Text=TagEditing.Common(songs,s=>s.Title);Album.Text=TagEditing.Common(songs,s=>s.Album);Year.Text=TagEditing.Common(songs,s=>s.Year);Genre.Text=TagEditing.Common(songs,s=>s.Genre);Comment.Text=TagEditing.Common(songs,s=>s.Comment);Track.Text=TagEditing.Common(songs,s=>s.Track);Acoustid.Text=TagEditing.Common(songs,s=>s.AcoustidId);DirectoryName.Text=TagEditing.Common(songs,s=>TagSchema.Value(s,"_directory"));FolderName.Text=TagEditing.Common(songs,s=>s.FolderName);foreach(var flag in new[]{ChangeArtist,ChangeTitle,ChangeAlbum,ChangeYear,ChangeGenre,ChangeComment,ChangeTrack,ChangeAcoustid})flag.Checked=false;foreach(var field in fields.Values)field.ReadOnly=songs.Count==0||field==DirectoryName||field==FolderName;Arrange();}finally{loading=false;}}
 }
}
