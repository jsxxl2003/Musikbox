using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace Musikbox {
 public static class StudioDrawing {
  public static float Scale(Control control){
#if NET8_0_OR_GREATER
   return control.DeviceDpi/96f;
#else
   return 1;
#endif
  }
  // Draw only ancestor surfaces, never their child controls or a Windows button adapter.
  // ButtonBase marks owner-drawn buttons Opaque on Windows, so OnPaint must initialise
  // every pixel, including the area outside the rounded button path.
  public static void ParentBackground(Graphics graphics,Control child){
   var state=graphics.Save();
   try{
    graphics.SetClip(child.ClientRectangle,CombineMode.Intersect);
    var parent=child.Parent;
    if(parent==null){using(var brush=new SolidBrush(ModernTheme.Background))graphics.FillRectangle(brush,child.ClientRectangle);return;}
    graphics.TranslateTransform(-child.Left,-child.Top);
    SurfaceBackground(graphics,parent);
   }finally{graphics.Restore(state);}
  }
  static void SurfaceBackground(Graphics graphics,Control control){
   var panel=control as StudioPanel;
   if(panel!=null){panel.PaintSurface(graphics);return;}
   if(control.BackColor.A<255)ParentBackground(graphics,control);
   using(var brush=new SolidBrush(control.BackColor.A==0?Color.Transparent:control.BackColor))graphics.FillRectangle(brush,control.ClientRectangle);
  }
  public static Font Font(float pixels,bool bold=false){return new Font("Segoe UI",pixels,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);}
  public static Color Hex(string hex){return ColorTranslator.FromHtml(hex);}
  public static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));if(d<=0){p.AddRectangle(r);return p;}p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
  public static void Icon(Graphics g,string name,Rectangle r,Color color){g.SmoothingMode=SmoothingMode.AntiAlias;float x=r.X,y=r.Y,w=r.Width,h=r.Height;using(var p=new Pen(color,1.7f)){p.StartCap=p.EndCap=LineCap.Round;
   if(name=="Search"){g.DrawEllipse(p,x+2,y+2,w*.57f,h*.57f);g.DrawLine(p,x+w*.64f,y+h*.64f,x+w-2,y+h-2);}
   else if(name=="Clear"){g.DrawLine(p,x+4,y+4,x+w-4,y+h-4);g.DrawLine(p,x+w-4,y+4,x+4,y+h-4);}
   else if(name=="Mic"){g.DrawArc(p,x+w*.34f,y+1,w*.32f,h*.32f,180,180);g.DrawLine(p,x+w*.34f,y+h*.17f,x+w*.34f,y+h*.52f);g.DrawLine(p,x+w*.66f,y+h*.17f,x+w*.66f,y+h*.52f);g.DrawArc(p,x+w*.34f,y+h*.36f,w*.32f,h*.32f,0,180);g.DrawArc(p,x+w*.17f,y+h*.3f,w*.66f,h*.55f,0,180);g.DrawLine(p,x+w/2,y+h*.85f,x+w/2,y+h-1);g.DrawLine(p,x+w*.3f,y+h-1,x+w*.7f,y+h-1);}
   else if(name=="Play"){using(var b=new SolidBrush(color))g.FillPolygon(b,new[]{new PointF(x+w*.28f,y+h*.15f),new PointF(x+w*.8f,y+h/2),new PointF(x+w*.28f,y+h*.85f)});}
   else if(name=="Pause"){g.DrawLine(p,x+w*.35f,y+3,x+w*.35f,y+h-3);g.DrawLine(p,x+w*.65f,y+3,x+w*.65f,y+h-3);}
   else if(name=="Previous"||name=="Next"){bool next=name=="Next";using(var b=new SolidBrush(color)){for(int i=0;i<2;i++){float l=x+2+i*w*.33f;g.FillPolygon(b,next?new[]{new PointF(l,y+3),new PointF(l+w*.34f,y+h/2),new PointF(l,y+h-3)}:new[]{new PointF(l+w*.34f,y+3),new PointF(l,y+h/2),new PointF(l+w*.34f,y+h-3)});}g.DrawLine(p,next?x+w-2:x+1,y+3,next?x+w-2:x+1,y+h-3);}}
   else if(name=="Disc"){g.DrawEllipse(p,x+2,y+2,w-4,h-4);g.DrawEllipse(p,x+5,y+5,w-10,h-10);}
   else if(name=="Volume"){g.DrawLines(p,new[]{new PointF(x+w*.1f,y+h*.35f),new PointF(x+w*.3f,y+h*.35f),new PointF(x+w*.5f,y+h*.15f),new PointF(x+w*.5f,y+h*.85f),new PointF(x+w*.3f,y+h*.65f),new PointF(x+w*.1f,y+h*.65f)});g.DrawArc(p,x+w*.4f,y+h*.13f,w*.4f,h*.74f,-65,130);g.DrawArc(p,x+w*.45f,y+1,w*.5f,h-2,-65,130);}
   else using(var font=StudioDrawing.Font(19*r.Width/22f))TextRenderer.DrawText(g,name,font,r,color,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.PreserveGraphicsClipping|TextFormatFlags.PreserveGraphicsTranslateTransform);
  }}
 }
 public sealed class StudioLabel:Label {
  public float TextPixels=16;public bool Bold;Font owned;
  void MatchDpi(){float scale=StudioDrawing.Scale(this);var next=StudioDrawing.Font(TextPixels*scale,Bold);Font=next;if(owned!=null)owned.Dispose();owned=next;}
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);MatchDpi();}
#if NET8_0_OR_GREATER
  protected override void OnDpiChangedAfterParent(EventArgs e){base.OnDpiChangedAfterParent(e);MatchDpi();}
#endif
  protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing&&owned!=null){owned.Dispose();owned=null;}}
 }
 public class StudioPanel:Panel {
  public Color Fill=StudioDrawing.Hex("#1b1a2a"),End=StudioDrawing.Hex("#161724"),Border=StudioDrawing.Hex("#2b293d");public int Radius=14;public bool Gradient=true;
  public StudioPanel(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;ResizeRedraw=true;}
  protected override void OnPaintBackground(PaintEventArgs e){PaintSurface(e.Graphics);}
  internal void PaintSurface(Graphics graphics){
   var state=graphics.Save();
   try{StudioDrawing.ParentBackground(graphics,this);if(Width<2||Height<2)return;graphics.SmoothingMode=SmoothingMode.AntiAlias;var r=new RectangleF(.5f,.5f,Width-1,Height-1);using(var path=StudioDrawing.Round(r,Radius*StudioDrawing.Scale(this))){if(Gradient)using(var b=new LinearGradientBrush(r,Fill,End,45))graphics.FillPath(b,path);else using(var b=new SolidBrush(Fill))graphics.FillPath(b,path);if(Border.A>0)using(var p=new Pen(Border))graphics.DrawPath(p,path);}}
   finally{graphics.Restore(state);}
  }
 }
 public class StudioActionButton:Button {
  public bool Active,Primary,Navigation,Transparent,WrapText;public string Symbol;public int Radius=9;bool hover;
  public StudioActionButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Color.Transparent;ForeColor=ModernTheme.Ink;Font=StudioDrawing.Font(15);Cursor=Cursors.Hand;UseVisualStyleBackColor=false;SetStyle(ControlStyles.Opaque|ControlStyles.ResizeRedraw,true);}
  protected override void OnPaintBackground(PaintEventArgs e){}
  protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnPaint(PaintEventArgs e){StudioDrawing.ParentBackground(e.Graphics,this);var g=e.Graphics;float scale=StudioDrawing.Scale(this);g.SmoothingMode=SmoothingMode.AntiAlias;var r=new RectangleF(.5f,.5f,Width-1,Height-1);bool color=Primary||Active;if(color||!Transparent&&!Navigation||hover){using(var path=StudioDrawing.Round(r,Radius*StudioDrawing.Scale(this))){if(color)using(var b=new LinearGradientBrush(r,StudioDrawing.Hex(Navigation?"#6222b9":"#7e2bdd"),StudioDrawing.Hex(Navigation?"#a73ff0":"#bb58ff"),0f))g.FillPath(b,path);else using(var b=new SolidBrush(StudioDrawing.Hex(hover?"#322545":"#252235")))g.FillPath(b,path);}}
   Color ink=Enabled?ForeColor:ModernTheme.Muted;int left=0;if(!String.IsNullOrEmpty(Symbol)){var icon=Navigation?new Rectangle((int)(12*scale),(Height-(int)(22*scale))/2,(int)(22*scale),(int)(22*scale)):new Rectangle((Width-(int)(22*scale))/2,(Height-(int)(22*scale))/2,(int)(22*scale),(int)(22*scale));StudioDrawing.Icon(g,Symbol,icon,ink);if(Navigation)left=(int)(42*scale);}
   if(!String.IsNullOrEmpty(Text)){var rect=new Rectangle(Navigation?left+0:0,0,Width-(Navigation?left+8:0),Height);using(var f=StudioDrawing.Font((Navigation?15:16)*scale,Navigation&&Active))TextRenderer.DrawText(g,Text,f,rect,ink,TextFormatFlags.PreserveGraphicsClipping|TextFormatFlags.PreserveGraphicsTranslateTransform|TextFormatFlags.VerticalCenter|(WrapText?TextFormatFlags.WordBreak:TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine)|TextFormatFlags.NoPadding|(Navigation?TextFormatFlags.Left:TextFormatFlags.HorizontalCenter));}
   if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(ClientRectangle,-4,-4));
  }
 }
 public sealed class StudioArt:Control {
  public string Initials="♫";public Image Artwork;public bool Disc,Mini;public int PaletteIndex;public float TextPixels=18;
  public StudioArt(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;}
  protected override void OnPaintBackground(PaintEventArgs e){StudioDrawing.ParentBackground(e.Graphics,this);}
  protected override void OnPaint(PaintEventArgs e){if(Width<2||Height<2)return;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var r=new RectangleF(.5f,.5f,Width-1,Height-1);string[] bases={"#0b0c12","#3a1835","#111b36","#1c1122","#151c3c","#1a3038","#252035"},glows={"#7126a6","#d65b92","#3260bb","#9d333a","#5542c5","#2b8e9d","#d38d4d"};int palette=Math.Abs(PaletteIndex%7);using(var path=StudioDrawing.Round(r,Disc?Width/2:11)){var clipState=g.Save();g.SetClip(path,CombineMode.Intersect);if(Artwork!=null){float scale=Math.Max((float)Width/Artwork.Width,(float)Height/Artwork.Height);g.DrawImage(Artwork,(Width-Artwork.Width*scale)/2,(Height-Artwork.Height*scale)/2,Artwork.Width*scale,Artwork.Height*scale);}else{using(var b=new LinearGradientBrush(r,StudioDrawing.Hex(Disc?"#5e21c4":Mini?"#602aa0":bases[palette]),StudioDrawing.Hex(Disc?"#c64cf8":Mini?"#191223":"#0d0d16"),55))g.FillRectangle(b,r);if(!Disc&&!Mini){using(var pathGlow=new GraphicsPath()){pathGlow.AddEllipse(-Width*.2f,-Height*.65f,Width*1.7f,Height*1.7f);using(var glow=new PathGradientBrush(pathGlow)){glow.CenterPoint=new PointF(Width*.65f,Height*.2f);glow.CenterColor=StudioDrawing.Hex(glows[palette]);glow.SurroundColors=new[]{Color.FromArgb(0,StudioDrawing.Hex(glows[palette]))};g.FillPath(glow,pathGlow);}}}}g.Restore(clipState);if(Disc)StudioDrawing.Icon(g,"Disc",new Rectangle(Width/4,Height/4,Width/2,Height/2),ModernTheme.Ink);else{using(var p=new Pen(StudioDrawing.Hex("#3c3351")))g.DrawPath(p,path);if(Artwork==null){float scale=StudioDrawing.Scale(this);using(var font=StudioDrawing.Font(TextPixels*scale,true)){var lines=new System.Collections.Generic.List<string>();string current="";foreach(string word in Initials.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries)){string trial=current.Length==0?word:current+" "+word;float width=TextRenderer.MeasureText(g,trial,font,new Size(10000,1000),TextFormatFlags.SingleLine|TextFormatFlags.NoPadding).Width+(trial.Length-1)*scale;if(current.Length>0&&width>Width-(Mini?2:18)*scale){lines.Add(current);current=word;}else current=trial;}if(current.Length>0)lines.Add(current);float lineHeight=(TextPixels+4)*scale,top=(Height-lines.Count*lineHeight)/2;for(int i=0;i<lines.Count;i++)TextRenderer.DrawText(g,lines[i],font,new Rectangle(0,(int)(top+i*lineHeight),Width,(int)lineHeight+2),ModernTheme.Ink,TextFormatFlags.SingleLine|TextFormatFlags.NoPadding|TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.PreserveGraphicsClipping|TextFormatFlags.PreserveGraphicsTranslateTransform);}}}}
  }
 }
 public sealed class StudioSlider:Control {
  int value;public int Minimum=0,Maximum=10000;public event EventHandler Scroll;
  public int Value {get{return value;}set{this.value=Math.Max(Minimum,Math.Min(Maximum,value));Invalidate();}}
  public StudioSlider(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;Cursor=Cursors.Hand;Height=24;TabStop=true;AccessibleRole=AccessibleRole.Slider;}
  void SetAt(int x){Value=Minimum+(int)(Math.Max(0,Math.Min(1,(x-8)/(double)Math.Max(1,Width-16)))*(Maximum-Minimum));if(Scroll!=null)Scroll(this,EventArgs.Empty);}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Capture=true;SetAt(e.X);}}protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(Capture)SetAt(e.X);}protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);Capture=false;}protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Right){Value+=(e.KeyCode==Keys.Left?-1:1)*Math.Max(1,(Maximum-Minimum)/100);if(Scroll!=null)Scroll(this,EventArgs.Empty);e.Handled=true;}base.OnKeyDown(e);}
  protected override void OnPaintBackground(PaintEventArgs e){StudioDrawing.ParentBackground(e.Graphics,this);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;int x=8+(int)((Width-16)*(Value-Minimum)/(double)Math.Max(1,Maximum-Minimum)),y=Height/2;using(var p=new Pen(ModernTheme.Edge,6)){p.StartCap=p.EndCap=LineCap.Round;g.DrawLine(p,8,y,Width-8,y);}using(var p=new Pen(ModernTheme.Accent,6)){p.StartCap=p.EndCap=LineCap.Round;g.DrawLine(p,8,y,Math.Max(8,x),y);}using(var b=new SolidBrush(ModernTheme.Accent))g.FillEllipse(b,x-7,y-7,14,14);}
 }
}
