using DisplayToolkit.Core.Capabilities;

namespace DisplayToolkit.Core.Features;

/// <summary>
/// The ASUS HDR preset (<c>0xE2</c>). The value is <c>(group &lt;&lt; 8) | preset</c>, where group 1 is HDR10 and
/// group 2 is Dolby Vision. It is 0 when no HDR signal is active, and only writable while one is.
/// </summary>
/// <remarks>
/// The capabilities string lists groups and presets separately (<c>0000 0100 0200 01 02 …</c>), so an option is
/// supported when both its group and its preset number are advertised.
/// Switching to some presets (True Black 400) makes Windows re-enumerate the display, hence
/// <see cref="WriteSettling.ModeSwitch"/>.
/// </remarks>
public sealed class HdrModeFeature(string id, string name, byte code, IReadOnlyList<FeatureOption> options)
    : EnumFeature(id, name, code, options)
{
    public const uint Hdr10Group = 0x01;
    public const uint DolbyVisionGroup = 0x02;

    public override WriteSettling Settling => WriteSettling.ModeSwitch;

    public static uint GroupOf(uint value) => (value >> 8) & 0xFF;

    public override IReadOnlyList<FeatureOption> SupportedOptions(MonitorCapabilities capabilities)
    {
        var advertised = capabilities.ValuesOf(Code);
        var groups = advertised.Where(value => value > 0xFF && (value & 0xFF) == 0).Select(GroupOf).ToHashSet();
        var presets = advertised.Where(value => value <= 0xFF).ToHashSet();

        return [.. Options.Where(option => groups.Contains(GroupOf(option.Value)) && presets.Contains(option.Value & 0xFF))];
    }
}
