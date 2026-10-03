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

/// <summary>User preferences that aren't stored elsewhere (start with Windows lives in the registry).</summary>
public sealed record AppSettingsData
{
    public AppTheme Theme { get; init; } = AppTheme.System;
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
