namespace DisplayToolkit.Automation.Profiles;

/// <summary>
/// A named set of settings for one monitor model. Only the settings in <see cref="Settings"/> are written when the
/// profile is applied; everything else stays as it is.
/// </summary>
public sealed record Profile
{
    public const string DefaultGlyph = "";

    public required Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>A Segoe Fluent Icons glyph.</summary>
    public string Glyph { get; init; } = DefaultGlyph;

    public Shortcut? Shortcut { get; init; }

    /// <summary>Offered in the quick settings profiles row and the tray menu.</summary>
    public bool ShowInQuickSettings { get; init; } = true;

    /// <summary>Setting id (see <see cref="ProfileSettings"/>) to raw value.</summary>
    public IReadOnlyDictionary<string, uint> Settings { get; init; } = new Dictionary<string, uint>();
}
