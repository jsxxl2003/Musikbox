using System;
using System.Drawing;
using System.Windows.Forms;
namespace Musikbox {
 public sealed class WorkspacePane:UserControl {
  readonly string caption;readonly Control body;readonly bool scroll;readonly StudioAutoScrollPanel holder=new StudioAutoScrollPanel{Dock=DockStyle.Fill};
  readonly Label placeholder=new Label{Dock=DockStyle.Fill,Text="In einem eigenen Fenster geöffnet.",TextAlign=ContentAlignment.MiddleCenter,Visible=false};
  PaneWindow window;MainView owner;bool docking;
  public string Caption {get{return caption;}}
  public bool IsHidden {get;private set;}
  public event Action BeforeMove,LayoutChanged;public event Action<Form> WindowCreated;
  public bool IsFloating {get{return window!=null&&!window.IsDisposed;}}
  public Form FloatingWindow {get{return window;}}
  public WorkspacePane(string caption,Control body,bool scroll=false){
   ResizeRedraw=true;this.caption=caption;this.body=body;this.scroll=scroll;Dock=DockStyle.Fill;BorderStyle=BorderStyle.None;Padding=new Padding(4);BackColor=ModernTheme.Navy;
   var header=new MetallicHeader{Dock=DockStyle.Top,Height=30,Tag="WindowChrome",BackColor=ModernTheme.NavyAlt};var title=new Label{Text=caption.ToUpperInvariant(),Dock=DockStyle.Fill,Padding=new Padding(6,0,0,0),TextAlign=ContentAlignment.MiddleCenter,AutoEllipsis=true,ForeColor=Color.White};
   var hide=new ChromeButton{Text="×",AccessibleName="Ausblenden",Dock=DockStyle.Right,Width=30,FlatStyle=FlatStyle.Flat,TabStop=false};hide.Click+=(s,e)=>HidePane();header.Controls.Add(title);header.Controls.Add(hide);holder.AutoScroll=scroll;holder.Controls.Add(placeholder);holder.Controls.Add(body);body.Dock=scroll?DockStyle.Top:DockStyle.Fill;Controls.Add(holder);Controls.Add(header);
   bool pending=false;Point pressed=Point.Empty,offset=Point.Empty;
   MouseEventHandler down=(s,e)=>{if(e.Button!=MouseButtons.Left||owner==null)return;pending=true;pressed=Control.MousePosition;var top=header.PointToScreen(Point.Empty);offset=new Point(pressed.X-top.X,pressed.Y-top.Y);((Control)s).Capture=true;};
   MouseEventHandler move=(s,e)=>{if(!pending||owner==null)return;Point now=Control.MousePosition;var threshold=new Rectangle(pressed.X-SystemInformation.DragSize.Width/2,pressed.Y-SystemInformation.DragSize.Height/2,SystemInformation.DragSize.Width,SystemInformation.DragSize.Height);if(threshold.Contains(now))return;pending=false;((Control)s).Capture=false;Float(owner);window.Location=new Point(now.X-Math.Min(offset.X,window.Width-60)-4,now.Y-offset.Y-4);window.BeginCaptionDrag(now);};
   foreach(Control handle in new Control[]{header,title}){handle.Cursor=Cursors.Default;handle.MouseDown+=down;handle.MouseMove+=move;handle.MouseUp+=(s,e)=>{if(e.Button!=MouseButtons.Left)return;pending=false;((Control)s).Capture=false;};handle.MouseCaptureChanged+=(s,e)=>{if(!((Control)s).Capture)pending=false;};}
   title.DoubleClick+=(s,e)=>{if(owner!=null)Float(owner);};
  }
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(!studioFlat)ModernTheme.PaintFrame(e.Graphics,ClientRectangle);}
  bool studioFlat;public void UseStudioSurface(){studioFlat=true;Tag="StudioExact";BackColor=ModernTheme.Background;holder.Tag="StudioExact";holder.BackColor=ModernTheme.Background;Padding=Padding.Empty;foreach(Control c in Controls)if(Object.Equals(c.Tag,"WindowChrome"))c.Visible=false;Invalidate();}
  public void Bind(MainView main){owner=main;new PaneEdgeResize(this,main);}
  public void Float(MainView main){
   if(IsFloating){ShowPane();window.BringToFront();return;}if(BeforeMove!=null)BeforeMove();owner=main;IsHidden=false;Visible=true;
   var host=new StudioAutoScrollPanel{Dock=DockStyle.Fill,AutoScroll=scroll,BackColor=ModernTheme.Navy};window=new PaneWindow(main,caption,DockBack){Text=caption+" · Musikbox-Werkstatt",Font=main.Font,Width=caption=="Titelliste"?850:390,Height=500,MinimumSize=new Size(320,220)};
   window.Controls.Add(host);host.BringToFront();holder.Controls.Remove(body);host.Controls.Add(body);placeholder.Visible=true;
   window.FormClosing+=(s,e)=>{if(!docking&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;HidePane();return;}if(BeforeMove!=null)BeforeMove();holder.Controls.Add(body);placeholder.Visible=false;window=null;Visible=!IsHidden;if(LayoutChanged!=null)LayoutChanged();};
   if(WindowCreated!=null)WindowCreated(window);window.Show(main);if(LayoutChanged!=null)LayoutChanged();
  }
  public void HidePane(){if(BeforeMove!=null)BeforeMove();IsHidden=true;if(IsFloating)window.Hide();else Visible=false;if(LayoutChanged!=null)LayoutChanged();}
  public void ShowPane(){IsHidden=false;Visible=true;if(IsFloating){window.Show();window.BringToFront();}if(LayoutChanged!=null)LayoutChanged();}
  public void DockBack(){var current=window;if(current==null||current.IsDisposed)return;docking=true;try{current.Close();}finally{docking=false;}}

  sealed class PaneWindow:ModernForm {
   readonly MainView main;readonly Control captionHandle;bool dragging;Point dragCursor,dragOrigin;
   public PaneWindow(MainView main,string caption,Action dock){ResizeRedraw=true;this.main=main;FormBorderStyle=FormBorderStyle.None;Padding=new Padding(4);Tag="ModernFrame";ShowInTaskbar=false;StartPosition=FormStartPosition.CenterParent;
    var header=new MetallicHeader{Dock=DockStyle.Top,Height=30,Tag="WindowChrome"};var label=new Label{Text=caption.ToUpperInvariant(),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,Padding=new Padding(7,0,0,0),AutoEllipsis=true,ForeColor=Color.White};var close=new ChromeButton{Text="×",AccessibleName="Ausblenden",Width=30,Dock=DockStyle.Right,TabStop=false};var back=new ChromeButton{Text="Andocken",AccessibleName="Andocken",DockIcon=true,Width=30,Dock=DockStyle.Right,TabStop=false};close.Click+=(s,e)=>Close();back.Click+=(s,e)=>dock();header.Controls.Add(label);header.Controls.Add(back);header.Controls.Add(close);Controls.Add(header);var tip=new StudioToolTip();tip.SetToolTip(back,"Andocken – zurück in die Hauptansicht");tip.SetToolTip(close,"Modul ausblenden");Disposed+=(s,e)=>tip.Dispose();
    var bottom=new MetallicSurface{Dock=DockStyle.Bottom,Height=18};bottom.Controls.Add(new ResizeGrip(this){Dock=DockStyle.Right});Controls.Add(bottom);
    captionHandle=label;
    foreach(Control handle in new Control[]{header,label}){handle.Cursor=Cursors.Default;handle.MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left)BeginDrag((Control)s,Control.MousePosition);};handle.MouseMove+=(s,e)=>{if(!dragging)return;Point now=Control.MousePosition;main.Magnets.Move(this,new Point(dragOrigin.X+now.X-dragCursor.X,dragOrigin.Y+now.Y-dragCursor.Y));};handle.MouseUp+=(s,e)=>{if(e.Button==MouseButtons.Left)EndDrag((Control)s);};handle.MouseCaptureChanged+=(s,e)=>{if(!((Control)s).Capture&&dragging){dragging=false;main.Magnets.End();}};}
   }
   public void BeginCaptionDrag(Point cursor){BeginDrag(captionHandle,cursor);}
   void BeginDrag(Control handle,Point cursor){dragging=true;dragCursor=cursor;dragOrigin=Location;handle.Capture=true;main.Magnets.Begin(this,(Control.ModifierKeys&Keys.Shift)!=0);}
   void EndDrag(Control handle){if(!dragging)return;dragging=false;handle.Capture=false;main.Magnets.End();}
   protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);ModernTheme.PaintFrame(e.Graphics,ClientRectangle);}
   protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg!=0x84||WindowState!=FormWindowState.Normal)return;long value=m.LParam.ToInt64();var point=PointToClient(new Point(unchecked((short)value),unchecked((short)(value>>16))));bool l=point.X<6,r=point.X>=ClientSize.Width-6,t=point.Y<6,b=point.Y>=ClientSize.Height-6;int hit=t?(l?13:r?14:12):b?(l?16:r?17:15):l?10:r?11:1;if(hit!=1)m.Result=(IntPtr)hit;}
   protected override bool ProcessCmdKey(ref Message msg,Keys keyData){return main.HandleHistoryKey(keyData,this)||base.ProcessCmdKey(ref msg,keyData);}
  }
 }
}
