using System.IO;

namespace DisplayToolkit.App.Services;

internal static class AppPaths
{
    // Initialized in this order: DataDirectory depends on the two before it.
    private static readonly string PortableDirectory = Path.Combine(AppContext.BaseDirectory, "data");

    /// <summary>
    /// Portable mode, for an external drive: a <c>data</c> folder next to <c>DisplayToolkit.exe</c> holds everything
    /// instead of <c>%AppData%</c>. Decided once at startup.
    /// </summary>
    public static bool IsPortable { get; } = !IsSampleData && Directory.Exists(PortableDirectory);

    /// <summary>
    /// Settings, profiles, layout, caches and the log: <c>%AppData%\DisplayToolkit</c>, or the <c>data</c> folder in
    /// portable mode. For development, the <c>DISPLAYTOOLKIT_DATA</c> environment variable points it elsewhere (sample
    /// data for screenshots).
    /// </summary>
    public static string DataDirectory { get; } = Directory.CreateDirectory(
        IsSampleData ? Environment.GetEnvironmentVariable("DISPLAYTOOLKIT_DATA")!
            : IsPortable ? PortableDirectory
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DisplayToolkit")).FullName;

    /// <summary>Running on sample data (<c>DISPLAYTOOLKIT_DATA</c>): its rules must never change the real monitor.</summary>
    public static bool IsSampleData => Environment.GetEnvironmentVariable("DISPLAYTOOLKIT_DATA") is { Length: > 0 };

    public static string CapabilitiesCache => Path.Combine(DataDirectory, "capabilities.json");

    public static string LogFile => Path.Combine(DataDirectory, "display-toolkit.log");
}
