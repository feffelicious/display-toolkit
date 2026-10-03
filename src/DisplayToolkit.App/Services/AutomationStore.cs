using System.IO;
using System.Text.Json;
using DisplayToolkit.Automation.Storage;
using Microsoft.Extensions.Logging;

namespace DisplayToolkit.App.Services;

/// <summary>Profiles and rules per monitor model, in <c>%AppData%\DisplayToolkit\automation.json</c>.</summary>
internal sealed class AutomationStore(ILogger<AutomationStore> logger)
{
    private static string FilePath => Path.Combine(AppPaths.DataDirectory, "automation.json");

    private Dictionary<string, MonitorAutomation>? _data;

    public MonitorAutomation Get(string model) => Data.GetValueOrDefault(model) ?? MonitorAutomation.Empty;

    public IReadOnlyDictionary<string, MonitorAutomation> GetAll() => Data;

    public void Set(string model, MonitorAutomation automation)
    {
        Data[model] = automation;
        Save();
    }

    /// <summary>Replaces everything (an import).</summary>
    public void SetAll(Dictionary<string, MonitorAutomation> data)
    {
        _data = data;
        Save();
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(FilePath, AutomationJson.Serialize(Data));
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Couldn't save profiles and rules");
        }
    }

    private Dictionary<string, MonitorAutomation> Data => _data ??= Load();

    private Dictionary<string, MonitorAutomation> Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return AutomationJson.Deserialize(File.ReadAllText(FilePath));
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            // Keep the unreadable file for the user (and a bug report) instead of overwriting it on the next save.
            logger.LogWarning(exception, "Ignoring unreadable profiles and rules");
            TryKeepCopy();
        }
        return [];
    }

    private void TryKeepCopy()
    {
        try
        {
            File.Copy(FilePath, FilePath + ".unreadable", overwrite: true);
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Couldn't keep a copy of the unreadable file");
        }
    }
}
