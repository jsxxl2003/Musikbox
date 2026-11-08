using System;
using System.Drawing;
using System.Windows.Forms;
namespace Musikbox {
 public static class AppMessage {
  public static DialogResult Show(IWin32Window owner,string text,string title="Hinweis",MessageBoxButtons buttons=MessageBoxButtons.OK,MessageBoxIcon icon=MessageBoxIcon.None){using(var view=new MessageView(text,title,buttons,icon))return owner==null?view.ShowDialog():view.ShowDialog(owner);}
 }
 public sealed class MessageView:ModernForm {
  readonly DialogResult dismissed;
  public MessageView(string text,string title,MessageBoxButtons buttons,MessageBoxIcon icon){
   Text=title;StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;Font=new Font("Segoe UI",10);ClientSize=new Size(610,Math.Max(150,Math.Min(530,TextRenderer.MeasureText(text,Font,new Size(570,10000),TextFormatFlags.WordBreak).Height+85)));Padding=new Padding(12);
   var surface=new StudioTextSurface{Dock=DockStyle.Fill};surface.Content.Text=text;Controls.Add(surface);
   if(icon!=MessageBoxIcon.None)Controls.Add(new StudioNoticeIcon(icon){Dock=DockStyle.Left,Width=45});
   var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=42,Padding=new Padding(0,6,0,0),FlowDirection=FlowDirection.RightToLeft,BackColor=ModernTheme.Background,Tag="StudioExact"};Controls.Add(actions);
   if(buttons==MessageBoxButtons.YesNo){var no=new StudioActionButton{Text="Nein",DialogResult=DialogResult.No,Width=100,Height=30};var yes=new StudioActionButton{Text="Ja",Primary=true,DialogResult=DialogResult.Yes,Width=100,Height=30};actions.Controls.Add(no);actions.Controls.Add(yes);AcceptButton=yes;CancelButton=no;dismissed=DialogResult.No;}else{var ok=new StudioActionButton{Text="OK",Primary=true,DialogResult=DialogResult.OK,Width=100,Height=30};actions.Controls.Add(ok);AcceptButton=CancelButton=ok;dismissed=DialogResult.OK;}
   Shown+=(s,e)=>{var button=AcceptButton as Control;if(button!=null)button.Focus();};
  }
  protected override void OnFormClosing(FormClosingEventArgs e){if(DialogResult==DialogResult.None||DialogResult==DialogResult.Cancel)DialogResult=dismissed;base.OnFormClosing(e);}
 }
}
