using System;
using System.IO;
using System.Drawing;
using System.Threading;
using Musikbox;
class CoverLiveProbe {
 static void Main(){try{var matches=CoverSearch.Search("ABBA","",CancellationToken.None,CoverNameMode.Artist);Console.WriteLine("LIVE NAMEN: "+matches.Count+" Veröffentlichungen");}catch(Exception e){Console.WriteLine("LIVE NAMEN NICHT BESTÄTIGT: "+e.Message);}
  try{string path=CoverSearch.Download(new CoverMatch{Id="76df3287-6cda-33eb-8e9a-044b5e15ffdd"},CancellationToken.None);try{using(var image=Image.FromFile(path))Console.WriteLine("LIVE COVER: JPEG geladen und dekodiert, "+image.Width+" x "+image.Height);}finally{File.Delete(path);}}catch(Exception e){Console.WriteLine("LIVE COVER NICHT BESTÄTIGT: "+e.Message);}
 }
}
