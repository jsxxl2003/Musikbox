using System;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace Musikbox {
 public class StudioSongGrid:DataGridView {
  bool desiredVisible=true;public bool DesiredVisible{get{return desiredVisible;}}
  public StudioSongGrid(){ScrollBars=ScrollBars.None;DoubleBuffered=true;}
  protected override CreateParams CreateParams{get{var p=base.CreateParams;p.Style&=~StudioScrollbars.NativeStyles;return p;}}
  protected override void OnParentChanged(EventArgs e){base.OnParentChanged(e);StudioScrollbars.EnsureFrame(this);}
  protected override void SetVisibleCore(bool value){desiredVisible=value;if(Parent is StudioScrollFrame frame&&!frame.SynchronizingVisibility){frame.SetContentVisible(value);return;}base.SetVisibleCore(value);}
  DataGridViewRow deferredClick;Point pressed;
  protected override void OnMouseDown(MouseEventArgs e){
   var hit=HitTest(e.X,e.Y);bool single=e.Button==MouseButtons.Left&&ModifierKeys==Keys.None&&hit.RowIndex>=0;
   var selection=single&&MultiSelect&&Rows[hit.RowIndex].Selected&&SelectedRows.Count>1?SelectedRows.Cast<DataGridViewRow>().ToArray():null;
   deferredClick=null;pressed=e.Location;base.OnMouseDown(e);
   if(selection!=null){ClearSelection();foreach(var row in selection)if(row.DataGridView==this)row.Selected=true;deferredClick=Rows[hit.RowIndex];}
   else if(single&&hit.RowIndex<Rows.Count&&(SelectedRows.Count!=1||!Rows[hit.RowIndex].Selected)){var row=Rows[hit.RowIndex];ClearSelection();row.Selected=true;}
  }
  protected override void OnMouseMove(MouseEventArgs e){var size=SystemInformation.DragSize;if(!new Rectangle(pressed.X-size.Width/2,pressed.Y-size.Height/2,size.Width,size.Height).Contains(e.Location))deferredClick=null;base.OnMouseMove(e);}
  protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);var row=deferredClick;deferredClick=null;if(e.Button==MouseButtons.Left&&row!=null&&row.DataGridView==this){ClearSelection();row.Selected=true;}}
 }
}
