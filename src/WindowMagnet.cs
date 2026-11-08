using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
namespace Musikbox {
 public static class MagnetGeometry {
  static bool Overlap(int a,int sizeA,int b,int sizeB){return Math.Min(a+sizeA,b+sizeB)-Math.Max(a,b)>=24;}
  public static bool Joined(Rectangle a,Rectangle b){return ((Math.Abs(a.Right-b.Left)<=2||Math.Abs(b.Right-a.Left)<=2)&&Overlap(a.Top,a.Height,b.Top,b.Height))||((Math.Abs(a.Bottom-b.Top)<=2||Math.Abs(b.Bottom-a.Top)<=2)&&Overlap(a.Left,a.Width,b.Left,b.Width));}
  public static Point Snap(Rectangle moving,IEnumerable<Rectangle> others,Rectangle screen,int distance=12){
   int dx=distance+1,dy=distance+1;Action<int> x=n=>{if(Math.Abs(n)<=distance&&Math.Abs(n)<Math.Abs(dx))dx=n;};Action<int> y=n=>{if(Math.Abs(n)<=distance&&Math.Abs(n)<Math.Abs(dy))dy=n;};
   x(screen.Left-moving.Left);x(screen.Right-moving.Right);y(screen.Top-moving.Top);y(screen.Bottom-moving.Bottom);
   foreach(var other in others){if(Overlap(moving.Top,moving.Height,other.Top,other.Height)){x(other.Left-moving.Right);x(other.Right-moving.Left);}if(Overlap(moving.Left,moving.Width,other.Left,other.Width)){y(other.Top-moving.Bottom);y(other.Bottom-moving.Top);}if(Math.Abs(other.Left-moving.Right)<=distance||Math.Abs(other.Right-moving.Left)<=distance){y(other.Top-moving.Top);y(other.Bottom-moving.Bottom);}if(Math.Abs(other.Top-moving.Bottom)<=distance||Math.Abs(other.Bottom-moving.Top)<=distance){x(other.Left-moving.Left);x(other.Right-moving.Right);}}
   return new Point(Math.Abs(dx)<=distance?dx:0,Math.Abs(dy)<=distance?dy:0);
  }
 }
 public sealed class WindowMagnet {
  readonly HashSet<Form> registered=new HashSet<Form>();Dictionary<Form,Rectangle> start;Form root;bool updating;
  public bool Enabled=true;
  public WindowMagnet(MainView main){Register(main);}
  public void Register(Form form){if(!registered.Add(form))return;form.ResizeBegin+=(s,e)=>Begin(form,(Control.ModifierKeys&Keys.Shift)!=0);form.Move+=(s,e)=>{if(root==form&&!updating&&start!=null&&form.Size==start[form].Size)Move(form,form.Location);};form.ResizeEnd+=(s,e)=>End();form.Disposed+=(s,e)=>{registered.Remove(form);if(root==form)End();};}
  IEnumerable<Form> VisibleWindows(){return registered.Where(f=>!f.IsDisposed&&f.Visible&&f.WindowState==FormWindowState.Normal);}
  public void Begin(Form form,bool detach){root=form;start=new Dictionary<Form,Rectangle>{{form,form.Bounds}};if(!Enabled||detach)return;var all=VisibleWindows().ToList();bool changed;do{changed=false;foreach(var item in all)if(!start.ContainsKey(item)&&start.Values.Any(b=>MagnetGeometry.Joined(b,item.Bounds))){start.Add(item,item.Bounds);changed=true;}}while(changed);}
  public void Move(Form form,Point location){if(updating||root!=form||start==null)return;var initial=start[form];int dx=location.X-initial.X,dy=location.Y-initial.Y;var union=start.Values.Aggregate(Rectangle.Union);union.Offset(dx,dy);if(Enabled){var adjustment=MagnetGeometry.Snap(union,VisibleWindows().Where(f=>!start.ContainsKey(f)).Select(f=>f.Bounds),Screen.FromRectangle(union).WorkingArea);dx+=adjustment.X;dy+=adjustment.Y;}updating=true;try{foreach(var item in start)if(!item.Key.IsDisposed)item.Key.Location=new Point(item.Value.X+dx,item.Value.Y+dy);}finally{updating=false;}}
  public void End(){root=null;start=null;}
 }
}
