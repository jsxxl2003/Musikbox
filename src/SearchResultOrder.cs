using System;using System.Collections;using System.ComponentModel;using System.Globalization;using System.Windows.Forms;
namespace Musikbox {
 public sealed class SearchResultOrder:IComparer {
  readonly string query,column;readonly bool descending;
  public SearchResultOrder(string query,string column,bool descending){this.query=query;this.column=column;this.descending=descending;}
  public int Compare(object a,object b){var x=(DataGridViewRow)a;var y=(DataGridViewRow)b;var first=x.Tag as Song;var second=y.Tag as Song;if(first==null||second==null)return 0;int rank=SearchText.SongRelevance(query,second).CompareTo(SearchText.SongRelevance(query,first));if(rank!=0)return rank;if(column!=null&&x.DataGridView.Columns.Contains(column)){int tie=StringComparer.CurrentCultureIgnoreCase.Compare(Convert.ToString(x.Cells[column].Value,CultureInfo.CurrentCulture),Convert.ToString(y.Cells[column].Value,CultureInfo.CurrentCulture));if(tie!=0)return descending?-tie:tie;}return StringComparer.OrdinalIgnoreCase.Compare(first.Source,second.Source);}
 }
 public partial class Controller {
  string lastRankedQuery;
  void ApplySearchResultOrder(){string query=v.SearchBox.Text;if(v.LibraryMode!="Titel"||String.IsNullOrWhiteSpace(query)||v.Songs.Rows.Count==0){if(String.IsNullOrWhiteSpace(query))lastRankedQuery=null;return;}var state=LibraryViewState.Capture(v.Songs,titleViewState);v.Songs.Sort(new SearchResultOrder(query,titleViewState.SortColumn,titleViewState.Descending));state.SortColumn=null;state.Restore(v.Songs,EditedPath,false);if(lastRankedQuery!=query)v.Songs.FirstDisplayedScrollingRowIndex=0;lastRankedQuery=query;v.ShowSearchRanking();}
 }
 public partial class MainView {public void ShowSearchRanking(){if(sortButton!=null)sortButton.Text="Relevanz ⌄";}}
}
