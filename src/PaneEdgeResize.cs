using System;
using System.Drawing;
using System.Windows.Forms;
namespace Musikbox {
 // Docked edges resize the shared layout division, preserving the actual controls.
 public sealed class PaneEdgeResize {
  readonly WorkspacePane pane; readonly MainView main;
  bool dragging; int edges; Point start; Action<int> horizontal,vertical;
  public PaneEdgeResize(WorkspacePane pane,MainView main){
   this.pane=pane;this.main=main;
   pane.MouseDown+=Down;pane.MouseMove+=Move;pane.MouseUp+=Up;
   pane.MouseCaptureChanged+=(s,e)=>{if(!pane.Capture){dragging=false;pane.Cursor=Cursors.Default;}};
   pane.MouseLeave+=(s,e)=>{if(!dragging)pane.Cursor=Cursors.Default;};
  }
  int Hit(Point p){int result=0;if(p.X<4)result|=1;else if(p.X>=pane.Width-4)result|=2;if(p.Y<4)result|=4;else if(p.Y>=pane.Height-4)result|=8;return result;}
  void Down(object sender,MouseEventArgs e){
   if(e.Button!=MouseButtons.Left||pane.IsFloating)return;edges=Hit(e.Location);if(edges==0)return;
   horizontal=Target(true);vertical=Target(false);start=pane.PointToScreen(e.Location);dragging=true;pane.Capture=true;
  }
  void Move(object sender,MouseEventArgs e){
   if(!dragging){int hit=Hit(e.Location);pane.Cursor=(hit&3)!=0?( (hit&12)!=0?((hit==5||hit==10)?Cursors.SizeNWSE:Cursors.SizeNESW):Cursors.SizeWE):(hit&12)!=0?Cursors.SizeNS:Cursors.Default;return;}
   Point now=pane.PointToScreen(e.Location);
   if((edges&3)!=0)horizontal((now.X-start.X)*((edges&1)!=0?-1:1));
   if((edges&12)!=0)vertical((now.Y-start.Y)*((edges&4)!=0?-1:1));
  }
  void Up(object sender,MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;dragging=false;pane.Capture=false;pane.Cursor=Cursors.Default;}
  Action<int> Target(bool width){
   if(!width&&(pane==main.PlayerPane||pane==main.SongPane)&&!main.PlayerPane.IsHidden&&!main.PlayerPane.IsFloating&&main.PlayerPane.Dock==DockStyle.Bottom){
    int initial=main.PlayerPane.Height;int available=main.PlayerPane.Parent.ClientSize.Height;
    return delta=>main.PlayerPane.Height=Math.Max(100,Math.Min(Math.Max(100,available-120),initial+(pane==main.PlayerPane?delta:-delta)));
   }
   for(Control parent=pane.Parent;parent!=null;parent=parent.Parent){
    var panel=parent as SplitterPanel;if(panel==null)continue;var split=panel.Parent as SplitContainer;
    if(split==null||split.Panel1Collapsed||split.Panel2Collapsed||(split.Orientation==Orientation.Vertical)!=width)continue;
    int initial=split.SplitterDistance;bool first=panel==split.Panel1;
    return delta=>{int total=width?split.ClientSize.Width:split.ClientSize.Height;int max=total-split.SplitterWidth-split.Panel2MinSize;if(max>=split.Panel1MinSize)split.SplitterDistance=Math.Max(split.Panel1MinSize,Math.Min(max,initial+(first?delta:-delta)));};
   }
   // No neighbour on this axis: grow/shrink the containing workspace.
   int size=width?main.Width:main.Height;
   return delta=>{if(width)main.Width=Math.Max(main.MinimumSize.Width,size+delta);else main.Height=Math.Max(main.MinimumSize.Height,size+delta);};
  }
 }
}
