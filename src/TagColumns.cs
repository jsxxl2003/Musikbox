using System;using System.Linq;using System.Windows.Forms;
namespace Musikbox {public static class TagColumns {
 public static void Standard(DataGridView grid){
  foreach(var field in TagSchema.Fields){if(!grid.Columns.Contains(field.Column))grid.Columns.Add(field.Column,field.Name);var column=grid.Columns[field.Column];column.HeaderText=field.Name;column.HeaderCell.ToolTipText=field.Format;column.ReadOnly=!field.Writable;column.SortMode=DataGridViewColumnSortMode.Automatic;}
  foreach(DataGridViewColumn column in grid.Columns)column.Visible=TagSchema.Fields.Any(f=>f.Column==column.Name&&f.DefaultVisible);
  for(int i=0;i<TagSchema.Fields.Length;i++)grid.Columns[TagSchema.Fields[i].Column].DisplayIndex=i;
  foreach(var field in TagSchema.Fields.Reverse())grid.Columns[field.Column].DisplayIndex=0;
  grid.AllowUserToOrderColumns=true;grid.AllowUserToResizeColumns=true;grid.SortCompare+=(s,e)=>{if(e.Column.Name!="Bitrate"&&e.Column.Name!="Tracknummer")return;long a,b;Int64.TryParse(Convert.ToString(e.CellValue1).Split('/')[0],out a);Int64.TryParse(Convert.ToString(e.CellValue2).Split('/')[0],out b);e.SortResult=a.CompareTo(b);e.Handled=true;};
  var menu=new ContextMenuStrip();menu.Opening+=(s,e)=>{menu.Items.Clear();foreach(DataGridViewColumn c in grid.Columns.Cast<DataGridViewColumn>().OrderBy(x=>x.DisplayIndex)){var column=c;var item=new ToolStripMenuItem(column.HeaderText){Checked=column.Visible};item.Click+=(a,b)=>{if(column.Visible&&grid.Columns.Cast<DataGridViewColumn>().Count(x=>x.Visible)==1)return;column.Visible=!column.Visible;};menu.Items.Add(item);}ModernTheme.Apply(menu);};
  foreach(DataGridViewColumn c in grid.Columns)c.HeaderCell.ContextMenuStrip=menu;grid.Disposed+=(s,e)=>menu.Dispose();
 }
}}
