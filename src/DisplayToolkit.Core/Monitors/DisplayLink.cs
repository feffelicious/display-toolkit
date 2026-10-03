using DisplayToolkit.Core.Native;

namespace DisplayToolkit.Core.Monitors;

/// <summary>How Windows is driving a monitor right now.</summary>
/// <param name="Connection">"DisplayPort", "HDMI", "USB-C"…, or empty if unknown.</param>
/// <param name="RefreshRate">In hertz, or 0 if unknown.</param>
public sealed record DisplayLinkInfo(string Connection, double RefreshRate)
{
    /// <summary>For example "DisplayPort, 240 Hz".</summary>
    public override string ToString() => (Connection, RefreshRate) switch
    {
        ("", 0) => string.Empty,
        (_, 0) => Connection,
        ("", _) => $"{RefreshRate:0} Hz",
        _ => $"{Connection}, {RefreshRate:0} Hz",
    };
}

/// <summary>
/// The physical link to a monitor as Windows sees it. More reliable than the monitor's own input register (VCP 0x60),
/// which on the PG32UCWM jumps between inputs while input auto-detection is on.
/// </summary>
public static class DisplayLink
{
    public static DisplayLinkInfo? Get(MonitorId id)
    {
        if (DisplayConfig.FindPath(id.Instance) is not { } path)
        {
            return null;
        }

        var target = path.Target;
        var refreshRate = target.RefreshRateDenominator == 0 ? 0 : (double)target.RefreshRateNumerator / target.RefreshRateDenominator;
        return new DisplayLinkInfo(ConnectionName(target.OutputTechnology), Math.Round(refreshRate));
    }

    /// <summary>DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY values, named the way people say them.</summary>
    private static string ConnectionName(uint outputTechnology) => outputTechnology switch
    {
        0 => "VGA",
        4 => "DVI",
        5 => "HDMI",
        10 => "DisplayPort",
        18 => "USB-C",
        _ => string.Empty,
    };
}
