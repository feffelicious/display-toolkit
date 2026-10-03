using DisplayToolkit.Core.Capabilities;
using DisplayToolkit.Core.Ddc;

namespace DisplayToolkit.Core.Features;

/// <summary>
/// One user-facing monitor setting and how it maps onto a VCP register. Features are immutable descriptors declared in
/// <see cref="FeatureCatalog"/>; their values live in <see cref="Monitors.MonitorSession"/>.
/// </summary>
public abstract class Feature(string id, string name, byte code)
{
    /// <summary>Stable identifier used by the UI, profiles and settings files. Never change an existing id.</summary>
    public string Id { get; } = id;

    /// <summary>English display name.</summary>
    public string Name { get; } = name;

    public byte Code { get; } = code;

    /// <summary>
    /// False for settings the monitor ignores while it shows HDR content. Not all of them report themselves as locked
    /// (<c>0xFE</c>): brightness still reads a normal value but writes have no effect.
    /// </summary>
    public bool AvailableInHdr { get; init; } = true;

    /// <summary>
    /// True when the feature shares its register with other features, so writes must read the register first and
    /// change only this feature's bits.
    /// </summary>
    public virtual bool IsPartialRegister => false;

    /// <summary>How long the monitor may take before a write reads back correctly.</summary>
    public WriteSettling Settling { get; init; } = WriteSettling.Immediate;

    public abstract bool IsSupportedBy(MonitorCapabilities capabilities);

    /// <summary>Extracts this feature's value from a register reply.</summary>
    public virtual uint Decode(VcpReply reply) => reply.Current;

    /// <summary>Extracts this feature's maximum from a register reply.</summary>
    public virtual uint DecodeMaximum(VcpReply reply) => reply.Maximum;

    /// <summary>
    /// Produces the register value to write. <paramref name="register"/> is the current register value, and is only
    /// meaningful when <see cref="IsPartialRegister"/> is true.
    /// </summary>
    public virtual uint Encode(uint value, uint register) => value;

    public override string ToString() => $"{Id} (0x{Code:X2})";
}

public enum WriteSettling
{
    /// <summary>The new value reads back almost immediately.</summary>
    Immediate,

    /// <summary>
    /// The write switches a panel mode (an HDR preset, OLED anti-flicker). Windows re-detects the monitor, which
    /// invalidates handles, and the monitor reports stale values for several seconds.
    /// </summary>
    ModeSwitch,

    /// <summary>
    /// An action, not a setting (pixel cleaning). The monitor may stop answering while it runs, so the write isn't read
    /// back; the value is reported as written.
    /// </summary>
    Unverified,
}
