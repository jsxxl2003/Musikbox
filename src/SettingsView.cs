using System;
using System.Drawing;
using System.Windows.Forms;
namespace Musikbox {
 public class SettingsView:ModernForm {
  public readonly TabControl Tabs=new StudioTabControl{Dock=DockStyle.Fill};
  public string SelectedCommand {get;private set;}
  public SettingsView(bool hasFolder,bool hasSong){
   Text="Einstellungen · Werkzeuge und Funktionen";Width=780;Height=470;MinimumSize=new Size(680,420);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);Padding=new Padding(12);
   AddPage("Audio","MP3Gain und FFmpeg","Lautstärke-Priorität, Zielpegel sowie MP3-Bitrate einstellen.\r\nIm Audio-Dialog stehen Windows-Downloadlinks und die Dateiauswahl bereit.\r\nZIP vollständig entpacken und anschließend das Programm auswählen.",new[]{"Audio-Einstellungen"},new[]{true});
   AddPage("Musikerkennung","Einzelne unbekannte Titel erkennen","fpcalc.exe ist im Programmpaket enthalten und wird automatisch verwendet.\r\nFür AcoustID nur den eigenen Anwendungsschlüssel hinterlegen.\r\nACRCloud ist eine getrennte Alternative mit eigenem Zugang.\r\nDie Erkennung startet ausschließlich auf deinen Aufruf für einen Titel.",new[]{"Fingerabdruck einrichten","ACRCloud-Zugang"},new[]{true,true});
   AddPage("Cover gestalten","Eigene Themenordner-Cover","Der Cover-Designer ist direkt eingebaut; kein Zusatzdownload nötig.\r\nTexte, Farben, Bildhintergründe und Serienvorlagen lassen sich bearbeiten.\r\nFür die Bearbeitung zuerst im Hauptfenster einen Themenordner auswählen.",new[]{"Cover gestalten","Cover-Serie"},new[]{hasFolder,hasFolder});
   AddPage("Cover suchen","Titelcover im Internet finden","Die Online-Coversuche ist direkt eingebaut; kein Zusatzdownload nötig.\r\nSie sucht anhand von Interpret und Titel bei MusicBrainz und Cover Art Archive.\r\nNach einer Musikerkennung zuerst die passenden Tags übernehmen.\r\nWahl zwischen Single-Cover und Themenordnercover vor jeder Zuordnung.\r\nNur die Online-Suche benötigt Internet.",new[]{"Cover online suchen"},new[]{hasSong});
   AddPage("Projektschutz","Rückgängig und automatische Sicherungen","Projektänderungen können über Projekt → Rückgängig / Wiederholen\r\nzurückgenommen werden. Der Verlauf gilt bis zum Öffnen eines anderen Projekts.\r\nAutomatische Sicherungen folgen nach übernommenen Änderungen und minütlich.\r\nDie letzten 20 Stände je Projekt bleiben erhalten, ohne Musik- oder Bilddateien.",new[]{"Projektsicherungen","Dateien wiederfinden"},new[]{true,true});
   var close=new Button{Text="Schließen",Dock=DockStyle.Bottom,Height=36,DialogResult=DialogResult.Cancel};Controls.Add(Tabs);Controls.Add(close);CancelButton=close;
  }
  void AddPage(string title,string heading,string description,string[] commands,bool[] enabled){var page=new TabPage(title);var panel=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(20),ColumnCount=1,RowCount=3};panel.RowStyles.Add(new RowStyle(SizeType.Absolute,40));panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,85));page.Controls.Add(panel);panel.Controls.Add(new Label{Text=heading,Dock=DockStyle.Fill,Font=new Font(Font,FontStyle.Bold)},0,0);panel.Controls.Add(new Label{Text=description,Dock=DockStyle.Fill},0,1);var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill};for(int i=0;i<commands.Length;i++){string command=commands[i];var button=new Button{Text=command,AutoSize=true,Height=34,Enabled=enabled[i]};button.Click+=(s,e)=>{SelectedCommand=command;DialogResult=DialogResult.OK;};buttons.Controls.Add(button);}panel.Controls.Add(buttons,0,2);Tabs.TabPages.Add(page);}
 }
}
