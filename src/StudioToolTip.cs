using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
namespace Musikbox {
 public static class StudioHints {
  static readonly ConditionalWeakTable<Control,StudioToolTip> tips=new ConditionalWeakTable<Control,StudioToolTip>();
  public static void Attach(Control control){if(!(control is DataGridView)&&!(control is ToolStrip))return;tips.GetValue(control,c=>{
   var tip=new StudioToolTip();c.Disposed+=(s,e)=>tip.Dispose();
   var grid=c as DataGridView;if(grid!=null){grid.ShowCellToolTips=false;grid.CellMouseEnter+=(s,e)=>{string text="";if(e.ColumnIndex>=0&&e.ColumnIndex<grid.Columns.Count){var cell=e.RowIndex>=0&&e.RowIndex<grid.Rows.Count?grid.Rows[e.RowIndex].Cells[e.ColumnIndex]:grid.Columns[e.ColumnIndex].HeaderCell;text=cell.ToolTipText;if(String.IsNullOrEmpty(text))text=Convert.ToString(cell.FormattedValue);if(!String.IsNullOrEmpty(cell.ErrorText))text+="\n"+cell.ErrorText;}tip.SetToolTip(grid,text);};grid.CellMouseLeave+=(s,e)=>tip.SetToolTip(grid,null);}
   var strip=c as ToolStrip;if(strip!=null){strip.ShowItemToolTips=false;strip.MouseMove+=(s,e)=>{var item=strip.GetItemAt(e.Location);string text=item==null?"":item.ToolTipText;if(String.IsNullOrEmpty(text)&&item!=null&&item.AutoToolTip)text=item.Text.Replace("&","");if(tip.GetToolTip(strip)!=text)tip.SetToolTip(strip,text);};strip.MouseLeave+=(s,e)=>tip.SetToolTip(strip,null);}
   return tip;
  });}
 }
 public sealed class StudioToolTip:ToolTip {
  public StudioToolTip(){OwnerDraw=true;IsBalloon=false;BackColor=ModernTheme.Navy;ForeColor=ModernTheme.Ink;Popup+=Measure;Draw+=PaintTip;}
  void Measure(object sender,PopupEventArgs e){var control=e.AssociatedControl;if(control==null)return;float scale=StudioDrawing.Scale(control);using(var font=StudioDrawing.Font(14*scale)){int padding=(int)(12*scale);var size=TextRenderer.MeasureText(GetToolTip(control),font,new Size((int)(400*scale),10000),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix);e.ToolTipSize=new Size(size.Width+padding*2,size.Height+padding*2);}}
  void PaintTip(object sender,DrawToolTipEventArgs e){DrawSurface(e.Graphics,e.Bounds,e.ToolTipText,e.AssociatedControl==null?1:StudioDrawing.Scale(e.AssociatedControl));}
  internal static void DrawSurface(Graphics graphics,Rectangle bounds,string text,float scale){using(var brush=new SolidBrush(ModernTheme.Navy))graphics.FillRectangle(brush,bounds);using(var pen=new Pen(ModernTheme.Selection,Math.Max(1,scale)))graphics.DrawRectangle(pen,bounds.X,bounds.Y,Math.Max(0,bounds.Width-1),Math.Max(0,bounds.Height-1));int padding=(int)(12*scale);bounds.Inflate(-padding,-padding);using(var font=StudioDrawing.Font(14*scale))TextRenderer.DrawText(graphics,text,font,bounds,ModernTheme.Ink,TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix|TextFormatFlags.NoPadding);}
 }
 public sealed class StudioNoticeIcon:Control {
  readonly string symbol;
  public StudioNoticeIcon(MessageBoxIcon icon){symbol=icon==MessageBoxIcon.Error?"×":icon==MessageBoxIcon.Warning?"!":icon==MessageBoxIcon.Question?"?":"i";SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;Tag="StudioExact";}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);float scale=StudioDrawing.Scale(this);int diameter=Math.Min(Width-8,(int)(30*scale));var circle=new Rectangle((Width-diameter)/2,(int)(8*scale),diameter,diameter);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var brush=new SolidBrush(ModernTheme.Selection))e.Graphics.FillEllipse(brush,circle);using(var pen=new Pen(ModernTheme.Accent,Math.Max(1,scale)))e.Graphics.DrawEllipse(pen,circle);using(var font=StudioDrawing.Font(20*scale,true))TextRenderer.DrawText(e.Graphics,symbol,font,circle,ModernTheme.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);}
 }
}
