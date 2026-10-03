using System.Windows.Threading;
using DisplayToolkit.App.ViewModels;

namespace DisplayToolkit.App.Services;

/// <summary>
/// The monitor the UI is showing, shared by the flyout and the main window so both bind to the same live state.
/// Lives on the UI thread.
/// </summary>
internal sealed class MonitorContext : IDisposable
{
    private readonly MonitorService _monitors;
    private readonly Dispatcher _dispatcher;

    public MonitorContext(MonitorService monitors, Dispatcher dispatcher)
    {
        _monitors = monitors;
        _dispatcher = dispatcher;
        monitors.Changed += (_, _) => Update();
        Update();
    }

    /// <summary>Raised when <see cref="Current"/> is replaced or discovery state changes.</summary>
    public event EventHandler? Changed;

    /// <summary>The monitor being controlled, or null while none is available.</summary>
    public MonitorViewModel? Current { get; private set; }

    public MonitorDiscoveryState State => _monitors.State;

    public Task RescanAsync() => _monitors.RescanAsync();

    public void Dispose() => Current?.Dispose();

    /// <summary>v1 controls one monitor: the first one that answered.</summary>
    private void Update()
    {
        var session = _monitors.Sessions.Count > 0 ? _monitors.Sessions[0] : null;
        if (Current?.Session != session)
        {
            Current?.Dispose();
            Current = session is null ? null : new MonitorViewModel(session, _dispatcher);
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
