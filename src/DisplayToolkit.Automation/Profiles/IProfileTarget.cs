using DisplayToolkit.Core.Features;
using DisplayToolkit.Core.Monitors;

namespace DisplayToolkit.Automation.Profiles;

/// <summary>The monitor a profile is applied to.</summary>
public interface IProfileTarget
{
    /// <summary>Windows HDR is on for this monitor.</summary>
    bool IsHdrActive { get; }

    bool Supports(Feature feature);

    /// <summary>Switches Windows HDR and waits until the monitor has settled. Returns false if it didn't switch.</summary>
    Task<bool> SetWindowsHdrAsync(bool enabled);

    Task<FeatureValue> WriteAsync(Feature feature, uint value);
}
