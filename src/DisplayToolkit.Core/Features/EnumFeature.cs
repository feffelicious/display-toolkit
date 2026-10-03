using DisplayToolkit.Core.Capabilities;

namespace DisplayToolkit.Core.Features;

/// <summary>A setting with a fixed set of choices, such as the GameVisual preset.</summary>
public class EnumFeature(string id, string name, byte code, IReadOnlyList<FeatureOption> options) : Feature(id, name, code)
{
    /// <summary>Every choice we know about, whether or not a given monitor supports it.</summary>
    public IReadOnlyList<FeatureOption> Options { get; } = options;

    public override bool IsSupportedBy(MonitorCapabilities capabilities) => capabilities.Supports(Code);

    /// <summary>
    /// The choices this monitor advertises. If the monitor lists no values for the code, all known choices are offered.
    /// </summary>
    public virtual IReadOnlyList<FeatureOption> SupportedOptions(MonitorCapabilities capabilities)
    {
        var advertised = capabilities.ValuesOf(Code);
        return advertised.Count == 0 ? Options : [.. Options.Where(option => advertised.Contains(option.Value))];
    }

    public override bool NeedsConfirmation(uint value) => FindOption(value)?.NeedsConfirmation == true;

    public FeatureOption? FindOption(uint value) => Options.FirstOrDefault(option => option.Value == value);
}
