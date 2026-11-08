using System;using System.Drawing;using System.Windows.Forms;using System.Runtime.InteropServices;
namespace Musikbox {
 public class StudioListBox:ListBox {
  int horizontalOffset;public int StudioHorizontalOffset{get{return horizontalOffset;}set{horizontalOffset=Math.Max(0,value);Invalidate();}}
  readonly StudioScrollbars.NativeStyleGate gate;
  protected override CreateParams CreateParams{get{var p=base.CreateParams;p.Style&=~StudioScrollbars.NativeStyles;return p;}}
  protected override void OnParentChanged(EventArgs e){base.OnParentChanged(e);StudioScrollbars.EnsureFrame(this);}
  public StudioListBox(){gate=new StudioScrollbars.NativeStyleGate(this);BackColor=ModernTheme.Silver;ForeColor=ModernTheme.Ink;BorderStyle=BorderStyle.FixedSingle;DrawMode=DrawMode.OwnerDrawFixed;ItemHeight=24;}
  protected override void OnDrawItem(DrawItemEventArgs e){using(var b=new SolidBrush((e.State&DrawItemState.Selected)!=0?ModernTheme.Selection:BackColor))e.Graphics.FillRectangle(b,e.Bounds);if(e.Index>=0&&e.Index<Items.Count){var state=e.Graphics.Save();try{e.Graphics.SetClip(e.Bounds,System.Drawing.Drawing2D.CombineMode.Intersect);var text=Rectangle.Inflate(e.Bounds,-4,0);text.X-=horizontalOffset;text.Width+=horizontalOffset;TextRenderer.DrawText(e.Graphics,GetItemText(Items[e.Index]),Font,text,ForeColor,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix|TextFormatFlags.PreserveGraphicsClipping|(HorizontalScrollbar?0:TextFormatFlags.EndEllipsis));}finally{e.Graphics.Restore(state);}}}
 }
 public class StudioProgressBar:ProgressBar {
  public StudioProgressBar(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=ModernTheme.Navy;ForeColor=ModernTheme.Accent;}
  protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(ModernTheme.Navy);var r=Rectangle.Inflate(ClientRectangle,-1,-1);using(var brush=new SolidBrush(ModernTheme.Edge))e.Graphics.FillRectangle(brush,r);r.Width=(int)(r.Width*Math.Max(0,Math.Min(1,(Value-Minimum)/(double)Math.Max(1,Maximum-Minimum))));using(var brush=new SolidBrush(ModernTheme.Accent))e.Graphics.FillRectangle(brush,r);}
 }
 public class StudioTabControl:TabControl {
  public StudioTabControl(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=ModernTheme.Navy;ForeColor=ModernTheme.Ink;}
  protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(ModernTheme.Navy);for(int i=0;i<TabCount;i++){var r=GetTabRect(i);using(var brush=new SolidBrush(i==SelectedIndex?ModernTheme.Selection:ModernTheme.Silver))e.Graphics.FillRectangle(brush,r);TextRenderer.DrawText(e.Graphics,TabPages[i].Text,Font,r,ModernTheme.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}}
 }
 public class StudioNumericUpDown:NumericUpDown {
  public StudioNumericUpDown(){BorderStyle=BorderStyle.FixedSingle;BackColor=ModernTheme.Silver;ForeColor=ModernTheme.Ink;foreach(Control child in Controls)if(!(child is TextBox)){var button=child;button.Paint+=(s,e)=>{var r=button.ClientRectangle;using(var brush=new SolidBrush(ModernTheme.Silver))e.Graphics.FillRectangle(brush,r);using(var pen=new Pen(Enabled?ModernTheme.Accent:ModernTheme.Muted,1.5f)){int x=r.Width/2;for(int part=0;part<2;part++){int y=(part==0?r.Height/4:3*r.Height/4),d=part==0?-1:1;e.Graphics.DrawLines(pen,new[]{new Point(x-3,y-d),new Point(x,y+d),new Point(x+3,y-d)});}}};}}
 }
 public class StudioListView:ListView {
  HeaderSurface header;readonly StudioScrollbars.NativeStyleGate scrollGate;
  protected override CreateParams CreateParams{get{var p=base.CreateParams;p.Style&=~StudioScrollbars.NativeStyles;return p;}}
  protected override void OnParentChanged(EventArgs e){base.OnParentChanged(e);StudioScrollbars.EnsureFrame(this);}
  [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr handle,int msg,IntPtr w,IntPtr l);
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);if(Environment.OSVersion.Platform==PlatformID.Win32NT){IntPtr h=SendMessage(Handle,0x101F,IntPtr.Zero,IntPtr.Zero);if(h!=IntPtr.Zero){header=new HeaderSurface(this);header.AssignHandle(h);}}}
  protected override void OnHandleDestroyed(EventArgs e){if(header!=null){header.ReleaseHandle();header=null;}base.OnHandleDestroyed(e);}
  sealed class HeaderSurface:NativeWindow {
   readonly StudioListView owner;public HeaderSurface(StudioListView owner){this.owner=owner;}
   [StructLayout(LayoutKind.Sequential)]struct Rect{public int Left,Top,Right,Bottom;}
   [DllImport("user32.dll")]static extern bool GetClientRect(IntPtr h,out Rect rect);
   [DllImport("user32.dll",EntryPoint="SendMessageW")]static extern IntPtr ItemRect(IntPtr h,int msg,IntPtr w,ref Rect rect);
   protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0xF||m.Msg==0x14||m.Msg==0x5){Rect client;if(!GetClientRect(Handle,out client))return;int right=0;for(int i=0;i<owner.Columns.Count;i++){var r=new Rect();ItemRect(Handle,0x1207,new IntPtr(i),ref r);right=Math.Max(right,r.Right);}if(right<client.Right)using(var g=Graphics.FromHwnd(Handle))using(var b=new SolidBrush(ModernTheme.NavyAlt))g.FillRectangle(b,right,0,client.Right-right,client.Bottom);}}
  }

  public StudioListView(){scrollGate=new StudioScrollbars.NativeStyleGate(this);OwnerDraw=true;BackColor=ModernTheme.Silver;ForeColor=ModernTheme.Ink;}
  protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e){using(var b=new SolidBrush(ModernTheme.NavyAlt))e.Graphics.FillRectangle(b,e.ColumnIndex==Columns.Count-1?new Rectangle(e.Bounds.X,e.Bounds.Y,Math.Max(e.Bounds.Width,ClientSize.Width-e.Bounds.X),e.Bounds.Height):e.Bounds);TextRenderer.DrawText(e.Graphics,e.Header.Text,Font,Rectangle.Inflate(e.Bounds,-5,0),ModernTheme.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);}
  protected override void OnDrawItem(DrawListViewItemEventArgs e){if(View!=View.Details){using(var b=new SolidBrush(e.Item.Selected?ModernTheme.Selection:BackColor))e.Graphics.FillRectangle(b,e.Bounds);TextRenderer.DrawText(e.Graphics,e.Item.Text,Font,e.Bounds,ForeColor,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);}}
  protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e){using(var b=new SolidBrush(e.Item.Selected?ModernTheme.Selection:BackColor))e.Graphics.FillRectangle(b,e.Bounds);TextRenderer.DrawText(e.Graphics,e.SubItem.Text,Font,Rectangle.Inflate(e.Bounds,-5,0),ForeColor,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);}
 }
}
