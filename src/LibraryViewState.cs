using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.ComponentModel;using System.Windows.Forms;
namespace Musikbox {
 public sealed class LibraryViewState {
  public readonly HashSet<string> Selected=new HashSet<string>(StringComparer.OrdinalIgnoreCase);public string Current,First,Column,SortColumn;public int FirstIndex,Horizontal;public bool Descending;
  public static string Key(DataGridViewRow row){if(row.Tag is Song s)return Path.GetFullPath(s.Source);if(row.Tag is Tuple<string,string> g)return g.Item1+"\n"+g.Item2;return null;}
  public static LibraryViewState Capture(DataGridView grid,LibraryViewState previous=null){if(grid.Rows.Count==0||(!grid.Visible&&previous?.First!=null))return previous??new LibraryViewState();var state=new LibraryViewState{First=previous?.First,FirstIndex=previous?.FirstIndex??0,Horizontal=grid.HorizontalScrollingOffset,SortColumn=grid.SortedColumn?.Name??previous?.SortColumn,Descending=grid.SortedColumn==null?previous?.Descending??false:grid.SortOrder==SortOrder.Descending,Column=grid.CurrentCell?.OwningColumn.Name??previous?.Column};foreach(DataGridViewRow row in grid.SelectedRows){string key=Key(row);if(key!=null)state.Selected.Add(key);}state.Current=grid.CurrentRow!=null&&grid.CurrentRow.Selected?Key(grid.CurrentRow):previous?.Current;int first=grid.FirstDisplayedScrollingRowIndex;if(first>=0&&first<grid.Rows.Count){state.First=Key(grid.Rows[first]);state.FirstIndex=first;}return state;}
  public void Restore(DataGridView grid,string anchor=null,bool ensureAnchor=true){if(grid.Rows.Count==0)return;if(SortColumn!=null&&grid.Columns.Contains(SortColumn)&&grid.Columns[SortColumn].SortMode!=DataGridViewColumnSortMode.NotSortable)grid.Sort(grid.Columns[SortColumn],Descending?ListSortDirection.Descending:ListSortDirection.Ascending);
   var rows=grid.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow).ToArray();var current=rows.FirstOrDefault(r=>String.Equals(Key(r),anchor??Current,StringComparison.OrdinalIgnoreCase));var column=Column!=null&&grid.Columns.Contains(Column)&&grid.Columns[Column].Visible?grid.Columns[Column]:grid.Columns.Cast<DataGridViewColumn>().Where(c=>c.Visible).OrderBy(c=>c.DisplayIndex).FirstOrDefault();if(current!=null&&column!=null)grid.CurrentCell=current.Cells[column.Index];grid.ClearSelection();foreach(var row in rows)row.Selected=Selected.Contains(Key(row)??"");if(current!=null&&ensureAnchor)current.Selected=true;
   var first=rows.FirstOrDefault(r=>String.Equals(Key(r),First,StringComparison.OrdinalIgnoreCase));int index=first?.Index??Math.Min(Math.Max(0,FirstIndex),rows.Length-1);try{if(index>=0&&!grid.Rows[index].Frozen&&grid.Rows[index].Visible)grid.FirstDisplayedScrollingRowIndex=index;grid.HorizontalScrollingOffset=Horizontal;if(current!=null&&ensureAnchor&&grid.Visible&&!current.Displayed&&!current.Frozen)grid.FirstDisplayedScrollingRowIndex=current.Index;}catch(InvalidOperationException){}catch(ArgumentOutOfRangeException){}
  }
 }
 public partial class Controller {
  LibraryViewState titleViewState=new LibraryViewState();readonly Dictionary<string,LibraryViewState> groupViewStates=new Dictionary<string,LibraryViewState>();string renderedGroupMode;Song editedSong;
  void SaveSelectedFolder(string oldPath,string name,string number){
   if(busy||v.LibraryMode!="Ordner")return;
   var row=v.Groups.CurrentRow;var entry=row?.Tag as Tuple<string,string>;
   if(entry==null||entry.Item1!="folder")throw new InvalidOperationException("Bitte zuerst einen Ordner auswählen.");
   var folder=p.Folders.FirstOrDefault(f=>f.Name==entry.Item2&&String.Equals(f.SourceDirectory,oldPath,StringComparison.OrdinalIgnoreCase));
   if(folder==null)throw new InvalidOperationException("Der ausgewählte Ordner wurde nicht gefunden.");
   name=(name??"").Trim();number=(number??"").Trim();
   if(name.Length==0||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0||name=="."||name=="..")throw new InvalidOperationException("Ungültiger Ordnername.");
   if(number.Length!=3||!number.All(Char.IsDigit))throw new InvalidOperationException("Die Ordnernummer muss genau drei Ziffern enthalten.");
   string newName=number+" "+name;
   if(p.Folders.Any(f=>f!=folder&&String.Equals(f.Name,newName,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Dieser Ordnername ist bereits vorhanden.");
   if(p.Folders.Any(f=>f!=folder&&f.Name!=null&&f.Name.StartsWith(number+" ",StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Diese Ordnernummer ist bereits vergeben.");
   string source=folder.SourceDirectory,target=source;
   if(!String.IsNullOrWhiteSpace(source)&&Directory.Exists(source)){
    string parent=Path.GetDirectoryName(Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar));
    if(String.IsNullOrWhiteSpace(parent))throw new InvalidOperationException("Der Stammordner darf nicht umbenannt werden.");
    target=Path.Combine(parent,newName);
    if(!String.Equals(source,target,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(target))throw new IOException("Der Zielordner existiert bereits.");
   }
   if(String.Equals(folder.Name,newName,StringComparison.Ordinal)&&String.Equals(source,target,StringComparison.Ordinal))return;
   var updates=new List<Tuple<Song,string,string>>();
   if(!String.IsNullOrWhiteSpace(source)&&Directory.Exists(source)&&!String.Equals(source,target,StringComparison.Ordinal)){
    string prefix=Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
    foreach(var song in AllSongs){
     if(String.IsNullOrWhiteSpace(song.Source))continue;
     string full=Path.GetFullPath(song.Source);
     if(full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))updates.Add(Tuple.Create(song,song.Source,Path.Combine(target,full.Substring(prefix.Length))));
    }
    Directory.Move(source,target);
   }
   try{
    foreach(var item in updates)item.Item1.Source=item.Item3;
    foreach(var f in p.Folders){
     if(String.IsNullOrWhiteSpace(f.SourceDirectory)||String.IsNullOrWhiteSpace(source))continue;
     string full=Path.GetFullPath(f.SourceDirectory);
     string prefix=Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
     if(String.Equals(full,Path.GetFullPath(source),StringComparison.OrdinalIgnoreCase))f.SourceDirectory=target;
     else if(full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))f.SourceDirectory=Path.Combine(target,full.Substring(prefix.Length));
    }
    folder.Name=newName;folder.Theme=name;foreach(var song in folder.Songs)song.FolderName=newName;
    Changed("Ordner bearbeiten");int index=p.Folders.IndexOf(folder);RefreshFolders(index);v.LibraryMode="Ordner";PopulateLibrary();
   }catch{
    foreach(var item in updates)item.Item1.Source=item.Item2;
    if(!String.Equals(source,target,StringComparison.Ordinal)&&Directory.Exists(target)&&!Directory.Exists(source))Directory.Move(target,source);
    throw;
   }
  }
  void SwitchLibraryMode(string mode){if(busy)return;v.Songs.EndEdit();CommitSideTags();recentImportPaths=null;libraryFolder=null;groupField=groupValue=null;v.LibraryMode=mode=="Alle"?"Titel":mode;PopulateLibrary();}
  void SelectGroupFolder(){if(refresh||v.LibraryMode!="Ordner"||v.Groups.CurrentRow==null)return;var row=v.Groups.CurrentRow;if(!row.Selected)return;var entry=row.Tag as Tuple<string,string>;if(entry==null||entry.Item1!="folder")return;int index=p.Folders.FindIndex(f=>f.Name==entry.Item2);if(index<0)return;bool before=refresh;refresh=true;try{v.Folders.SelectedIndex=index;}finally{refresh=before;}if(Current!=null)ShowFolderArtwork(Current);}
  void CaptureLibraryViews(){titleViewState=LibraryViewState.Capture(v.Songs,titleViewState);if(renderedGroupMode!=null){groupViewStates.TryGetValue(renderedGroupMode,out var previous);groupViewStates[renderedGroupMode]=LibraryViewState.Capture(v.Groups,previous);}}
  void RestoreActiveLibraryView(){bool before=refresh;refresh=true;try{if(v.LibraryMode=="Titel")titleViewState.Restore(v.Songs,EditedPath);else if(groupViewStates.TryGetValue(v.LibraryMode,out var state))state.Restore(v.Groups,null,false);else v.RestoreGroupSort();}finally{refresh=before;}}
  string EditedPath{get{return editedSong==null?null:Path.GetFullPath(editedSong.Source);}}
 }
}
