namespace DisplayToolkit.Core.Monitors;

public interface IMonitorEnumerator
{
    /// <summary>
    /// Opens a connection to every physical monitor currently attached. The caller owns the returned connections and
    /// must dispose the ones it doesn't keep.
    /// </summary>
    IReadOnlyList<MonitorConnection> Enumerate();
}
