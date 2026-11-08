using System;using System.Drawing;using System.Linq;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace Musikbox {
 // No painting over native scrollbars. Their window styles are excluded and
 // actual scrolling is driven by application-owned rails in a separate frame.
 public static class StudioScrollbars {
  public const int NativeStyles=0x00200000|0x00100000;
  public static void Attach(Control control){if(control is StudioScrollFrame||control is StudioScrollPanel||control is StudioScrollRail)return;if(control is StudioSongGrid||control is StudioListBox||control is StudioCheckedListBox||control is StudioListView||(control is StudioTextBox&&((TextBox)control).Multiline))EnsureFrame(control);}
  public static Control Surface(Control control){return control.Parent is StudioScrollFrame?control.Parent:control;}
  public static void EnsureFrame(Control control){var parent=control.Parent;if(parent==null||parent is StudioScrollFrame||control.IsDisposed)return;
   int index=parent.Controls.GetChildIndex(control);var bounds=control.Bounds;DockStyle dock=control.Dock;AnchorStyles anchor=control.Anchor;var margin=control.Margin;Size minimum=control.MinimumSize;
   var table=parent as TableLayoutPanel;var position=table==null?new TableLayoutPanelCellPosition():table.GetPositionFromControl(control);int colSpan=table==null?1:table.GetColumnSpan(control),rowSpan=table==null?1:table.GetRowSpan(control);
   parent.SuspendLayout();try{var frame=new StudioScrollFrame(control){Bounds=bounds,Dock=dock,Anchor=anchor,Margin=margin,MinimumSize=minimum,TabIndex=control.TabIndex};if(table!=null){table.Controls.Add(frame,position.Column,position.Row);table.SetColumnSpan(frame,colSpan);table.SetRowSpan(frame,rowSpan);}else parent.Controls.Add(frame);parent.Controls.SetChildIndex(frame,Math.Min(index,parent.Controls.Count-1));}finally{parent.ResumeLayout(true);}
  }
  [StructLayout(LayoutKind.Sequential)]struct StyleChange{public int OldStyle,NewStyle;}
  // Prevent Windows from adding a native scrollbar before it could be painted.
  internal sealed class NativeStyleGate:NativeWindow {
   readonly Control owner;public NativeStyleGate(Control owner){this.owner=owner;owner.HandleCreated+=(s,e)=>AssignHandle(owner.Handle);owner.HandleDestroyed+=(s,e)=>ReleaseHandle();owner.Disposed+=(s,e)=>ReleaseHandle();if(owner.IsHandleCreated)AssignHandle(owner.Handle);}
   protected override void WndProc(ref Message m){if(m.Msg==0x7C&&unchecked((int)m.WParam.ToInt64())==-16&&m.LParam!=IntPtr.Zero){var change=Marshal.PtrToStructure<StyleChange>(m.LParam);change.NewStyle&=~NativeStyles;Marshal.StructureToPtr(change,m.LParam,false);}base.WndProc(ref m);if((m.Msg==0x5||m.Msg==0x180||m.Msg==0x181||m.Msg==0x182||m.Msg==0x184||m.Msg==0x197||m.Msg==0x114||m.Msg==0x115||m.Msg==0x100)&&owner.Parent is StudioScrollFrame frame)frame.RefreshRange();}
  }
 }
 public sealed class StudioScrollFrame:Panel,IMessageFilter {
  public readonly Control Content;public readonly StudioScrollRail Vertical=new StudioScrollRail{SmallChange=1,AccessibleName="Vertikaler Bildlauf"},Horizontal=new StudioScrollRail{Horizontal=true,SmallChange=24,AccessibleName="Horizontaler Bildlauf"};
  int suspendUpdates;public void SuspendUpdates(){suspendUpdates++;}public void ResumeUpdates(){if(suspendUpdates>0)suspendUpdates--;RefreshRange();}
  bool layingOut,sync,filterInstalled;internal bool SynchronizingVisibility;public bool IsLayingOut{get{return layingOut;}}
  public StudioScrollFrame(Control content){Content=content;Tag="StudioExact";BackColor=ModernTheme.Navy;DoubleBuffered=true;Vertical.Visible=Horizontal.Visible=false;Controls.Add(Vertical);Controls.Add(Horizontal);content.Parent=null;content.Dock=DockStyle.None;content.Anchor=AnchorStyles.Top|AnchorStyles.Left;content.Margin=Padding.Empty;Controls.Add(content);if(content is StudioSongGrid)SetContentVisible(((StudioSongGrid)content).DesiredVisible);Vertical.ValueChanged+=(s,e)=>SetOffset(false,Vertical.Value);Horizontal.ValueChanged+=(s,e)=>SetOffset(true,Horizontal.Value);
   content.SizeChanged+=(s,e)=>RefreshRange();content.FontChanged+=(s,e)=>RefreshRange();content.TextChanged+=(s,e)=>RefreshRange();content.Invalidated+=(s,e)=>RefreshRange();
   var grid=content as DataGridView;if(grid!=null){grid.ScrollBars=ScrollBars.None;grid.Scroll+=(s,e)=>RefreshRange();grid.RowsAdded+=(s,e)=>RefreshRange();grid.RowsRemoved+=(s,e)=>RefreshRange();grid.ColumnWidthChanged+=(s,e)=>RefreshRange();grid.ColumnStateChanged+=(s,e)=>RefreshRange();grid.RowHeightChanged+=(s,e)=>RefreshRange();grid.Sorted+=(s,e)=>RefreshRange();grid.HandleCreated+=(s,e)=>RefreshRange();}
   var list=content as ListBox;if(list!=null)list.SelectedIndexChanged+=(s,e)=>RefreshRange();var view=content as ListView;if(view!=null)view.ColumnWidthChanged+=(s,e)=>RefreshRange();
  }
  public void SetContentVisible(bool value){if(SynchronizingVisibility)return;SynchronizingVisibility=true;try{Content.Visible=value;Visible=value;}finally{SynchronizingVisibility=false;}}
  protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);RefreshRange();}protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);RefreshRange();}
  public void RefreshRange(){if(suspendUpdates>0||sync||layingOut||Content==null||IsDisposed||Disposing||Content.IsDisposed||Content.Disposing)return;layingOut=true;sync=true;try{int rail=Math.Max(10,(int)Math.Round(12*StudioDrawing.Scale(this)));bool v=Vertical.Visible,h=Horizontal.Visible;for(int pass=0;pass<3;pass++){int w=Math.Max(1,ClientSize.Width-(v?rail:0)),height=Math.Max(1,ClientSize.Height-(h?rail:0));Content.SetBounds(0,0,w,height);ReadRanges();bool nv=Vertical.Maximum>0,nh=Horizontal.Maximum>0;if(nv==v&&nh==h)break;v=nv;h=nh;}Vertical.Visible=v;Horizontal.Visible=h;int width=Math.Max(1,ClientSize.Width-(v?rail:0)),hh=Math.Max(1,ClientSize.Height-(h?rail:0));Content.SetBounds(0,0,width,hh);Vertical.SetBounds(width,0,rail,hh);Horizontal.SetBounds(0,hh,width,rail);Vertical.BringToFront();Horizontal.BringToFront();}finally{sync=false;layingOut=false;}}
  void Range(StudioScrollRail rail,int page,int content,int value){rail.SetRange(page,content);rail.Value=value;}
  void ReadRanges(){
   var grid=Content as DataGridView;if(grid!=null){int count=grid.Rows.Count-(grid.NewRowIndex>=0?1:0),page=grid.IsHandleCreated?grid.DisplayedRowCount(false):0;if(page==0)page=Math.Max(1,(grid.ClientSize.Height-grid.ColumnHeadersHeight)/Math.Max(1,grid.RowTemplate.Height));Range(Vertical,page,count,Math.Max(0,grid.FirstDisplayedScrollingRowIndex));int headers=grid.RowHeadersVisible?grid.RowHeadersWidth:0;int total=grid.Columns.Cast<DataGridViewColumn>().Where(c=>c.Visible).Sum(c=>c.Width);Range(Horizontal,Math.Max(1,grid.ClientSize.Width-headers),total,grid.HorizontalScrollingOffset);return;}
   if(!Content.IsHandleCreated){Range(Vertical,1,1,0);Range(Horizontal,1,1,0);return;}
   var list=Content as ListBox;if(list!=null){int page=Math.Max(1,list.ClientSize.Height/Math.Max(1,list.ItemHeight));Range(Vertical,page,list.Items.Count,list.Items.Count==0?0:list.TopIndex);int width=list.HorizontalExtent;if(list.HorizontalScrollbar&&width==0){foreach(var item in list.Items)width=Math.Max(width,TextRenderer.MeasureText(list.GetItemText(item),list.Font).Width+8);}Range(Horizontal,list.ClientSize.Width,width,list is StudioListBox?((StudioListBox)list).StudioHorizontalOffset:NativeScroll.Position(Content.Handle,0));return;}
   var text=Content as TextBox;if(text!=null){int count=NativeScroll.Send(Content.Handle,0xBA,0,0),page=Math.Max(1,text.ClientSize.Height/Math.Max(1,text.Font.Height));Range(Vertical,page,count,NativeScroll.Send(Content.Handle,0xCE,0,0));int width=text.WordWrap?text.ClientSize.Width:text.Lines.Select(l=>TextRenderer.MeasureText(l,text.Font).Width+8).DefaultIfEmpty(0).Max();Range(Horizontal,text.ClientSize.Width,width,NativeScroll.Position(Content.Handle,0));return;}
   var view=Content as ListView;if(view!=null){int page=Math.Max(1,NativeScroll.Send(Content.Handle,0x1028,0,0)),top=NativeScroll.Send(Content.Handle,0x1027,0,0);Range(Vertical,page,view.Items.Count,top);Range(Horizontal,view.ClientSize.Width,view.Columns.Cast<ColumnHeader>().Sum(c=>c.Width),NativeScroll.Position(Content.Handle,0));return;}
  }
  void SetOffset(bool horizontal,int value){if(sync||!Content.IsHandleCreated)return;sync=true;try{var grid=Content as DataGridView;if(grid!=null){if(horizontal)grid.HorizontalScrollingOffset=value;else if(value<grid.Rows.Count&&value>=0&&!grid.Rows[value].Frozen&&grid.Rows[value].Visible)grid.FirstDisplayedScrollingRowIndex=value;}
    else if(Content is ListBox){if(horizontal){if(Content is StudioListBox)((StudioListBox)Content).StudioHorizontalOffset=value;else NativeScroll.SetPosition(Content.Handle,0,value);}else if(((ListBox)Content).Items.Count>0)((ListBox)Content).TopIndex=value;}
    else if(Content is TextBox){if(horizontal)NativeScroll.SetPosition(Content.Handle,0,value);else NativeScroll.Send(Content.Handle,0xB6,0,value-NativeScroll.Send(Content.Handle,0xCE,0,0));}
    else if(Content is ListView){var view=(ListView)Content;int delta=horizontal?value-NativeScroll.Position(Content.Handle,0):value-NativeScroll.Send(Content.Handle,0x1027,0,0);int row=view.Items.Count>0?Math.Max(1,view.GetItemRect(0).Height):Math.Max(1,view.Font.Height+4);NativeScroll.Send(Content.Handle,0x1014,horizontal?delta:0,horizontal?0:delta*row);}
   }catch(InvalidOperationException){}catch(ArgumentOutOfRangeException){}finally{sync=false;}RefreshRange();}
  public bool PreFilterMessage(ref Message m){if((m.Msg!=0x20A&&m.Msg!=0x20E)||!Visible||IsDisposed)return false;Control target=Control.FromChildHandle(m.HWnd);for(Control c=target;c!=null;c=c.Parent){if(c is StudioScrollFrame&&c!=this)return false;if(c==this){bool horizontal=m.Msg==0x20E||(ModifierKeys&Keys.Shift)!=0;var bar=horizontal?Horizontal:Vertical;if(bar.Maximum==0)return false;int delta=unchecked((short)((m.WParam.ToInt64()>>16)&0xffff));bar.ScrollWheel(m.Msg==0x20E?-delta:delta);return true;}}return false;}
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);if(!filterInstalled){Application.AddMessageFilter(this);filterInstalled=true;}}protected override void OnHandleDestroyed(EventArgs e){if(filterInstalled){Application.RemoveMessageFilter(this);filterInstalled=false;}base.OnHandleDestroyed(e);}protected override void Dispose(bool disposing){if(filterInstalled){Application.RemoveMessageFilter(this);filterInstalled=false;}base.Dispose(disposing);}
 }
 internal static class NativeScroll {
  [StructLayout(LayoutKind.Sequential)]struct Info{public int Size;public uint Mask;public int Min,Max;public uint Page;public int Pos,Track;}
  [DllImport("user32.dll",EntryPoint="SendMessageW")]static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
  [DllImport("user32.dll")]static extern bool GetScrollInfo(IntPtr h,int bar,ref Info info);
  [DllImport("user32.dll")]static extern int SetScrollInfo(IntPtr h,int bar,ref Info info,bool redraw);
  public static int Send(IntPtr h,int msg,int w,int l){return unchecked((int)SendMessage(h,msg,new IntPtr(w),new IntPtr(l)).ToInt64());}
  public static int Position(IntPtr h,int bar){var info=new Info{Size=Marshal.SizeOf<Info>(),Mask=4};return GetScrollInfo(h,bar,ref info)?info.Pos:0;}
  public static void SetPosition(IntPtr h,int bar,int value){var info=new Info{Size=Marshal.SizeOf<Info>(),Mask=4,Pos=value};SetScrollInfo(h,bar,ref info,false);Send(h,bar==0?0x114:0x115,4|(value<<16),0);}
 }
}
