using System.Diagnostics;
using DisplayToolkit.Core.Ddc;
using DisplayToolkit.Core.Monitors;
using Microsoft.Extensions.Logging;

namespace DisplayToolkit.App.Services;

public enum MonitorDiscoveryState
{
    Searching,
    Ready,
    NoMonitors,
    NotResponding,
}

/// <summary>
/// Finds controllable monitors and keeps one <see cref="MonitorSession"/> per monitor. Call it from the UI thread;
/// events are raised there too.
/// </summary>
internal sealed class MonitorService(IMonitorEnumerator enumerator, CapabilitiesCache capabilitiesCache, ILogger<MonitorService> logger)
    : IDisposable
{
    private readonly List<MonitorSession> _sessions = [];
    private Task? _rescan;
    private bool _rescanRequested;

    public event EventHandler? Changed;

    public IReadOnlyList<MonitorSession> Sessions => _sessions;

    public MonitorDiscoveryState State { get; private set; } = MonitorDiscoveryState.Searching;

    /// <summary>ASUS DisplayWidget Center talks to the monitor too, and the two programs can collide on the bus.</summary>
    public static bool IsConflictingAppRunning() => Process.GetProcessesByName("DisplayWidgetCenter").Length > 0;

    public static void CloseConflictingApp()
    {
        foreach (var process in Process.GetProcessesByName("DisplayWidgetCenter"))
        {
            using (process)
            {
                process.Kill();
            }
        }
    }

    /// <summary>
    /// Re-enumerates monitors: opens sessions for new ones and drops ones that are gone. Calls made while a scan runs
    /// collapse into one follow-up scan, because display changes arrive in bursts.
    /// </summary>
    public Task RescanAsync()
    {
        if (_rescan is { IsCompleted: false })
        {
            _rescanRequested = true;
            return _rescan;
        }
        return _rescan = RunRescansAsync();
    }

    public void Dispose()
    {
        foreach (var session in _sessions)
        {
            session.Dispose();
        }
        _sessions.Clear();
    }

    private async Task RunRescansAsync()
    {
        do
        {
            _rescanRequested = false;
            await RescanOnceAsync();
        }
        while (_rescanRequested);
    }

    private async Task RescanOnceAsync()
    {
        var connections = await Task.Run(enumerator.Enumerate);
        var anyFailed = false;

        foreach (var gone in _sessions.Where(session => connections.All(connection => connection.Id != session.Id)).ToList())
        {
            logger.LogInformation("Monitor {Id} disconnected", gone.Id);
            _sessions.Remove(gone);
            gone.Dispose();
        }

        foreach (var connection in connections)
        {
            if (_sessions.Any(session => session.Id == connection.Id))
            {
                connection.Dispose();
                continue;
            }

            try
            {
                var cached = capabilitiesCache.Get(connection.Id.Model);
                var session = await MonitorSession.OpenAsync(connection, enumerator, knownCapabilities: cached);
                capabilitiesCache.Set(connection.Id.Model, session.Capabilities);
                _sessions.Add(session);
                logger.LogInformation("Connected to {Name} ({Id}), {Count} features", session.Name, session.Id, session.Features.Count);
            }
            catch (DdcException exception)
            {
                // Typical causes: DDC/CI disabled in the monitor menu, a monitor without DDC/CI (laptop panel), or a
                // monitor that is asleep. A later rescan (flyout open, display change) tries again.
                anyFailed = true;
                logger.LogWarning(exception, "Couldn't open {Name} ({Id})", connection.Name, connection.Id);
            }
        }

        State = _sessions.Count > 0 ? MonitorDiscoveryState.Ready
            : anyFailed ? MonitorDiscoveryState.NotResponding
            : MonitorDiscoveryState.NoMonitors;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
