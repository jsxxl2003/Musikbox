using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace Musikbox {
 public partial class MainView:ModernForm {
  public ToolStripMenuItem OnlineCover=new ToolStripMenuItem("Covervorschläge beim Abspielen"){CheckOnClick=true,Checked=true};
  public PlayerView Player=new PlayerView();
  public SideTagEditor TagEditor=new SideTagEditor();
  public event Action<string[],int> PathsDropped;
  public event Action<bool> HistoryRequested;
  public event Action SongSelectionDismissed;
  public event Action LayoutChanging;
  public WindowMagnet Magnets {get;private set;}
  readonly ToolStripMenuItem magnetic=new ToolStripMenuItem("Magnetisch andocken"){CheckOnClick=true,Checked=true};
  public WorkspacePane FolderPane,SongPane,TagPane,ArtworkPane,PlayerPane;
  readonly string layoutFile;
  readonly System.Collections.Generic.Dictionary<WorkspacePane,ToolStripMenuItem> paneItems=new System.Collections.Generic.Dictionary<WorkspacePane,ToolStripMenuItem>();
  public SplitContainer FolderCoverSplit=new SplitContainer{Width=300,Height=400,Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,FixedPanel=FixedPanel.Panel1,SplitterWidth=8,SplitterDistance=120,Panel1MinSize=70,Panel2MinSize=100};
  public Func<bool> CanDrop=()=>true;
  readonly System.Collections.Generic.HashSet<Control> dropControls=new System.Collections.Generic.HashSet<Control>();
  public ListBox Folders=new StudioListBox{Dock=DockStyle.Fill,SelectionMode=SelectionMode.One};
  public DataGridView Songs=new StudioSongGrid{Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=true,BackgroundColor=Color.White,BorderStyle=BorderStyle.None};
  public ToolStrip Tools=new ToolStrip{GripStyle=ToolStripGripStyle.Hidden,Padding=new Padding(8),LayoutStyle=ToolStripLayoutStyle.Flow,AutoSize=true};
  readonly System.Collections.Generic.Dictionary<string,ToolStripMenuItem> commands=new System.Collections.Generic.Dictionary<string,ToolStripMenuItem>();
  public Label Counts=new Label{Dock=DockStyle.Bottom,Height=22,Padding=new Padding(8,3,0,0),BackColor=Color.FromArgb(235,240,245)};
  readonly System.Collections.Generic.Dictionary<string,ToolStripDropDownButton> menus=new System.Collections.Generic.Dictionary<string,ToolStripDropDownButton>();
  public Label BackupStatus=new Label{Visible=false,Dock=DockStyle.Bottom,Height=24,Padding=new Padding(10,3,0,0),ForeColor=Color.DimGray,Text="Automatische Projektsicherung aktiv"};
  public Label Status=new Label{Visible=false,Dock=DockStyle.Bottom,Height=30,Padding=new Padding(10,6,0,0)};
  public Label Details=new Label{Dock=DockStyle.Top,Height=55,Padding=new Padding(8)};
  public PictureBox TitleCover=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom};
  public SplitContainer WorkspaceSplit=new SplitContainer{Width=1200,Height=700,Dock=DockStyle.Fill,SplitterDistance=320,SplitterWidth=8,FixedPanel=FixedPanel.Panel1,Panel1MinSize=300,Panel2MinSize=400};
  public SplitContainer SidebarSplit=new SplitContainer{Width=300,Height=700,Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,FixedPanel=FixedPanel.Panel1,SplitterWidth=8,SplitterDistance=310,Panel1MinSize=220,Panel2MinSize=100};
  public TabControl CoverTabs=new StudioTabControl{Dock=DockStyle.Fill};
  readonly TableLayoutPanel coverLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=new Padding(0)};
  public Label CoverStatus=new Label{Dock=DockStyle.Bottom,Height=36,ForeColor=Color.DimGray,AutoEllipsis=true};
  public PictureBox Cover=new PictureBox{Dock=DockStyle.Bottom,Height=140,SizeMode=PictureBoxSizeMode.Zoom};
  public ProgressBar Progress=new StudioProgressBar{Visible=false,Dock=DockStyle.Bottom,Height=8};
  public ToolStripButton Cancel=new ToolStripButton("Abbrechen"){Enabled=false};
  public MainView(string layoutFile=null) {
   this.layoutFile=layoutFile??Path.Combine(PortablePaths.Data,"Fensterlayout.json");
   Text=AppIdentity.WindowTitle;Width=1250;Height=780;MinimumSize=new Size(960,600);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);
   Songs.EnableHeadersVisualStyles=false;Songs.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(235,240,245);Songs.ColumnHeadersDefaultCellStyle.ForeColor=Color.FromArgb(40,55,70);Songs.DefaultCellStyle.SelectionBackColor=Color.FromArgb(217,238,239);Songs.DefaultCellStyle.SelectionForeColor=Color.FromArgb(20,55,65);Songs.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(248,250,252);Songs.GridColor=Color.FromArgb(235,239,244);Songs.RowTemplate.Height=28;
   BackColor=Color.FromArgb(241,244,248);
   Magnets=new WindowMagnet(this);var split=WorkspaceSplit;
   var sidebar=new StudioAutoScrollPanel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.FromArgb(241,244,248),Padding=new Padding(8)};
   Folders.Dock=DockStyle.Fill;Folders.BorderStyle=BorderStyle.None;Cover.Dock=DockStyle.Fill;
   var folderImage=new TabPage("Ordnercover");var titleImage=new TabPage("Titelcover");coverLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));coverLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));coverLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,36));Cover.Margin=new Padding(0);CoverStatus.Margin=new Padding(0);CoverStatus.Dock=DockStyle.Fill;coverLayout.Controls.Add(Cover,0,0);coverLayout.Controls.Add(CoverStatus,0,1);folderImage.Controls.Add(coverLayout);titleImage.Controls.Add(TitleCover);CoverTabs.TabPages.Add(folderImage);CoverTabs.TabPages.Add(titleImage);
   var songsArea=new Panel{Dock=DockStyle.Fill};songsArea.Controls.Add(Songs);songsArea.Controls.Add(Details);
   FolderPane=new WorkspacePane("Ordnerübersicht",Folders);ArtworkPane=new WorkspacePane("Cover",CoverTabs);TagPane=new WorkspacePane("Tag-Bearbeitung",TagEditor,true);SongPane=new WorkspacePane("Titelliste",songsArea);PlayerPane=new WorkspacePane("Player",Player){Dock=DockStyle.Bottom,Height=160};Player.Visible=true;PlayerPane.HidePane();
   FolderCoverSplit.Panel1.Controls.Add(FolderPane);FolderCoverSplit.Panel2.Controls.Add(ArtworkPane);SidebarSplit.Panel1.Controls.Add(FolderCoverSplit);SidebarSplit.Panel2.Controls.Add(TagPane);TagEditor.Height=320;sidebar.Controls.Add(SidebarSplit);split.Panel1.Controls.Add(sidebar);var right=new Panel{Dock=DockStyle.Fill};right.Controls.Add(SongPane);right.Controls.Add(PlayerPane);split.Panel2.Controls.Add(right);
   foreach(var pane in Panes){pane.Bind(this);pane.BeforeMove+=()=>{if(LayoutChanging!=null)LayoutChanging();};pane.LayoutChanged+=UpdatePaneLayout;pane.WindowCreated+=window=>{EnableDrop(window);Magnets.Register(window);};}
   var windows=CommandMenu("Ansicht");var separate=new ToolStripMenuItem("Bereich als Fenster öffnen");foreach(var pane in Panes){var selected=pane;var toggle=new ToolStripMenuItem(pane.Caption){Checked=!pane.IsHidden,ShortcutKeys=Keys.Alt|((Keys)((int)Keys.D1+paneItems.Count)),ShortcutKeyDisplayString="Alt+"+(paneItems.Count+1)};toggle.Click+=(sender,e)=>{if(selected.IsHidden)selected.ShowPane();else selected.HidePane();};paneItems.Add(pane,toggle);windows.DropDownItems.Add(toggle);var floating=new ToolStripMenuItem(pane.Caption);floating.Click+=(sender,e)=>selected.Float(this);separate.DropDownItems.Add(floating);}windows.DropDownItems.Add(new ToolStripSeparator());windows.DropDownItems.Add(separate);
   magnetic.Click+=(sender,e)=>Magnets.Enabled=magnetic.Checked;magnetic.ToolTipText="Berührende Fenster gemeinsam verschieben. Umschalt beim Ziehen trennt ein Modul.";windows.DropDownItems.Add(magnetic);
   windows.DropDownItems.Add(new ToolStripSeparator());var tile=new ToolStripMenuItem("Fenster nebeneinander");tile.Click+=(sender,e)=>TilePanes();windows.DropDownItems.Add(tile);var quiet=new ToolStripMenuItem("Nur Ordner und Titel");quiet.Click+=(sender,e)=>QuietPaneLayout();windows.DropDownItems.Add(quiet);var reset=new ToolStripMenuItem("Standardansicht wiederherstellen");reset.Click+=(sender,e)=>ResetPaneLayout();windows.DropDownItems.Add(reset);
   var save=new ToolStripMenuItem("Anordnung speichern");save.Click+=(sender,e)=>{try{SavePaneLayout();}catch(Exception ex){AppMessage.Show(this,"Die Fensteranordnung konnte nicht gespeichert werden:\r\n"+ex.Message);}};windows.DropDownItems.Add(save);
   foreach(string col in new[]{"Interpret","Titel","Album"})Songs.Columns.Add(col,col);
   var source=new DataGridViewTextBoxColumn{Name="Quelle",HeaderText="Quelldatei",ReadOnly=true,FillWeight=65};Songs.Columns.Add(source);TrackColumns.Configure(Songs);
   Controls.Add(split);Controls.Add(Progress);Controls.Add(Status);Controls.Add(BackupStatus);Controls.Add(Counts);Controls.Add(Tools);
   Shown+=(sender,e)=>{SidebarSplit.SplitterDistance=Math.Max(SidebarSplit.Panel1MinSize,Math.Min(310,SidebarSplit.Height-SidebarSplit.Panel2MinSize-SidebarSplit.SplitterWidth));LoadPaneLayout();};
   EnableDrop(this);
   Songs.EditingControlShowing+=(sender,e)=>EnableDrop(e.Control);
   InstallStudioLayout();
  }
  public void CoverMessage(string text,bool warning){CoverStatus.Text=text;coverLayout.RowStyles[1].Height=warning?36:18;}
  public WorkspacePane[] Panes {get{return new[]{FolderPane,SongPane,TagPane,ArtworkPane,PlayerPane};}}
  public bool HandleHistoryKey(Keys keys,Control root){if((keys&Keys.Modifiers)==Keys.Alt){int index=(int)(keys&Keys.KeyCode)-(int)Keys.D1;if(index>=0&&index<Panes.Length){var pane=Panes[index];if(pane.IsHidden)pane.ShowPane();else pane.HidePane();return true;}}if(keys!=(Keys.Control|Keys.Z)&&keys!=(Keys.Control|Keys.Y))return false;Control focused=root;while(focused.HasChildren){var child=focused.Controls.Cast<Control>().FirstOrDefault(c=>c.ContainsFocus);if(child==null)break;focused=child;}var text=focused as TextBoxBase;if(text!=null&&!text.ReadOnly)return false;if(CanDrop()&&HistoryRequested!=null){HistoryRequested(keys==(Keys.Control|Keys.Y));return true;}return false;}
  protected override bool ProcessCmdKey(ref Message msg,Keys keyData){return HandleHistoryKey(keyData,this)||base.ProcessCmdKey(ref msg,keyData);}
  void UpdatePaneLayout(){
   if(studioInstalled){UpdateStudioPanes();return;}
   foreach(var item in paneItems)item.Value.Checked=!item.Key.IsHidden;
   bool folders=FolderPane.IsFloating||FolderPane.IsHidden,covers=ArtworkPane.IsFloating||ArtworkPane.IsHidden,tags=TagPane.IsFloating||TagPane.IsHidden;
   PlayerPane.Visible=!PlayerPane.IsFloating&&!PlayerPane.IsHidden;
   bool right=(SongPane.IsFloating||SongPane.IsHidden)&&(PlayerPane.IsFloating||PlayerPane.IsHidden);
   PlayerPane.Dock=SongPane.IsHidden&&!PlayerPane.IsHidden&&!PlayerPane.IsFloating?DockStyle.Fill:DockStyle.Bottom;if(PlayerPane.Dock==DockStyle.Bottom&&PlayerPane.Height<100)PlayerPane.Height=160;
   WorkspaceSplit.Panel1Collapsed=false;WorkspaceSplit.Panel2Collapsed=false;if(right&&!(folders&&covers&&tags)){WorkspaceSplit.Panel2Collapsed=true;}else if(folders&&covers&&tags){WorkspaceSplit.Panel1Collapsed=true;return;}
   SidebarSplit.Panel1Collapsed=false;SidebarSplit.Panel2Collapsed=false;FolderCoverSplit.Panel1Collapsed=false;FolderCoverSplit.Panel2Collapsed=false;
   if(folders&&covers)SidebarSplit.Panel1Collapsed=true;else if(tags)SidebarSplit.Panel2Collapsed=true;
   if(folders&&!covers)FolderCoverSplit.Panel1Collapsed=true;else if(covers&&!folders)FolderCoverSplit.Panel2Collapsed=true;
  }
  public void ResetPaneLayout(){if(studioInstalled){RestoreStudioLayout();return;}foreach(var pane in Panes){pane.DockBack();if(pane==PlayerPane)pane.HidePane();else pane.ShowPane();}UpdatePaneLayout();WorkspaceSplit.SplitterDistance=320;SidebarSplit.SplitterDistance=Math.Max(SidebarSplit.Panel1MinSize,Math.Min(310,SidebarSplit.Height-SidebarSplit.Panel2MinSize-SidebarSplit.SplitterWidth));FolderCoverSplit.SplitterDistance=Math.Max(FolderCoverSplit.Panel1MinSize,Math.Min(120,FolderCoverSplit.Height-FolderCoverSplit.Panel2MinSize-FolderCoverSplit.SplitterWidth));}
  public void TilePanes(){var area=Screen.FromControl(this).WorkingArea;int width=area.Width/2,height=area.Height/3;for(int i=0;i<Panes.Length;i++){Panes[i].Float(this);Panes[i].FloatingWindow.Bounds=new Rectangle(area.Left+i%2*width,area.Top+i/2*height,width,height);}}
  public void QuietPaneLayout(){FolderPane.DockBack();SongPane.DockBack();FolderPane.ShowPane();SongPane.ShowPane();TagPane.HidePane();ArtworkPane.HidePane();PlayerPane.HidePane();}
  public void SavePaneLayout(){WorkspaceLayout.Save(layoutFile,new WorkspaceLayout{Version=3,Magnetic=Magnets.Enabled,SidebarWidth=WorkspaceSplit.SplitterDistance,SidebarTop=SidebarSplit.SplitterDistance,FolderHeight=FolderCoverSplit.SplitterDistance,Panes=Panes.Select(p=>{var b=p.IsFloating?p.FloatingWindow.Bounds:Rectangle.Empty;return new PaneLayout{Name=p.Caption,Floating=p.IsFloating,Hidden=p.IsHidden,X=b.X,Y=b.Y,Width=b.Width,Height=b.Height};}).ToList()});Status.Text="Fensteranordnung gespeichert – wird beim nächsten Start geladen.";}
  public void LoadPaneLayout(){try{var layout=WorkspaceLayout.Load(layoutFile);if(layout==null)return;ApplyPaneLayout(layout);}catch{ResetPaneLayout();Status.Text="Gespeicherte Fensteranordnung nicht lesbar – Standardansicht wiederhergestellt.";}}
  public void ApplyPaneLayout(WorkspaceLayout layout){if(studioInstalled){if(layout.Version<3)return;foreach(var state in layout.Panes){var item=Panes.FirstOrDefault(x=>x.Caption==state.Name);if(item==null)continue;if(state.Floating){item.Float(this);item.FloatingWindow.Bounds=WorkspaceLayout.VisibleBounds(new Rectangle(state.X,state.Y,state.Width,state.Height),Screen.AllScreens.Select(screen=>screen.WorkingArea).ToArray());}if(state.Hidden)item.HidePane();else item.ShowPane();}return;}ResetPaneLayout();Magnets.Enabled=layout.Version<2||layout.Magnetic;magnetic.Checked=Magnets.Enabled;WorkspaceSplit.SplitterDistance=Math.Max(300,Math.Min(layout.SidebarWidth,WorkspaceSplit.Width-WorkspaceSplit.Panel2MinSize-WorkspaceSplit.SplitterWidth));SidebarSplit.SplitterDistance=Math.Max(SidebarSplit.Panel1MinSize,Math.Min(layout.SidebarTop,SidebarSplit.Height-SidebarSplit.Panel2MinSize-SidebarSplit.SplitterWidth));FolderCoverSplit.SplitterDistance=Math.Max(FolderCoverSplit.Panel1MinSize,Math.Min(layout.FolderHeight,FolderCoverSplit.Height-FolderCoverSplit.Panel2MinSize-FolderCoverSplit.SplitterWidth));foreach(var state in layout.Panes){var pane=Panes.FirstOrDefault(p=>p.Caption==state.Name);if(pane==null)continue;if(state.Floating){pane.Float(this);pane.FloatingWindow.Bounds=WorkspaceLayout.VisibleBounds(new Rectangle(state.X,state.Y,state.Width,state.Height),Screen.AllScreens.Select(screen=>screen.WorkingArea).ToArray());}if(state.Hidden)pane.HidePane();else pane.ShowPane();}}
  protected override void Dispose(bool disposing){if(disposing&&FolderPane!=null)foreach(var pane in Panes)pane.DockBack();base.Dispose(disposing);}
  void EnableDrop(Control control) {
   if(!dropControls.Add(control)){foreach(Control child in control.Controls)EnableDrop(child);return;}control.AllowDrop=true;control.ControlAdded+=(s,e)=>EnableDrop(e.Control);control.Disposed+=(sender,e)=>dropControls.Remove(control);
   control.MouseDown+=(sender,e)=>{if(e.Button!=MouseButtons.Left||!CanDrop())return;Control clicked=(Control)sender;for(Control parent=clicked;parent!=null;parent=parent.Parent)if(parent==ModuleHost)return;
    if(clicked==Songs){var hit=Songs.HitTest(e.X,e.Y);if(hit.Type!=DataGridViewHitTestType.None)return;}
    else {if(!(clicked is Panel||clicked is Form||clicked is Label||clicked is PictureBox))return;for(Control parent=clicked;parent!=null;parent=parent.Parent)if(parent==TagEditor||parent==Player||Object.Equals(parent.Tag,"WindowChrome"))return;}
    if(SongSelectionDismissed!=null)SongSelectionDismissed();
   };
   control.DragEnter+=(sender,e)=>e.Effect=CanDrop()&&e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;
   control.DragOver+=(sender,e)=>e.Effect=CanDrop()&&e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;
   control.DragDrop+=(sender,e)=>{
    if(!CanDrop()||!e.Data.GetDataPresent(DataFormats.FileDrop))return;
    var paths=e.Data.GetData(DataFormats.FileDrop) as string[];if(paths==null||paths.Length==0)return;
    int selected=Folders.SelectedIndex;
    if(Object.ReferenceEquals(sender,Folders)){int hit=Folders.IndexFromPoint(Folders.PointToClient(new Point(e.X,e.Y)));if(hit>=0)selected=hit;}
    ReceiveFileDrop((Control)sender,paths,selected);
   };
   foreach(Control child in control.Controls)EnableDrop(child);
  }
  public void ReceiveFileDrop(Control source,string[] paths,int folderIndex){
   if(!CanDrop()||paths==null||paths.Length==0)return;
   for(Control c=source;c!=null;c=c.Parent)if(c==TagEditor){if(TagPathsDropped!=null)TagPathsDropped(paths);return;}
   if(ModuleHost.Visible){
    for(Control c=source;c!=null;c=c.Parent)if(c==ModuleHost){if(moduleFilesDropped!=null)moduleFilesDropped(paths);else if(TagPathsDropped!=null)TagPathsDropped(paths);return;}
   }
   if(PathsDropped!=null)PathsDropped(paths,folderIndex);
  }
  ToolStripDropDownButton CommandMenu(string name){ToolStripDropDownButton menu;if(!menus.TryGetValue(name,out menu)){menu=new ToolStripDropDownButton(name);menus.Add(name,menu);Tools.Items.Add(menu);}return menu;}
  public void ImportMode(ToolStripItem item){CommandMenu("Import").DropDownItems.Add(item);}
  public void ArrangeMenus(){string[] order={"Projekt","Import","Ordner","Titel","Kontrolle","Ansicht","Einstellungen","Werkzeuge","Hilfe"};Tools.Items.Clear();foreach(string name in order)if(menus.ContainsKey(name))Tools.Items.Add(menus[name]);Tools.Items.Add(Cancel);CommandMenu("Titel").DropDownItems.Add(new ToolStripSeparator());CommandMenu("Titel").DropDownItems.Add(OnlineCover);}
  public ToolStripMenuItem Button(string title,Action action){
   string group=new[]{"Speichern","Öffnen","Rückgängig","Wiederholen","Projektsicherungen","Dateien wiederfinden"}.Contains(title)?"Projekt":new[]{"Ordner laden","Titel hinzufügen","Importbericht"}.Contains(title)?"Import":new[]{"Neuer Ordner","Ordner bearbeiten","↑","↓","Entfernen","Cover","Cover gestalten","Cover-Serie"}.Contains(title)?"Ordner":new[]{"Alle auswählen","Markierung umkehren","Markierung aufheben","Titel entfernen","Tags bearbeiten","Im Player öffnen","Abspielen","Pause","Stopp","Cover online suchen","Mit Fingerabdruck erkennen","Titel erkennen (ACRCloud)"}.Contains(title)?"Titel":new[]{"Sortiervorschau","Prüfen","Sammlungsübersicht"}.Contains(title)?"Kontrolle":new[]{"Einstellungen öffnen","Audio-Einstellungen","Fingerabdruck einrichten","ACRCloud-Zugang"}.Contains(title)?"Einstellungen":title=="Hilfe"?"Hilfe":"Werkzeuge";
   var item=new ToolStripMenuItem(title);item.Click+=(s,e)=>action();CommandMenu(group).DropDownItems.Add(item);commands[title]=item;return item;
  }
  public void BusyControls(bool state){OnlineCover.Enabled=!state;TagEditor.Enabled=!state;SearchBox.Enabled=true;Microphone.Enabled=!state;foreach(var button in studioNav.Values)button.Enabled=!state;}
  ToolStripMenuItem ContextCommand(string label,string command){var item=new ToolStripMenuItem(label);item.Click+=(s,e)=>{if(CanDrop()&&commands.ContainsKey(command))commands[command].PerformClick();};return item;}
  ToolStripMenuItem ImportContextMenu(){var menu=new ToolStripMenuItem("Import");menu.DropDownItems.Add(ContextCommand("Ordner laden…","Ordner laden"));menu.DropDownItems.Add(ContextCommand("Titel hinzufügen…","Titel hinzufügen"));menu.DropDownItems.Add(ContextCommand("Titel suchen…","Titel hinzufügen"));return menu;}
  public void ConfigureContextMenus(){
   var folderMenu=new ContextMenuStrip();
   folderMenu.Items.Add(ContextCommand("Ordner bearbeiten…","Ordner bearbeiten"));
   folderMenu.Items.Add(ImportContextMenu());
   folderMenu.Items.Add(ContextCommand("Cover auswählen…","Cover"));folderMenu.Items.Add(ContextCommand("Cover gestalten…","Cover gestalten"));folderMenu.Items.Add(ContextCommand("Cover-Serie…","Cover-Serie"));
   folderMenu.Items.Add(new ToolStripSeparator());
   folderMenu.Items.Add(ContextCommand("Nach oben verschieben","↑"));folderMenu.Items.Add(ContextCommand("Nach unten verschieben","↓"));
   folderMenu.Items.Add(new ToolStripSeparator());folderMenu.Items.Add(ContextCommand("Ordner aus Projekt entfernen…","Entfernen"));
   folderMenu.Opening+=(s,e)=>{e.Cancel=!CanDrop();foreach(ToolStripItem item in folderMenu.Items)item.Enabled=item.Text=="Import"||Folders.SelectedIndex>=0;};
   Folders.MouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Right||!CanDrop())return;int index=Folders.IndexFromPoint(e.Location);Folders.SelectedIndex=index;};Folders.ContextMenuStrip=folderMenu;
   var songMenu=new ContextMenuStrip();songMenu.Items.Add(ImportContextMenu());songMenu.Items.Add(new ToolStripSeparator());songMenu.Items.Add(ContextCommand("Im Player öffnen…","Im Player öffnen"));songMenu.Items.Add(ContextCommand("Abspielen","Abspielen"));songMenu.Items.Add(ContextCommand("Player pausieren","Pause"));songMenu.Items.Add(ContextCommand("Player stoppen","Stopp"));songMenu.Items.Add(new ToolStripSeparator());songMenu.Items.Add(ContextCommand("Cover online suchen…","Cover online suchen"));songMenu.Items.Add(ContextCommand("Mit Fingerabdruck erkennen…","Mit Fingerabdruck erkennen"));songMenu.Items.Add(ContextCommand("Mit ACRCloud erkennen…","Titel erkennen (ACRCloud)"));songMenu.Items.Add(new ToolStripSeparator());
   var artist=new ToolStripMenuItem("Interpret bearbeiten");var title=new ToolStripMenuItem("Titel bearbeiten");
   artist.Click+=(s,e)=>EditSelectedCell(0);title.Click+=(s,e)=>EditSelectedCell(1);songMenu.Items.Add(artist);songMenu.Items.Add(title);
   songMenu.Items.Add(ContextCommand("Tags bearbeiten (auch Mehrfachauswahl)…","Tags bearbeiten"));
   songMenu.Items.Add(new ToolStripSeparator());songMenu.Items.Add(ContextCommand("Ausgewählte Titel aus Projekt entfernen…","Titel entfernen"));
   songMenu.Opening+=(s,e)=>{e.Cancel=!CanDrop();foreach(ToolStripItem item in songMenu.Items)item.Enabled=item.Text=="Import"||(Songs.CurrentRow!=null&&Songs.SelectedRows.Count>0);};
   Songs.MouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Right||!CanDrop())return;var hit=Songs.HitTest(e.X,e.Y);if(hit.Type==DataGridViewHitTestType.ColumnHeader)return;if(hit.RowIndex<0){Songs.ClearSelection();return;}var selected=Songs.SelectedRows.Cast<DataGridViewRow>().ToList();bool keep=Songs.Rows[hit.RowIndex].Selected;if(!keep)Songs.ClearSelection();Songs.CurrentCell=Songs.Rows[hit.RowIndex].Cells[0];if(keep)foreach(var row in selected)row.Selected=true;Songs.Rows[hit.RowIndex].Selected=true;};Songs.ContextMenuStrip=songMenu;
  }
  void EditSelectedCell(int column){if(!CanDrop()||Songs.CurrentRow==null)return;Songs.CurrentCell=Songs.CurrentRow.Cells[column];Songs.BeginEdit(true);}
  public string Ask(string title,string initial) {
   using(var d=new ModernForm{Text=title,Width=540,Height=165,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}) {
    var text=new StudioTextBox{Left=15,Top=15,Width=490,Text=initial??""};var ok=new Button{Text="Übernehmen",Left=280,Top=55,Width=110,DialogResult=DialogResult.OK};var cancel=new Button{Text="Abbrechen",Left=395,Top=55,Width=110,DialogResult=DialogResult.Cancel};d.Controls.AddRange(new Control[]{text,ok,cancel});d.AcceptButton=ok;d.CancelButton=cancel;return d.ShowDialog(this)==DialogResult.OK?text.Text:null;
   }
  }
  public void Report(string title,string text) {
   using(var d=new StudioReportView(title,text))d.ShowDialog(this);
  }
 }
}
