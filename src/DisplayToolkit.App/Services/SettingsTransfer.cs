using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using DisplayToolkit.Automation.Storage;

namespace DisplayToolkit.App.Services;

/// <summary>
/// Exports and imports everything the user set up in one file: preferences, profiles and rules, and the quick settings
/// layout, for every monitor model. Machine-specific things stay out: window position, the last Windows location
/// and the capabilities cache.
/// </summary>
internal sealed class SettingsTransfer(AppSettings settings, AutomationStore automationStore, AutomationService automation, LayoutStore layouts)
{
    private const int FormatVersion = 1;

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public void Export(string path)
    {
        var portable = settings.Current with { MainWindowPlacement = null, LastWindowsLocation = null };
        var bundle = new JsonObject
        {
            ["format"] = FormatVersion,
            ["exportedBy"] = $"Display Toolkit {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}",
            ["settings"] = JsonNode.Parse(JsonSerializer.Serialize(portable, SettingsJsonContext.Default.AppSettingsData)),
            ["automation"] = JsonNode.Parse(AutomationJson.Serialize(automationStore.GetAll())),
            ["layouts"] = JsonNode.Parse(JsonSerializer.Serialize(layouts.GetAll(), LayoutJsonContext.Default.DictionaryStringListString)),
        };
        File.WriteAllText(path, bundle.ToJsonString(Indented));
    }

    /// <summary>Replaces the current setup with the file's. Returns false if the file isn't an export.</summary>
    /// <exception cref="IOException">The file couldn't be read.</exception>
    public bool Import(string path)
    {
        JsonObject bundle;
        AppSettingsData imported;
        Dictionary<string, MonitorAutomation> automations;
        Dictionary<string, List<string>> layoutData;
        try
        {
            bundle = JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? throw new JsonException("Not an object.");
            if (bundle["format"]?.GetValue<int>() is not FormatVersion)
            {
                return false;
            }
            imported = bundle["settings"]?.Deserialize(SettingsJsonContext.Default.AppSettingsData) ?? new AppSettingsData();
            automations = bundle["automation"] is { } automationNode ? AutomationJson.Deserialize(automationNode.ToJsonString()) : [];
            layoutData = bundle["layouts"]?.Deserialize(LayoutJsonContext.Default.DictionaryStringListString) ?? [];
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException)
        {
            return false;
        }

        // Keep this PC's own things.
        settings.Update(current => imported with
        {
            MainWindowPlacement = current.MainWindowPlacement,
            LastWindowsLocation = current.LastWindowsLocation,
        });
        automationStore.SetAll(automations);
        layouts.ReplaceAll(layoutData);
        automation.Reload();
        return true;
    }
}
