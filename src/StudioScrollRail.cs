using System;using System.Drawing;using System.Windows.Forms;
namespace Musikbox {
 public sealed class StudioScrollRail:Control {
  int position,maximum,viewport=1,content=1,anchor,anchorValue,pageDirection;bool dragging,hover;readonly Timer repeat=new Timer();double wheelRemainder;
  public bool Horizontal{get;set;}public int SmallChange{get;set;}=24;
  public event EventHandler ValueChanged;
  public int Value{get{return position;}set{int next=Math.Max(0,Math.Min(maximum,value));if(position==next)return;position=next;Invalidate();ValueChanged?.Invoke(this,EventArgs.Empty);}}
  public int Maximum{get{return maximum;}}public int Viewport{get{return viewport;}}
  public StudioScrollRail(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);BackColor=ModernTheme.Navy;ForeColor=ModernTheme.Accent;Tag="StudioExact";TabStop=true;AccessibleRole=AccessibleRole.ScrollBar;AccessibleName="Bildlauf";repeat.Interval=350;repeat.Tick+=(s,e)=>{repeat.Interval=60;Value+=pageDirection*viewport;};}
  public void SetRange(int page,int length){viewport=Math.Max(1,page);content=Math.Max(viewport,length);maximum=content-viewport;Value=Math.Min(position,maximum);Invalidate();}
  int Length{get{return Horizontal?Width:Height;}}int Cross{get{return Horizontal?Height:Width;}}int At(Point p){return Horizontal?p.X:p.Y;}
  public Rectangle ThumbBounds{get{int inset=Math.Max(1,(int)Math.Round(2*StudioDrawing.Scale(this)));int track=Math.Max(1,Length-2*inset),length=Math.Min(track,Math.Max((int)(24*StudioDrawing.Scale(this)),(int)Math.Round(track*viewport/(double)content)));int start=inset+(maximum==0?0:(int)Math.Round((track-length)*position/(double)maximum)),width=Math.Min(Cross,Math.Max(3,(int)(5*StudioDrawing.Scale(this))));return Horizontal?new Rectangle(start,(Cross-width)/2,length,width):new Rectangle((Cross-width)/2,start,width,length);}}
  protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);if(maximum<=0)return;var r=ThumbBounds;if(r.Width<1||r.Height<1)return;e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var path=StudioDrawing.Round(r,Math.Max(1,Math.Min(r.Width,r.Height)/2f)))using(var b=new SolidBrush(hover||dragging?Color.FromArgb(195,135,255):ForeColor))e.Graphics.FillPath(b,path);}
  protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hover=true;Invalidate();}protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=false;Invalidate();}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||maximum==0)return;Focus();Capture=true;var thumb=ThumbBounds;int at=At(e.Location),start=Horizontal?thumb.Left:thumb.Top,end=Horizontal?thumb.Right:thumb.Bottom;if(at>=start&&at<end){dragging=true;anchor=at;anchorValue=position;}else{pageDirection=at<start?-1:1;Value+=pageDirection*viewport;repeat.Interval=350;repeat.Start();}}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!dragging)return;var thumb=ThumbBounds;int travel=Length-2*Math.Max(1,(int)(2*StudioDrawing.Scale(this)))-(Horizontal?thumb.Width:thumb.Height);if(travel>0)Value=anchorValue+(int)Math.Round((At(e.Location)-anchor)*maximum/(double)travel);}
  protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;repeat.Stop();Capture=false;}protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture){dragging=false;repeat.Stop();}}
  public void ScrollWheel(int delta){int lines=SystemInformation.MouseWheelScrollLines;if(lines==0)return;wheelRemainder+=delta/120.0*(lines<0?viewport:lines*Math.Max(1,SmallChange));int step=(int)wheelRemainder;wheelRemainder-=step;Value-=step;}
  protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);ScrollWheel(e.Delta);}
  protected override bool IsInputKey(Keys key){switch(key&Keys.KeyCode){case Keys.Up:case Keys.Down:case Keys.Left:case Keys.Right:case Keys.Home:case Keys.End:case Keys.PageUp:case Keys.PageDown:return true;}return base.IsInputKey(key);}
  protected override void OnKeyDown(KeyEventArgs e){switch(e.KeyCode){case Keys.Up:case Keys.Left:Value-=SmallChange;break;case Keys.Down:case Keys.Right:Value+=SmallChange;break;case Keys.PageUp:Value-=viewport;break;case Keys.PageDown:Value+=viewport;break;case Keys.Home:Value=0;break;case Keys.End:Value=maximum;break;default:base.OnKeyDown(e);return;}e.Handled=e.SuppressKeyPress=true;}
  protected override void Dispose(bool disposing){if(disposing)repeat.Dispose();base.Dispose(disposing);}
 }
}
