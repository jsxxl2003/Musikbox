using System;using System.Drawing;using System.Windows.Forms;
namespace Musikbox {
 // Application-owned scrollbar. There is no native scrollbar to flash through,
 // and painting never opens, disables or replaces a Windows visual-style handle.
 public class StudioScrollPanel:StudioPanel,IMessageFilter {
  readonly StudioScrollRail rail;int offset,contentHeight;bool filterInstalled,shifting;
  public int ScrollOffset{get{return offset;}}
  public int RailWidth{get{return Math.Max(8,(int)Math.Round(10*StudioDrawing.Scale(this)));}}
  public StudioScrollRail ScrollRail{get{return rail;}}
  public int ContentHeight{get{return contentHeight;}set{contentHeight=Math.Max(0,value);UpdateRange();}}
  public StudioScrollPanel(){base.AutoScroll=false;rail=new StudioScrollRail{Name="Studio-Bildlauf"};rail.ValueChanged+=(s,e)=>MoveContent(rail.Value);Controls.Add(rail);}
  void MoveContent(int next){int delta=offset-next;if(delta==0)return;offset=next;shifting=true;SuspendLayout();try{foreach(Control c in Controls)if(c!=rail)c.Top+=delta;}finally{ResumeLayout(false);shifting=false;}Invalidate();}
  void UpdateRange(){if(rail==null||shifting)return;rail.SetRange(ClientSize.Height,contentHeight);rail.SetBounds(Math.Max(0,ClientSize.Width-RailWidth),0,RailWidth,Math.Max(0,ClientSize.Height));rail.Visible=rail.Maximum>0;rail.BringToFront();}
  public void ScrollTo(int position){rail.Value=position;}
  public void EnsureVisible(Control child){if(child.Parent!=this||child==rail)return;if(child.Top<0)ScrollTo(offset+child.Top);else if(child.Bottom>ClientSize.Height)ScrollTo(offset+child.Bottom-ClientSize.Height);}
  protected override void OnControlAdded(ControlEventArgs e){base.OnControlAdded(e);if(e.Control==rail||e.Control is StudioScrollRail)return;e.Control.Enter+=(s,a)=>EnsureVisible(e.Control);}
  protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);UpdateRange();}
  protected override void OnMouseWheel(MouseEventArgs e){rail.ScrollWheel(e.Delta);var handled=e as HandledMouseEventArgs;if(handled!=null)handled.Handled=true;}
  public bool PreFilterMessage(ref Message m){if(m.Msg!=0x20A||!Visible||rail.Maximum==0)return false;var target=Control.FromHandle(m.HWnd);for(var c=target;c!=null;c=c.Parent)if(c is StudioScrollFrame)return false;else if(c==this){rail.ScrollWheel((short)((m.WParam.ToInt64()>>16)&0xffff));return true;}return false;}
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);if(!filterInstalled){Application.AddMessageFilter(this);filterInstalled=true;}}
  protected override void OnHandleDestroyed(EventArgs e){if(filterInstalled){Application.RemoveMessageFilter(this);filterInstalled=false;}base.OnHandleDestroyed(e);}
  protected override void Dispose(bool disposing){if(filterInstalled){Application.RemoveMessageFilter(this);filterInstalled=false;}base.Dispose(disposing);}
 }
}
