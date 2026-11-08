using System;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace Musikbox {
 // One scrollable toolbar region above the shared list. Tall wrapped toolbars
 // remain reachable without adding another file list or covering the table.
 public sealed class StudioModuleScroll:StudioScrollPanel {
  readonly Control content;bool arranging;
  public int NaturalHeight{get{if(content==null)return 0;if(content is FlowLayoutPanel)return content.GetPreferredSize(new Size(Math.Max(1,ClientSize.Width-RailWidth),0)).Height;int height=content.Padding.Vertical;foreach(Control c in content.Controls){if(c.Dock==DockStyle.Top)height+=c is FlowLayoutPanel?c.GetPreferredSize(new Size(Math.Max(1,content.ClientSize.Width-content.Padding.Horizontal),0)).Height:c.Height;else if(c.Dock==DockStyle.Bottom)height+=c.Height;else if(c.Dock==DockStyle.Fill)height+=76;}return height;}}
  public StudioModuleScroll(Control content){this.content=content;Radius=0;Fill=End=ModernTheme.Background;Border=Color.Transparent;Gradient=false;content.Dock=DockStyle.None;Controls.Add(content);}
  protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);Arrange();}
  protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);Arrange();}
  void Arrange(){if(content==null||arranging)return;arranging=true;try{content.Width=Math.Max(1,ClientSize.Width-RailWidth);content.Height=Math.Max(ClientSize.Height,NaturalHeight);content.Left=0;ContentHeight=content.Height;content.Top=-ScrollOffset;}finally{arranging=false;}}
 }
}
