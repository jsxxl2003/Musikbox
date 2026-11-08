using System;
using System.Linq;
using System.Collections.Generic;
namespace Musikbox {
 // Serialized snapshots own all nested state, including cover designs.
 public class ProjectHistory {
  class Step {public byte[] Bytes;public string Label;}
  readonly List<Step> undo=new List<Step>(),redo=new List<Step>();
  byte[] current,saved;readonly int limit;readonly long budget;
  public ProjectHistory(int limit=30,long budget=64*1024*1024){if(limit<1||budget<1)throw new ArgumentOutOfRangeException();this.limit=limit;this.budget=budget;}
  public bool CanUndo {get{return undo.Count>0;}}public bool CanRedo {get{return redo.Count>0;}}
  public string UndoLabel {get{return CanUndo?undo[undo.Count-1].Label:"";}}public string RedoLabel {get{return CanRedo?redo[redo.Count-1].Label:"";}}
  public bool IsDirty {get{return saved==null||current==null||!saved.SequenceEqual(current);}}
  public void Reset(Project project,bool isSaved){current=Storage.Bytes(project);saved=isSaved?current:null;undo.Clear();redo.Clear();}
  public void MarkSaved(){saved=current;}
  public bool Record(Project project,string label,bool force=false){var bytes=Storage.Bytes(project);if(!force&&current!=null&&current.SequenceEqual(bytes))return false;if(current!=null)undo.Add(new Step{Bytes=current,Label=label});current=bytes;redo.Clear();Trim();return true;}
  void Trim(){while(undo.Count>limit||(undo.Count>1&&undo.Sum(s=>(long)s.Bytes.Length)+redo.Sum(s=>(long)s.Bytes.Length)>budget))undo.RemoveAt(0);}
  public Project Undo(){if(!CanUndo)return null;var step=undo[undo.Count-1];var project=Storage.FromBytes(step.Bytes);undo.RemoveAt(undo.Count-1);redo.Add(new Step{Bytes=current,Label=step.Label});current=step.Bytes;return project;}
  public Project Redo(){if(!CanRedo)return null;var step=redo[redo.Count-1];var project=Storage.FromBytes(step.Bytes);redo.RemoveAt(redo.Count-1);undo.Add(new Step{Bytes=current,Label=step.Label});current=step.Bytes;Trim();return project;}
 }
}
