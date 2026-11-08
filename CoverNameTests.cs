using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Threading;
using Musikbox;
class CoverNameTests {
 static int tests;static void Check(bool yes,string text){if(!yes)throw new Exception(text);tests++;Console.WriteLine("OK "+text);}
 static bool Fails(Action a){try{a();return false;}catch{return true;}}
 static string Decode(string value){return Uri.UnescapeDataString(value);}
 static void Main(string[] args){string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
  Check(CoverSearch.Query("Alan Parsons","").StartsWith("artist:")&&!CoverSearch.Query("Alan Parsons","").Contains("recording:"),"Nur Interpretenname ohne Titel möglich");
  Check(CoverSearch.Query("","Don't Answer Me").StartsWith("recording:"),"Nur Titelname ohne Interpreten möglich");
  Check(CoverSearch.Query("Parsons Alan","Answer Me").Contains("(\"Parsons\" OR /.*parsons.*/) AND (\"Alan\" OR /.*alan.*/)")&&!CoverSearch.Query("Parsons Alan","Answer Me").Contains("Parsons Alan\""),"Namen werden als Wörter statt starre vollständige Phrase gesucht");
  Check(CoverSearch.Query("AC/DC","Ein \"Titel\"").Contains("\\\"Titel\\\""),"Anführungszeichen in Namen bleiben als Text maskiert");
  Check(CoverSearch.Query("A OR B","").Contains("\"OR\""),"Suchoperatoren in Namen werden als Namen behandelt");
  Check(CoverSearch.Query("","Don’t Answer Me").Contains("Don't"),"Typografischer Apostroph für Namenssuche normalisiert");
  Check(Fails(()=>CoverSearch.Query(" ","")),"Leere Namenssuche vor Netzzugriff abgewiesen");
  Check(Fails(()=>CoverSearch.Query(new string('x',251),"")),"Überlange Suchnamen verständlich abgewiesen");
  string fragments=Decode(CoverSearch.SearchUrl("","",CoverNameMode.FreeName,"PAR ans"));Check(fragments.Contains("/.*par.*/")&&fragments.Contains("/.*ans.*/")&&fragments.Contains(" AND "),"Freie Suche kombiniert Wortteile feldübergreifend und normalisiert Großschreibung");
  Check(!CoverSearch.Query("a.* OR x","").Contains("/.*a.*"),"Eingegebene Suchsyntax wird nicht als regulärer Ausdruck ausgeführt");
  string artistUrl=Decode(CoverSearch.SearchUrl("ABBA",@"C:\Privat\falsch.mp3",CoverNameMode.Artist));Check(artistUrl.Contains("/release-group/")&&artistUrl.Contains("ABBA")&&!artistUrl.Contains("Privat")&&!artistUrl.Contains("falsch"),"Nur-Interpret-Modus ignoriert ausdrücklich Titel/Dateinamentext und sucht unterschiedliche Singles/Alben");
  Check(Decode(CoverSearch.SearchUrl("Falscher Name","Dancing Queen",CoverNameMode.Title)).Contains("recording:")&&!Decode(CoverSearch.SearchUrl("Falscher Name","Dancing Queen",CoverNameMode.Title)).Contains("Falscher"),"Nur-Titel-Modus ignoriert falsche Interpretenangabe");
  Check(CoverSearch.SearchUrl("ABBA","").Contains("release-group")&&CoverSearch.SearchUrl("","Dancing Queen").Contains("recording"),"Automatischer Modus unterstützt einseitig bekannte Namen");
  string free=Decode(CoverSearch.SearchUrl("Ignorieren","Auch ignorieren",CoverNameMode.FreeName,"ABBA Waterloo"));Check(free.Contains("artist:")&&free.Contains("recording:")&&free.Contains("release:")&&free.Contains("Waterloo")&&!free.Contains("Ignorieren"),"Freie Namen durchsuchen Interpret, Titel und Veröffentlichungsnamen");
  Check(!Decode(CoverSearch.BrowserUrl("ABBA","Geheimer Dateiname",CoverNameMode.Artist)).Contains("Geheimer")&&CoverSearch.BrowserUrl("AC/DC","",CoverNameMode.Artist).StartsWith("https://www.bing.com/images/search?"),"Browser-Ausweichsuche übermittelt nur die gewählte Namensart");
  string group="11111111-1111-1111-1111-111111111111",release="22222222-2222-2222-2222-222222222222";
  string json="{\"release-groups\":[{\"id\":\""+group+"\",\"title\":\"Waterloo\",\"primary-type\":\"Single\",\"first-release-date\":\"1974\",\"artist-credit\":[{\"artist\":{\"name\":\"ABBA\"}}]},null,{\"id\":\"bad\"}]}";
  var parsed=CoverSearch.Parse(Encoding.UTF8.GetBytes(json));Check(parsed.Count==1&&parsed[0].IsGroup&&parsed[0].Artist=="ABBA"&&parsed[0].Type=="Single","Interpreten-Suchantwort enthält Künstler, Veröffentlichungsname und Typ");
  Check(parsed[0].ToString().Contains("ABBA")&&parsed[0].ToString().Contains("Waterloo"),"Trefferliste zeigt Namen statt undurchsichtiger Dateinamen");
  string editions="{\"recordings\":[{\"title\":\"Song\",\"artist-credit\":[{\"name\":\"Interpret\"}],\"releases\":[{\"id\":\""+release+"\",\"title\":\"Album\",\"release-group\":{\"id\":\""+group+"\",\"primary-type\":\"Album\"}},{\"id\":\"33333333-3333-3333-3333-333333333333\",\"title\":\"Zweite Ausgabe\",\"release-group\":{\"id\":\""+group+"\"}}]}]}";
  var match=CoverSearch.Parse(Encoding.UTF8.GetBytes(editions)).Single();Check(match.GroupId==group&&match.Artist=="Interpret"&&!match.IsGroup,"Mehrere Ausgaben derselben Veröffentlichung zusammengefasst; Gruppenfallback verfügbar");
  Check(CoverSearch.Parse(Encoding.UTF8.GetBytes("{\"recordings\":[null,{\"releases\":[null]}],\"release-groups\":[]}")).Count==0,"Unvollständige Suchtreffer ohne Absturz ignoriert");
  string captured=null;var results=CoverSearch.Search("ABBA","",CancellationToken.None,CoverNameMode.Artist,null,(url,token,max)=>{captured=url;return Encoding.UTF8.GetBytes(json);});Check(results.Count==1&&captured.Contains("release-group")&&captured.Contains("fmt=json"),"Suchablauf nutzt richtigen Endpunkt mit Namensabfrage (Offline-Transport)");
  bool html=false;try{CoverSearch.Search("ABBA","",CancellationToken.None,CoverNameMode.Artist,null,(u,t,m)=>Encoding.UTF8.GetBytes("<html>Site Unavailable</html>"));}catch(Exception e){html=e.Message.Contains("ungültige Antwort");}Check(html,"HTTP-200-Webseite statt JSON als Dienstfehler erklärt");
  byte[] image;using(var bitmap=new Bitmap(40,40))using(var stream=new MemoryStream()){bitmap.Save(stream,System.Drawing.Imaging.ImageFormat.Png);image=stream.ToArray();}
  int calls=0;string path=CoverSearch.Download(match,CancellationToken.None,(url,token,max)=>{calls++;if(calls==1){Check(url.Contains("/release/"),"Zuerst Cover der genauen Ausgabe versucht");throw new CoverMissingException();}Check(url.Contains("/release-group/"+group),"Fehlendes Ausgabecover über Album-/Single-Gruppe ergänzt");return image;});Check(calls==2&&File.Exists(path)&&match.GroupCoverUsed,"Gruppen-Cover wird geladen und als andere mögliche Ausgabe markiert");File.Delete(path);
  calls=0;Check(Fails(()=>CoverSearch.Download(match,CancellationToken.None,(u,t,m)=>{calls++;throw new Exception("offline");}))&&calls==1,"Netzwerkfehler nicht als angeblich fehlendes Cover verschleiert");
  Check(Fails(()=>CoverSearch.SaveImage(Encoding.UTF8.GetBytes("<html>Fehler</html>"))),"HTML-Antwort statt Bild verständlich abgewiesen");
  string source=Path.Combine(root,"eigenes.png");File.WriteAllBytes(source,image);string imported=CoverSearch.ImportImage(source);Check(File.Exists(imported)&&File.ReadAllBytes(source).SequenceEqual(image),"Manuell ausgewähltes Cover als Arbeitskopie geladen, Quelle erhalten");File.Delete(imported);
  Check(CoverSearch.Redirect(new Uri("https://coverartarchive.org/release/id/front-500"),"http://archive.org/image.jpg").AbsoluteUri=="https://archive.org/image.jpg","Historische HTTP-Bildweiterleitung vor Abruf auf HTTPS angehoben");
  Check(CoverSearch.Redirect(new Uri("https://archive.org/a/"),"../b.jpg").AbsoluteUri=="https://archive.org/b.jpg","Relative HTTPS-Weiterleitung korrekt aufgelöst");
  Check(Fails(()=>CoverSearch.Redirect(new Uri("https://archive.org/"),"file:///private"))&&Fails(()=>CoverSearch.Redirect(new Uri("https://archive.org/"),"")),"Ungültige Bildweiterleitungen abgewiesen");
  using(var cancel=new CancellationTokenSource()){cancel.Cancel();bool canceled=false;try{CoverSearch.Search("ABBA","",cancel.Token);}catch(OperationCanceledException){canceled=true;}Check(canceled,"Abgebrochene Namenssuche startet keine Anfrage");}
  Console.WriteLine("BESTANDEN: "+tests+" Namens-/Coversuche-Prüfungen; Transport mit Offline-Antworten");
 }
}
