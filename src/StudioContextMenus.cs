using System;
using System.Drawing;
using System.Windows.Forms;
namespace Musikbox {
 public static class StudioContextMenus {
  static ContextMenuStrip Make(Control owner,Func<bool> canUndo,Action undo,Func<bool> canCut,Action cut,Func<bool> canCopy,Action copy,Func<bool> canPaste,Action paste,Func<bool> canDelete,Action delete,Action selectAll) {
   var menu=new ContextMenuStrip();
   var undoItem=menu.Items.Add("Rückgängig",null,(s,e)=>undo());
   menu.Items.Add(new ToolStripSeparator());
   var cutItem=menu.Items.Add("Ausschneiden",null,(s,e)=>cut());
   var copyItem=menu.Items.Add("Kopieren",null,(s,e)=>copy());
   var pasteItem=menu.Items.Add("Einfügen",null,(s,e)=>paste());
   var deleteItem=menu.Items.Add("Löschen",null,(s,e)=>delete());
   menu.Items.Add(new ToolStripSeparator());
   var allItem=menu.Items.Add("Alle auswählen",null,(s,e)=>selectAll());
   menu.Opening+=(s,e)=>{
    undoItem.Enabled=canUndo();cutItem.Enabled=canCut();copyItem.Enabled=canCopy();
    pasteItem.Enabled=canPaste();deleteItem.Enabled=canDelete();allItem.Enabled=owner.TextLengthSafe()>0;
   };
   ModernTheme.Apply(menu);
   return menu;
  }
  static int TextLengthSafe(this Control c){return (c.Text??"").Length;}
  public static void AttachText(TextBoxBase t){
   t.ShortcutsEnabled=true;
   t.ContextMenuStrip=Make(t,()=>t.CanUndo,()=>t.Undo(),()=>!t.ReadOnly&&t.SelectionLength>0,()=>t.Cut(),()=>t.SelectionLength>0,()=>t.Copy(),()=>!t.ReadOnly&&Clipboard.ContainsText(),()=>t.Paste(),()=>!t.ReadOnly&&t.SelectionLength>0,()=>t.SelectedText="",()=>t.SelectAll());
  }
  public static void AttachEditableCombo(ComboBox c){
   c.ContextMenuStrip=Make(c,()=>false,()=>{},()=>c.SelectionLength>0,()=>{Clipboard.SetText(c.SelectedText);c.SelectedText="";},()=>c.SelectionLength>0,()=>Clipboard.SetText(c.SelectedText),()=>Clipboard.ContainsText(),()=>c.SelectedText=Clipboard.GetText(),()=>c.SelectionLength>0,()=>c.SelectedText="",()=>c.SelectAll());
  }
  public static void AppendEditingCommands(ContextMenuStrip menu){
   menu.Items.Add(new ToolStripSeparator());
   foreach(string label in new[]{"Rückgängig","Ausschneiden","Kopieren","Einfügen","Löschen","Alle auswählen"}){
    var item=menu.Items.Add(label);item.Enabled=false;
   }
  }
 }
}
