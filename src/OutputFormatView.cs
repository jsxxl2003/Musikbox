using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace Musikbox {
 public class OutputFormatView : ModernForm {
  readonly CheckedListBox tokens = new CheckedListBox {
   Dock=DockStyle.Fill, CheckOnClick=true, BorderStyle=BorderStyle.None,
   BackColor=ModernTheme.Navy, ForeColor=ModernTheme.Ink,
   DrawMode=DrawMode.Normal, IntegralHeight=false
  };
  readonly TextBox text = new TextBox {
   Dock=DockStyle.Fill, BorderStyle=BorderStyle.FixedSingle,
   BackColor=ModernTheme.Silver, ForeColor=ModernTheme.Ink
  };
  bool changing;
  public string Pattern { get { return text.Text; } }
  public OutputFormatView(string pattern) {
   Text="Ausgabeformat und Metadatenfelder"; Width=640; Height=520;
   StartPosition=FormStartPosition.CenterParent;
   BackColor=ModernTheme.Background; ForeColor=ModernTheme.Ink;
   var row=new Panel { Dock=DockStyle.Top, Height=60, Padding=new Padding(12),BackColor=ModernTheme.Background };
   var bar=new FlowLayoutPanel { Dock=DockStyle.Bottom, Height=66, Padding=new Padding(10,10,8,8),BackColor=ModernTheme.Background,WrapContents=false };
   row.Controls.Add(text);
   Controls.Add(tokens);Controls.Add(row);Controls.Add(bar);
   text.Text=pattern??"";
   var selected=OutputFormatSettings.Keys(text.Text);
   foreach(var key in selected.Concat(OutputFormatSettings.Available).Distinct())
    tokens.Items.Add("%"+key+"%",selected.Contains(key));
   tokens.ItemCheck+=(sender,e)=>{
    if(changing)return;
    BeginInvoke((Action)(()=>RebuildPattern()));
   };
   text.TextChanged+=(sender,e)=>{
    if(changing)return;
    changing=true;
    try {
     var keys=OutputFormatSettings.Keys(text.Text);
     for(int i=0;i<tokens.Items.Count;i++)
      tokens.SetItemChecked(i,keys.Contains(((string)tokens.Items[i]).Trim('%')));
    } finally { changing=false; }
   };
   Action<int> move=delta=>{
    int index=tokens.SelectedIndex,next=index+delta;
    if(index<0||next<0||next>=tokens.Items.Count)return;
    string a=(string)tokens.Items[index],b=(string)tokens.Items[next];
    bool ac=tokens.GetItemChecked(index),bc=tokens.GetItemChecked(next);
    changing=true;
    try {
     tokens.Items[index]=b;tokens.Items[next]=a;
     tokens.SetItemChecked(index,bc);tokens.SetItemChecked(next,ac);
     tokens.SelectedIndex=next;
    } finally { changing=false; }
    RebuildPattern();
   };
   var up=new StudioActionButton{Text="Nach oben",Width=128,Height=40};
   var down=new StudioActionButton{Text="Nach unten",Width=128,Height=40};
   var save=new StudioActionButton{Text="Speichern",Primary=true,Width=128,Height=40};
   var cancel=new StudioActionButton{Text="Abbrechen",Width=128,Height=40};
   up.Click+=(sender,e)=>move(-1);
   down.Click+=(sender,e)=>move(1);
   save.Click+=(sender,e)=>{DialogResult=DialogResult.OK;Close();};
   cancel.Click+=(sender,e)=>{DialogResult=DialogResult.Cancel;Close();};
   bar.Controls.AddRange(new Control[]{up,down,save,cancel});
   AcceptButton=save;CancelButton=cancel;
   ModernTheme.Apply(this);
   // Native text box and custom-painted studio buttons: no white Windows theme surfaces.
   text.BackColor=ModernTheme.Silver;text.ForeColor=ModernTheme.Ink;
   tokens.BackColor=ModernTheme.Navy;tokens.ForeColor=ModernTheme.Ink;
  }
  void RebuildPattern() {
   if(changing)return;
   changing=true;
   try {
    text.Text=String.Join(" - ",tokens.CheckedItems.Cast<string>());
   } finally { changing=false; }
  }
 }
}
