using System.Diagnostics;
using DisplayToolkit.Core.Ddc;

namespace DisplayToolkit.Core.Monitors;

/// <summary>
/// Wraps a monitor's DDC channel with the rules real hardware needs: spacing between calls, retries, and reconnecting
/// with a fresh handle when the old one dies (which happens when an HDR preset switch makes Windows re-enumerate the
/// display). Not thread-safe; used only from the session's worker thread.
/// </summary>
internal sealed class ResilientChannel(MonitorConnection connection, IMonitorEnumerator enumerator, MonitorSessionOptions options)
    : IDisposable
{
    private MonitorConnection _connection = connection;
    private long _lastCallTimestamp;

    public MonitorId Id => _connection.Id;

    public string GetCapabilities() => Invoke(channel => channel.GetCapabilities());

    public VcpReply Get(byte code) => Invoke(channel => channel.Get(code));

    public void Set(byte code, uint value) => Invoke(channel =>
    {
        channel.Set(code, value);
        return true;
    });

    public void Dispose() => _connection.Dispose();

    private T Invoke<T>(Func<IDdcChannel, T> call)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                WaitForSpacing();
                return call(_connection.Channel);
            }
            catch (DdcException) when (attempt < options.Retries)
            {
                Sleep(options.RetryDelay);

                // Before the last attempt, assume the handle went stale and look the monitor up again.
                if (attempt == options.Retries - 1)
                {
                    TryReconnect();
                }
            }
            finally
            {
                _lastCallTimestamp = Stopwatch.GetTimestamp();
            }
        }
    }

    private void WaitForSpacing()
    {
        var remaining = options.CommandSpacing - Stopwatch.GetElapsedTime(_lastCallTimestamp);
        if (remaining > TimeSpan.Zero)
        {
            Sleep(remaining);
        }
    }

    private bool TryReconnect()
    {
        IReadOnlyList<MonitorConnection> found;
        try
        {
            found = enumerator.Enumerate();
        }
        catch (DdcException)
        {
            return false;
        }

        var match = found.FirstOrDefault(candidate => candidate.Id == _connection.Id);
        foreach (var other in found.Where(candidate => candidate != match))
        {
            other.Dispose();
        }

        if (match is null)
        {
            return false;
        }

        _connection.Dispose();
        _connection = match;
        return true;
    }

    private static void Sleep(TimeSpan duration)
    {
        if (duration > TimeSpan.Zero)
        {
            Thread.Sleep(duration);
        }
    }
}
