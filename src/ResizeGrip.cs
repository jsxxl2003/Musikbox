using System;
using System.Drawing;
using System.Windows.Forms;
namespace Musikbox {
 public sealed class ResizeGrip:Control {
  readonly Form window;bool resizing;Point startCursor;Size startSize;
  public ResizeGrip(Form window){this.window=window;Size=new Size(18,18);Cursor=Cursors.SizeNWSE;AccessibleName="Fenstergröße ändern";AccessibleDescription="Ziehen, um Breite und Höhe des Fensters anzupassen.";TabStop=false;DoubleBuffered=true;}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var light=new Pen(Color.FromArgb(242,245,250)))using(var shade=new Pen(Color.FromArgb(99,112,132))){for(int offset=4;offset<=14;offset+=4){e.Graphics.DrawLine(light,Width-offset-1,Height-2,Width-2,Height-offset-1);e.Graphics.DrawLine(shade,Width-offset-2,Height-2,Width-2,Height-offset-2);}}}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;startCursor=PointToScreen(e.Location);startSize=window.Size;resizing=true;Capture=true;}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!resizing)return;Point now=PointToScreen(e.Location);int width=Math.Max(window.MinimumSize.Width,startSize.Width+now.X-startCursor.X),height=Math.Max(window.MinimumSize.Height,startSize.Height+now.Y-startCursor.Y);if(window.MaximumSize.Width>0)width=Math.Min(width,window.MaximumSize.Width);if(window.MaximumSize.Height>0)height=Math.Min(height,window.MaximumSize.Height);window.Size=new Size(width,height);}
  protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button!=MouseButtons.Left)return;resizing=false;Capture=false;}
  protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)resizing=false;}
 }
}
