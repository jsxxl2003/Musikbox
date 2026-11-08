using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace Musikbox {
 public class CollectionOverviewView:ModernForm {
  public int TargetFolder=-1,TargetSong=-1;
  readonly CollectionSummary summary;
  readonly DataGridView issues=Grid();
  readonly ComboBox filter=new StudioComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=285};
  static DataGridView Grid(){return new StudioSongGrid{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,BackgroundColor=ModernTheme.Navy,BorderStyle=BorderStyle.None};}
  public CollectionOverviewView(CollectionSummary data){
   summary=data;Text="Sammlungsübersicht";Width=1120;Height=720;MinimumSize=new Size(900,550);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
   var header=new Label{Dock=DockStyle.Top,Height=65,Padding=new Padding(16,12,10,0),BackColor=Color.FromArgb(29,43,61),ForeColor=Color.White,Font=new Font("Segoe UI",12),Text=data.FolderCount+" / 99 Ordner     ·     "+data.SongCount.ToString("N0")+" Titel     ·     "+data.FreeFolders+" freie Ordnerplätze\r\n"+data.FreeInExistingFolders.ToString("N0")+" freie Titelplätze in vorhandenen Ordnern"};
   var tabs=new StudioTabControl{Dock=DockStyle.Fill};var hints=new TabPage("Hinweise ("+data.Issues.Count+")");var folders=new TabPage("Alle Ordner ("+data.FolderCount+")");tabs.TabPages.AddRange(new[]{hints,folders});
   var toolbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=47,Padding=new Padding(8),WrapContents=false};
   filter.Items.AddRange(new object[]{"Alle Hinweise","Fehlende Angaben ("+data.MissingTagSongs+" Titel)","Mögliche Dubletten ("+data.DuplicateSongs+" Titel)","Fehlende Ordnercover ("+data.MissingCovers+")","Überschrittene Ordnerkapazität"});
   toolbar.Controls.Add(filter);var open=new Button{Text="Auswahl öffnen",AutoSize=true};toolbar.Controls.Add(open);
   foreach(string name in new[]{"Art","Ordner","Interpret","Titel","Hinweis"})issues.Columns.Add(name,name);
   issues.Columns["Hinweis"].FillWeight=220;var link=new DataGridViewLinkColumn{Name="Öffnen",HeaderText="",Text="Öffnen",UseColumnTextForLinkValue=true,FillWeight=55};issues.Columns.Add(link);
   issues.CellContentClick+=(s,e)=>{if(e.RowIndex>=0&&e.ColumnIndex==issues.Columns["Öffnen"].Index)Go((CollectionIssue)issues.Rows[e.RowIndex].Tag);};
   issues.CellDoubleClick+=(s,e)=>{if(e.RowIndex>=0)Go((CollectionIssue)issues.Rows[e.RowIndex].Tag);};
   open.Click+=(s,e)=>{if(issues.CurrentRow!=null)Go((CollectionIssue)issues.CurrentRow.Tag);};
   var note=new Label{Dock=DockStyle.Bottom,Height=65,Padding=new Padding(10,6,10,0),Text=data.MissingTagSongs+" Titel mit fehlenden Angaben · "+data.DuplicateGroups+" mögliche Dublettengruppen · "+data.MissingCovers+" Ordner ohne erreichbares Cover\r\nDubletten beruhen auf Interpret und Titel. Eingebettete MP3-Bilder werden nicht geprüft.\r\nDiese Übersicht ändert und löscht keine Einträge."};
   hints.Controls.Add(issues);hints.Controls.Add(toolbar);hints.Controls.Add(note);
   var all=Grid();foreach(string name in new[]{"Ordner","Thema","Titel","Freie Plätze","Cover"})all.Columns.Add(name,name);all.Columns.Add(new DataGridViewLinkColumn{Name="Öffnen",HeaderText="",Text="Öffnen",UseColumnTextForLinkValue=true,FillWeight=55});
   foreach(var f in data.Folders){int row=all.Rows.Add(f.Name,f.Theme,f.Songs,f.FreeSlots,f.CoverStatus);all.Rows[row].Tag=f;}
   all.CellContentClick+=(s,e)=>{if(e.RowIndex>=0&&e.ColumnIndex==all.Columns["Öffnen"].Index){var f=(CollectionFolderInfo)all.Rows[e.RowIndex].Tag;TargetFolder=f.FolderIndex;DialogResult=DialogResult.OK;Close();}};
   all.CellDoubleClick+=(s,e)=>{if(e.RowIndex>=0){var f=(CollectionFolderInfo)all.Rows[e.RowIndex].Tag;TargetFolder=f.FolderIndex;DialogResult=DialogResult.OK;Close();}};
   folders.Controls.Add(all);
   var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=45,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(5)};var close=new Button{Text="Schließen",DialogResult=DialogResult.Cancel,AutoSize=true};bottom.Controls.Add(close);CancelButton=close;
   Controls.Add(tabs);Controls.Add(header);Controls.Add(bottom);
   filter.SelectedIndexChanged+=(s,e)=>Populate();filter.SelectedIndex=0;
  }
  static string Kind(CollectionIssueKind k){switch(k){case CollectionIssueKind.MissingTags:return "Fehlende Angaben";case CollectionIssueKind.PossibleDuplicate:return "Mögliche Dublette";case CollectionIssueKind.MissingCover:return "Ordnercover";default:return "Kapazität";}}
  void Populate(){issues.Rows.Clear();int index=filter.SelectedIndex;foreach(var issue in summary.Issues){if(index>0&&(int)issue.Kind!=index-1)continue;int row=issues.Rows.Add(Kind(issue.Kind),issue.FolderName,issue.Artist,issue.Title,issue.Message);issues.Rows[row].Tag=issue;}if(issues.Rows.Count==0){issues.ClearSelection();}}
  void Go(CollectionIssue issue){TargetFolder=issue.FolderIndex;TargetSong=issue.SongIndex;DialogResult=DialogResult.OK;Close();}
 }
}
