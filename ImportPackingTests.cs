using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Musikbox;
class ImportPackingTests {
 static int count;static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Console.WriteLine("OK "+message);}
 static BoxFolder Folder(string name,int amount,string cover=null){return new BoxFolder{Name=name,Theme=Rules.Theme(name),Cover=cover,Songs=Enumerable.Range(0,amount).Select(i=>new Song{Source=name+"/"+i+".mp3",Artist=i.ToString("D3"),Title="Titel",Album="Original"}).ToList()};}
 static void Main(string[] args){string root=args[0];Directory.CreateDirectory(root);var p=new Project();p.Folders.Add(Folder("000 Country",150));p.Folders.Add(Folder("001 Country",150));p.Folders.Add(Folder("002 Rock",110));p.Folders.Add(Folder("003 Rock",40));
  var packed=Rules.Sorted(ImportPacking.Prepare(p,()=>{}));
  Check(packed.Folders.Select(f=>f.Songs.Count).SequenceEqual(new[]{99,99,99,3,99,51}),"Mehrere übervolle Quellen: Country 99/99/99/3, Rock 99/51");
  Check(packed.Folders.Where(f=>f.Theme=="Country").SelectMany(f=>f.Songs).Count()==300&&packed.Folders.Where(f=>f.Theme=="Rock").SelectMany(f=>f.Songs).Count()==150,"Verschiedene Themen bleiben getrennt");
  Check(packed.Folders.SelectMany(f=>f.Songs).Select(s=>s.Source).Distinct().Count()==450,"Kein Titel verloren oder doppelt");
  Check(p.Folders.Count==4&&p.Folders[0].Songs.Count==150&&p.Folders[0].Songs[0].Album=="Original","Eingabe einschließlich Tags bleibt unverändert");
  Check(Storage.Bytes(packed).SequenceEqual(Storage.Bytes(Rules.Sorted(packed))),"Erneutes Sortieren erhält kompakte Verteilung");
  string a=Path.Combine(root,"a.jpg"),b=Path.Combine(root,"b.jpg"),c=Path.Combine(root,"c.jpg");File.WriteAllBytes(a,new byte[]{1,2});File.Copy(a,b);File.WriteAllBytes(c,new byte[]{3,4});
  var covers=new Project();covers.Folders.Add(Folder("000 Thema",100,a));covers.Folders.Add(Folder("001 Thema",98,b));covers.Folders.Add(Folder("002 Thema",10,c));var cp=Rules.Sorted(ImportPacking.Prepare(covers,()=>{}));
  Check(cp.Folders.Select(f=>f.Songs.Count).SequenceEqual(new[]{99,99,10}),"Identische Coverdateien an verschiedenen Pfaden dürfen zusammen aufgefüllt werden");
  Check(cp.Folders.Last().Cover==c&&cp.Folders.Last().Songs.All(s=>s.Source.StartsWith("002")),"Abweichendes Themenbild bleibt bei seinen Titeln");
  bool cancel=false;try{ImportPacking.Prepare(p,()=>{throw new OperationCanceledException();});}catch(OperationCanceledException){cancel=true;}Check(cancel&&p.Folders.Count==4,"Abbruch ohne Teiländerungen");
  Check(packed.Folders.Count<=99&&packed.Folders.All(f=>f.Songs.Count<=99)&&packed.Folders.Select(f=>f.Name.Substring(0,3)).SequenceEqual(Enumerable.Range(0,packed.Folders.Count).Select(i=>i.ToString("D3"))),"Kapazität und fortlaufende Ordnernummern gültig");
  Console.WriteLine("BESTANDEN: "+count+" themengetrennte Import-Aufteilungsprüfungen");
 }
}
