namespace DisplayToolkit.Core.Ddc;

/// <summary>The monitor's answer to a VCP read.</summary>
public readonly record struct VcpReply(uint Current, uint Maximum)
{
    /// <summary>
    /// ASUS monitors answer <c>0xFE</c>/<c>0xFE</c> for settings that are unavailable in the current mode,
    /// for example Shadow Boost while HDR is on.
    /// </summary>
    public bool IsLocked => Current == 0xFE && Maximum == 0xFE;
}
