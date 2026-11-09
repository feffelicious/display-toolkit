using DisplayToolkit.Core.Ddc;

namespace DisplayToolkit.Core.Features;

/// <summary>An on/off setting with its own register. Most use 0/1, but MCCS audio mute uses 1 = on, 2 = off.</summary>
public sealed class SwitchFeature(string id, string name, byte code, uint offValue = 0, uint onValue = 1)
    : EnumFeature(id, name, code, [new(offValue, "Off"), new(onValue, "On")])
{
    public uint OffValue { get; } = offValue;

    public uint OnValue { get; } = onValue;

    public bool IsOn(uint value) => value == OnValue;

    /// <summary>
    /// True when the monitor reports a maximum of 0 while the setting is greyed out in its menu (ELMB on the PG27UCWM).
    /// Off by default: MCCS leaves a switch's maximum undefined, and some ASUS switches may read 0 even when usable.
    /// </summary>
    public bool UnavailableAtZeroMaximum { get; init; }

    public override bool IsUnavailable(VcpReply reply) =>
        base.IsUnavailable(reply) || (UnavailableAtZeroMaximum && reply.Maximum == 0);
}
