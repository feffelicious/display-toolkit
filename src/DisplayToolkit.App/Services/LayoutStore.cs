using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace DisplayToolkit.App.Services;

/// <summary>
/// The user's flyout tile order, per monitor model, in <c>%AppData%\DisplayToolkit\layout.json</c>:
/// <c>{ "AUSAA6A": ["picture-mode", "hdr", …] }</c>.
/// </summary>
internal sealed class LayoutStore(ILogger<LayoutStore> logger)
{
    private static string FilePath => Path.Combine(AppPaths.DataDirectory, "layout.json");

    public IReadOnlyList<string>? Get(string model) => Load().GetValueOrDefault(model);

    public Dictionary<string, List<string>> GetAll() => Load();

    public void Set(string model, IReadOnlyList<string> tileIds)
    {
        var layouts = Load();
        layouts[model] = [.. tileIds];
        Save(layouts);
    }

    /// <summary>Raised when every layout was replaced (an import).</summary>
    public event EventHandler? Replaced;

    public void ReplaceAll(Dictionary<string, List<string>> layouts)
    {
        Save(layouts);
        Replaced?.Invoke(this, EventArgs.Empty);
    }

    private void Save(Dictionary<string, List<string>> layouts)
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(layouts, LayoutJsonContext.Default.DictionaryStringListString));
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Couldn't save the flyout layout");
        }
    }

    private Dictionary<string, List<string>> Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize(File.ReadAllText(FilePath), LayoutJsonContext.Default.DictionaryStringListString) ?? [];
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            logger.LogWarning(exception, "Ignoring an unreadable flyout layout");
        }
        return [];
    }
}

[JsonSerializable(typeof(Dictionary<string, List<string>>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class LayoutJsonContext : JsonSerializerContext;
