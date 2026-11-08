using System;using System.Drawing;using System.Windows.Forms;
namespace Musikbox {
 public class StudioAutoScrollPanel:StudioScrollPanel {
  bool scrolling,arranging;public new bool AutoScroll{get{return scrolling;}set{scrolling=value;if(!value){ScrollTo(0);ContentHeight=0;}PerformLayout();}}
  protected override void OnLayout(LayoutEventArgs e){if(arranging){base.OnLayout(e);return;}arranging=true;try{base.OnLayout(e);int height=0;foreach(Control c in Controls){if(c is StudioScrollRail)continue;if(c.Dock==DockStyle.Top){c.Top=-ScrollOffset;height=Math.Max(height,c.Height);}else if(c.Dock!=DockStyle.Fill)height=Math.Max(height,c.Bottom+ScrollOffset);}ContentHeight=scrolling?height:0;}finally{arranging=false;}}
 }
}
