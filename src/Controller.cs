using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
namespace Musikbox {
 public partial class Controller {
  readonly ChromeSpeechBridge chromeSpeech=new ChromeSpeechBridge();readonly bool preferChromeSpeech;
  readonly Playback playback;
  readonly System.Windows.Forms.Timer playerTimer=new System.Windows.Forms.Timer{Interval=250};Song playerSong;string playerFile;
  readonly MainView v;Project p=new Project();bool refresh,dirty,busy,selectionRefreshQueued;CancellationTokenSource cancel;
  string lastImportReport="Noch kein Drag-and-drop-Import ausgeführt.";
  List<Song> sideSelection=new List<Song>();
  bool sideCommitting;
  readonly ProjectHistory history=new ProjectHistory();
  readonly System.Windows.Forms.Timer backupTimer=new System.Windows.Forms.Timer{Interval=60000},backupDelay=new System.Windows.Forms.Timer{Interval=1500};
  readonly string backupRoot;ProjectBackups backups;long backupRevision;bool backupPending,backupRunning;
  ToolStripMenuItem undoCommand,redoCommand;string projectFile;
  FolderImportMode folderImportMode=FolderImportMode.PreserveStructure;
  BoxFolder Current {get{if(v.LibraryMode=="Ordner"){var row=v.Groups.CurrentRow;var group=row!=null&&row.Selected?row.Tag as Tuple<string,string>:null;return group==null?null:p.Folders.FirstOrDefault(f=>f.Name==group.Item2);}return v.Folders.SelectedItem as BoxFolder;}}
  public Controller(MainView view,string backupRoot=null,Playback player=null,VoiceService speech=null) {
   preferChromeSpeech=false;voice=speech??new VoiceService();
   playback=player??new Playback();
   this.backupRoot=backupRoot;history.Reset(p,true);backups=new ProjectBackups(null,backupRoot);
   v=view;v.CanDrop=()=>!busy;v.PathsDropped+=ImportDropped;
   v.Button("Ordner laden",()=>Run(Import));v.Button("Neuer Ordner",()=>Run(NewFolder));
   v.Button("Ordner bearbeiten",()=>Run(EditFolder));v.Button("↑",()=>Move(-1));v.Button("↓",()=>Move(1));
   v.Button("Alle auswählen",()=>Run(()=>SelectSongs(false)));v.Button("Markierung umkehren",()=>Run(()=>SelectSongs(true)));v.Button("Markierung aufheben",()=>Run(DismissSongSelection));
   v.Button("Entfernen",()=>Run(Remove));v.Button("Titel hinzufügen",()=>Run(AddSongs));v.Button("Titel entfernen",()=>Run(RemoveSongs));v.Button("Titel löschen",()=>Run(DeleteSongsFromDisk));v.Button("Tags bearbeiten",()=>Run(EditTags));v.Button("Cover",()=>Run(SetCover));v.Button("Cover gestalten",()=>Run(DesignCover));v.Button("Cover-Serie",()=>Run(CoverSeries));v.Button("Im Player öffnen",()=>Run(OpenPlayer));v.Button("Abspielen",()=>Run(PlaySong));v.Button("Pause",()=>RunPlayback(()=>{playback.Pause();UpdatePlayer();}));v.Button("Stopp",()=>RunPlayback(StopPlayer));v.Button("Cover online suchen",()=>Run(SearchCover));v.Button("Mit Fingerabdruck erkennen",()=>Run(IdentifyFingerprints));v.Button("Fingerabdruck einrichten",()=>Run(FingerprintConfig));v.Button("Titel erkennen (ACRCloud)",()=>Run(IdentifySong));v.Button("ACRCloud-Zugang",()=>Run(RecognitionConfig));
   var importMode=new ToolStripDropDownButton("Ordnerimport: Struktur übernehmen");
   var structure=new ToolStripMenuItem("Ordnerstruktur übernehmen"){Checked=true};
   var albums=new ToolStripMenuItem("Nach Album-Tags neu ordnen");
   structure.Click+=(s,e)=>{folderImportMode=FolderImportMode.PreserveStructure;structure.Checked=true;albums.Checked=false;importMode.Text="Ordnerimport: Struktur übernehmen";};
   albums.Click+=(s,e)=>{folderImportMode=FolderImportMode.ByAlbumTags;structure.Checked=false;albums.Checked=true;importMode.Text="Ordnerimport: Nach Album-Tags";};
   importMode.ToolTipText="Gilt für hineingezogene Ordner. Einzelne Dateien werden nach Album-Tags in neue Ordner eingeordnet.";
   importMode.DropDownItems.AddRange(new ToolStripItem[]{structure,albums});v.ImportMode(importMode);
   v.Button("Importbericht",()=>v.Report("Letzter Importbericht – mit Strg+A, Strg+C kopieren",lastImportReport));

   v.Button("Einstellungen öffnen",()=>Run(OpenSettings));v.Button("Audio-Einstellungen",()=>Run(GainSettings));v.Button("Sortiervorschau",()=>Run(Preview));v.Button("Prüfen",()=>Run(Check));v.Button("Sammlungsübersicht",()=>Run(Overview));v.Button("Export",()=>Run(StartExport));
   undoCommand=v.Button("Rückgängig",()=>Run(()=>RestoreHistory(false)));redoCommand=v.Button("Wiederholen",()=>Run(()=>RestoreHistory(true)));
   undoCommand.ShortcutKeyDisplayString="Strg+Z";redoCommand.ShortcutKeyDisplayString="Strg+Y";v.HistoryRequested+=redo=>Run(()=>RestoreHistory(redo));
   v.Button("Projektsicherungen",()=>Run(RecoverBackup));v.Button("Dateien wiederfinden",()=>Run(RelinkSources));
   v.Button("Speichern",()=>Run(Save));v.Button("Öffnen",()=>Run(Open));v.Button("Hilfe",Help);
   v.ArrangeMenus();v.ConfigureContextMenus();v.Cancel.Click+=(s,e)=>{if(cancel!=null)cancel.Cancel();};
   v.Folders.SelectedIndexChanged+=(s,e)=>{if(refresh)return;CommitSideTags();libraryFolder=Current;groupField=groupValue=null;v.LibraryMode="Titel";v.CoverTabs.SelectedIndex=0;ShowSongs();};
   v.LayoutChanging+=()=>{v.Songs.EndEdit();CommitSideTags();};
   v.SongSelectionDismissed+=()=>Run(DismissSongSelection);
   v.Songs.SelectionChanged+=(s,e)=>{if(refresh||sideCommitting||selectionRefreshQueued)return;selectionRefreshQueued=true;v.BeginInvoke((Action)(()=>{selectionRefreshQueued=false;if(v.IsDisposed||refresh||sideCommitting)return;CommitSideTags();SyncSideTags();}));};v.TagEditor.ApplyRequested+=SaveSideTagsToFiles;
   v.Songs.CellEndEdit+=(s,e)=>{if(refresh||e.RowIndex<0)return;var song=(Song)v.Songs.Rows[e.RowIndex].Tag;string value=Convert.ToString(v.Songs.Rows[e.RowIndex].Cells[e.ColumnIndex].Value).Trim();if(e.ColumnIndex==0)song.Artist=value;else if(e.ColumnIndex==1)song.Title=value;else if(e.ColumnIndex==2&&song.Album!=value){song.Album=value;song.AlbumOverride=true;}else if(v.Songs.Columns[e.ColumnIndex].Name=="Jahr")song.Year=value;else if(v.Songs.Columns[e.ColumnIndex].Name=="Genre")song.Genre=value;else if(v.Songs.Columns[e.ColumnIndex].Name=="Kommentar")song.Comment=value;else if(v.Songs.Columns[e.ColumnIndex].Name=="Tracknummer")song.Track=value;var key=TagSchema.Fields.FirstOrDefault(f=>f.Column==v.Songs.Columns[e.ColumnIndex].Name&&f.Writable);if(key!=null){if(song.PendingTags==null)song.PendingTags=new Dictionary<string,string>();song.PendingTags[key.Key]=value;}Changed("Titel bearbeiten");UpdateStatus();SyncSideTags();};
   v.Songs.DataError+=(s,e)=>{e.ThrowException=false;};
   v.FormClosing+=(s,e)=>{CancelLibrarySearch();voice.Stop();if(busy){AppMessage.Show(v,"Bitte zuerst den laufenden Vorgang abbrechen und dessen Ende abwarten.");e.Cancel=true;}else if(!CanDiscard())e.Cancel=true;else try{FlushBackup();}catch(Exception ex){e.Cancel=AppMessage.Show(v,"Die Projektsicherung ist fehlgeschlagen:\r\n"+ex.Message+"\r\nTrotzdem schließen?","Projektsicherung",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes;}};
   v.FormClosed+=(s,e)=>{backupTimer.Dispose();backupDelay.Dispose();playerTimer.Dispose();playback.Dispose();};
   v.Player.StartRequested+=()=>RunPlayback(StartPlayer);v.Player.PauseRequested+=()=>RunPlayback(()=>{playback.Pause();UpdatePlayer();});v.Player.StopRequested+=()=>RunPlayback(StopPlayer);v.Player.SeekRequested+=fraction=>RunPlayback(()=>{playback.Seek(fraction);UpdatePlayer();});v.Player.CloseRequested+=()=>RunPlayback(()=>{ResetPlayer();v.PlayerPane.HidePane();});playerTimer.Tick+=(s,e)=>{try{var state=playback.Read();if(state.Mode==PlaybackMode.Finished){PlayFollowingSong();}else UpdatePlayer();}catch(Exception ex){PlayerError(ex.Message);}};
   backupTimer.Tick+=(s,e)=>{if(dirty)backupPending=true;StartBackup();};backupDelay.Tick+=(s,e)=>{backupDelay.Stop();StartBackup();};backupTimer.Start();
   InitializeStudio();RefreshFolders(0);
  }
  void OpenSettings(){using(var d=new SettingsView(Current!=null,SelectedSong!=null))if(d.ShowDialog(v)==DialogResult.OK){switch(d.SelectedCommand){case "Audio-Einstellungen":GainSettings();break;case "Fingerabdruck einrichten":FingerprintConfig();break;case "ACRCloud-Zugang":RecognitionConfig();break;case "Cover gestalten":DesignCover();break;case "Cover-Serie":CoverSeries();break;case "Cover online suchen":SearchCover();break;case "Projektsicherungen":RecoverBackup();break;case "Dateien wiederfinden":RelinkSources();break;}}}
  void Changed(string label,bool force=false){searchIndex=null;CancelLibrarySearch();if(history.Record(p,label,force)){dirty=history.IsDirty;QueueBackup();}UpdateHistoryCommands();}
  void UpdateHistoryCommands(){if(undoCommand==null)return;undoCommand.Enabled=!busy&&history.CanUndo;redoCommand.Enabled=!busy&&history.CanRedo;undoCommand.ToolTipText=history.CanUndo?history.UndoLabel:"Keine Projektänderung rückgängig zu machen";redoCommand.ToolTipText=history.CanRedo?history.RedoLabel:"Keine Projektänderung zu wiederholen";}
  void SelectSongs(bool invert){
   var rows=v.Songs.Rows.Cast<DataGridViewRow>().Where(row=>!invert||!row.Selected).ToList();
   refresh=true;try{v.Songs.CurrentCell=rows.Count==0?null:rows[0].Cells[0];v.Songs.ClearSelection();foreach(var row in rows)row.Selected=true;}finally{refresh=false;}SyncSideTags();
  }
  void DismissSongSelection(){v.Songs.EndEdit();CommitSideTags();refresh=true;try{v.Songs.ClearSelection();v.Songs.CurrentCell=null;}finally{refresh=false;}SyncSideTags();v.ActiveControl=null;}
  void ClearSideSelection(){artworkGeneration++;editedSong=null;artworkSong=null;artworkSource=null;reloadArtwork=true;titleViewState=new LibraryViewState();groupViewStates.Clear();renderedGroupMode=null;v.SetCurrentSong(null,null);sideSelection.Clear();v.TagEditor.ShowSelection(new List<Song>(),"");}
  void RestoreHistory(bool redo){int index=v.Folders.SelectedIndex;string label=redo?history.RedoLabel:history.UndoLabel;if(label.StartsWith("Dateiaktion:")){FileTagAction action;if(!fileActions.TryGetValue(label.Substring(12),out action))throw new Exception("Datei-Rücknahme nicht mehr verfügbar.");action.Restore(redo);}var restored=redo?history.Redo():history.Undo();if(restored==null)return;ResetPlayer();ClearSideSelection();p=restored;dirty=history.IsDirty;RefreshFolders(index);QueueBackup();v.Status.Text=(redo?"Wiederholt":"Rückgängig gemacht")+" · "+(dirty?"Projekt bitte speichern":"Gespeicherter Stand erreicht");}
  void ReplaceProject(Project project,string file,bool saved){ResetPlayer();ClearSideSelection();p=project;PortablePaths.Configure(p);libraryFolder=null;groupField=groupValue=null;projectFile=file;history.Reset(p,saved);dirty=!saved;backups=new ProjectBackups(file,backupRoot);RefreshFolders(0);v.BackupStatus.ForeColor=Color.DimGray;v.BackupStatus.Text="Automatische Projektsicherung aktiv · zuletzt übernommene Änderungen werden gesichert";if(dirty)QueueBackup();}
  void QueueBackup(){backupPending=true;if(v.IsDisposed)return;backupDelay.Stop();backupDelay.Start();}
  async void StartBackup(){if(!backupPending||busy||backupRunning||v.IsDisposed)return;backupRunning=true;bool failed=false;ProjectBackups store=backups;
   try{byte[] snapshot=Storage.Bytes(p);long revision=++backupRevision;backupPending=false;string file=await Task.Run(()=>store.Save(snapshot,revision));if(!v.IsDisposed&&store==backups&&file!=null){v.BackupStatus.ForeColor=Color.DimGray;v.BackupStatus.Text="Projekt automatisch gesichert: "+DateTime.Now.ToString("HH:mm:ss")+" · Wiederherstellung unter Projekt → Projektsicherungen";}}
   catch(Exception ex){failed=true;if(!v.IsDisposed&&store==backups){backupPending=true;v.BackupStatus.ForeColor=Color.Firebrick;v.BackupStatus.Text="Projektsicherung fehlgeschlagen: "+ex.Message;}}
   finally{backupRunning=false;if(backupPending&&!v.IsDisposed&&(!failed||store!=backups))QueueBackup();}
  }
  void FlushBackup(){if(!backupPending&&!dirty)return;backups.Save(Storage.Bytes(p),++backupRevision);backupPending=false;}
  void RecoverBackup(){using(var d=new BackupRecoveryView(ProjectBackups.List(backupRoot)))if(d.ShowDialog(v)==DialogResult.OK){var loaded=Storage.Load(d.SelectedFile);if(!CanDiscard())return;FlushBackup();ReplaceProject(loaded,null,false);v.Status.Text="Sicherung wiederhergestellt · bitte als Projekt speichern";}}
  async void RelinkSources(){if(busy)return;List<string> roots=null;cancel=new CancellationTokenSource();var scanToken=cancel.Token;SetBusy(true);v.Status.Text="Dateiverweise werden im Hintergrund geprueft ...";
   try{roots=await Task.Run(()=>SourceRelinking.SuggestedRoots(p,()=>scanToken.ThrowIfCancellationRequested()));}
   catch(OperationCanceledException){return;}catch(Exception ex){AppMessage.Show(v,ex.Message,"Dateien wiederfinden");return;}
   finally{cancel.Dispose();cancel=null;SetBusy(false);}
   if(v.IsDisposed)return;string oldRoot,newRoot;using(var d=new RelinkLocationView(roots)){if(d.ShowDialog(v)!=DialogResult.OK)return;oldRoot=d.OldRoot.Text;newRoot=d.NewRoot.Text;}
   cancel=new CancellationTokenSource();var token=cancel.Token;SetBusy(true);RelinkPlan plan=null;v.Status.Text="Fehlende Dateiverweise werden am neuen Speicherort geprüft …";
   try{plan=await Task.Run(()=>SourceRelinking.Plan(p,oldRoot,newRoot,()=>token.ThrowIfCancellationRequested()));}catch(OperationCanceledException){v.Status.Text="Dateisuche abgebrochen · keine Zuordnungen geändert";}catch(Exception ex){AppMessage.Show(v,ex.Message,"Dateien wiederfinden");}finally{cancel.Dispose();cancel=null;SetBusy(false);}
   if(plan==null)return;using(var d=new RelinkReviewView(plan))if(d.ShowDialog(v)==DialogResult.OK){try{var candidate=SourceRelinking.Apply(p,plan);int index=v.Folders.SelectedIndex;ResetPlayer();ClearSideSelection();p=candidate;Changed("Dateien wiederverbinden");RefreshFolders(index);}catch(Exception ex){AppMessage.Show(v,ex.Message,"Zuordnung nicht übernommen");}}
  }
  void Run(Action action){if(busy)return;try{v.Songs.EndEdit();CommitSideTags();action();}catch(Exception e){AppMessage.Show(v,e.Message,"Vorgang nicht abgeschlossen",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
  int artworkGeneration;Song artworkSong;string artworkSource;bool reloadArtwork=true;
  void InvalidateArtwork(){reloadArtwork=true;artworkGeneration++;}
  async void SyncSideTags(bool captureSelection=true){
   if(captureSelection&&v.LibraryMode=="Titel"&&!refresh){var selected=SelectedSongs();if(selected.Count>0){sideSelection=selected;var row=v.Songs.CurrentRow;editedSong=row!=null&&row.Selected&&row.Tag is Song?(Song)row.Tag:selected[0];}}
   sideSelection=sideSelection.Where(song=>AllSongs.Contains(song)).ToList();if(editedSong!=null&&!AllSongs.Contains(editedSong))editedSong=AllSongs.FirstOrDefault(song=>String.Equals(song.Source,editedSong.Source,StringComparison.OrdinalIgnoreCase));if(editedSong==null)editedSong=sideSelection.FirstOrDefault();
   int generation=++artworkGeneration;var song=editedSong;v.TagEditor.ShowSelection(sideSelection,Current==null?"":Current.Theme);bool same=song!=null&&artworkSong==song&&artworkSource==song.Source;if(same&&!reloadArtwork&&v.TitleCover.Image!=null){v.SetCurrentSong(song,v.TitleCover.Image);return;}v.SetCurrentSong(song,same?v.TitleCover.Image:null);if(!same&&v.TitleCover.Image!=null){v.TitleCover.Image.Dispose();v.TitleCover.Image=null;}artworkSong=song;artworkSource=song?.Source;
   if(song==null)return;string artwork=song.Artwork,source=song.Source;
   try{await Task.Delay(100);if(generation!=artworkGeneration||v.IsDisposed)return;byte[] bytes=await Task.Run(()=>{if(!String.IsNullOrEmpty(artwork)&&File.Exists(artwork))return File.ReadAllBytes(artwork);return File.Exists(source)?MediaMetadata.Artwork(source):null;});if(generation!=artworkGeneration||v.IsDisposed||editedSong!=song||song.Source!=source)return;reloadArtwork=false;if(bytes==null){var previous=v.TitleCover.Image;v.TitleCover.Image=null;v.SetCurrentSong(song,null);previous?.Dispose();return;}var old=v.TitleCover.Image;using(var stream=new MemoryStream(bytes))using(var image=Image.FromStream(stream))v.TitleCover.Image=new Bitmap(image);v.SetCurrentSong(song,v.TitleCover.Image);old?.Dispose();}catch{if(generation==artworkGeneration&&!v.IsDisposed){reloadArtwork=true;v.SetCurrentSong(song,null);var failed=v.TitleCover.Image;v.TitleCover.Image=null;failed?.Dispose();}}
  }

  void CommitSideTags(){if(sideCommitting||!v.TagEditor.Pending||sideSelection.Count==0)return;sideCommitting=true;try{
   bool artist=v.TagEditor.ChangeArtist.Checked,title=v.TagEditor.ChangeTitle.Checked,album=v.TagEditor.ChangeAlbum.Checked;
   if((artist&&String.IsNullOrWhiteSpace(v.TagEditor.Artist.Text))||(title&&String.IsNullOrWhiteSpace(v.TagEditor.Title.Text))||(album&&String.IsNullOrWhiteSpace(v.TagEditor.Album.Text))){if(AppMessage.Show(v,"Ein angehaktes Feld ist leer. Dieses Feld bei den zuvor ausgewählten Titeln leeren?","Tags leeren",MessageBoxButtons.YesNo)!=DialogResult.Yes){v.TagEditor.ShowSelection(sideSelection,sideSelection[0].Album);return;}}
   string[] keys={"artist","title","album","year","genre","comment","track","acoustid_id"};
   var flags=new[]{v.TagEditor.ChangeArtist,v.TagEditor.ChangeTitle,v.TagEditor.ChangeAlbum,v.TagEditor.ChangeYear,v.TagEditor.ChangeGenre,v.TagEditor.ChangeComment,v.TagEditor.ChangeTrack,v.TagEditor.ChangeAcoustid};
   var inputs=new[]{v.TagEditor.Artist,v.TagEditor.Title,v.TagEditor.Album,v.TagEditor.Year,v.TagEditor.Genre,v.TagEditor.Comment,v.TagEditor.Track,v.TagEditor.Acoustid};
   foreach(var song in sideSelection){if(song.PendingTags==null)song.PendingTags=new Dictionary<string,string>();for(int i=0;i<keys.Length;i++)if(flags[i].Checked)song.PendingTags[keys[i]]=inputs[i].Text;}
   bool extra=false;foreach(var song in sideSelection){if(v.TagEditor.ChangeTrack.Checked&&song.Track!=v.TagEditor.Track.Text){song.Track=v.TagEditor.Track.Text;extra=true;}if(v.TagEditor.ChangeAcoustid.Checked&&song.AcoustidId!=v.TagEditor.Acoustid.Text){song.AcoustidId=v.TagEditor.Acoustid.Text;extra=true;}if(v.TagEditor.ChangeYear.Checked&&song.Year!=v.TagEditor.Year.Text){song.Year=v.TagEditor.Year.Text;extra=true;}if(v.TagEditor.ChangeGenre.Checked&&song.Genre!=v.TagEditor.Genre.Text){song.Genre=v.TagEditor.Genre.Text;extra=true;}if(v.TagEditor.ChangeComment.Checked&&song.Comment!=v.TagEditor.Comment.Text){song.Comment=v.TagEditor.Comment.Text;extra=true;}}
   if(TagEditing.Apply(sideSelection,artist,v.TagEditor.Artist.Text,title,v.TagEditor.Title.Text,album,v.TagEditor.Album.Text)>0||extra){Changed("Tags bearbeiten");foreach(DataGridViewRow row in v.Songs.Rows){var song=row.Tag as Song;if(song!=null){row.Cells[0].Value=song.Artist;row.Cells[1].Value=song.Title;row.Cells[2].Value=song.Album;row.Cells["Jahr"].Value=song.Year;row.Cells["Genre"].Value=song.Genre;row.Cells["Kommentar"].Value=song.Comment;}}UpdateStatus();}v.SetCurrentSong(editedSong,v.TitleCover.Image);v.TagEditor.ShowSelection(sideSelection,Current==null?"":Current.Theme);
  }finally{sideCommitting=false;}}
  bool CanDiscard(){CommitSideTags();return true;}
  void UpdateCounts(){if(importCounter==null)v.SetLibraryCounters(LibraryCounter.FromSongs(AllSongs));v.Counts.Text="Ordner: "+p.Folders.Count.ToString("N0")+" · Titel: "+p.Folders.Sum(f=>f.Songs.Count).ToString("N0");}
  void ReadProgress(int folders,int songs){v.BeginInvoke((Action)(()=>{if(!busy)return;v.Status.Text="Dateien und Tags werden eingelesen …";}));}
  void AudioProgress(int done,int total,string text){v.BeginInvoke((Action)(()=>{if(!busy)return;v.Progress.Value=total==0?0:done*100/total;v.Status.Text=text;}));}
  void UpdateStatus(){UpdateHistoryCommands();UpdateCounts();v.Status.Text=p.Folders.Count+"/99 Ordner · "+p.Folders.Sum(f=>f.Songs.Count)+" Titel"+(dirty?" · nicht gespeichert":"")+(p.Normalize?" · MP3Gain: "+p.TargetDb+" dB":p.FfmpegLoudness?" · FFmpeg: "+p.TargetLufs+" LUFS":" · Lautstärke unverändert")+" · Dateiänderungen werden mit Speichern übernommen";}
  void RefreshFolders(int index){refresh=true;try{v.Folders.BeginUpdate();v.Folders.Items.Clear();foreach(var f in p.Folders)v.Folders.Items.Add(f);v.Folders.EndUpdate();if(p.Folders.Count>0)v.Folders.SelectedIndex=Math.Max(0,Math.Min(index,p.Folders.Count-1));}finally{refresh=false;}if(libraryFolder!=null)libraryFolder=Current;ShowSongs();UpdateStatus();}
  void ShowFolderArtwork(BoxFolder folder){
   var previous=v.Cover.Image;v.Cover.Image=null;previous?.Dispose();
   try{string path=folder.Cover;
   if(!File.Exists(path)&&Directory.Exists(folder.SourceDirectory))path=FolderArtwork.Find(folder.SourceDirectory);
   if(!File.Exists(path)){v.CoverMessage("Kein Ordnercover gefunden. Rechtsklick → Cover auswählen…",true);return;}
   using(var image=Image.FromFile(path))v.Cover.Image=new Bitmap(image);v.CoverMessage(Path.GetFileName(path),false);}
   catch{v.CoverMessage("Ordnercover nicht lesbar. Rechtsklick → Cover auswählen…",true);}
  }
  void ShowSongs(){if(v.Cover.Image!=null){v.Cover.Image.Dispose();v.Cover.Image=null;}if(Current!=null)ShowFolderArtwork(Current);PopulateLibrary();}
  void ImportDropped(string[] paths,int targetIndex){ImportDropped(paths,targetIndex,false,false);}
  async void ImportDropped(string[] paths,int targetIndex,bool keepModule,bool prepareAudio=false){
   if(busy)return;v.Songs.EndEdit();cancel=new CancellationTokenSource();var token=cancel.Token;SetBusy(true);var counter=BeginImportCounters();DropResult completed=null;
   try {
    completed=await Task.Run(()=>{var read=DropImport.Read(p,paths,targetIndex,()=>token.ThrowIfCancellationRequested(),folderImportMode,ReadProgress,true,song=>counter.Add(song));ReadProgress(read.FoldersAdded,read.FilesAdded);
     if(prepareAudio&&read.FilesAdded>0)ImportAudio.Prepare(read.Project,read.Project.Folders.SelectMany(f=>f.Songs).Where(s=>!AllSongs.Any(old=>String.Equals(old.Source,s.Source,StringComparison.OrdinalIgnoreCase))),()=>token.ThrowIfCancellationRequested(),AudioProgress);return read;});

    token.ThrowIfCancellationRequested();lastImportReport=completed.Report;HashSet<string> addedPaths=new HashSet<string>(completed.Project.Folders.SelectMany(f=>f.Songs).Select(song=>Path.GetFullPath(song.Source)),StringComparer.OrdinalIgnoreCase);completed.FilesAdded=addedPaths.Count;recentImportPaths=addedPaths;
    if(completed.FilesAdded>0){voice.Stop();ResetPlayer();ClearSideSelection();gainInputs.Clear();conversionInputs.Clear();libraryFolder=null;groupField=groupValue=null;v.LibraryMode="Titel";p=completed.Project;if(!keepModule){v.SearchBox.Clear();v.SelectStudioPage("Bibliothek");}Changed("Titel und Ordner durch neuen Import ersetzen");RefreshFolders(completed.SelectedIndex);}
    if(completed.Warnings.Count>0)v.Report("Hinweise zum Musikimport",String.Join("\r\n",completed.Warnings));else if(completed.FilesAdded==0)v.Status.Text="Bibliothek unverändert.";
   }catch(DropImportException e){lastImportReport=e.Report;v.Report("Import nicht abgeschlossen – Bericht mit Strg+A, Strg+C kopieren",lastImportReport);}catch(OperationCanceledException){lastImportReport="Import abgebrochen. Es wurde nichts übernommen.";AppMessage.Show(v,lastImportReport);}catch(Exception e){AppMessage.Show(v,e.Message,"Musikimport nicht abgeschlossen");}finally{EndImportCounters();cancel.Dispose();cancel=null;SetBusy(false);if(completed!=null)v.Status.Text+=" · Letzter Import: "+completed.FilesAdded+" Titel, "+completed.FoldersAdded+" Ordner, "+completed.Skipped+" doppelte Auswahlen übersprungen";}
  }
  void Import(){
   string root;using(var d=new AppFolderDialog{StartAtDrives=true,Description="Musikordner oder übergeordneten Sammlungsordner wählen"}){if(d.ShowDialog(v)!=DialogResult.OK)return;root=d.SelectedPath;}
   ImportDropped(new[]{root},-1);
  }
  void NewFolder(){if(p.Folders.Count>=99)throw new Exception("Bereits 99 Ordner vorhanden.");string name=v.Ask("Neuer Ordner – z. B. 033 Country von damals bis heute","");if(name==null)return;if(String.IsNullOrWhiteSpace(name)||Rules.SafeName(name)!=name)throw new Exception("Bitte gültigen Windows-Ordnernamen eingeben.");if(p.Folders.Any(f=>f.Name.Equals(name,StringComparison.OrdinalIgnoreCase)))throw new Exception("Ordnername bereits vorhanden.");var added=new BoxFolder{Name=name,Theme=Rules.Theme(name),Group=""};p.Folders.Add(added);p.Folders.Sort((a,b)=>StringComparer.OrdinalIgnoreCase.Compare(a.Name,b.Name));Changed("Ordner anlegen");RefreshFolders(p.Folders.IndexOf(added));}
  void EditFolder(){var f=Current;if(f==null)return;string name=v.Ask("Ordnername mit Nummer",f.Name);if(name==null)return;string theme=v.Ask("Themenname / Standardalbum ohne Nummer",f.Theme);if(theme==null)return;string group=v.Ask("Gemeinsame Sortiergruppe – leer = einzelner Ordner",f.Group);if(group==null)return;
   if(String.IsNullOrWhiteSpace(name)||Rules.SafeName(name)!=name||String.IsNullOrWhiteSpace(theme))throw new Exception("Ordnername oder Thema ungültig.");if(p.Folders.Any(x=>x!=f&&x.Name.Equals(name,StringComparison.OrdinalIgnoreCase)))throw new Exception("Ordnername bereits vorhanden.");f.Name=name;f.Theme=theme.Trim();foreach(var song in f.Songs)if(!song.AlbumOverride)song.Album=f.Theme;f.Group=group.Trim();p.Folders.Sort((a,b)=>StringComparer.OrdinalIgnoreCase.Compare(a.Name,b.Name));Changed("Ordner bearbeiten");RefreshFolders(p.Folders.IndexOf(f));}
  void Move(int delta){Run(()=>{int i=v.Folders.SelectedIndex,j=i+delta;if(i<0||j<0||j>=p.Folders.Count)return;Rules.MoveFolder(p,i,j);Changed("Ordner verschieben");RefreshFolders(j);});}
  void Remove(){var f=Current;if(f==null)return;if(AppMessage.Show(v,"Ordner aus dem Projekt entfernen? Die Originaldateien bleiben erhalten.","Entfernen",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;int i=v.Folders.SelectedIndex;p.Folders.Remove(f);Changed("Ordner entfernen");RefreshFolders(i);}
  void SetCover(){if(Current==null)return;using(var d=new AppOpenFileDialog{Filter="Bilder|*.jpg;*.jpeg;*.png"})if(d.ShowDialog(v)==DialogResult.OK){using(var img=Image.FromFile(d.FileName)){}Current.Cover=d.FileName;Changed("Ordnercover auswählen");ShowSongs();}}
  Song SelectedSong {get{return v.LibraryMode=="Titel"&&v.Songs.CurrentRow!=null&&v.Songs.CurrentRow.Selected?v.Songs.CurrentRow.Tag as Song:editedSong;}}
  void DesignCover(){var folder=Current;if(folder==null)return;using(var d=new CoverDesignerView(folder.CoverDesign??CoverDesign.Default(),folder.Name,folder.Theme))if(d.ShowDialog(v)==DialogResult.OK){string image=CoverArt.Save(d.Design,folder.Name,folder.Theme);folder.CoverDesign=d.Design;folder.Cover=image;Changed("Cover gestalten");ShowSongs();UpdateStatus();}}
  async void CoverSeries(){var source=Current;if(source==null)return;if(source.CoverDesign==null){AppMessage.Show(v,"Bitte zuerst ein Cover gestalten. Danach kann die Vorlage auf weitere Ordner angewendet werden.");return;}
   var list=new StudioCheckedListBox{Dock=DockStyle.Fill,CheckOnClick=true};foreach(var f in p.Folders)list.Items.Add(f,false);using(var dialog=new ModernForm{Text="Cover-Vorlage auf Themenordner anwenden",Width=550,Height=500,StartPosition=FormStartPosition.CenterParent}){
    var ok=new Button{Text="Cover-Serie erstellen",Dock=DockStyle.Bottom,Height=38,DialogResult=DialogResult.OK};dialog.Controls.Add(list);dialog.Controls.Add(ok);if(dialog.ShowDialog(v)!=DialogResult.OK)return;var folders=list.CheckedItems.Cast<BoxFolder>().ToList();if(folders.Count==0)return;
    cancel=new CancellationTokenSource();var token=cancel.Token;SetBusy(true);try{var images=await Task.Run(()=>{var result=new List<Tuple<BoxFolder,string>>();try{for(int i=0;i<folders.Count;i++){token.ThrowIfCancellationRequested();var f=folders[i];int done=i;v.BeginInvoke((Action)(()=>{v.Progress.Value=done*100/folders.Count;v.Status.Text="Cover-Serie: "+(done+1)+" / "+folders.Count+" Ordnerbilder";}));result.Add(Tuple.Create(f,CoverArt.Save(source.CoverDesign,f.Name,f.Theme)));}token.ThrowIfCancellationRequested();return result;}catch{foreach(var image in result)try{File.Delete(image.Item2);}catch{}throw;}});foreach(var image in images){image.Item1.Cover=image.Item2;image.Item1.CoverDesign=CoverArt.Copy(source.CoverDesign);}Changed("Cover-Serie");ShowSongs();}catch(OperationCanceledException){AppMessage.Show(v,"Cover-Serie abgebrochen. Es wurde nichts übernommen.");}catch(Exception ex){AppMessage.Show(v,ex.Message);}finally{cancel.Dispose();cancel=null;SetBusy(false);}}
  }
  string PlayerCaption {get{return playerSong==null?"":String.IsNullOrWhiteSpace(playerSong.Artist)&&String.IsNullOrWhiteSpace(playerSong.Title)?Path.GetFileName(playerFile):playerSong.Artist+" – "+playerSong.Title;}}
  string PlaybackFile(Song song){if(MediaProbe57.Extensions.Contains(Path.GetExtension(song.Source).ToLowerInvariant())){string cache=Path.Combine(PortablePaths.Data,"Player-Audio");Directory.CreateDirectory(cache);string target=Path.Combine(cache,Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(song.Source)+"|"+File.GetLastWriteTimeUtc(song.Source).Ticks+"|"+new FileInfo(song.Source).Length)))+".wav");if(!File.Exists(target)){string error;Ffmpeg.Run(p.FfmpegExecutable,"-hide_banner -nostdin -v error -n -i "+Ffmpeg.Quote(Path.GetFullPath(song.Source))+" -map 0:a:0 -vn -c:a pcm_s16le "+Ffmpeg.Quote(target),()=>{},out error);}return target;}string file=ImportAudio.Ready(p,song)?song.PreparedSource:song.Source;if(String.IsNullOrEmpty(file)||!File.Exists(file))throw new Exception("Die Audiodatei fehlt. Bitte über Projekt → Dateien wiederfinden neu verbinden.");return Path.GetFullPath(file);}
  void LoadPlayer(Song song){string file=PlaybackFile(song);if(playerSong!=song||!String.Equals(playerFile,file,StringComparison.OrdinalIgnoreCase)){playback.Clear();playerSong=song;playerFile=file;RefreshPlayerArtwork();}v.PlayerPane.ShowPane();UpdatePlayer();}
  void OpenPlayer(){var song=SelectedSong;if(song==null)return;LoadPlayer(song);}
  void PlaySong(){var song=SelectedSong;if(song==null)return;LoadPlayer(song);StartPlayer();}
  void StartPlayer(){if(playerSong==null){if(busy)return;OpenPlayer();}if(playerSong==null)return;var before=playback.Read();bool fresh=before.Mode!=PlaybackMode.Playing&&before.Mode!=PlaybackMode.Paused;playback.Play(playerFile);playerTimer.Start();UpdatePlayer();if(fresh&&!busy&&v.OnlineCover.Checked&&p.Folders.Any(f=>f.Songs.Contains(playerSong)))ChooseTitleCover(playerSong);}
  void PlayFollowingSong(){
   var rows=v.Songs.Rows.Cast<DataGridViewRow>().Where(row=>!row.IsNewRow&&row.Visible&&row.Tag is Song).ToList();
   var order=playlistOrder!=null&&playlistOrder.Count>0?playlistOrder:rows.Select(row=>(Song)row.Tag).ToList();
   int index=order.IndexOf(playerSong);Song next=index>=0&&index+1<order.Count?order[index+1]:null;
   if(next==null){playerTimer.Stop();UpdatePlayer();return;}
   var nextRow=rows.FirstOrDefault(row=>Object.ReferenceEquals(row.Tag,next));
   if(nextRow!=null){v.Songs.ClearSelection();var cell=nextRow.Cells.Cast<DataGridViewCell>().FirstOrDefault(c=>c.Visible);if(cell!=null)v.Songs.CurrentCell=cell;nextRow.Selected=true;if(!nextRow.Frozen)v.Songs.FirstDisplayedScrollingRowIndex=nextRow.Index;}
   LoadPlayer(next);StartPlayer();
  }

  void StopPlayer(){playback.Stop();playerTimer.Stop();UpdatePlayer();}
  void ResetPlayer(){string error=null;try{playback.Clear();}catch(Exception ex){error=ex.Message;}playerTimer.Stop();playerSong=null;playerFile=null;RefreshPlayerArtwork();UpdatePlayer();if(error!=null)v.Player.ShowError(error);}
  void UpdatePlayer(){v.Player.ShowState(playback.Read(),PlayerCaption,playerSong!=null);}
  void PlayerError(string message){try{playback.Clear();}catch{}playerTimer.Stop();UpdatePlayer();v.Player.ShowError(message);}
  void RunPlayback(Action action){try{action();}catch(Exception ex){PlayerError(ex.Message);}}
  void SearchCover(){var song=v.LibraryMode=="Ordner"?SelectedSongs().FirstOrDefault():SelectedSong;if(song==null)return;ChooseTitleCover(song);}
  void ChooseTitleCover(Song song){var folder=p.Folders.FirstOrDefault(f=>f.Songs.Contains(song));if(folder==null)return;using(var d=new TitleCoverChoiceView(song,folder.Cover))if(d.ShowDialog(v)==DialogResult.OK){var selected=SelectedSongs();if(!selected.Contains(song))selected=new List<Song>{song};SaveFileChanges(EmbeddedArtwork.Plan(selected,d.SelectedArtwork,d.AppendArtwork),"Titelcover");}}
  void FingerprintConfig(){AcoustIdSettings settings;try{settings=AcoustIdSettings.Load();}catch{settings=new AcoustIdSettings();}using(var d=new FingerprintSettingsView(settings))d.ShowDialog(v);}
  async void IdentifyFingerprints(){
   var songs=v.Songs.SelectedRows.Cast<DataGridViewRow>().Select(row=>row.Tag as Song).Where(item=>item!=null).ToList();
   if(songs.Count!=1){AppMessage.Show(v,"Bitte genau einen unbekannten Titel zur Erkennung auswählen.");return;}var song=songs[0];
   AcoustIdSettings settings;try{settings=AcoustIdSettings.Load();settings.Validate();}catch{FingerprintConfig();try{settings=AcoustIdSettings.Load();settings.Validate();}catch{return;}}
   if(AppMessage.Show(v,"Diesen Titel anhand seines Klangs erkennen?\r\nFingerabdruck und Dauer werden an AcoustID übertragen, keine Musikdatei.\r\nDen Vorschlag prüfst du anschließend vor der Übernahme.\r\nDer öffentliche Dienst ist für nichtkommerzielle Anwendungen vorgesehen.","Fingerabdruck-Erkennung",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
   cancel=new CancellationTokenSource();var token=cancel.Token;SetBusy(true);FingerprintResult result=null;v.Status.Text="Fingerabdruck wird berechnet und abgefragt: "+Path.GetFileName(song.Source);
   try{result=await Task.Run(()=>new FingerprintResult{Song=song,Matches=AcoustId.Identify(song,settings,token)});}
   catch(OperationCanceledException){AppMessage.Show(v,"Erkennung abgebrochen. Es wurden keine Tags übernommen.");}
   catch(Exception ex){AppMessage.Show(v,ex.Message,"Fingerabdruck-Erkennung");}
   finally{cancel.Dispose();cancel=null;SetBusy(false);}
   if(result==null)return;using(var d=new FingerprintReviewView(result))if(d.ShowDialog(v)==DialogResult.OK){var match=d.SelectedMatch;if(match!=null){if(TagEditing.Apply(new[]{song},true,match.Artist,true,match.Title)>0){Changed("Erkennung übernehmen");ShowSongs();UpdateStatus();v.Status.Text="Interpret und Titel übernommen · Projekt bitte speichern";}ChooseTitleCover(song);}}
  }

  void RecognitionConfig(){RecognitionSettings settings;try{settings=RecognitionSettings.Load();}catch{settings=new RecognitionSettings();}
   using(var d=new ModernForm{Text="Optionale Musikerkennung – eigener ACRCloud-Zugang",Width=650,Height=370,StartPosition=FormStartPosition.CenterParent}){var host=new StudioTextBox{Left=170,Top=20,Width=420,Text=settings.Host};var key=new StudioTextBox{Left=170,Top=65,Width=420,Text=settings.Key??"",UseSystemPasswordChar=true};var secret=new StudioTextBox{Left=170,Top=110,Width=420,Text=settings.Secret??"",UseSystemPasswordChar=true};
    d.Controls.AddRange(new Control[]{new Label{Text="Projekt-Host",Left=15,Top=23,Width=150},host,new Label{Text="Access Key",Left=15,Top=68,Width=150},key,new Label{Text="Access Secret",Left=15,Top=113,Width=150},secret,new Label{Left=15,Top=155,Width=590,Height=90,Text="Optional: eigener ACRCloud-Account und Projektzugang erforderlich.\r\nTarif und mögliche Kosten beim Anbieter prüfen. Bei 'Titel erkennen (ACRCloud)' wird\r\neine 12-Sekunden-Audioprobe übertragen. Schlüssel werden verschlüsselt\r\nfür dein Windows-Benutzerkonto gespeichert, nicht im Musikbox-Projekt."}});
    var save=new Button{Text="Speichern",Left=345,Top=260,Width=120};var close=new Button{Text="Abbrechen",Left=475,Top=260,Width=120,DialogResult=DialogResult.Cancel};save.Click+=(s,e)=>{try{new RecognitionSettings{Host=host.Text.Trim(),Key=key.Text.Trim(),Secret=secret.Text.Trim()}.Save();d.DialogResult=DialogResult.OK;}catch(Exception ex){AppMessage.Show(d,ex.Message);}};d.Controls.Add(save);d.Controls.Add(close);d.ShowDialog(v);
   }
  }
  async void IdentifySong(){var song=SelectedSong;if(song==null)return;RecognitionSettings settings;try{settings=RecognitionSettings.Load();settings.Validate();}catch{RecognitionConfig();return;}
   if(AppMessage.Show(v,"Eine 12-Sekunden-Probe dieses Titels an ACRCloud senden? Der Dienst kann nach deinem Anbietertarif kostenpflichtig sein.","Titel erkennen",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
   cancel=new CancellationTokenSource();var token=cancel.Token;SetBusy(true);RecognizedSong result=null;try{result=await Task.Run(()=>Recognition.Identify(song,p,settings,token));}catch(OperationCanceledException){AppMessage.Show(v,"Erkennung abgebrochen.");}catch(Exception ex){AppMessage.Show(v,ex.Message,"Erkennung");}finally{cancel.Dispose();cancel=null;SetBusy(false);}if(result==null)return;
   if(AppMessage.Show(v,"Erkannt: "+result.Artist+" – "+result.Title+"\r\nOriginalalbum: "+result.Album+"\r\nTrefferwert: "+result.Score+"\r\nInterpret und Titel ins Projekt übernehmen? Das Themenalbum bleibt erhalten.","Erkennungsvorschlag",MessageBoxButtons.YesNo)==DialogResult.Yes){TagEditing.Apply(new[]{song},true,result.Artist,true,result.Title);Changed("ACRCloud-Vorschlag übernehmen");ShowSongs();UpdateStatus();ChooseTitleCover(song);}
  }
  async void AddSongs(){var f=Current;if(f==null){AppMessage.Show(v,"Bitte zuerst einen Themenordner auswählen.");return;}string[] files;
   using(var d=new AppOpenFileDialog{StartAtDrives=true,Filter="Audiodateien|*.mp3;*.flac;*.wav;*.m4a;*.aac;*.ogg;*.opus;*.wma;*.aif;*.aiff",Multiselect=true}){if(d.ShowDialog(v)!=DialogResult.OK)return;files=d.FileNames;}
   int selected=v.Folders.SelectedIndex;var warnings=new List<string>();var additions=new List<Song>();
   var existing=new HashSet<string>(p.Folders.Where(x=>x==f||(!String.IsNullOrWhiteSpace(f.Group)&&String.Equals(x.Group,f.Group,StringComparison.OrdinalIgnoreCase))).SelectMany(x=>x.Songs).Select(x=>x.Source),StringComparer.OrdinalIgnoreCase);
   cancel=new CancellationTokenSource();var token=cancel.Token;SetBusy(true);
   try {
    await Task.Run(()=>{foreach(string file in files){token.ThrowIfCancellationRequested();if(!existing.Add(file))continue;try{var added=Ffmpeg.ReadSong(file,p,()=>token.ThrowIfCancellationRequested());added.Album=f.Theme;additions.Add(added);if(additions.Count%25==0)ReadProgress(1,additions.Count);}catch(OperationCanceledException){throw;}catch(Exception e){warnings.Add(Path.GetFileName(file)+": "+e.Message);}}});
    if(additions.Count>0){var candidate=p.WithFolders(new List<BoxFolder>());foreach(var x in p.Folders)candidate.Folders.Add(new BoxFolder{Name=x.Name,Theme=x.Theme,Group=x.Group,SourceDirectory=x.SourceDirectory,Cover=x.Cover,CoverDesign=x.CoverDesign,Songs=x.Songs.Select(song=>song.Copy()).ToList()});candidate.Folders[selected].Songs.AddRange(additions);int before=p.Folders.Count;var sorted=Rules.Sorted(candidate);
    ReadProgress(1,additions.Count);await Task.Run(()=>ImportAudio.Prepare(p,additions,()=>token.ThrowIfCancellationRequested(),AudioProgress));
    var prepared=additions.ToDictionary(x=>x.Source,StringComparer.OrdinalIgnoreCase);foreach(var song in sorted.Folders.SelectMany(x=>x.Songs))if(prepared.ContainsKey(song.Source)){song.PreparedSource=prepared[song.Source].PreparedSource;song.PreparedProfile=prepared[song.Source].PreparedProfile;}
    p=sorted;Changed("Titel hinzufügen");RefreshFolders(selected);if(p.Folders.Count>before)AppMessage.Show(v,(p.Folders.Count-before)+" Folgeordner automatisch angelegt. Alle Titel wurden alphabetisch verteilt. Nachfolgende Ordnernummern wurden bei Bedarf weitergeschoben.","Thema aufgeteilt");}
    if(warnings.Count>0)v.Report("Nicht hinzugefügte Titel",String.Join("\r\n",warnings));
   }catch(OperationCanceledException){AppMessage.Show(v,"Hinzufügen abgebrochen. Es wurden keine Titel übernommen.");}catch(Exception e){AppMessage.Show(v,e.Message,"Hinzufügen nicht abgeschlossen");}finally{cancel.Dispose();cancel=null;SetBusy(false);}
  }
  async void EditTags(){
   if(Current==null)return;var songs=SelectedSongs();
   if(songs.Count==0){AppMessage.Show(v,"Bitte zuerst einen oder mehrere Titel auswählen. Mehrfachauswahl mit Strg oder Umschalt.");return;}
   SetBusy(true);try{await Task.Run(()=>{foreach(var song in songs)if(File.Exists(song.Source)){var read=MediaMetadata.Read(song.Source);song.Bitrate=read.Bitrate;song.TagType=read.TagType;song.Duration=read.Duration;}});}catch(Exception ex){AppMessage.Show(v,ex.Message,"Dateiinformationen");}finally{SetBusy(false);}
   using(var dialog=new TagEditorView(songs,Current==null?"":Current.Theme))if(dialog.ShowDialog(v)==DialogResult.OK)SaveDialogTagChanges(songs,dialog.Edits);
  }
  void DeleteSongsFromDisk(){
   var songs=SelectedSongs().Where(x=>x!=null).GroupBy(x=>x.Source,StringComparer.OrdinalIgnoreCase).Select(x=>x.First()).ToList();
   if(songs.Count==0)return;
   if(AppMessage.Show(v,songs.Count+" ausgewählte Titel endgültig vom Datenträger löschen?\r\nDiese Aktion kann nicht rückgängig gemacht werden.","Dateien endgültig löschen",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
   var deleted=new List<Song>();var errors=new List<string>();
   foreach(var song in songs){
    try{if(String.IsNullOrWhiteSpace(song.Source))throw new IOException("Dateipfad fehlt");if(File.Exists(song.Source))File.Delete(song.Source);deleted.Add(song);}
    catch(Exception ex){errors.Add(song.Source+": "+ex.Message);}
   }
   if(deleted.Count>0){LibraryOperations.Remove(p,deleted);Changed("Dateien vom Datenträger löschen");RefreshFolders(v.Folders.SelectedIndex);}
   if(errors.Count>0)AppMessage.Show(v,"Nicht gelöscht:\r\n"+String.Join("\r\n",errors.Take(10)),"Löschfehler");
  }
  void RemoveSongs(){var f=Current;if(f==null)return;var songs=SelectedSongs();if(songs.Count==0)return;if(AppMessage.Show(v,songs.Count+" Titel aus dem Projekt entfernen? Originale bleiben erhalten.","Titel entfernen",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;LibraryOperations.Remove(p,songs);Changed("Titel entfernen");RefreshFolders(v.Folders.SelectedIndex);}
  string Summary(Project sorted){var text=new System.Text.StringBuilder();text.AppendLine("Audio: "+(sorted.FfmpegLoudness?"FFmpeg-Lautheitsanpassung, MP3-Neucodierung":sorted.Normalize?"MP3Gain-Lautstärkeanpassung":"Lautstärke unverändert"));text.AppendLine("MP3-Ausgabe bei Neucodierung: "+Ffmpeg.EncodingLabel(sorted));text.AppendLine("Neu kodierte Titel: "+sorted.Folders.SelectMany(f=>f.Songs).Count(song=>!ImportAudio.Ready(sorted,song)&&Ffmpeg.NeedsEncoding(sorted,song)));text.AppendLine("Passende Import-Arbeitskopien: "+sorted.Folders.SelectMany(f=>f.Songs).Count(song=>ImportAudio.Ready(sorted,song)));text.AppendLine();foreach(var f in sorted.Folders){text.AppendLine(f.Name+" — "+f.Songs.Count+" Titel — Standardalbum: "+f.Theme);int i=0;foreach(var s in f.Songs)text.AppendLine("  "+(++i).ToString("D2")+"  "+s.Artist+" | "+s.Title+" | "+Rules.AlbumFor(s,f));text.AppendLine();}return text.ToString();}
  void Preview(){var sorted=Rules.Sorted(p);v.Report("Vorschau – alphabetisch nach Interpret und Titel",Summary(sorted));if(AppMessage.Show(v,"Diese Verteilung ins Projekt übernehmen?","Sortierung",MessageBoxButtons.YesNo)==DialogResult.Yes){p=sorted;Changed("Sortierung übernehmen");RefreshFolders(0);}}
  void Overview(){
   var summary=CollectionOverview.Analyze(p);
   using(var d=new CollectionOverviewView(summary))if(d.ShowDialog(v)==DialogResult.OK&&d.TargetFolder>=0&&d.TargetFolder<p.Folders.Count){
    v.Folders.SelectedIndex=d.TargetFolder;ShowSongs();
    if(d.TargetSong>=0&&d.TargetSong<v.Songs.Rows.Count){v.Songs.ClearSelection();v.Songs.CurrentCell=v.Songs.Rows[d.TargetSong].Cells[0];v.Songs.Rows[d.TargetSong].Selected=true;v.Songs.FirstDisplayedScrollingRowIndex=d.TargetSong;v.Songs.Focus();}
   }
  }
  void Check(){var sorted=Rules.Sorted(p);var errors=Rules.Validate(sorted);var warnings=new List<string>();foreach(var f in sorted.Folders){foreach(var group in f.Songs.GroupBy(s=>(s.Artist+"\0"+s.Title).ToUpperInvariant()).Where(g=>g.Count()>1))warnings.Add("Mögliche Dublette in "+f.Name+": "+group.First().Artist+" – "+group.First().Title);foreach(var s in f.Songs)if(!s.AlbumOverride&&s.Album!=f.Theme)warnings.Add("Album wird angepasst: "+s.Artist+" – "+s.Title);}
   v.Report("Prüfergebnis","FEHLER: "+errors.Count+"\r\n"+String.Join("\r\n",errors)+"\r\n\r\nHINWEISE: "+warnings.Count+"\r\n"+String.Join("\r\n",warnings));}
  void Save(){using(var d=new AppSaveFileDialog{Filter="Musikbox-Projekt|*.musikbox.json",FileName=String.IsNullOrEmpty(projectFile)?"Meine-Musikbox.musikbox.json":Path.GetFileName(projectFile)})if(d.ShowDialog(v)==DialogResult.OK){Storage.Save(p,d.FileName);if(!String.Equals(projectFile,d.FileName,StringComparison.OrdinalIgnoreCase)){backups=new ProjectBackups(d.FileName,backupRoot);projectFile=d.FileName;}history.Record(p,"Projekt bearbeiten");history.MarkSaved();dirty=false;QueueBackup();UpdateStatus();}}
  void Open(){using(var d=new AppOpenFileDialog{Filter="Musikbox-Projekt|*.json"})if(d.ShowDialog(v)==DialogResult.OK){var loaded=Storage.Load(d.FileName);var sorted=Rules.Sorted(loaded);if(!CanDiscard())return;FlushBackup();ReplaceProject(sorted,d.FileName,Storage.Bytes(sorted).SequenceEqual(Storage.Bytes(loaded)));}}
  void SetBusy(bool state){if(state){CancelLibrarySearch();CommitSideTags();}busy=state;v.BusyControls(state);foreach(ToolStripItem item in v.Tools.Items)item.Enabled=!state;v.Cancel.Enabled=state&&cancel!=null;v.Folders.Enabled=!state;v.Songs.Enabled=!state;v.Progress.Value=0;if(!state){SyncSideTags();UpdateStatus();if(!String.IsNullOrWhiteSpace(v.SearchBox.Text))QueueLibrarySearch(v.SearchBox.Text,false);if(backupPending)QueueBackup();}else v.Status.Text="Bitte warten …";}
  async void StartExport(){
   try {
    ImportAudio.ValidatePending(p,p.Folders.SelectMany(f=>f.Songs));var sorted=Rules.Sorted(p);var errors=Rules.Validate(sorted);if(errors.Count>0)throw new Exception(String.Join("\r\n",errors.Take(25)));
    v.Report("Exportvorschau",Summary(sorted));string target;using(var d=new AppFolderDialog{Description="Ziel wählen: neuer Export-Unterordner wird automatisch angelegt"}){if(d.ShowDialog(v)!=DialogResult.OK)return;target=d.SelectedPath;}
    if(AppMessage.Show(v,"Jetzt "+sorted.Folders.Sum(f=>f.Songs.Count)+" Titel als Kopien exportieren?\r\nIndividuell bearbeitete Album-Tags bleiben erhalten; sonst gilt der Themenname. Vorhandene Exporte bleiben erhalten.\r\n"+(p.Normalize?"Lautstärkeanpassung EIN: "+p.TargetDb+" dB mit Clipping-Schutz.":p.FfmpegLoudness?"FFmpeg-Lautheitsanpassung EIN: "+p.TargetLufs+" LUFS (MP3-Neucodierung).":"Lautstärkeanpassung AUS.")+"","Export starten",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
    cancel=new CancellationTokenSource();SetBusy(true);var token=cancel.Token;
    string path=await Task.Run(()=>Exporter.Export(sorted,target,()=>token.ThrowIfCancellationRequested(),(n,name)=>v.BeginInvoke((Action)(()=>{v.Progress.Value=n;v.Status.Text=n+" % · "+name;}))));
    AppMessage.Show(v,"Export erfolgreich:\r\n"+path+"\r\n\r\nFür die Musikbox die nummerierten Ordner aus diesem Exportordner auf den Datenträger kopieren. Bestehende Musik vorher sichern.","Fertig");
   }catch(OperationCanceledException){AppMessage.Show(v,"Export abgebrochen. Unvollständige Kopien werden nach Möglichkeit entfernt.");}catch(Exception e){AppMessage.Show(v,e.Message,"Export nicht abgeschlossen");}finally{if(cancel!=null){cancel.Dispose();cancel=null;}SetBusy(false);}
  }
  void GainSettings(){
   using(var d=new ModernForm{Text="Audio-Einstellungen",Width=720,Height=640,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}) {
    var mode=new StudioComboBox{Left=200,Top=16,Width=480,DropDownStyle=ComboBoxStyle.DropDownList};mode.Items.AddRange(new object[]{"Lautstärke unverändert","MP3Gain – ohne MP3-Neucodierung","FFmpeg – Lautheitsanpassung mit MP3-Neucodierung"});mode.SelectedIndex=p.Normalize?1:p.FfmpegLoudness?2:0;
    var modeLabel=new Label{Left=18,Top=20,Width=180,Text="Lautstärke-Priorität:"};
    var info=new Label{Left=18,Top=55,Width=660,Height=55,Text="Die Auswahl wird beim Import automatisch im Hintergrund angewendet.\r\nArbeitskopien werden lokal gespeichert. Originaldateien bleiben erhalten."};
    var gainLabel=new Label{Left=18,Top=117,Width=180,Text="MP3Gain-Ziel (dB):"};var gainTarget=new StudioNumericUpDown{Left=200,Top=114,Width=85,Minimum=80,Maximum=95,DecimalPlaces=1,Increment=0.5m,Value=(decimal)(p.TargetDb>=80&&p.TargetDb<=95?p.TargetDb:89)};
    var lufsLabel=new Label{Left=330,Top=117,Width=210,Text="FFmpeg-Ziel (LUFS):"};var lufsTarget=new StudioNumericUpDown{Left=545,Top=114,Width=85,Minimum=-23,Maximum=-10,DecimalPlaces=1,Increment=0.5m,Value=(decimal)(p.TargetLufs>=-23&&p.TargetLufs<=-10?p.TargetLufs:-16)};
    var peak=new Label{Left=18,Top=155,Width=660,Height=35,Text="FFmpeg: True-Peak-Ziel -2 dBTP; MP3-Ausgabe 44,1 kHz, Stereo.\r\nDie dB-Skala von MP3Gain und LUFS sind unterschiedliche Skalen."};
    var encodingLabel=new Label{Left=18,Top=205,Width=180,Text="MP3-Bitratenmodus:"};var encoding=new StudioComboBox{Left=200,Top=201,Width=480,DropDownStyle=ComboBoxStyle.DropDownList};encoding.Items.AddRange(new object[]{"Feste Bitrate (CBR)","Variable Bitrate (VBR)"});encoding.SelectedIndex=p.VariableBitrate?1:0;
    var bitrateLabel=new Label{Left=18,Top=247,Width=180,Text="CBR-Bitrate (kbit/s):"};var bitrate=new StudioComboBox{Left=200,Top=243,Width=90,DropDownStyle=ComboBoxStyle.DropDownList};foreach(int b in Ffmpeg.Bitrates)bitrate.Items.Add(b);bitrate.SelectedItem=Ffmpeg.Bitrates.Contains(p.Mp3Bitrate)?p.Mp3Bitrate:192;
    var qualityLabel=new Label{Left=330,Top=247,Width=135,Text="VBR-Qualität:"};var quality=new StudioComboBox{Left=465,Top=243,Width=215,DropDownStyle=ComboBoxStyle.DropDownList};quality.Items.AddRange(new object[]{"V0 – höchste Qualität","V1","V2 – Standard","V3","V4","V5 – kleinere Dateien"});quality.SelectedIndex=Math.Max(0,Math.Min(5,p.VbrQuality));
    var bitrateHint=new Label{Left=18,Top=283,Width=660,Height=42,Text="Standard: 192 kbit/s CBR. Bei VBR bestimmt die Qualitätsstufe die Bitrate;\r\nes gibt keinen festen kbit/s-Wert. Ausgabe: 44,1 kHz, Stereo."};
    var reencode=new CheckBox{Left=18,Top=490,Width=660,Height=40,Text="Auch vorhandene MP3s auf diese Qualität neu kodieren (optional, Qualitätsverlust möglich)",Checked=p.ReencodeMp3};
    Action updateEncoding=()=>{bitrate.Enabled=encoding.SelectedIndex==0;quality.Enabled=encoding.SelectedIndex==1;};encoding.SelectedIndexChanged+=(sender,e)=>updateEncoding();updateEncoding();
    var gainPath=new StudioTextBox{Left=18,Top=335,Width=315,ReadOnly=true,Text=p.GainExecutable??""};var chooseGain=new Button{Left=340,Top=332,Width=165,Text="mp3gain.exe wählen"};
    var ffPath=new StudioTextBox{Left=18,Top=378,Width=315,ReadOnly=true,Text=p.FfmpegExecutable??""};var chooseFf=new Button{Left=340,Top=375,Width=165,Text="ffmpeg.exe wählen"};
    var downloadGain=new Button{Left=515,Top=332,Width=165,Text="MP3Gain ZIP laden"};var downloadFf=new Button{Left=515,Top=375,Width=165,Text="FFmpeg ZIP laden"};
    var gainPage=new LinkLabel{Left=18,Top=546,Width=175,Height=25,Text="MP3Gain-Downloadseite"};var ffPage=new LinkLabel{Left=200,Top=546,Width=180,Height=25,Text="FFmpeg-Downloadseite"};
    Action<string> openDownload=url=>v.Report("Anbieteradresse",url);
    downloadGain.Click+=(sender,e)=>openDownload("https://sourceforge.net/projects/mp3gain/files/MP3Gain-Windows%20%28Stable%29/1.2.5/mp3gain-win-1_2_5.zip/download");
    downloadFf.Click+=(sender,e)=>openDownload("https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip");
    gainPage.LinkClicked+=(sender,e)=>openDownload("https://mp3gain.sourceforge.net/download.php");ffPage.LinkClicked+=(sender,e)=>openDownload("https://ffmpeg.org/download.html");
    Action<TextBox,string> choose=(box,name)=>{using(var file=new AppOpenFileDialog{Filter="Programm|"+name,Title=name+" auswählen"})if(file.ShowDialog(d)==DialogResult.OK){if(!Path.GetFileName(file.FileName).Equals(name,StringComparison.OrdinalIgnoreCase)){AppMessage.Show(d,"Bitte "+name+" auswählen.");return;}box.Text=file.FileName;}};
    chooseGain.Click+=(sender,e)=>choose(gainPath,"mp3gain.exe");chooseFf.Click+=(sender,e)=>choose(ffPath,"ffmpeg.exe");
    var hint=new Label{Left=18,Top=419,Width=660,Height=60,Text="MP3Gain, FFmpeg und ffprobe sind im portablen Paket enthalten.\r\nDie Verarbeitung läuft innerhalb der Musikbox-Werkstatt im Hintergrund.\r\nAlternative Werkzeugpfade können bei Bedarf ausgewählt werden."};
    var ok=new Button{Left=410,Top=540,Width=130,Text="Übernehmen"};var no=new Button{Left=550,Top=540,Width=130,Text="Abbrechen",DialogResult=DialogResult.Cancel};
    ok.Click+=(sender,e)=>{try{var candidate=p.WithFolders(p.Folders);candidate.Normalize=mode.SelectedIndex==1;candidate.FfmpegLoudness=mode.SelectedIndex==2;candidate.TargetDb=(double)gainTarget.Value;candidate.TargetLufs=(double)lufsTarget.Value;candidate.Mp3Bitrate=(int)bitrate.SelectedItem;candidate.VariableBitrate=encoding.SelectedIndex==1;candidate.VbrQuality=quality.SelectedIndex;candidate.ReencodeMp3=reencode.Checked;candidate.GainExecutable=gainPath.Text;candidate.FfmpegExecutable=ffPath.Text;Gain.Validate(candidate);Ffmpeg.Validate(candidate,!String.IsNullOrWhiteSpace(ffPath.Text));p=candidate;Changed("Audio-Einstellungen");UpdateStatus();d.DialogResult=DialogResult.OK;}catch(Exception ex){AppMessage.Show(d,ex.Message);}};
    mode.SelectedIndexChanged+=(sender,e)=>{gainTarget.Enabled=mode.SelectedIndex==1;lufsTarget.Enabled=mode.SelectedIndex==2;};gainTarget.Enabled=mode.SelectedIndex==1;lufsTarget.Enabled=mode.SelectedIndex==2;
    d.Controls.AddRange(new Control[]{encodingLabel,encoding,bitrateLabel,bitrate,qualityLabel,quality,bitrateHint,reencode,mode,modeLabel,info,gainLabel,gainTarget,lufsLabel,lufsTarget,peak,gainPath,chooseGain,ffPath,chooseFf,hint,ok,no});d.AcceptButton=ok;d.CancelButton=no;d.ShowDialog(v);
   }
  }
  void Help(){v.Report("Musikbox-Werkstatt Studio",File.ReadAllText(PortablePaths.Document("START-STUDIO.txt"),System.Text.Encoding.UTF8));}

 }
 static class Program {
  [STAThread] static void Main(string[] args){ 
#if NET8_0_OR_GREATER
Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
#endif
Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);try{PortablePaths.Initialize();Application.ThreadException+=(s,e)=>{ProgramErrors.Record(e.Exception);AppMessage.Show(null,e.Exception.Message,"Programmfehler");};var v=new MainView();new Controller(v);if(args.Length>0&&System.IO.File.Exists(args[0]))v.Shown+=(s,e)=>v.OpenProjectPath(args[0]);Application.Run(v);}catch(Exception ex){ProgramErrors.Record(ex);AppMessage.Show(null,ex.ToString(),"Musikbox-Werkstatt – Startfehler",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
 }
}
