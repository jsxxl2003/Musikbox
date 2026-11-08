using System;using System.Windows.Forms;
namespace Musikbox {
 public class StudioTextBox:TextBox {
  readonly StudioScrollbars.NativeStyleGate gate;public StudioTextBox(){gate=new StudioScrollbars.NativeStyleGate(this);}
  protected override CreateParams CreateParams{get{var p=base.CreateParams;p.Style&=~StudioScrollbars.NativeStyles;return p;}}
  protected override void OnParentChanged(EventArgs e){base.OnParentChanged(e);if(Multiline)StudioScrollbars.EnsureFrame(this);}
  protected override void OnMultilineChanged(EventArgs e){base.OnMultilineChanged(e);if(Multiline)StudioScrollbars.EnsureFrame(this);}
 }
 public class StudioCheckedListBox:CheckedListBox {
  readonly StudioScrollbars.NativeStyleGate gate;public StudioCheckedListBox(){IntegralHeight=false;BackColor=ModernTheme.Navy;ForeColor=ModernTheme.Ink;BorderStyle=BorderStyle.None;gate=new StudioScrollbars.NativeStyleGate(this);}
  protected override CreateParams CreateParams{get{var p=base.CreateParams;p.Style&=~StudioScrollbars.NativeStyles;return p;}}
  protected override void OnParentChanged(EventArgs e){base.OnParentChanged(e);StudioScrollbars.EnsureFrame(this);}
 }
}
