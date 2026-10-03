using DisplayToolkit.Core.Native;

namespace DisplayToolkit.Core.Monitors;

/// <summary>Whether Windows can drive a monitor in HDR, and whether it currently does.</summary>
public readonly record struct HdrState(bool IsSupported, bool IsEnabled);

/// <summary>
/// Reads and switches Windows HDR ("Use HDR" in Settings) for a monitor. This is the Windows side; the monitor's own
/// HDR preset is <see cref="Features.FeatureCatalog.HdrMode"/>.
/// </summary>
public static class WindowsHdr
{
    /// <summary>Windows 11 24H2 added a dedicated HDR switch; older builds use the advanced-color switch.</summary>
    private static readonly bool HasHdrStateApi = Environment.OSVersion.Version.Build >= 26100;

    public static HdrState? GetState(MonitorId id)
    {
        if (DisplayConfig.FindPath(id.Instance) is not { } path || DisplayConfig.GetAdvancedColorInfo(path) is not { } info)
        {
            return null;
        }
        return new HdrState(info.IsSupported, info.IsEnabled);
    }

    /// <summary>Turns Windows HDR on or off. The screen blanks for a second or two while the mode changes.</summary>
    public static bool SetEnabled(MonitorId id, bool enabled) =>
        DisplayConfig.FindPath(id.Instance) is { } path && DisplayConfig.SetHdr(path, enabled, HasHdrStateApi);
}
