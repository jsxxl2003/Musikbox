using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Threading.Tasks;using System.Windows.Forms;
namespace Musikbox {
 public static class MetadataFilePlan {
  public static List<TagChange> Create(IEnumerable<Song> selection,IDictionary<string,string> edits){
   var result=new List<TagChange>();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach(var song in selection){string path=Path.GetFullPath(song.Source);if(!seen.Add(path))continue;
    var fields=song.PendingTags==null?new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase):new Dictionary<string,string>(song.PendingTags,StringComparer.OrdinalIgnoreCase);
    foreach(var pair in edits){if(!new[]{"artist","title","album","year","genre","comment","track","acoustid_id"}.Contains(pair.Key.ToLowerInvariant()))throw new ArgumentException("Unbekanntes Metadatenfeld: "+pair.Key);fields[pair.Key]=(pair.Value??"").Trim();}
    result.Add(new TagChange{Song=song,Source=path,Target=path,Fields=fields,ArtworkPath=String.IsNullOrEmpty(song.Artwork)?null:song.Artwork,Description="Metadaten in Originaldatei speichern"});
   }return result;
  }
 }
 public partial class Controller {
  async void SaveDialogTagChanges(List<Song> songs,Dictionary<string,string> edits){if(busy)return;bool started=false;try{var plan=MetadataFilePlan.Create(songs,edits);SetBusy(true);started=true;string root=Path.Combine(PortablePaths.Data,"Datei-Rueckgaengig");var action=await Task.Run(()=>FileTagAction.Apply(plan,root));fileActions[action.Id]=action;InvalidateArtwork();Changed("Dateiaktion:"+action.Id,true);PopulateLibrary();}catch(Exception ex){AppMessage.Show(v,ex.Message,"Tags konnten nicht gespeichert werden");}finally{if(started)SetBusy(false);}}

  async void SaveSideTagsToFiles(){
   if(busy)return;var selected=sideSelection.ToList();if(selected.Count==0){AppMessage.Show(v,"Bitte zuerst Titel markieren.");return;}
   var fields=new[]{v.TagEditor.Artist,v.TagEditor.Title,v.TagEditor.Album,v.TagEditor.Year,v.TagEditor.Genre,v.TagEditor.Comment};
   var flags=new[]{v.TagEditor.ChangeArtist,v.TagEditor.ChangeTitle,v.TagEditor.ChangeAlbum,v.TagEditor.ChangeYear,v.TagEditor.ChangeGenre,v.TagEditor.ChangeComment};
   var texts=fields.Select(x=>x.Text).ToArray();var checkedFields=flags.Select(x=>x.Checked).ToArray();string[] names={"artist","title","album","year","genre","comment"};
   var edits=new Dictionary<string,string>();for(int i=0;i<names.Length;i++)if(checkedFields[i])edits[names[i]]=texts[i];if(v.TagEditor.ChangeTrack.Checked)edits["track"]=v.TagEditor.Track.Text;if(v.TagEditor.ChangeAcoustid.Checked)edits["acoustid_id"]=v.TagEditor.Acoustid.Text;
   if(edits.Any(x=>(x.Key=="artist"||x.Key=="title"||x.Key=="album")&&String.IsNullOrWhiteSpace(x.Value))&&AppMessage.Show(v,"Ein geändertes Feld ist leer. Dieses Feld bei den ausgewählten Dateien leeren?","Tags leeren",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
   // The button must not silently claim a successful file write when no tag was changed.
   if(edits.Count==0&&!selected.Any(song=>song.PendingTags!=null&&song.PendingTags.Count>0)){
    v.Status.Text="Keine Metadatenänderungen zum Speichern.";return;
   }
   bool started=false,saved=false;int written=0;
   try{
    var plan=MetadataFilePlan.Create(selected,edits);written=plan.Count;string backupRoot=Path.Combine(PortablePaths.Data,"Datei-Rueckgaengig");
    // Explicit file save owns these edits; do not commit them into project history first.
    sideCommitting=true;try{SetBusy(true);started=true;}finally{sideCommitting=false;}
    v.Status.Text="Metadaten werden in "+written+" Dateien gespeichert …";
    var action=await Task.Run(()=>FileTagAction.Apply(plan,backupRoot));fileActions[action.Id]=action;InvalidateArtwork();
    v.TagEditor.ShowSelection(selected,Current==null?"":Current.Theme);Changed("Dateiaktion:"+action.Id,true);PopulateLibrary();saved=true;
   }catch(Exception ex){AppMessage.Show(v,ex.Message,"Metadaten konnten nicht gespeichert werden");}
   finally{if(started){SetBusy(false);if(!saved){for(int i=0;i<fields.Length;i++){fields[i].Text=texts[i];flags[i].Checked=checkedFields[i];}}}}
   if(saved)v.Status.Text="Metadaten in "+written+" Dateien gespeichert";
  }
 }
}
