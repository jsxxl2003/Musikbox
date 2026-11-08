using System;using System.Linq;using System.Collections.Generic;using System.Threading;using System.Threading.Tasks;using System.Windows.Forms;using System.IO;
namespace Musikbox {public partial class Controller {
 CancellationTokenSource librarySearchCancel;int librarySearchGeneration,libraryRenderGeneration=-1;bool libraryRefreshBefore;LibrarySearchIndex searchIndex;
 void CancelLibrarySearch(){if(libraryRenderGeneration>=0){libraryRenderGeneration=-1;refresh=libraryRefreshBefore;updatingLibrary=false;if(!v.IsDisposed)v.EndLibraryUpdate();}librarySearchGeneration++;if(librarySearchCancel!=null){librarySearchCancel.Cancel();librarySearchCancel=null;}}
 async void QueueLibrarySearch(string query,bool reveal=true){
  if(!busy){v.Songs.EndEdit();CommitSideTags();}CancelLibrarySearch();int generation=librarySearchGeneration;var source=new CancellationTokenSource();librarySearchCancel=source;var token=source.Token;
  try{
   await Task.Delay(180,token);if(busy||v.IsDisposed)return;
   if(searchIndex==null)searchIndex=new LibrarySearchIndex(p.Folders.SelectMany(f=>f.Songs.Select(song=>new LibrarySearchIndex.Entry(f,song))));
   var index=searchIndex;bool spoken=voiceQueryText==query;
   var entries=await Task.Run(()=>index.Filter(query,spoken,token),token);
   token.ThrowIfCancellationRequested();if(generation!=librarySearchGeneration||v.IsDisposed||v.SearchBox.Text!=query)return;
   titleViewState=LibraryViewState.Capture(v.Songs,titleViewState);var selected=new HashSet<Song>(SelectedSongs());bool before=refresh;libraryRefreshBefore=before;libraryRenderGeneration=generation;refresh=true;updatingLibrary=true;v.BeginLibraryUpdate();
   try{
    v.PresentSearchResults(reveal);v.Songs.Rows.Clear();int color=0;
    // Add bounded batches so input and cancellation remain responsive on large imports.
    for(int offset=0;offset<entries.Length;offset+=192){
     token.ThrowIfCancellationRequested();if(generation!=librarySearchGeneration)return;
     var rows=new List<DataGridViewRow>();foreach(var entry in entries.Skip(offset).Take(192)){
      var song=entry.Song;song.StudioColorIndex=color++;var row=new DataGridViewRow();row.CreateCells(v.Songs);row.Tag=song;
      row.Cells[v.Songs.Columns["Interpret"].Index].Value=song.Artist;row.Cells[v.Songs.Columns["Titel"].Index].Value=song.Title;row.Cells[v.Songs.Columns["Album"].Index].Value=song.Album;row.Cells[v.Songs.Columns["Quelle"].Index].Value=Path.GetFileName(song.Source);
      row.Cells[v.Songs.Columns["Tracknummer"].Index].Value=song.Track;row.Cells[v.Songs.Columns["Jahr"].Index].Value=song.Year;row.Cells[v.Songs.Columns["Genre"].Index].Value=song.Genre;row.Cells[v.Songs.Columns["Dauer"].Index].Value=song.Duration;row.Cells[v.Songs.Columns["Ordner"].Index].Value=TagSchema.Value(song,"_directory");row.Cells[v.Songs.Columns["Tag"].Index].Value=song.TagType;row.Cells[v.Songs.Columns["Bitrate"].Index].Value=song.Bitrate>0?song.Bitrate.ToString():"";row.Cells[v.Songs.Columns["Dateipfad"].Index].Value=TagSchema.Value(song,"_path");row.Cells[v.Songs.Columns["Kommentar"].Index].Value=song.Comment;if(v.Songs.Columns.Contains("AcoustidId"))row.Cells[v.Songs.Columns["AcoustidId"].Index].Value=song.AcoustidId;rows.Add(row);
     }
     v.Songs.Rows.AddRange(rows.ToArray());foreach(var row in rows)row.Selected=selected.Contains((Song)row.Tag);await Task.Delay(1,token);
    }
    TrackColumns.Load(v.Songs);if(selected.Count==0)v.Songs.ClearSelection();titleViewState.Restore(v.Songs,EditedPath);SyncSideTags(false);v.Counts.Text=entries.Length.ToString("N0")+" angezeigte Titel · "+p.Folders.Count+" Ordner · Gesamte Bibliothek";
   }finally{if(libraryRenderGeneration==generation){libraryRenderGeneration=-1;refresh=before;updatingLibrary=false;v.EndLibraryUpdate();}}
  }catch(OperationCanceledException){}catch(Exception ex){if(generation==librarySearchGeneration&&!v.IsDisposed)v.Status.Text="Suche: "+ex.Message;}finally{if(librarySearchCancel==source)librarySearchCancel=null;source.Dispose();}
 }
}}
