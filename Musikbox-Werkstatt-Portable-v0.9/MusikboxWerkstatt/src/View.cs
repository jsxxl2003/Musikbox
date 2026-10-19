using System;
using System.Drawing;
using System.Windows.Forms;
namespace Musikbox {
 public class MainView:Form {
  public event Action<string[],int> PathsDropped;
  public Func<bool> CanDrop=()=>true;
  readonly System.Collections.Generic.HashSet<Control> dropControls=new System.Collections.Generic.HashSet<Control>();
  public ListBox Folders=new ListBox{Dock=DockStyle.Fill,SelectionMode=SelectionMode.One};
  public DataGridView Songs=new DataGridView{Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=true,BackgroundColor=Color.White,BorderStyle=BorderStyle.None};
  public ToolStrip Tools=new ToolStrip{GripStyle=ToolStripGripStyle.Hidden,Padding=new Padding(8),LayoutStyle=ToolStripLayoutStyle.Flow,AutoSize=true};
  public Label Status=new Label{Dock=DockStyle.Bottom,Height=30,Padding=new Padding(10,6,0,0)};
  public Label Details=new Label{Dock=DockStyle.Top,Height=55,Padding=new Padding(8)};
  public PictureBox Cover=new PictureBox{Dock=DockStyle.Bottom,Height=140,SizeMode=PictureBoxSizeMode.Zoom};
  public ProgressBar Progress=new ProgressBar{Dock=DockStyle.Bottom,Height=8};
  public ToolStripButton Cancel=new ToolStripButton("Abbrechen"){Enabled=false};
  public MainView() {
   Text="Musikbox-Werkstatt · Portable Testversion 0.9";Width=1250;Height=780;MinimumSize=new Size(960,600);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);
   var heading=new Label{Text="MUSIKBOX-WERKSTATT",Dock=DockStyle.Top,Height=55,Padding=new Padding(16,12,0,0),Font=new Font("Segoe UI",18,FontStyle.Bold),BackColor=Color.FromArgb(29,43,61),ForeColor=Color.White};
   var split=new SplitContainer{Width=1200,Dock=DockStyle.Fill,SplitterDistance=300,Panel1MinSize=220,Panel2MinSize=400};
   split.Panel1.Controls.Add(Folders);split.Panel1.Controls.Add(Cover);
   var label=new Label{Text="Themenordner · eigene Reihenfolge",Dock=DockStyle.Top,Height=30,Padding=new Padding(5)};split.Panel1.Controls.Add(label);
   split.Panel2.Controls.Add(Songs);split.Panel2.Controls.Add(Details);
   foreach(string col in new[]{"Interpret","Titel","Album"})Songs.Columns.Add(col,col);
   var source=new DataGridViewTextBoxColumn{Name="Quelle",HeaderText="Quelldatei",ReadOnly=true,FillWeight=65};Songs.Columns.Add(source);
   Controls.Add(split);Controls.Add(Progress);Controls.Add(Status);Controls.Add(Tools);Controls.Add(heading);
   EnableDrop(this);
   Songs.EditingControlShowing+=(sender,e)=>EnableDrop(e.Control);
  }
  void EnableDrop(Control control) {
   if(!dropControls.Add(control))return;control.AllowDrop=true;
   control.DragEnter+=(sender,e)=>e.Effect=CanDrop()&&e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;
   control.DragOver+=(sender,e)=>e.Effect=CanDrop()&&e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;
   control.DragDrop+=(sender,e)=>{
    if(!CanDrop()||!e.Data.GetDataPresent(DataFormats.FileDrop))return;
    var paths=e.Data.GetData(DataFormats.FileDrop) as string[];if(paths==null||paths.Length==0)return;
    int selected=Folders.SelectedIndex;
    if(Object.ReferenceEquals(sender,Folders)){int hit=Folders.IndexFromPoint(Folders.PointToClient(new Point(e.X,e.Y)));if(hit>=0)selected=hit;}
    if(PathsDropped!=null)PathsDropped(paths,selected);
   };
   foreach(Control child in control.Controls)EnableDrop(child);
  }
  public ToolStripButton Button(string title,Action action) {var b=new ToolStripButton(title);b.Click+=(s,e)=>action();Tools.Items.Add(b);return b;}
  public string Ask(string title,string initial) {
   using(var d=new Form{Text=title,Width=540,Height=165,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}) {
    var text=new TextBox{Left=15,Top=15,Width=490,Text=initial??""};var ok=new Button{Text="Übernehmen",Left=280,Top=55,Width=110,DialogResult=DialogResult.OK};var cancel=new Button{Text="Abbrechen",Left=395,Top=55,Width=110,DialogResult=DialogResult.Cancel};d.Controls.AddRange(new Control[]{text,ok,cancel});d.AcceptButton=ok;d.CancelButton=cancel;return d.ShowDialog(this)==DialogResult.OK?text.Text:null;
   }
  }
  public void Report(string title,string text) {
   using(var d=new Form{Text=title,Width=850,Height=600,StartPosition=FormStartPosition.CenterParent}) {
    d.Controls.Add(new TextBox{Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Both,WordWrap=false,Text=text,Font=new Font("Consolas",10)});d.ShowDialog(this);
   }
  }
 }
}
