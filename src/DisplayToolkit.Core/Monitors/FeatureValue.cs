using DisplayToolkit.Core.Features;

namespace DisplayToolkit.Core.Monitors;

/// <summary>The known state of one feature on one monitor.</summary>
/// <param name="Value">
/// The decoded value. While <see cref="FeatureStatus.Pending"/> this is the requested value (optimistic); after a
/// failure it is the last confirmed value.
/// </param>
/// <param name="Maximum">The maximum the monitor reported, for sliders.</param>
public sealed record FeatureValue(Feature Feature, uint Value, uint Maximum, FeatureStatus Status);

public enum FeatureStatus
{
    /// <summary>Read from or confirmed by the monitor.</summary>
    Confirmed,

    /// <summary>A write is queued or in flight.</summary>
    Pending,

    /// <summary>
    /// Sent, but the monitor waits for the user to confirm it in its on-screen menu. <see cref="FeatureValue.Value"/>
    /// is the requested value.
    /// </summary>
    AwaitingConfirmation,

    /// <summary>The last write didn't take effect. <see cref="FeatureValue.Value"/> is the last confirmed value.</summary>
    Failed,

    /// <summary>The monitor reports the setting as unavailable in its current mode (for example in HDR).</summary>
    Locked,
}
