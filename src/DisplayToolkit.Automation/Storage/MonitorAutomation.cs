using System.Text.Json;
using System.Text.Json.Serialization;
using DisplayToolkit.Automation.Profiles;
using DisplayToolkit.Automation.Rules;

namespace DisplayToolkit.Automation.Storage;

/// <summary>The profiles and rules of one monitor model. Rules are in priority order.</summary>
public sealed record MonitorAutomation
{
    public static MonitorAutomation Empty { get; } = new();

    public IReadOnlyList<Profile> Profiles { get; init; } = [];

    public IReadOnlyList<Rule> Rules { get; init; } = [];

    public Profile? FindProfile(Guid id) => Profiles.FirstOrDefault(profile => profile.Id == id);
}

/// <summary>
/// Reads and writes <c>automation.json</c>: <c>{ "AUSAA6A": { "profiles": [...], "rules": [...] } }</c>, keyed by
/// monitor model.
/// </summary>
public static class AutomationJson
{
    public static string Serialize(IReadOnlyDictionary<string, MonitorAutomation> data) =>
        JsonSerializer.Serialize(data, AutomationJsonContext.Default.IReadOnlyDictionaryStringMonitorAutomation);

    /// <exception cref="JsonException">The text isn't valid automation data.</exception>
    public static Dictionary<string, MonitorAutomation> Deserialize(string json) =>
        new(JsonSerializer.Deserialize(json, AutomationJsonContext.Default.IReadOnlyDictionaryStringMonitorAutomation) ?? new Dictionary<string, MonitorAutomation>());
}

[JsonSerializable(typeof(IReadOnlyDictionary<string, MonitorAutomation>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class AutomationJsonContext : JsonSerializerContext;
