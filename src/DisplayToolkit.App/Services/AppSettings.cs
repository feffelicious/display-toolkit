using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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

/// <summary>User preferences that aren't stored elsewhere (start with Windows lives in the registry).</summary>
public sealed record AppSettingsData
{
    public AppTheme Theme { get; init; } = AppTheme.System;

    /// <summary>Show a small overlay when a shortcut switches a profile.</summary>
    public bool ShowShortcutOverlay { get; init; } = true;

    /// <summary>Use the location from Windows for sunrise and sunset; otherwise <see cref="ManualLocation"/>.</summary>
    public bool UseWindowsLocation { get; init; } = true;

    public SavedLocation? ManualLocation { get; init; }

    /// <summary>The last position Windows reported, for when it can't be asked (location turned off, offline).</summary>
    public SavedLocation? LastWindowsLocation { get; init; }
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
