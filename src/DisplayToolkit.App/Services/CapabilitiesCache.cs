using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DisplayToolkit.Core.Capabilities;
using Microsoft.Extensions.Logging;

namespace DisplayToolkit.App.Services;

/// <summary>
/// Remembers each monitor model's capabilities string. Requesting it is slow (1–2 s) and the most failure-prone DDC/CI
/// operation, while it only changes with a firmware update.
/// </summary>
internal sealed class CapabilitiesCache(ILogger<CapabilitiesCache> logger)
{
    private readonly Lock _gate = new();
    private Dictionary<string, string>? _entries;

    public MonitorCapabilities? Get(string model)
    {
        lock (_gate)
        {
            return Load().TryGetValue(model, out var raw) ? CapabilitiesParser.Parse(raw) : null;
        }
    }

    public void Set(string model, MonitorCapabilities capabilities)
    {
        lock (_gate)
        {
            var entries = Load();
            if (entries.GetValueOrDefault(model) == capabilities.Raw)
            {
                return;
            }
            entries[model] = capabilities.Raw;
            try
            {
                File.WriteAllText(AppPaths.CapabilitiesCache, JsonSerializer.Serialize(entries, CacheJsonContext.Default.DictionaryStringString));
            }
            catch (IOException exception)
            {
                logger.LogWarning(exception, "Couldn't save the capabilities cache");
            }
        }
    }

    private Dictionary<string, string> Load()
    {
        if (_entries is not null)
        {
            return _entries;
        }

        try
        {
            if (File.Exists(AppPaths.CapabilitiesCache))
            {
                _entries = JsonSerializer.Deserialize(File.ReadAllText(AppPaths.CapabilitiesCache), CacheJsonContext.Default.DictionaryStringString);
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            logger.LogWarning(exception, "Ignoring an unreadable capabilities cache");
        }
        return _entries ??= [];
    }
}

[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class CacheJsonContext : JsonSerializerContext;
