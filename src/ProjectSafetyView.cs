using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace Musikbox {
 public class BackupRecoveryView:ModernForm {
  public readonly ListView Items=new StudioListView{Dock=DockStyle.Fill,View=System.Windows.Forms.View.Details,FullRowSelect=true,MultiSelect=false,HideSelection=false};
  public string SelectedFile {get{return Items.SelectedItems.Count==1?((ProjectBackupEntry)Items.SelectedItems[0].Tag).File:null;}}
  public BackupRecoveryView(IList<ProjectBackupEntry> entries){
   Text="Projektsicherung wiederherstellen";Width=860;Height=480;MinimumSize=new Size(700,400);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
   var note=new Label{Dock=DockStyle.Top,Height=65,Padding=new Padding(12),Text="Automatische Sicherungen enthalten Tags, Zuordnungen und Dateiverweise.\r\nMusik und Bilddateien sind nicht enthalten. Wiederherstellung öffnet eine ungespeicherte Projektkopie."};
   Items.Columns.Add("Sicherungszeit",180);Items.Columns.Add("Projekt",600);
   foreach(var entry in entries){var row=new ListViewItem(entry.Time.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss")){Tag=entry};row.SubItems.Add(entry.ProjectName);row.ToolTipText=entry.File;Items.Items.Add(row);}Items.ShowItemToolTips=true;
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=48,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(6)};var close=new Button{Text="Abbrechen",AutoSize=true,DialogResult=DialogResult.Cancel};var open=new Button{Text="Wiederherstellen",AutoSize=true,Enabled=false};Items.SelectedIndexChanged+=(s,e)=>open.Enabled=SelectedFile!=null;open.Click+=(s,e)=>{if(SelectedFile!=null)DialogResult=DialogResult.OK;};buttons.Controls.Add(close);buttons.Controls.Add(open);
   Controls.Add(Items);Controls.Add(buttons);Controls.Add(note);if(entries.Count==0)note.Text="Noch keine Projektsicherungen vorhanden. Änderungen werden automatisch gesichert.\r\nDie Sicherungen enthalten Dateiverweise; Musik und Bilder bitte separat aufbewahren.";CancelButton=close;
  }
 }
 public class RelinkLocationView:ModernForm {
  public readonly ComboBox OldRoot=new StudioComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDown};
  public readonly TextBox NewRoot=new StudioTextBox{Dock=DockStyle.Fill,ReadOnly=true};
  public RelinkLocationView(IList<string> suggestions){
   Text="Verschobene Dateien wiederfinden";Width=790;Height=340;MinimumSize=new Size(700,330);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
   var panel=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(15),ColumnCount=2,RowCount=5};panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,175));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));foreach(int size in new[]{72,40,40,42,50})panel.RowStyles.Add(new RowStyle(SizeType.Absolute,size));Controls.Add(panel);
   var note=new Label{Dock=DockStyle.Fill,Text="Wähle den neuen Ort derselben Ordnerstruktur. Beispiel: E:\\Musik → F:\\Musik.\r\nUnterordner und Dateinamen müssen gleich geblieben sein. Geprüft werden nur\r\nfehlende Verweise; die Zuordnungen siehst du vor der Übernahme."};panel.Controls.Add(note,0,0);panel.SetColumnSpan(note,2);
   panel.Controls.Add(new Label{Text="Bisheriger Speicherort",AutoSize=true},0,1);panel.Controls.Add(OldRoot,1,1);OldRoot.Items.AddRange(suggestions.Cast<object>().ToArray());if(suggestions.Count>0)OldRoot.SelectedIndex=0;
   panel.Controls.Add(new Label{Text="Neuer Speicherort",AutoSize=true},0,2);panel.Controls.Add(NewRoot,1,2);
   var choose=new Button{Text="Neuen Ordner auswählen",AutoSize=true};choose.Click+=(s,e)=>{using(var d=new AppFolderDialog{Description="Neuen Speicherort wählen, der dem alten Ort oben entspricht"})if(d.ShowDialog(this)==DialogResult.OK)NewRoot.Text=d.SelectedPath;};panel.Controls.Add(choose,1,3);
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var no=new Button{Text="Abbrechen",AutoSize=true,DialogResult=DialogResult.Cancel};var ok=new Button{Text="Zuordnung prüfen",AutoSize=true};ok.Click+=(s,e)=>{try{SourceRelinking.Root(OldRoot.Text);if(!Directory.Exists(NewRoot.Text))throw new Exception("Bitte den neuen Speicherort auswählen.");DialogResult=DialogResult.OK;}catch(Exception ex){AppMessage.Show(this,ex.Message);}};buttons.Controls.Add(no);buttons.Controls.Add(ok);panel.Controls.Add(buttons,1,4);CancelButton=no;
  }
 }
 public class RelinkReviewView:ModernForm {
  public readonly DataGridView Grid=new StudioSongGrid{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,RowHeadersVisible=false};
  public RelinkReviewView(RelinkPlan plan){
   Text="Dateizuordnungen prüfen";Width=1100;Height=530;MinimumSize=new Size(800,400);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
   var note=new Label{Dock=DockStyle.Top,Height=82,Padding=new Padding(12),Text=plan.Found+" von "+plan.Entries.Count+" fehlenden Verweisen am neuen Ort gefunden.\r\nNur gefundene Zuordnungen werden übernommen. Fehlende bleiben erhalten.\r\nDie Prüfung vergleicht Pfade und Vorhandensein, keine Audioinhalte.\r\nArbeitskopien neu verbundener Titel werden bei Bedarf aus der Quelle neu erstellt."};
   foreach(string label in new[]{"Art","Bisheriger Pfad","Neuer Pfad","Ergebnis"})Grid.Columns.Add(label,label);Grid.Columns[0].FillWeight=50;
   foreach(var entry in plan.Entries){int i=Grid.Rows.Add(entry.Kind,entry.OldPath,entry.NewPath??"",entry.Status);if(!entry.Found)Grid.Rows[i].DefaultCellStyle.BackColor=Color.LemonChiffon;foreach(DataGridViewCell cell in Grid.Rows[i].Cells)cell.ToolTipText=Convert.ToString(cell.Value);}
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=45,FlowDirection=FlowDirection.RightToLeft};var no=new Button{Text="Abbrechen",AutoSize=true,DialogResult=DialogResult.Cancel};var yes=new Button{Text="Gefundene zuordnen",AutoSize=true,Enabled=plan.Found>0,DialogResult=DialogResult.OK};buttons.Controls.Add(no);buttons.Controls.Add(yes);Controls.Add(Grid);Controls.Add(buttons);Controls.Add(note);CancelButton=no;
  }
 }
}
