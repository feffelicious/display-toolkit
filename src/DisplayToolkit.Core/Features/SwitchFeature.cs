namespace DisplayToolkit.Core.Features;

/// <summary>An on/off setting with its own register. Most use 0/1, but MCCS audio mute uses 1 = on, 2 = off.</summary>
public sealed class SwitchFeature(string id, string name, byte code, uint offValue = 0, uint onValue = 1)
    : EnumFeature(id, name, code, [new(offValue, "Off"), new(onValue, "On")])
{
    public uint OffValue { get; } = offValue;

    public uint OnValue { get; } = onValue;

    public bool IsOn(uint value) => value == OnValue;
}
