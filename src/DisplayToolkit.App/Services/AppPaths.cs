using System.IO;

namespace DisplayToolkit.App.Services;

internal static class AppPaths
{
    private static readonly string AppDataDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DisplayToolkit");

    /// <summary>Portable mode, for an external drive: a <c>data</c> folder next to <c>DisplayToolkit.exe</c>.</summary>
    private static readonly string PortableDirectory = Path.Combine(AppContext.BaseDirectory, "data");

    /// <summary>Where the installer puts the app (<c>INSTALLFOLDER</c> in <c>installer/Package.wxs</c>).</summary>
    private static readonly string InstallDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Display Toolkit");

    private static string? _dataDirectory;

    /// <summary>
    /// Settings, profiles, layout, caches and the log: <c>%AppData%\DisplayToolkit</c>, or the <c>data</c> folder next to
    /// the exe in portable mode. Decided and created on first use, so <see cref="IsFirstPortableStart"/> comes before it.
    /// For development, the <c>DISPLAYTOOLKIT_DATA</c> environment variable points it elsewhere (sample data for
    /// screenshots).
    /// </summary>
    public static string DataDirectory => _dataDirectory ??= Directory.CreateDirectory(
        IsSampleData ? Environment.GetEnvironmentVariable("DISPLAYTOOLKIT_DATA")!
            : Directory.Exists(PortableDirectory) ? PortableDirectory
            : AppDataDirectory).FullName;

    /// <summary>Settings live in the <c>data</c> folder next to the exe instead of <c>%AppData%</c>.</summary>
    public static bool IsPortable => string.Equals(DataDirectory, Path.GetFullPath(PortableDirectory), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The first start of a copy that wasn't installed (the zip), with no settings anywhere yet: the user picks where
    /// they go. Once either folder exists, the choice is made. The installed app always uses <c>%AppData%</c>.
    /// </summary>
    public static bool IsFirstPortableStart => _dataDirectory is null && !IsSampleData && !IsInstalled
        && !Directory.Exists(PortableDirectory) && !Directory.Exists(AppDataDirectory);

    /// <summary>Running on sample data (<c>DISPLAYTOOLKIT_DATA</c>): its rules must never change the real monitor.</summary>
    public static bool IsSampleData => Environment.GetEnvironmentVariable("DISPLAYTOOLKIT_DATA") is { Length: > 0 };

    public static string CapabilitiesCache => Path.Combine(DataDirectory, "capabilities.json");

    public static string LogFile => Path.Combine(DataDirectory, "display-toolkit.log");

    /// <summary>Turns portable mode on. Throws <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/> if the app's folder is read-only.</summary>
    public static void CreatePortableFolder() => Directory.CreateDirectory(PortableDirectory);

    private static bool IsInstalled =>
        string.Equals(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory), InstallDirectory, StringComparison.OrdinalIgnoreCase);
}
