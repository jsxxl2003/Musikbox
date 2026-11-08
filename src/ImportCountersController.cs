using System;using System.Windows.Forms;
namespace Musikbox {public partial class Controller {
 LibraryCounter importCounter;
 readonly Timer importCountTimer=new Timer{Interval=100};
 void InitializeImportCounters(){importCountTimer.Tick+=(s,e)=>{var counter=importCounter;if(counter!=null&&!v.IsDisposed)v.SetLibraryCounters(counter.Snapshot());};}
 LibraryCounter BeginImportCounters(){var counter=new LibraryCounter();foreach(var song in AllSongs)counter.Add(song);importCounter=counter;v.SetLibraryCounters(counter.Snapshot());importCountTimer.Start();return counter;}
 void EndImportCounters(){importCountTimer.Stop();importCounter=null;if(!v.IsDisposed)v.SetLibraryCounters(LibraryCounter.FromSongs(AllSongs));}
}}
