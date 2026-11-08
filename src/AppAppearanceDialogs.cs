using System;
using System.Linq;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
namespace Musikbox {
 public sealed class ColorSwatch:Button {
  public Color Value {get;set;}
  protected override void OnPaint(PaintEventArgs e){using(var brush=new SolidBrush(Value))e.Graphics.FillRectangle(brush,ClientRectangle);ModernTheme.PaintFrame(e.Graphics,ClientRectangle);if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(ClientRectangle,-5,-5));}
 }
 public sealed class AppColorDialog:ModernForm {
  public Color Color {get;set;}
  public readonly TextBox Hex=new StudioTextBox{Dock=DockStyle.Fill};public readonly NumericUpDown Red=Channel(),Green=Channel(),Blue=Channel(),Alpha=Channel();
  readonly Panel preview=new Panel{Dock=DockStyle.Fill,Height=70};readonly Label state=new Label{Dock=DockStyle.Bottom,Height=28};bool syncing;
  static NumericUpDown Channel(){return new StudioNumericUpDown{Minimum=0,Maximum=255,Dock=DockStyle.Fill};}
  public AppColorDialog(){Color=System.Drawing.Color.White;Text="Farbe auswählen";ClientSize=new Size(560,430);MinimumSize=new Size(500,480);StartPosition=FormStartPosition.CenterParent;MinimizeBox=false;MaximizeBox=false;Padding=new Padding(12);Font=new Font("Segoe UI",10);
   var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=7};for(int i=0;i<4;i++)layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,85));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,70));for(int i=2;i<7;i++)layout.RowStyles.Add(new RowStyle(SizeType.Absolute,36));layout.Controls.Add(preview,0,0);layout.SetColumnSpan(preview,4);
   preview.Paint+=(s,e)=>{using(var brush=new SolidBrush(Color))e.Graphics.FillRectangle(brush,preview.ClientRectangle);ModernTheme.PaintFrame(e.Graphics,preview.ClientRectangle);};
   var palette=new FlowLayoutPanel{Dock=DockStyle.Fill};foreach(var color in new[]{System.Drawing.Color.White,System.Drawing.Color.Black,System.Drawing.Color.Gray,System.Drawing.Color.Silver,System.Drawing.Color.Red,System.Drawing.Color.Orange,System.Drawing.Color.Yellow,System.Drawing.Color.Green,System.Drawing.Color.Cyan,System.Drawing.Color.Blue,System.Drawing.Color.Navy,System.Drawing.Color.Purple}){var selected=color;var swatch=new ColorSwatch{Value=color,Size=new Size(32,26),AccessibleName="#"+color.ToArgb().ToString("X8")};swatch.Click+=(s,e)=>{Color=selected;Sync();};palette.Controls.Add(swatch);}layout.Controls.Add(palette,0,1);layout.SetColumnSpan(palette,4);
   string[] labels={"Rot","Grün","Blau","Deckkraft"};var numbers=new[]{Red,Green,Blue,Alpha};for(int i=0;i<4;i++){layout.Controls.Add(new Label{Text=labels[i],Dock=DockStyle.Fill},0,i+2);layout.Controls.Add(numbers[i],1,i+2);layout.SetColumnSpan(numbers[i],3);numbers[i].ValueChanged+=(s,e)=>{if(syncing)return;Color=System.Drawing.Color.FromArgb((int)Alpha.Value,(int)Red.Value,(int)Green.Value,(int)Blue.Value);Hex.Text="#"+Color.ToArgb().ToString("X8");preview.Invalidate();};}layout.Controls.Add(new Label{Text="Hex-Farbwert",Dock=DockStyle.Fill},0,6);layout.Controls.Add(Hex,1,6);layout.SetColumnSpan(Hex,3);
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=42,FlowDirection=FlowDirection.RightToLeft};var no=new Button{Text="Abbrechen",DialogResult=DialogResult.Cancel,AutoSize=true};var ok=new Button{Text="Übernehmen",AutoSize=true};ok.Click+=(s,e)=>{if(ApplyHex())DialogResult=DialogResult.OK;};buttons.Controls.Add(no);buttons.Controls.Add(ok);Controls.Add(layout);Controls.Add(state);Controls.Add(buttons);AcceptButton=ok;CancelButton=no;
  }
  protected override void OnLoad(EventArgs e){Sync();base.OnLoad(e);}
  void Sync(){syncing=true;Red.Value=Color.R;Green.Value=Color.G;Blue.Value=Color.B;Alpha.Value=Color.A;Hex.Text="#"+Color.ToArgb().ToString("X8");syncing=false;preview.Invalidate();}
  public bool ApplyHex(){string value=Hex.Text.Trim().TrimStart('#');uint rgb;if((value.Length!=6&&value.Length!=8)||!UInt32.TryParse(value,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out rgb)){state.Text="Bitte #RRGGBB oder #AARRGGBB eingeben.";return false;}Color=System.Drawing.Color.FromArgb(unchecked((int)(value.Length==6?rgb|0xff000000:rgb)));state.Text="";Sync();return true;}
 }
 public sealed class AppFontDialog:ModernForm {
  Font selected;public new Font Font {get{return selected;}set{if(selected!=null)selected.Dispose();selected=(Font)value.Clone();}}
  public readonly ComboBox Family=new StudioComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDown,AutoCompleteMode=AutoCompleteMode.SuggestAppend,AutoCompleteSource=AutoCompleteSource.ListItems};
  public readonly NumericUpDown PointSize=new StudioNumericUpDown{Minimum=1,Maximum=350,DecimalPlaces=1,Dock=DockStyle.Fill};public readonly CheckBox Bold=new CheckBox{Text="Fett",Dock=DockStyle.Fill};
  readonly Label sample=new Label{Dock=DockStyle.Fill,Text="Musikbox · Cover · Aa Bb Cc 0123456789",TextAlign=ContentAlignment.MiddleCenter,Tag="FontSample"},state=new Label{Dock=DockStyle.Bottom,Height=28};bool syncing;
  public AppFontDialog(){base.Font=new Font("Segoe UI",10);selected=(Font)base.Font.Clone();Text="Schriftart auswählen";ClientSize=new Size(650,350);MinimumSize=new Size(550,300);StartPosition=FormStartPosition.CenterParent;MinimizeBox=false;MaximizeBox=false;Padding=new Padding(12);
   foreach(var family in FontFamily.Families.OrderBy(x=>x.Name))Family.Items.Add(family.Name);var settings=new TableLayoutPanel{Dock=DockStyle.Top,Height=95,ColumnCount=3,RowCount=2};settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,65));settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,20));settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,15));settings.Controls.Add(new Label{Text="Schriftfamilie",Dock=DockStyle.Fill},0,0);settings.Controls.Add(new Label{Text="Größe (pt)",Dock=DockStyle.Fill},1,0);settings.Controls.Add(Family,0,1);settings.Controls.Add(PointSize,1,1);settings.Controls.Add(Bold,2,1);Family.TextChanged+=(s,e)=>Preview();PointSize.ValueChanged+=(s,e)=>Preview();Bold.CheckedChanged+=(s,e)=>Preview();
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=42,FlowDirection=FlowDirection.RightToLeft};var no=new Button{Text="Abbrechen",DialogResult=DialogResult.Cancel,AutoSize=true};var ok=new Button{Text="Übernehmen",AutoSize=true};ok.Click+=(s,e)=>{if(ApplyFont())DialogResult=DialogResult.OK;};buttons.Controls.Add(no);buttons.Controls.Add(ok);Controls.Add(sample);Controls.Add(state);Controls.Add(buttons);Controls.Add(settings);AcceptButton=ok;CancelButton=no;
  }
  protected override void OnLoad(EventArgs e){syncing=true;Family.Text=selected.Name;PointSize.Value=(decimal)Math.Max(1,Math.Min(350,selected.SizeInPoints));Bold.Checked=selected.Bold;syncing=false;base.OnLoad(e);Preview();}
  void Preview(){if(syncing)return;try{using(var test=new Font(Family.Text,Math.Min(44,(float)PointSize.Value),Bold.Checked?FontStyle.Bold:FontStyle.Regular)){var old=sample.Font;sample.Font=(Font)test.Clone();if(old!=null&&!Object.ReferenceEquals(old,ModernTheme.FieldLabelFont)&&!Object.ReferenceEquals(old,base.Font))old.Dispose();}}catch{}}
  public bool ApplyFont(){try{if(!Family.Items.Cast<string>().Any(x=>String.Equals(x,Family.Text,StringComparison.CurrentCultureIgnoreCase)))throw new Exception("Bitte eine installierte Schriftfamilie auswählen.");var next=new Font(Family.Text,(float)PointSize.Value,Bold.Checked?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Point);selected.Dispose();selected=next;state.Text="";return true;}catch(Exception ex){state.Text=ex.Message;return false;}}
  protected override void Dispose(bool disposing){if(disposing&&selected!=null){selected.Dispose();selected=null;}base.Dispose(disposing);}
 }
}
