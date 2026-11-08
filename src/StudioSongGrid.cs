using System;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace Musikbox {
 public class StudioSongGrid:DataGridView {
  bool desiredVisible=true;public bool DesiredVisible{get{return desiredVisible;}}
  public StudioSongGrid(){ScrollBars=ScrollBars.None;DoubleBuffered=true;RowHeadersVisible=true;RowHeadersWidth=26;RowHeadersWidthSizeMode=DataGridViewRowHeadersWidthSizeMode.DisableResizing;}
  protected override CreateParams CreateParams{get{var p=base.CreateParams;p.Style&=~StudioScrollbars.NativeStyles;return p;}}
  protected override void OnParentChanged(EventArgs e){base.OnParentChanged(e);StudioScrollbars.EnsureFrame(this);}
  protected override void SetVisibleCore(bool value){desiredVisible=value;if(Parent is StudioScrollFrame frame&&!frame.SynchronizingVisibility){frame.SetContentVisible(value);return;}base.SetVisibleCore(value);}
  DataGridViewRow deferredClick;Point pressed;bool rubberSelecting;Point rubberStart;
  protected override void OnMouseDown(MouseEventArgs e){
   var hit=HitTest(e.X,e.Y);
   if(e.Button==MouseButtons.Left&&ModifierKeys==Keys.None&&((hit.RowIndex<0&&hit.Type==DataGridViewHitTestType.None)||hit.Type==DataGridViewHitTestType.RowHeader)){
    rubberSelecting=true;rubberStart=e.Location;Capture=true;ClearSelection();return;
   }
   bool single=e.Button==MouseButtons.Left&&ModifierKeys==Keys.None&&hit.RowIndex>=0;
   var selection=single&&MultiSelect&&Rows[hit.RowIndex].Selected&&SelectedRows.Count>1?SelectedRows.Cast<DataGridViewRow>().ToArray():null;
   deferredClick=null;pressed=e.Location;base.OnMouseDown(e);
   if(selection!=null){ClearSelection();foreach(var row in selection)if(row.DataGridView==this)row.Selected=true;deferredClick=Rows[hit.RowIndex];}
   else if(single&&hit.RowIndex<Rows.Count&&(SelectedRows.Count!=1||!Rows[hit.RowIndex].Selected)){var row=Rows[hit.RowIndex];ClearSelection();row.Selected=true;}
  }
  protected override void OnMouseMove(MouseEventArgs e){
   if(rubberSelecting){
    int top=Math.Min(rubberStart.Y,e.Y),bottom=Math.Max(rubberStart.Y,e.Y);
    if(e.Y<ColumnHeadersHeight&&FirstDisplayedScrollingRowIndex>0)FirstDisplayedScrollingRowIndex=Math.Max(0,FirstDisplayedScrollingRowIndex-1);
    else if(e.Y>ClientSize.Height-12&&Rows.Count>0){int first=FirstDisplayedScrollingRowIndex;if(first>=0&&first<Rows.Count-1)FirstDisplayedScrollingRowIndex=first+1;}
    foreach(DataGridViewRow row in Rows){
     if(!row.Visible)continue;
     var rect=GetRowDisplayRectangle(row.Index,false);
     row.Selected=rect.Bottom>=top&&rect.Top<=bottom;
    }
    return;
   }
   var size=SystemInformation.DragSize;if(!new Rectangle(pressed.X-size.Width/2,pressed.Y-size.Height/2,size.Width,size.Height).Contains(e.Location))deferredClick=null;base.OnMouseMove(e);}
  protected override void OnRowPostPaint(DataGridViewRowPostPaintEventArgs e){base.OnRowPostPaint(e);using(var brush=new SolidBrush(ModernTheme.Navy))e.Graphics.FillRectangle(brush,0,e.RowBounds.Top,RowHeadersWidth,e.RowBounds.Height);}
  protected override void OnMouseUp(MouseEventArgs e){if(rubberSelecting){rubberSelecting=false;Capture=false;return;}base.OnMouseUp(e);var row=deferredClick;deferredClick=null;if(e.Button==MouseButtons.Left&&row!=null&&row.DataGridView==this){ClearSelection();row.Selected=true;}}
 }
}
