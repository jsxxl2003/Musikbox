using System;using System.Linq;using System.Drawing;using System.Collections.Generic;using System.Windows.Forms;
namespace Musikbox {
 public class TagEditorView:ModernForm {
  readonly Dictionary<string,TextBox> values=new Dictionary<string,TextBox>();readonly Dictionary<string,CheckBox> changes=new Dictionary<string,CheckBox>();
  public Dictionary<string,string> Edits{get{return changes.Where(x=>x.Value.Checked).ToDictionary(x=>x.Key,x=>values[x.Key].Text);}}
  public bool SetArtist{get{return changes["artist"].Checked;}}public bool SetTitle{get{return changes["title"].Checked;}}public bool SetAlbum{get{return changes["album"].Checked;}}
  public string ArtistValue{get{return values["artist"].Text;}}public string TitleValue{get{return values["title"].Text;}}public string AlbumValue{get{return values["album"].Text;}}
  public TagEditorView(List<Song> songs,string theme){
   Text="Tags bearbeiten – "+songs.Count+" Titel";Width=650;Height=820;MinimumSize=new Size(500,500);StartPosition=FormStartPosition.CenterParent;Padding=new Padding(12);
   var editor=new StudioScrollPanel{Dock=DockStyle.Fill,Gradient=false,Fill=ModernTheme.Background,Border=Color.Transparent,Radius=0};
   var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,Padding=new Padding(0,10,0,0)};
   var save=MainView.StudioButton("Speichern",()=>{if(Edits.Count==0)return;if(Edits.Any(x=>(x.Key=="artist"||x.Key=="title"||x.Key=="album")&&String.IsNullOrWhiteSpace(x.Value))&&AppMessage.Show(this,"Ein angehaktes Feld ist leer. Dieses Feld bei den ausgewählten Dateien leeren?","Tags leeren",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;DialogResult=DialogResult.OK;},true);
   var cancel=MainView.StudioButton("Abbrechen",()=>{DialogResult=DialogResult.Cancel;});save.Enabled=false;bottom.Controls.Add(save);bottom.Controls.Add(cancel);Controls.Add(editor);Controls.Add(bottom);
   var captions=new List<Control>();var boxes=new List<TextBox>();bool loading=true;
   foreach(var field in TagSchema.Fields){var f=field;Control label;if(f.Writable){var flag=new CheckBox{Text=f.Name+" · "+f.Format,AutoSize=false,ForeColor=ModernTheme.Ink};changes.Add(f.Key,flag);label=flag;flag.CheckedChanged+=(s,e)=>save.Enabled=changes.Values.Any(x=>x.Checked);}else label=new Label{Text=f.Name+" · "+f.Format+" · schreibgeschützt",ForeColor=ModernTheme.Muted};
    var distinct=songs.Select(x=>TagSchema.Value(x,f.Key)).Distinct().Take(2).ToArray();var box=new StudioTextBox{ReadOnly=!f.Writable,Text=distinct.Length==1?distinct[0]:"",BorderStyle=BorderStyle.FixedSingle};values.Add(f.Key,box);captions.Add(label);boxes.Add(box);editor.Controls.Add(label);editor.Controls.Add(box);box.TextChanged+=(s,e)=>{if(!loading&&f.Writable)changes[f.Key].Checked=true;};
   }
   Action arrange=()=>{float scale=DeviceDpi/96f;int row=(int)(70*scale),gap=(int)(8*scale),width=Math.Max(60,editor.ClientSize.Width-editor.RailWidth-gap*2);for(int i=0;i<captions.Count;i++){int y=i*row-editor.ScrollOffset;captions[i].SetBounds(gap,y+gap,width,(int)(24*scale));boxes[i].SetBounds(gap,y+(int)(34*scale),width,(int)(28*scale));}editor.ContentHeight=row*captions.Count+gap;};editor.SizeChanged+=(s,e)=>arrange();Shown+=(s,e)=>arrange();loading=false;AcceptButton=save;CancelButton=cancel;
  }
 }
}
