using System.Reflection;
[assembly: AssemblyTitle("Musikbox-Werkstatt")]
[assembly: AssemblyDescription("Eigenständige portable Windows-Anwendung · Studio · C# / .NET 8")]
[assembly: AssemblyVersion("0.64.0.0")]
[assembly: AssemblyFileVersion("0.64.0.0")]

namespace Musikbox {
 public static class AppIdentity {
  public static string Version {get{var value=typeof(AppIdentity).Assembly.GetName().Version;return "v"+value.Major+"."+value.Minor.ToString("D2");}}
  public static string WindowTitle {get{return "Musikbox-Werkstatt Studio "+Version;}}
 }
}
