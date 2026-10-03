using System.Collections.Frozen;

namespace DisplayToolkit.Core.Capabilities;

/// <summary>What a monitor says it supports, parsed from its MCCS capabilities string.</summary>
public sealed class MonitorCapabilities
{
    public static readonly MonitorCapabilities Empty = new(string.Empty, null, null, new Dictionary<byte, IReadOnlyList<uint>>());

    private readonly FrozenDictionary<byte, IReadOnlyList<uint>> _vcp;

    public MonitorCapabilities(string raw, string? model, string? mccsVersion, IReadOnlyDictionary<byte, IReadOnlyList<uint>> vcp)
    {
        Raw = raw;
        Model = model;
        MccsVersion = mccsVersion;
        _vcp = vcp.ToFrozenDictionary();
    }

    /// <summary>The unparsed capabilities string, kept for diagnostics.</summary>
    public string Raw { get; }

    public string? Model { get; }

    public string? MccsVersion { get; }

    public IEnumerable<byte> VcpCodes => _vcp.Keys.Order();

    public bool Supports(byte code) => _vcp.ContainsKey(code);

    /// <summary>
    /// The values the monitor advertises for <paramref name="code"/>, in the order given. Empty when the code is
    /// continuous or advertised without a value list.
    /// </summary>
    /// <remarks>
    /// For ASUS bitmask registers (<c>0xFC</c>, <c>0xFD</c>) the single value is the mask of supported bits.
    /// </remarks>
    public IReadOnlyList<uint> ValuesOf(byte code) => _vcp.TryGetValue(code, out var values) ? values : [];
}
