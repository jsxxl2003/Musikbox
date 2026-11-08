using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using Musikbox;
class CoverTests {
 static int tests;static void Check(bool value,string text){if(!value)throw new Exception(text);tests++;Console.WriteLine("OK "+text);}
 static void Main(string[] args){string work=Path.GetFullPath(args[0]);Directory.CreateDirectory(work);var design=CoverDesign.Default();
  Check(CoverArt.Expand("{Nummer} {Thema} {Jahr}","031 Charts 1990","Charts 1990")=="031 Charts 1990 1990","Serien-Platzhalter für Ordner, Thema und Jahr");
  Check(CoverArt.Expand("{Jahr}","000 Rock","Rock")=="","Themen ohne Jahr erhalten keine erfundene Jahreszahl");
  var copy=CoverArt.Copy(design);copy.Texts[0].Text="Andere Vorlage";Check(design.Texts[0].Text!="Andere Vorlage","Coverentwurf unabhängig bearbeitbar");
  string jpg=Path.Combine(work,"Folder.jpg");using(var image=CoverArt.Render(design,"001 Charts 1990","Charts 1990")){Check(image.Width==1200&&image.Height==1200,"Quadratisches Cover mit 1200 Pixeln gerendert");image.Save(jpg,System.Drawing.Imaging.ImageFormat.Jpeg);}
  design.Gradient=false;design.Border=false;using(var plain=CoverArt.Render(design,"000 Test","Test",300))Check(plain.GetPixel(0,0).ToArgb()==design.Background,"Hintergrundfarbe ohne Verlauf und Rahmen");
  var project=new Project{Folders=new List<BoxFolder>{new BoxFolder{Name="000 Test",Theme="Test",CoverDesign=design,Cover=jpg}}};string file=Path.Combine(work,"cover.json");Storage.Save(project,file);var loaded=Storage.Load(file);Check(loaded.Folders[0].CoverDesign.Texts.Count==3,"Bearbeitbarer Coverentwurf im Projekt gespeichert");Check(Rules.Sorted(project).Folders[0].CoverDesign!=null,"Sortierung verliert Coverentwurf nicht");
  string source=Path.Combine(work,"source.mp3");File.WriteAllBytes(source,new byte[512]);var song=new Song{Source=source,Artist="Test",Title="Titel",Album="Thema",Artwork=jpg};string output=Path.Combine(work,"out.mp3");Tags.WriteCopy(song,"Thema",output,()=>{});
  Check(Tags.Read(output).Other.Count(x=>Encoding.ASCII.GetString(x,0,4)=="APIC")==1,"Ausgewähltes Titelcover als ID3v2.3-APIC eingebettet");
  song.Source=output;string twice=Path.Combine(work,"twice.mp3");Tags.WriteCopy(song,"Thema",twice,()=>{});Check(Tags.Read(twice).Other.Count(x=>Encoding.ASCII.GetString(x,0,4)=="APIC")==1,"Ersetzen des Covers erzeugt keine doppelten APIC-Frames");
  Check(project.Folders[0].Cover==jpg&&File.ReadAllBytes(source).All(b=>b==0),"Themenordnercover und Originaldatei unverändert");
  var matches=CoverSearch.Parse(Encoding.UTF8.GetBytes("{\"recordings\":[{\"title\":\"Titel\",\"releases\":[{\"id\":\"11111111-1111-1111-1111-111111111111\",\"title\":\"Album\",\"date\":\"1990\"},{\"id\":\"11111111-1111-1111-1111-111111111111\",\"title\":\"Doppelt\"},{\"id\":\"bad\"}]}]}"));Check(matches.Count==1&&matches[0].Release=="Album","Online-Antwort: gültige Veröffentlichungen ohne doppelte IDs");
  Check(CoverSearch.Parse(Encoding.UTF8.GetBytes("{}")).Count==0,"Leere Suchantwort ohne Absturz");
  Check(CoverSearch.Query("AC/DC","Ein \"Titel\"").Contains("\\\"Titel\\\""),"Anführungszeichen in Suchtext sicher maskiert");
  bool missing=false;try{CoverSearch.Query("","");}catch{missing=true;}Check(missing,"Leere Suchnamen vor Online-Anfrage gemeldet");
  var recognized=Recognition.Parse(Encoding.UTF8.GetBytes("{\"status\":{\"code\":0},\"metadata\":{\"music\":[{\"title\":\"Song\",\"artists\":[{\"name\":\"Artist\"}],\"album\":{\"name\":\"Album\"},\"score\":98}]}}"));Check(recognized.Artist=="Artist"&&recognized.Title=="Song"&&recognized.Score==98,"Erkennungstreffer korrekt gelesen (Offline-Antwortfixture)");
  bool noMatch=false;try{Recognition.Parse(Encoding.UTF8.GetBytes("{\"status\":{\"code\":1001,\"msg\":\"No result\"}}"));}catch{noMatch=true;}Check(noMatch,"Kein Erkennungstreffer verständlich behandelt");
  Check(Recognition.Signature("key","secret","123")=="MZLf/+bRnP3aSJS7OVafzRCl5cs=","Signatur mit unabhängig berechnetem HMAC verglichen");
  bool host=false;try{new RecognitionSettings{Host="evil.example",Key="a",Secret="b"}.Validate();}catch{host=true;}Check(host,"Erkennungsschlüssel nicht an fremde Hosts versendbar");
  var originalCoverSong=new Song{Source=source,Artist="Interpret",Title="Titel",Album="Thema"};byte[] beforeCover=File.ReadAllBytes(source);Check(TitleCoverChoice.Apply(originalCoverSong,jpg)&&originalCoverSong.Artwork==jpg,"Themenordnerbild bewusst als Titelcover zugeordnet");Check(!TitleCoverChoice.Apply(originalCoverSong,jpg),"Identische Coverwahl erzeugt keine weitere Änderung");string themed=Path.Combine(work,"themed-title.mp3");Tags.WriteCopy(originalCoverSong,"Thema",themed,()=>{});Check(Tags.Read(themed).Other.Single(x=>Encoding.ASCII.GetString(x,0,4)=="APIC").SequenceEqual(Tags.ArtworkFrame(jpg)),"Gewähltes Themenbild exakt als APIC in Titel-Exportkopie eingebettet");Check(File.ReadAllBytes(source).SequenceEqual(beforeCover)&&originalCoverSong.Album=="Thema"&&project.Folders[0].Cover==jpg,"Titelcover-Wahl verändert weder Musikquelle noch Themenname oder Ordnercover");bool missingCover=false;try{TitleCoverChoice.Apply(originalCoverSong,Path.Combine(work,"fehlt.jpg"));}catch{missingCover=true;}Check(missingCover&&originalCoverSong.Artwork==jpg,"Nicht erreichbares Ersatzcover erhält bisherige Zuordnung");
  Console.WriteLine("BESTANDEN: "+tests+" Cover- und Erkennungsprüfungen (Online-Dienste mit Fixtures)");
 }
}
