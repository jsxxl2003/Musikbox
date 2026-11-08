using System;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace Musikbox {
 // A short synchronous UI transaction: old pixels remain until layout and child
 // construction finish. It changes redraw only, never Windows theme handles.
 public sealed class StudioRedrawScope:IDisposable {
  [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
  readonly Control root;bool disposed,locked;
  public StudioRedrawScope(Control root){this.root=root;if(root!=null){root.SuspendLayout();if(root.IsHandleCreated&&root.Visible&&Environment.OSVersion.Platform==PlatformID.Win32NT){SendMessage(root.Handle,0xB,IntPtr.Zero,IntPtr.Zero);locked=true;}}}
  public void Dispose(){if(disposed)return;disposed=true;if(root==null||root.IsDisposed)return;root.ResumeLayout(true);if(locked)SendMessage(root.Handle,0xB,new IntPtr(1),IntPtr.Zero);root.Invalidate(true);}

 }
 public class StudioBufferedPanel:Panel {
  public StudioBufferedPanel(){SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);BackColor=ModernTheme.Background;Tag="StudioExact";}
 }
}
