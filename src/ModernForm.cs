using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Musikbox {
 public class ModernForm:Form {
  [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr h,int attribute,ref int value,int size);
  public ModernForm(){Font=new Font("Segoe UI",10);BackColor=ModernTheme.Background;ForeColor=ModernTheme.Ink;AutoScaleMode=AutoScaleMode.Dpi;}
  protected override void OnLoad(EventArgs e){if(!String.IsNullOrEmpty(Text)&&!Text.Contains(AppIdentity.Version))Text+=" · "+AppIdentity.Version;ModernTheme.Apply(this);base.OnLoad(e);if(Environment.OSVersion.Platform==PlatformID.Win32NT)try{int dark=1;DwmSetWindowAttribute(Handle,20,ref dark,4);}catch{}}
 }
}
