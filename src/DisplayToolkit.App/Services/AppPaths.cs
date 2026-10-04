using System.IO;

namespace DisplayToolkit.App.Services;

internal static class AppPaths
{
    /// <summary>
    /// <c>%AppData%\DisplayToolkit</c>: settings, profiles, layout and caches. For development, the
    /// <c>DISPLAYTOOLKIT_DATA</c> environment variable points it elsewhere (sample data for screenshots).
    /// </summary>
    public static string DataDirectory { get; } = Directory.CreateDirectory(
        Environment.GetEnvironmentVariable("DISPLAYTOOLKIT_DATA") is { Length: > 0 } custom
            ? custom
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DisplayToolkit")).FullName;

    /// <summary>Running on sample data (<c>DISPLAYTOOLKIT_DATA</c>): its rules must never change the real monitor.</summary>
    public static bool IsSampleData => Environment.GetEnvironmentVariable("DISPLAYTOOLKIT_DATA") is { Length: > 0 };

    public static string CapabilitiesCache => Path.Combine(DataDirectory, "capabilities.json");

    public static string LogFile => Path.Combine(DataDirectory, "display-toolkit.log");
}
