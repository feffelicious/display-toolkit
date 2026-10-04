using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DisplayToolkit.Automation.Profiles;
using Microsoft.Extensions.Logging;

namespace DisplayToolkit.App.Services;

public enum AppTheme
{
    System,
    Light,
    Dark,
}

/// <summary>A position in degrees, north and east positive.</summary>
public sealed record SavedLocation(double Latitude, double Longitude);

/// <summary>Where the main window was, in device-independent pixels (its normal size when it was maximized).</summary>
public sealed record WindowPlacement(double Left, double Top, double Width, double Height, bool IsMaximized);

/// <summary>User preferences that aren't stored elsewhere (start with Windows lives in the registry).</summary>
/// <remarks>
/// Properties with defaults are settable rather than init-only: the JSON source generator assigns every init-only
/// property when reading, so a file written by an older version would replace the defaults with empty values.
/// </remarks>
public sealed record AppSettingsData
{
    public AppTheme Theme { get; set; } = AppTheme.System;

    /// <summary>Show a small overlay when a shortcut switches a profile or changes a setting.</summary>
    public bool ShowShortcutOverlay { get; set; } = true;

    /// <summary>
    /// App shortcuts by action id (<see cref="ShortcutService"/>). A missing action has its default; null means the
    /// user removed it.
    /// </summary>
    public Dictionary<string, Shortcut?> Shortcuts { get; set; } = [];

    /// <summary>The mouse wheel over the tray icon changes brightness.</summary>
    public bool ScrollOverTrayIcon { get; set; } = true;

    /// <summary>How dark Target mode makes everything but the window in use, from 0 to 1.</summary>
    public double TargetModeDim { get; set; } = 0.6;

    /// <summary>The device path (<c>MonitorId.Instance</c>) of the monitor the app controls, when there are several.</summary>
    public string? SelectedMonitor { get; init; }

    /// <summary>
    /// Use the location from Windows for sunrise and sunset; otherwise <see cref="ManualLocation"/>. Off until the user
    /// turns it on: asking Windows can show its own location prompts.
    /// </summary>
    public bool UseWindowsLocation { get; set; }

    public SavedLocation? ManualLocation { get; init; }

    /// <summary>The last position Windows reported, for when it can't be asked (location turned off, offline).</summary>
    public SavedLocation? LastWindowsLocation { get; init; }

    public WindowPlacement? MainWindowPlacement { get; init; }

    /// <summary>Look for a newer release on GitHub once a day.</summary>
    public bool CheckForUpdates { get; set; } = true;

    public DateTimeOffset? LastUpdateCheck { get; init; }

    /// <summary>The version whose reminder the user closed in quick settings ("1.1.0").</summary>
    public string? DismissedUpdateVersion { get; init; }

    /// <summary>The version a Windows notification was already shown for, so it's shown once.</summary>
    public string? NotifiedUpdateVersion { get; init; }
}

/// <summary>Loads and saves <c>%AppData%\DisplayToolkit\settings.json</c>.</summary>
internal sealed class AppSettings(ILogger<AppSettings> logger)
{
    private static string FilePath => Path.Combine(AppPaths.DataDirectory, "settings.json");

    private AppSettingsData? _current;

    public event EventHandler? Changed;

    public AppSettingsData Current => _current ??= Load();

    public void Update(Func<AppSettingsData, AppSettingsData> change)
    {
        _current = change(Current);
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_current, SettingsJsonContext.Default.AppSettingsData));
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Couldn't save settings");
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private AppSettingsData Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize(File.ReadAllText(FilePath), SettingsJsonContext.Default.AppSettingsData) ?? new();
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            logger.LogWarning(exception, "Ignoring unreadable settings");
        }
        return new();
    }
}

[JsonSerializable(typeof(AppSettingsData))]
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
