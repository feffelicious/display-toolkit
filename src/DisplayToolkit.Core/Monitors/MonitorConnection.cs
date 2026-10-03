using DisplayToolkit.Core.Ddc;

namespace DisplayToolkit.Core.Monitors;

/// <summary>A monitor found during enumeration, with an open DDC/CI channel. Owns the channel.</summary>
public sealed class MonitorConnection(MonitorId id, string name, IDdcChannel channel) : IDisposable
{
    public MonitorId Id { get; } = id;

    /// <summary>Name reported by the monitor's EDID, for example <c>PG32UCWM</c>.</summary>
    public string Name { get; } = name;

    public IDdcChannel Channel { get; } = channel;

    public void Dispose() => Channel.Dispose();
}
