namespace DisplayToolkit.Core.Features;

/// <summary>One choice of an <see cref="EnumFeature"/>: the raw value written to the monitor and its English name.</summary>
public sealed record FeatureOption(uint Value, string Name)
{
    /// <summary>
    /// The monitor asks the user to confirm this choice in its on-screen menu before applying it (proximity
    /// "Tailored", which then calibrates). The register keeps its old value until then.
    /// </summary>
    public bool NeedsConfirmation { get; init; }
}
