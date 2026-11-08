using System;
using System.Linq;
using System.Collections.Generic;
using Musikbox;
namespace MusikboxTests {
 // Simulates command replies only, never claims to decode/play real audio.
 public class PlaybackTestDevice:IPlaybackCommands {
  public readonly List<string> Commands=new List<string>();
  public int Position,OpenCount;public string Mode="stopped",Length="240000",FailContains;
  public string Execute(string command){Commands.Add(command);if(FailContains!=null&&command.Contains(FailContains))throw new Exception("Simulierter Gerätefehler");
   if(command.StartsWith("open ")){OpenCount++;Position=0;Mode="stopped";}
   if(command.StartsWith("play "))Mode="playing";
   if(command.StartsWith("pause "))Mode="paused";
   if(command.StartsWith("stop "))Mode="stopped";
   if(command.StartsWith("seek ")){Position=command.EndsWith("to start")?0:Int32.Parse(command.Split(' ').Last());Mode="stopped";}
   if(command.StartsWith("status ")){if(command.EndsWith(" mode"))return Mode;if(command.EndsWith(" length"))return Length;if(command.EndsWith(" position"))return Position.ToString(System.Globalization.CultureInfo.InvariantCulture);}
   return "";
  }
 }
}
