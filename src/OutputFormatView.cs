using System;using System.Drawing;using System.Linq;using System.Text.RegularExpressions;using System.Windows.Forms;
namespace Musikbox {public class OutputFormatView:ModernForm {
 readonly CheckedListBox tokens=new StudioCheckedListBox{Dock=DockStyle.Fill,CheckOnClick=true,BackColor=ModernTheme.Navy,ForeColor=ModernTheme.Ink,BorderStyle=BorderStyle.None};readonly TextBox text=new StudioTextBox{Dock=DockStyle.Fill};bool changing;
 public string Pattern{get{return text.Text;}}
 public OutputFormatView(string pattern){Text="Ausgabeformat und Metadatenfelder";Width=640;Height=520;StartPosition=FormStartPosition.CenterParent;var bar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=50};var row=new Panel{Dock=DockStyle.Top,Height=64,Padding=new Padding(8)};row.Controls.Add(text);text.Text=pattern;Controls.Add(tokens);Controls.Add(row);Controls.Add(bar);var keys=OutputFormatSettings.Keys(pattern).Concat(OutputFormatSettings.Available).Distinct().ToArray();foreach(var key in keys)tokens.Items.Add("%"+key+"%",OutputFormatSettings.Keys(pattern).Contains(key));
 tokens.ItemCheck+=(s,e)=>{if(changing)return;string token=(string)tokens.Items[e.Index];if(e.NewValue==CheckState.Checked){if(!text.Text.Contains(token))text.Text+=(text.Text.Length>0?" - ":"")+token;}else text.Text=text.Text.Replace(token,"");};
 text.TextChanged+=(s,e)=>{if(changing)return;changing=true;try{var selected=OutputFormatSettings.Keys(text.Text);for(int j=0;j<tokens.Items.Count;j++)tokens.SetItemChecked(j,selected.Contains(((string)tokens.Items[j]).Trim('%')));}finally{changing=false;}};
 Action<int> move=delta=>{int index=tokens.SelectedIndex,next=index+delta;if(index<0||next<0||next>=tokens.Items.Count)return;string a=(string)tokens.Items[index],b=(string)tokens.Items[next];bool ac=tokens.GetItemChecked(index),bc=tokens.GetItemChecked(next);changing=true;try{tokens.Items[index]=b;tokens.Items[next]=a;tokens.SetItemChecked(index,bc);tokens.SetItemChecked(next,ac);tokens.SelectedIndex=next;string marker="\u0001";if(text.Text.Contains(a)&&text.Text.Contains(b))text.Text=text.Text.Replace(a,marker).Replace(b,a).Replace(marker,b);}finally{changing=false;}};
 bar.Padding=new Padding(10,7,10,7);bar.Height=62;bar.BackColor=ModernTheme.Navy;
 var up=new Button{Text="Nach oben",Width=126,Height=38};
 var down=new Button{Text="Nach unten",Width=126,Height=38};
 var save=new Button{Text="Speichern",Width=126,Height=38,Tag="Primary"};
 var cancel=new Button{Text="Abbrechen",Width=126,Height=38};
 up.Click+=(s,e)=>move(-1);down.Click+=(s,e)=>move(1);
 save.Click+=(s,e)=>DialogResult=DialogResult.OK;
 cancel.Click+=(s,e)=>DialogResult=DialogResult.Cancel;
 bar.Controls.AddRange(new Control[]{up,down,save,cancel});
 AcceptButton=save;CancelButton=cancel;
 ModernTheme.Apply(this);
 text.BackColor=ModernTheme.Silver;text.ForeColor=ModernTheme.Ink;text.BorderStyle=BorderStyle.FixedSingle;
 foreach(var button in new[]{up,down,save,cancel}){
  button.UseVisualStyleBackColor=false;button.FlatStyle=FlatStyle.Flat;
  button.FlatAppearance.BorderColor=ModernTheme.Edge;
  button.BackColor=button==save?ModernTheme.Accent:ModernTheme.Silver;
  button.ForeColor=ModernTheme.Ink;
 }
 }
}}
