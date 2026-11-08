using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
namespace Musikbox {
 public static class TrackColumns {
  sealed class LoadState{public int Generation;}
  static readonly ConditionalWeakTable<DataGridView,LoadState> loads=new ConditionalWeakTable<DataGridView,LoadState>();
  static readonly string[] detailNames={"Bitrate","Änderungsdatum","Erstelldatum","Dateigröße","Format","Dateipfad","Tag"};
  public static void Configure(DataGridView grid){
   grid.AllowUserToOrderColumns=true;grid.AllowUserToResizeColumns=true;grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.None;
   foreach(DataGridViewColumn c in grid.Columns){c.Width=c.Name=="Quelle"?240:200;c.MinimumWidth=60;}
   foreach(string name in new[]{"Bitrate","Änderungsdatum","Erstelldatum","Dateigröße","Format","Dateipfad"})
    grid.Columns.Add(new DataGridViewTextBoxColumn{Name=name,HeaderText=name,ReadOnly=true,Visible=false,Width=name=="Dateipfad"?320:160,MinimumWidth=60,SortMode=DataGridViewColumnSortMode.NotSortable});
   foreach(string name in new[]{"Ordner","Jahr","Genre","Dauer","Kommentar"})grid.Columns.Add(new DataGridViewTextBoxColumn{Name=name,HeaderText=name,ReadOnly=name=="Ordner"||name=="Dauer",Width=name=="Jahr"||name=="Dauer"?85:160,MinimumWidth=60,Visible=name!="Kommentar",SortMode=DataGridViewColumnSortMode.Automatic});
   string[] studioOrder={"Ordner","Interpret","Titel","Album","Jahr","Genre","Dauer"};for(int i=0;i<studioOrder.Length;i++){var c=grid.Columns[studioOrder[i]];c.DisplayIndex=i;c.Width=c.Name=="Jahr"?65:c.Name=="Dauer"?75:c.Name=="Titel"?220:c.Name=="Genre"?130:170;}grid.Columns["Quelle"].Visible=false;
   grid.Columns["Dauer"].ValueType=typeof(double);
   grid.CellFormatting+=(s,e)=>{if(e.ColumnIndex>=0&&grid.Columns[e.ColumnIndex].Name=="Dauer"&&e.Value is double){e.Value=TagSchema.Length((double)e.Value);e.FormattingApplied=true;}};
   grid.ColumnStateChanged+=(s,e)=>{if(e.StateChanged==DataGridViewElementStates.Visible&&e.Column.Visible&&detailNames.Contains(e.Column.Name))Load(grid);};
   grid.Disposed+=(s,e)=>Interlocked.Increment(ref loads.GetValue(grid,_=>new LoadState()).Generation);
   var menu=new ContextMenuStrip();
   foreach(DataGridViewColumn column in grid.Columns){
    var c=column;var item=new ToolStripMenuItem(c.HeaderText){Checked=c.Visible,Tag=c};
    item.Click+=(s,e)=>{if(c.Visible&&grid.Columns.Cast<DataGridViewColumn>().Count(x=>x.Visible)==1)return;grid.EndEdit();c.Visible=!c.Visible;item.Checked=c.Visible;};
    menu.Items.Add(item);c.HeaderCell.ContextMenuStrip=menu;
   }
   menu.Opening+=(s,e)=>{foreach(ToolStripMenuItem item in menu.Items)item.Checked=((DataGridViewColumn)item.Tag).Visible;};
   grid.Disposed+=(s,e)=>menu.Dispose();
  }
  public static void Load(DataGridView grid){
   var state=loads.GetValue(grid,_=>new LoadState());int generation=Interlocked.Increment(ref state.Generation);
   if(grid.IsDisposed||!grid.IsHandleCreated||!detailNames.Any(n=>grid.Columns.Contains(n)&&grid.Columns[n].Visible))return;
   bool bitrate=grid.Columns["Bitrate"].Visible;if(!bitrate&&!grid.Columns["Tag"].Visible&&!new[]{"Änderungsdatum","Erstelldatum","Dateigröße","Format"}.Any(n=>grid.Columns[n].Visible))return;
   var rows=grid.Rows.Cast<DataGridViewRow>().Where(r=>r.Tag is Song).ToArray();var paths=rows.Select(r=>((Song)r.Tag).Source).ToArray();if(!new[]{"Änderungsdatum","Erstelldatum","Dateigröße","Format"}.Any(n=>grid.Columns[n].Visible)&&rows.All(r=>((Song)r.Tag).Bitrate>0&&((Song)r.Tag).TagType!=null))return;
   var thread=new Thread(()=>{
    var values=new string[paths.Length][];
    for(int i=0;i<paths.Length;i++){if(generation!=Volatile.Read(ref state.Generation))return;values[i]=Read(paths[i],bitrate);}
    if(grid.IsDisposed||!grid.IsHandleCreated||generation!=Volatile.Read(ref state.Generation))return;
    try{grid.BeginInvoke((Action)(()=>{if(grid.IsDisposed||generation!=state.Generation)return;for(int i=0;i<rows.Length;i++){var row=rows[i];if(row.DataGridView!=grid||!(row.Tag is Song)||((Song)row.Tag).Source!=paths[i])continue;for(int j=0;j<detailNames.Length;j++)row.Cells[detailNames[j]].Value=values[i][j];var song=(Song)row.Tag;int rate;if(Int32.TryParse(values[i][0],out rate))song.Bitrate=rate;song.TagType=values[i][6];}}));}catch(InvalidOperationException){}
   });thread.IsBackground=true;thread.SetApartmentState(ApartmentState.STA);thread.Start();
  }

  public static string[] Read(string path){return Read(path,true);}
  static string[] Read(string path,bool bitrate){
   var result=new[]{"—","—","—","—","—",path??"",""};
   try{var file=new FileInfo(path);result[5]=file.FullName;result[4]=file.Extension.TrimStart('.').ToUpperInvariant();if(!file.Exists)return result;
    result[1]=file.LastWriteTime.ToString("dd.MM.yyyy HH:mm");result[2]=file.CreationTime.ToString("dd.MM.yyyy HH:mm");result[3]=(file.Length/1048576.0).ToString("N2")+" MB";
    using(var audio=TagLib.File.Create(file.FullName)){result[0]=audio.Properties.AudioBitrate.ToString();result[6]=TagSchema.Types(audio);}
   }catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}catch(TagLib.UnsupportedFormatException){}catch(TagLib.CorruptFileException){}
   return result;
  }
  static object Call(object target,string name,params object[] args){return target.GetType().InvokeMember(name,BindingFlags.InvokeMethod,null,target,args);}
  static string ReadBitrate(string path){
   object shell=null,folder=null,item=null;
   try{shell=Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));folder=Call(shell,"NameSpace",Path.GetDirectoryName(path));if(folder==null)return "—";item=Call(folder,"ParseName",Path.GetFileName(path));if(item==null)return "—";object value=Call(item,"ExtendedProperty","System.Audio.EncodingBitrate");double rate;if(value!=null&&Double.TryParse(Convert.ToString(value),out rate)&&rate>0)return Math.Round(rate/1000).ToString("N0")+" kbit/s";}
   catch(Exception){return "—";}finally{foreach(var obj in new[]{item,folder,shell})if(obj!=null&&Marshal.IsComObject(obj))Marshal.ReleaseComObject(obj);}
   return "—";
  }
 }
}
