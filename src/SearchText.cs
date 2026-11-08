using System;using System.Linq;using System.Text.RegularExpressions;using System.Collections.Generic;
namespace Musikbox {public static class SearchText {
 public static string Clean(string text){string s=text??"",old;do{old=s;s=Regex.Replace(s,@"\([^()]*\)|\[[^\[\]]*\]"," ");}while(s!=old);return Regex.Replace(s,@"\s+"," ").Trim();}
 public static string[] Tokens(string text){return Regex.Matches(LibrarySearch.Normalize(Clean(text)),@"[\p{L}\p{N}]+").Cast<Match>().Select(m=>m.Value).ToArray();}
 static readonly HashSet<string> joins=new HashSet<string>(new[]{"feat","ft","featuring","x","and","und","with","mit","vs","versus"});
 public static string[] Meaningful(string text){return Tokens(text).Where(t=>!joins.Contains(t)).ToArray();}
 public static string[] QueryTerms(string text){return Regex.Split(Clean(text),@"\s+").Where(t=>t.Length>0&&t!="&"&&t!="+"&&!joins.Contains(t.ToLowerInvariant().TrimEnd('.'))).ToArray();}
 public static string[] Artists(string text){string clean=Clean(text);var result=new List<string>{clean};if(new[]{"AC/DC","Earth, Wind & Fire","Simon & Garfunkel","Kool & The Gang","Florence and the Machine"}.Any(s=>LibrarySearch.Compact(s)==LibrarySearch.Compact(clean)))return result.ToArray();result.AddRange(Regex.Split(clean,@"\s+(?:feat\.?|ft\.?|featuring|with|mit|vs\.?|x|&|\+|and|und)\s+",RegexOptions.IgnoreCase));return result.Select(s=>s.Trim()).Where(s=>s.Length>0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();}
 public static double Relevance(string q,string candidate){var wanted=Meaningful(q);var words=Tokens(candidate);return wanted.Length==0?0:wanted.Average(a=>words.Select(b=>a==b?1d:b.Contains(a)?.86:LibrarySearch.Sound(a)==LibrarySearch.Sound(b)?.8:Math.Min(a.Length,b.Length)>=4&&LibrarySearch.Near(LibrarySearch.Sound(a),LibrarySearch.Sound(b),Math.Max(a.Length,b.Length)>=8?2:1)?.67:0).DefaultIfEmpty(0).Max());}
}}
