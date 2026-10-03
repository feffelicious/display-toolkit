namespace DisplayToolkit.Core.Ddc;

/// <summary>
/// A raw DDC/CI connection to one physical monitor. Calls block for tens of milliseconds and are not thread-safe:
/// only <see cref="Monitors.MonitorSession"/> should use a channel, from its worker thread.
/// </summary>
/// <remarks>Implementations throw <see cref="DdcException"/> when the monitor doesn't answer.</remarks>
public interface IDdcChannel : IDisposable
{
    /// <summary>Requests the MCCS capabilities string. Slow (often 1–2 s).</summary>
    string GetCapabilities();

    VcpReply Get(byte code);

    void Set(byte code, uint value);
}
