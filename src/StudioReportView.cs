using System;
using System.Drawing;
using System.Windows.Forms;
namespace Musikbox {
 public sealed class StudioTextSurface:StudioPanel {
  public readonly TextBox Content=new StudioTextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,WordWrap=true,BackColor=ModernTheme.Navy,ForeColor=ModernTheme.Ink,Font=StudioDrawing.Font(14),Tag="StudioExact"};
  public StudioTextSurface(){Fill=End=ModernTheme.Navy;Gradient=false;Padding=new Padding(14);Controls.Add(Content);}
 }
 public sealed class StudioReportView:ModernForm {
  public readonly StudioTextSurface Body=new StudioTextSurface{Dock=DockStyle.Fill};
  public StudioReportView(string title,string text){
   Text=title;ClientSize=new Size(840,500);MinimumSize=new Size(540,300);StartPosition=FormStartPosition.CenterParent;Padding=new Padding(16);Tag="StudioExact";
   var heading=new StudioLabel{Text=title,TextPixels=22,Bold=true,Font=StudioDrawing.Font(22,true),Dock=DockStyle.Top,Height=42,ForeColor=ModernTheme.Ink,BackColor=Color.Transparent};
   Body.Content.Text=text??"";Controls.Add(Body);Controls.Add(heading);
   var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=50,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(0,10,0,0),BackColor=Color.Transparent,Tag="StudioExact"};
   var close=new StudioActionButton{Text="Schließen",Primary=true,Width=125,Height=38,DialogResult=DialogResult.OK};
   var copy=new StudioActionButton{Text="Kopieren",Width=115,Height=38};copy.Click+=(s,e)=>{if(!String.IsNullOrEmpty(Body.Content.Text))Clipboard.SetText(Body.Content.Text);};
   actions.Controls.Add(close);actions.Controls.Add(copy);Controls.Add(actions);AcceptButton=CancelButton=close;Shown+=(s,e)=>close.Focus();
  }
 }
}
