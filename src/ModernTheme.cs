using System;using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
namespace Musikbox {
 public class MetallicHeader:Panel {public MetallicHeader(){DoubleBuffered=true;BackColor=ModernTheme.ChromeDark;ForeColor=ModernTheme.Ink;Font=ModernTheme.CaptionFont;}}
 public sealed class MetallicSurface:Panel {public MetallicSurface(){DoubleBuffered=true;BackColor=ModernTheme.Silver;}}
 public sealed class ChromeButton:Button {public bool DockIcon{get;set;}public string Glyph{get;set;}public ChromeButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;ForeColor=ModernTheme.Ink;BackColor=ModernTheme.ChromeDark;}protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(String.IsNullOrEmpty(Text))TextRenderer.DrawText(e.Graphics,Glyph=="Minimize"?"−":Glyph=="Maximize"?"□":"×",Font,ClientRectangle,ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}}
 public static class ModernTheme {
  public static readonly Color Background=Color.FromArgb(14,13,22),Navy=Color.FromArgb(25,24,38),NavyAlt=Color.FromArgb(29,27,43),Selection=Color.FromArgb(99,44,155),Silver=Color.FromArgb(34,32,53),ChromeDark=Color.FromArgb(21,19,33),Edge=Color.FromArgb(48,44,67),Ink=Color.FromArgb(236,234,247),WarningBack=Color.FromArgb(46,31,62),Accent=Color.FromArgb(162,59,241),Muted=Color.FromArgb(170,166,190);
  public static readonly Font CaptionFont=new Font("Segoe UI",9.5f),FieldLabelFont=new Font("Segoe UI",9.5f);
  public static void PaintFrame(Graphics g,Rectangle r){if(r.Width<2||r.Height<2)return;using(var p=new Pen(Edge))g.DrawRectangle(p,r.X,r.Y,r.Width-1,r.Height-1);}
  static readonly ConditionalWeakTable<Control,object> styled=new ConditionalWeakTable<Control,object>();
  public static void Apply(Control c){
   styled.GetValue(c,control=>{control.ControlAdded+=(s,e)=>Apply(e.Control);return new object();});
   StudioScrollbars.Attach(c);StudioHints.Attach(c);
   if(c.ContextMenuStrip!=null)StyleMenu(c.ContextMenuStrip);
   if(c is TextBoxBase && c.ContextMenuStrip==null)StudioContextMenus.AttachText((TextBoxBase)c);
   if(c is ComboBox && ((ComboBox)c).DropDownStyle!=ComboBoxStyle.DropDownList && c.ContextMenuStrip==null)StudioContextMenus.AttachEditableCombo((ComboBox)c);
   if(c is StudioActionButton||c is StudioArt||c is StudioSlider)return;
   if(c is StudioPanel){foreach(Control child in c.Controls.Cast<Control>().ToArray())Apply(child);return;}
   if(Object.Equals(c.Tag,"StudioExact")){foreach(Control child in c.Controls.Cast<Control>().ToArray())Apply(child);return;}
   if(c is TextBoxBase&&((TextBoxBase)c).BorderStyle==BorderStyle.Fixed3D)((TextBoxBase)c).BorderStyle=BorderStyle.FixedSingle;if(c is ListView)((ListView)c).BorderStyle=BorderStyle.FixedSingle;if(c is ListBox)((ListBox)c).BorderStyle=BorderStyle.FixedSingle;
   c.ForeColor=Ink;if(c.Font.Size<9&&c.Font.Unit!=GraphicsUnit.Pixel)c.Font=FieldLabelFont;
   if(c is ColorSwatch)return;
   var grid=c as DataGridView;if(grid!=null){grid.EnableHeadersVisualStyles=false;grid.BackgroundColor=Navy;grid.BorderStyle=BorderStyle.None;grid.GridColor=Edge;grid.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.Single;grid.ColumnHeadersDefaultCellStyle.BackColor=Silver;grid.ColumnHeadersDefaultCellStyle.ForeColor=Muted;grid.ColumnHeadersDefaultCellStyle.SelectionBackColor=Silver;grid.DefaultCellStyle.BackColor=Navy;grid.DefaultCellStyle.ForeColor=Ink;grid.DefaultCellStyle.SelectionBackColor=Selection;grid.DefaultCellStyle.SelectionForeColor=Color.White;grid.AlternatingRowsDefaultCellStyle.BackColor=NavyAlt;grid.RowHeadersDefaultCellStyle.BackColor=Silver;grid.RowTemplate.Height=34;grid.ColumnHeadersHeight=36;}
   else if(c is TextBoxBase||c is ComboBox||c is NumericUpDown||c is ListBox||c is ListView||c is TreeView){c.BackColor=Silver;}
   else if(c is Button){var b=(Button)c;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderColor=Edge;b.UseVisualStyleBackColor=false;b.FlatAppearance.MouseDownBackColor=Accent;b.FlatAppearance.MouseOverBackColor=Selection;b.BackColor=Object.Equals(b.Tag,"Primary")?Accent:Silver;}
   else if(c is CheckBox){var box=(CheckBox)c;box.UseVisualStyleBackColor=false;box.FlatStyle=FlatStyle.Flat;box.FlatAppearance.BorderColor=Edge;box.FlatAppearance.CheckedBackColor=Accent;box.FlatAppearance.MouseOverBackColor=Selection;box.BackColor=Navy;}
   else if(c is RadioButton){var radio=(RadioButton)c;radio.UseVisualStyleBackColor=false;radio.FlatStyle=FlatStyle.Flat;radio.FlatAppearance.CheckedBackColor=Accent;radio.BackColor=Navy;}
   else if(c is Label){c.BackColor=Color.Transparent;}
   else if(c is SplitContainer)c.BackColor=Edge;
   else c.BackColor=c is Form?Background:Navy;
   var strip=c as ToolStrip;if(strip!=null){strip.BackColor=ChromeDark;strip.Renderer=new StudioMenuRenderer();foreach(ToolStripItem item in strip.Items)StyleItem(item);}
   if(c.ContextMenuStrip!=null){c.ContextMenuStrip.Renderer=new StudioMenuRenderer();foreach(ToolStripItem item in c.ContextMenuStrip.Items)StyleItem(item);}
   var list=c as ListBox;if(list!=null&&list.DrawMode==DrawMode.Normal){list.DrawMode=DrawMode.OwnerDrawFixed;list.ItemHeight=Math.Max(24,list.Font.Height+8);list.DrawItem+=(s,e)=>{using(var brush=new SolidBrush((e.State&DrawItemState.Selected)!=0?Selection:Silver))e.Graphics.FillRectangle(brush,e.Bounds);if(e.Index>=0&&e.Index<list.Items.Count)TextRenderer.DrawText(e.Graphics,list.GetItemText(list.Items[e.Index]),list.Font,Rectangle.Inflate(e.Bounds,-4,0),Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};}
   var tabs=c as TabControl;if(tabs!=null){tabs.DrawMode=TabDrawMode.OwnerDrawFixed;if(!Object.Equals(tabs.Tag,"StyledTabs")){tabs.Tag="StyledTabs";tabs.DrawItem+=(s,e)=>{var r=tabs.GetTabRect(e.Index);using(var b=new SolidBrush(e.Index==tabs.SelectedIndex?Accent:Silver))e.Graphics.FillRectangle(b,r);TextRenderer.DrawText(e.Graphics,tabs.TabPages[e.Index].Text,tabs.Font,r,Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);};}}
   foreach(Control child in c.Controls.Cast<Control>().ToArray())Apply(child);
  }
  public static void StyleMenu(ContextMenuStrip menu){menu.BackColor=Silver;menu.ForeColor=Ink;menu.Renderer=new StudioMenuRenderer();foreach(ToolStripItem item in menu.Items)StyleItem(item);}
  static void StyleItem(ToolStripItem item){item.ForeColor=Ink;item.BackColor=Silver;var m=item as ToolStripDropDownItem;if(m!=null){StudioHints.Attach(m.DropDown);m.DropDown.Renderer=new StudioMenuRenderer();foreach(ToolStripItem child in m.DropDownItems)StyleItem(child);}}
  sealed class StudioMenuRenderer:ToolStripProfessionalRenderer {
   public StudioMenuRenderer():base(new StudioColors()){RoundedEdges=false;}
   protected override void OnRenderImageMargin(ToolStripRenderEventArgs e){using(var brush=new SolidBrush(Silver))e.Graphics.FillRectangle(brush,e.AffectedBounds);}
   protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){using(var brush=new SolidBrush(e.Item.Enabled&&(e.Item.Selected||e.Item.Pressed)?Selection:Silver))e.Graphics.FillRectangle(brush,new Rectangle(Point.Empty,e.Item.Size));}
   protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){e.TextColor=e.Item.Enabled?Ink:Muted;base.OnRenderItemText(e);}
   protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e){var r=e.ImageRectangle;TextRenderer.DrawText(e.Graphics,"✓",e.Item.Font,r,e.Item.Enabled?Ink:Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
  }
  sealed class StudioColors:ProfessionalColorTable {public override Color ToolStripGradientBegin{get{return ChromeDark;}}public override Color ToolStripGradientMiddle{get{return ChromeDark;}}public override Color ToolStripGradientEnd{get{return ChromeDark;}}public override Color ToolStripDropDownBackground{get{return Silver;}}public override Color ImageMarginGradientBegin{get{return Silver;}}public override Color ImageMarginGradientMiddle{get{return Silver;}}public override Color ImageMarginGradientEnd{get{return Silver;}}public override Color MenuItemSelected{get{return Selection;}}public override Color MenuItemSelectedGradientBegin{get{return Selection;}}public override Color MenuItemSelectedGradientEnd{get{return Selection;}}public override Color MenuBorder{get{return Edge;}}}
 }
}
