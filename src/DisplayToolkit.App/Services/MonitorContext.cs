using System.Windows.Threading;
using DisplayToolkit.App.ViewModels;
using DisplayToolkit.Core.Monitors;

namespace DisplayToolkit.App.Services;

/// <summary>
/// The monitor the app controls, shared by quick settings, the main window, automation and shortcuts so they all work
/// on the same live state. With several monitors the user picks one; the choice is remembered. Lives on the UI thread.
/// </summary>
internal sealed class MonitorContext : IDisposable
{
    /// <summary>EDID manufacturer code of ASUS: preferred when nothing was picked yet.</summary>
    private const string AsusManufacturer = "AUS";

    private readonly MonitorService _monitors;
    private readonly AppSettings _settings;
    private readonly Dispatcher _dispatcher;

    public MonitorContext(MonitorService monitors, AppSettings settings, Dispatcher dispatcher)
    {
        _monitors = monitors;
        _settings = settings;
        _dispatcher = dispatcher;
        monitors.Changed += (_, _) => Update();
        Update();
    }

    /// <summary>Raised when <see cref="Current"/> is replaced, or the monitors or discovery state change.</summary>
    public event EventHandler? Changed;

    /// <summary>The monitor being controlled, or null while none is available.</summary>
    public MonitorViewModel? Current { get; private set; }

    /// <summary>Every monitor that answered, in the order Windows lists them.</summary>
    public IReadOnlyList<MonitorSession> Monitors => _monitors.Sessions;

    public MonitorDiscoveryState State => _monitors.State;

    public Task RescanAsync() => _monitors.RescanAsync();

    /// <summary>The monitor's name, numbered when several of the same model are connected ("PG32UCWM (2)").</summary>
    public string NameOf(MonitorSession monitor)
    {
        var sameName = Monitors.Where(other => other.Name == monitor.Name).ToList();
        return sameName.Count > 1 ? $"{monitor.Name} ({sameName.IndexOf(monitor) + 1})" : monitor.Name;
    }

    /// <summary>Controls <paramref name="monitor"/> from now on, here and after a restart.</summary>
    public void Select(MonitorSession monitor)
    {
        _settings.Update(current => current with { SelectedMonitor = monitor.Id.Instance });
        Update();
    }

    public void Dispose() => Current?.Dispose();

    /// <summary>The picked monitor if it's connected; otherwise the first ASUS monitor, otherwise the first one.</summary>
    private void Update()
    {
        var sessions = _monitors.Sessions;
        var session = sessions.FirstOrDefault(session => session.Id.Instance == _settings.Current.SelectedMonitor)
            ?? sessions.FirstOrDefault(session => session.Id.Model.StartsWith(AsusManufacturer, StringComparison.OrdinalIgnoreCase))
            ?? (sessions.Count > 0 ? sessions[0] : null);
        if (Current?.Session != session)
        {
            Current?.Dispose();
            Current = session is null ? null : new MonitorViewModel(session, _dispatcher);
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
