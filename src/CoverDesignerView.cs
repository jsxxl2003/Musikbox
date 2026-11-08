using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace Musikbox {
 public class CoverDesignerView:ModernForm {
  public CoverDesign Design;
  readonly PictureBox preview=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(235,240,245)};
  readonly ListBox texts=new StudioListBox{FormattingEnabled=true,Width=280,Height=95};readonly TextBox value=new StudioTextBox{Width=280};readonly NumericUpDown size=new StudioNumericUpDown{Minimum=10,Maximum=350,Width=100};readonly CheckBox bold=new CheckBox{Text="Fett",AutoSize=true};
  readonly string folder,theme;bool loading,dragging;Point origin;float startX,startY;
  CoverText Current {get{return texts.SelectedItem as CoverText;}}
  public CoverDesignerView(CoverDesign design,string name,string topic){Design=CoverArt.Copy(design);folder=name;theme=topic;Text="Cover-Designer";Width=1050;Height=750;MinimumSize=new Size(850,620);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);BackColor=ModernTheme.Navy;
   var layout=new SplitContainer{Dock=DockStyle.Fill,Width=1000,SplitterDistance=310,Panel1MinSize=295,Panel2MinSize=400};var controls=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=false,BackColor=ModernTheme.Navy,Padding=new Padding(12)};layout.Panel1.Controls.Add(new StudioModuleScroll(controls){Dock=DockStyle.Fill});layout.Panel2.Controls.Add(preview);Controls.Add(layout);
   Action<string,Action> button=(title,action)=>{var b=new StudioActionButton{Text=title,Width=280,Height=34,Primary=title=="Cover übernehmen"};b.Click+=(s,e)=>{try{action();}catch(Exception ex){AppMessage.Show(this,ex.Message,"Cover-Designer");}};controls.Controls.Add(b);};
   controls.Controls.Add(new Label{Text="TEXTFELDER",AutoSize=true});controls.Controls.Add(texts);controls.Controls.Add(value);controls.Controls.Add(size);controls.Controls.Add(bold);
   button("Text hinzufügen",()=>{Design.Texts.Add(new CoverText{Text="Neuer Text",Y=600});LoadTexts(Design.Texts.Count-1);});button("Text zentrieren",()=>{if(Current==null)return;Current.X=50;Current.Alignment=1;Draw();});button("Text links ausrichten",()=>{if(Current==null)return;Current.Alignment=0;Draw();});
   button("Text entfernen",()=>{if(Current==null)return;Design.Texts.Remove(Current);LoadTexts(0);});
   button("Schriftart wählen",()=>{if(Current==null)return;using(var d=new AppFontDialog{Font=new Font(Current.Font,Current.Size,Current.Bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel)})if(d.ShowDialog(this)==DialogResult.OK){Current.Font=d.Font.Name;Current.Bold=d.Font.Bold;Current.Size=Math.Max(10,Math.Min(350,d.Font.SizeInPoints*96/72));LoadCurrent();}});
   button("Textfarbe wählen",()=>{if(Current==null)return;Choose(Current.Color,c=>Current.Color=c);});button("Hintergrundfarbe",()=>Choose(Design.Background,c=>Design.Background=c));button("Zweite Verlaufsfarbe",()=>Choose(Design.EndColor,c=>Design.EndColor=c));
   var gradient=new CheckBox{Text="Farbverlauf",Checked=Design.Gradient,AutoSize=true};gradient.CheckedChanged+=(s,e)=>{Design.Gradient=gradient.Checked;Draw();};controls.Controls.Add(gradient);
   var border=new CheckBox{Text="Heller Rahmen",Checked=Design.Border,AutoSize=true};border.CheckedChanged+=(s,e)=>{Design.Border=border.Checked;Draw();};controls.Controls.Add(border);
   button("Hintergrundbild auswählen",()=>{using(var d=new AppOpenFileDialog{Filter="Bilder|*.jpg;*.jpeg;*.png"})if(d.ShowDialog(this)==DialogResult.OK){using(var test=Image.FromFile(d.FileName)){}Design.ImagePath=d.FileName;Draw();}});button("Hintergrundbild entfernen",()=>{Design.ImagePath=null;Draw();});
   controls.Controls.Add(new Label{Text="Text im Vorschaubild ziehen.\r\nPlatzhalter: {Thema}, {Jahr}, {Nummer}\r\nTexte bleiben im Projekt bearbeitbar.",Width=280,Height=60});
   var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=48,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(8),BackColor=ModernTheme.Navy};var cancelButton=new StudioActionButton{Text="Abbrechen",AutoSize=true,DialogResult=DialogResult.Cancel};var acceptButton=new StudioActionButton{Text="Cover übernehmen",AutoSize=true,Primary=true};acceptButton.Click+=(s,e)=>{DialogResult=DialogResult.OK;Close();};footer.Controls.Add(cancelButton);footer.Controls.Add(acceptButton);Controls.Add(footer);CancelButton=cancelButton;
   texts.Format+=(s,e)=>e.Value=((CoverText)e.ListItem).Text;texts.SelectedIndexChanged+=(s,e)=>LoadCurrent();
   value.TextChanged+=(s,e)=>{if(!loading&&Current!=null){Current.Text=value.Text;Draw();}};size.ValueChanged+=(s,e)=>{if(!loading&&Current!=null){Current.Size=(float)size.Value;Draw();}};bold.CheckedChanged+=(s,e)=>{if(!loading&&Current!=null){Current.Bold=bold.Checked;Draw();}};
   preview.MouseDown+=(s,e)=>{if(Current==null||e.Button!=MouseButtons.Left)return;dragging=true;origin=e.Location;startX=Current.X;startY=Current.Y;preview.Capture=true;};
   preview.MouseMove+=(s,e)=>{if(!dragging||Current==null)return;float scale=Math.Min(preview.ClientSize.Width,preview.ClientSize.Height)/1200f;if(scale<=0)return;Current.X=Math.Max(0,Math.Min(1100,startX+(e.X-origin.X)/scale));Current.Y=Math.Max(0,Math.Min(1150,startY+(e.Y-origin.Y)/scale));Draw();};preview.MouseUp+=(s,e)=>{dragging=false;preview.Capture=false;};LoadTexts(0);FormClosed+=(s,e)=>{if(preview.Image!=null)preview.Image.Dispose();};
  }
  void LoadTexts(int index){texts.DataSource=null;texts.DataSource=Design.Texts;if(Design.Texts.Count>0)texts.SelectedIndex=Math.Min(index,Design.Texts.Count-1);LoadCurrent();}
  void LoadCurrent(){loading=true;if(Current!=null){value.Text=Current.Text;size.Value=(decimal)Math.Max(10,Math.Min(350,Current.Size));bold.Checked=Current.Bold;}value.Enabled=size.Enabled=bold.Enabled=Current!=null;loading=false;Draw();}
  void Choose(int old,Action<int> set){using(var d=new AppColorDialog{Color=Color.FromArgb(old)})if(d.ShowDialog(this)==DialogResult.OK){set(d.Color.ToArgb());Draw();}}
  void Draw(){var next=CoverArt.Render(Design,folder,theme,700);var old=preview.Image;preview.Image=next;if(old!=null)old.Dispose();}
 }
}
