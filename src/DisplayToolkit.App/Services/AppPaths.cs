using System.IO;

namespace DisplayToolkit.App.Services;

internal static class AppPaths
{
    /// <summary><c>%AppData%\DisplayToolkit</c>: settings, profiles, layout and caches.</summary>
    public static string DataDirectory { get; } = Directory.CreateDirectory(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DisplayToolkit")).FullName;

    public static string CapabilitiesCache => Path.Combine(DataDirectory, "capabilities.json");

    public static string LogFile => Path.Combine(DataDirectory, "display-toolkit.log");
}
