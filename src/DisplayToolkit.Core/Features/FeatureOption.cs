namespace DisplayToolkit.Core.Features;

/// <summary>One choice of an <see cref="EnumFeature"/>: the raw value written to the monitor and its English name.</summary>
public sealed record FeatureOption(uint Value, string Name);
