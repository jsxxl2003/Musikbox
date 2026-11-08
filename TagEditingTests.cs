using System;
using System.Collections.Generic;
using Musikbox;
class TagEditingTests {
 static int tests;static void Check(bool b,string text){if(!b)throw new Exception(text);tests++;Console.WriteLine("OK "+text);}
 static void Main(){var a=new Song{Artist="Alt",Title="Erster",Album="Thema",Source="Quelle.mp3",PreparedSource="Kopie.mp3",PreparedProfile="Profil"};var b=new Song{Artist="Alt",Title="Zweiter",Album="Thema"};var songs=new List<Song>{a,b};
  Check(TagEditing.Common(songs,s=>s.Artist)=="Alt"&&TagEditing.Common(songs,s=>s.Title)=="","Gemeinsame und unterschiedliche Werte unterschieden");
  Check(TagEditing.Apply(songs,false,"Neu",false,"Neu")==0&&a.Artist=="Alt"&&b.Title=="Zweiter","Nicht angehakte Felder bleiben unverändert");
  Check(TagEditing.Apply(songs,true,"  Künstler  ",false,"nicht übernehmen")==2&&a.Artist=="Künstler"&&b.Artist=="Künstler"&&a.Title=="Erster"&&b.Title=="Zweiter","Stapelbearbeitung ändert nur gewähltes Feld und trimmt Ränder");
  Check(TagEditing.Apply(songs,true,"Künstler",false,"")==0,"Unveränderte Werte zählen nicht als Änderung");
  Check(TagEditing.Apply(new[]{a},false,"",true,"Grüße 🎵")==1&&a.Title=="Grüße 🎵"&&b.Title=="Zweiter","Unicode-Titel für einzelne Auswahl");
  Check(a.Source=="Quelle.mp3"&&a.PreparedSource=="Kopie.mp3"&&a.PreparedProfile=="Profil"&&a.Album=="Thema","Quellpfad, Audio-Arbeitskopie und Themenalbum unverändert");
  Check(TagEditing.Apply(songs,true,"",false,"")==2&&a.Artist==""&&b.Artist=="","Explizites Leeren gewählter Felder möglich");
  Console.WriteLine("BESTANDEN: "+tests+" Tag-Editor-Prüfungen");
 }
}
