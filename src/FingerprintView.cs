using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace Musikbox {
 public class FingerprintSettingsView : ModernForm {
  public readonly TextBox Executable=new StudioTextBox{Dock=DockStyle.Fill,ReadOnly=true},ClientKey=new StudioTextBox{Dock=DockStyle.Fill,UseSystemPasswordChar=true};
  public FingerprintSettingsView(AcoustIdSettings settings){
   Text="Fingerabdruck-Erkennung einrichten · AcoustID";Width=760;Height=490;MinimumSize=new Size(680,470);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
   var panel=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=2,RowCount=7};panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,185));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));Controls.Add(panel);
   panel.RowStyles.Add(new RowStyle(SizeType.Absolute,38));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,45));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,38));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,45));panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,35));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
   panel.Controls.Add(new Label{Text="Fingerabdruck-Werkzeug",AutoSize=true},0,0);Executable.Text=BundledFpcalc.Resolve(settings.Executable)??"Im Programmpaket nicht gefunden – ZIP vollständig entpacken.";panel.Controls.Add(Executable,1,0);
   var included=new Label{Text="fpcalc ist im portablen Programmpaket enthalten und wird automatisch verwendet.",Dock=DockStyle.Fill};panel.Controls.Add(included,1,1);
   panel.Controls.Add(new Label{Text="Anwendungsschlüssel",AutoSize=true},0,2);ClientKey.Text=settings.ClientKey??"";panel.Controls.Add(ClientKey,1,2);
   var links=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};links.Controls.Add(Link("Anwendung registrieren","https://acoustid.org/new-application"));panel.SetColumnSpan(links,2);panel.Controls.Add(links,0,3);
   var note=new Label{Dock=DockStyle.Fill,Text="1. fpcalc.exe ist bereits enthalten – keine zusätzliche Einrichtung nötig.\r\n2. Bei AcoustID anmelden und eine eigene Anwendung registrieren.\r\n   Den Client/API Key eintragen, nicht den Benutzer-Schlüssel für Einsendungen.\r\n\r\nOnline übertragen werden Fingerabdruck und Dauer, keine Musikdatei.\r\nDer öffentliche AcoustID-Dienst ist für nichtkommerzielle Anwendungen vorgesehen.\r\nFür betrieblichen Einsatz die Bedingungen mit dem Anbieter klären.\r\nZugangsdaten werden für dein Windows-Benutzerkonto verschlüsselt gespeichert."};panel.SetColumnSpan(note,2);panel.Controls.Add(note,0,4);
   var terms=Link("Anbieterbedingungen / gewerbliche Nutzung","https://acoustid.org/webservice");panel.SetColumnSpan(terms,2);panel.Controls.Add(terms,0,5);
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var no=new Button{Text="Abbrechen",AutoSize=true,DialogResult=DialogResult.Cancel};var save=new Button{Text="Speichern",AutoSize=true};save.Click+=(s,e)=>{try{new AcoustIdSettings{Executable=Executable.Text,ClientKey=ClientKey.Text.Trim()}.Save();DialogResult=DialogResult.OK;}catch(Exception ex){AppMessage.Show(this,ex.Message,"Einrichtung");}};buttons.Controls.Add(no);buttons.Controls.Add(save);panel.SetColumnSpan(buttons,2);panel.Controls.Add(buttons,0,6);CancelButton=no;AcceptButton=save;
  }
  LinkLabel Link(string text,string url){var link=new LinkLabel{Text=text,AutoSize=true,Margin=new Padding(0,4,22,4)};link.LinkClicked+=(s,e)=>{try{AppMessage.Show(this,"Anbieteradresse zum Kopieren:\r\n"+url,"AcoustID");}catch{AppMessage.Show(this,"Bitte im Browser öffnen:\r\n"+url);}};return link;}
 }
 public class FingerprintReviewView : ModernForm {
  public readonly DataGridView Grid=new StudioSongGrid{Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false};
  public FingerprintMatch SelectedMatch {get{Grid.EndEdit();return Grid.Rows.Cast<DataGridViewRow>().Where(row=>Convert.ToBoolean(row.Cells[0].Value)).Select(row=>row.Tag as FingerprintMatch).FirstOrDefault(match=>match!=null);}}
  bool changing;
  public FingerprintReviewView(FingerprintResult result){
   Text="Erkennungsvorschlag prüfen · AcoustID";Width=920;Height=470;MinimumSize=new Size(800,420);Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.CenterParent;
   var panel=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(12),ColumnCount=1,RowCount=4};panel.RowStyles.Add(new RowStyle(SizeType.Absolute,65));panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,32));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,45));Controls.Add(panel);
   var heading=new Label{Dock=DockStyle.Fill,Text="Datei: "+Path.GetFileName(result.Song.Source)+" · Bisher: "+result.Song.Artist+" / "+result.Song.Title+"\r\nPassenden Vorschlag anhaken und übernehmen. Trefferwerte sind keine garantierten Wahrscheinlichkeiten.\r\nNur Interpret und Titel im Projekt ändern sich; Themenalbum und Originaldatei bleiben erhalten."};panel.Controls.Add(heading,0,0);panel.Controls.Add(Grid,0,1);
   Grid.Columns.Add(new DataGridViewCheckBoxColumn{Name="Apply",HeaderText="Übernehmen",FillWeight=60});
   foreach(string name in new[]{"Interpret","Titel","Trefferwert"})Grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=name,ReadOnly=true,SortMode=DataGridViewColumnSortMode.NotSortable});
   foreach(var match in result.Matches){int index=Grid.Rows.Add(false,match.Artist,match.Title,match.Score.ToString("0.000"));Grid.Rows[index].Tag=match;}
   panel.Controls.Add(new Label{Dock=DockStyle.Fill,Text=result.Matches.Count==0?(result.Error??"Kein Treffer mit Interpret und Titel in der Datenbank gefunden."):result.Matches.Count+" Vorschlag/Vorschläge. Bitte mit einer Hörprobe abgleichen."},0,2);
   Grid.CurrentCellDirtyStateChanged+=(s,e)=>{if(Grid.IsCurrentCellDirty)Grid.CommitEdit(DataGridViewDataErrorContexts.Commit);};
   Grid.CellValueChanged+=(s,e)=>{if(changing||e.ColumnIndex!=0||e.RowIndex<0)return;var row=Grid.Rows[e.RowIndex];var pair=row.Tag as FingerprintMatch;if(pair==null||!Convert.ToBoolean(row.Cells[0].Value))return;changing=true;try{foreach(DataGridViewRow other in Grid.Rows){var p=other.Tag as FingerprintMatch;if(other!=row&&p!=null)other.Cells[0].Value=false;}}finally{changing=false;}};
   Grid.DataError+=(s,e)=>e.ThrowException=false;
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var close=new Button{Text="Verwerfen",AutoSize=true,DialogResult=DialogResult.Cancel};var apply=new Button{Text="Auswahl übernehmen",AutoSize=true};apply.Click+=(s,e)=>{if(SelectedMatch==null){AppMessage.Show(this,"Bitte mindestens einen Vorschlag zur Übernahme anhaken.");return;}DialogResult=DialogResult.OK;};buttons.Controls.Add(close);buttons.Controls.Add(apply);panel.Controls.Add(buttons,0,3);CancelButton=close;
  }
 }
}
