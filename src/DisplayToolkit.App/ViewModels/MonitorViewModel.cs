using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DisplayToolkit.Core.Features;
using DisplayToolkit.Core.Monitors;

namespace DisplayToolkit.App.ViewModels;

/// <summary>One monitor as the UI sees it: a <see cref="FeatureState"/> per supported feature, kept current.</summary>
public sealed partial class MonitorViewModel : ObservableObject, IDisposable
{
    /// <summary>How long after switching Windows HDR before the monitor reports its new state reliably.</summary>
    private static readonly TimeSpan HdrSettleTime = TimeSpan.FromSeconds(2);

    private readonly Dispatcher _dispatcher;
    private readonly Dictionary<Feature, FeatureState> _features;

    public MonitorViewModel(MonitorSession session, Dispatcher dispatcher)
    {
        Session = session;
        _dispatcher = dispatcher;
        _features = session.Features.ToDictionary(feature => feature, feature => new FeatureState(session, feature));

        foreach (var (feature, state) in _features)
        {
            if (session.GetValue(feature) is { } value)
            {
                state.Apply(value);
            }
        }

        session.ValueChanged += OnValueChanged;
        RefreshWindowsHdr();
    }

    public MonitorSession Session { get; }

    public string Name => Session.Name;

    /// <summary>Windows HDR state ("Use HDR"), or null if Windows can't drive this monitor in HDR.</summary>
    [ObservableProperty]
    public partial HdrState? WindowsHdr { get; private set; }

    public bool IsHdrActive => WindowsHdr?.IsEnabled == true;

    /// <summary>True while Windows switches HDR on or off (the screen blanks for a second or two).</summary>
    [ObservableProperty]
    public partial bool IsHdrSwitching { get; private set; }

    /// <summary>
    /// Switches Windows HDR, then re-reads every feature: the monitor changes brightness, color and which settings are
    /// available when the signal type changes.
    /// </summary>
    public async Task SetWindowsHdrAsync(bool enabled)
    {
        if (IsHdrSwitching)
        {
            return;
        }

        IsHdrSwitching = true;
        try
        {
            await Task.Run(() => Core.Monitors.WindowsHdr.SetEnabled(Session.Id, enabled));
            await Task.Delay(HdrSettleTime);
            RefreshWindowsHdr();
            await Session.RefreshAsync();
        }
        finally
        {
            IsHdrSwitching = false;
        }
    }

    /// <summary>The state of a feature, or null if this monitor doesn't support it.</summary>
    public FeatureState? this[Feature feature] => _features.GetValueOrDefault(feature);

    public IReadOnlyList<FeatureOption> OptionsOf(EnumFeature feature) => Session.OptionsOf(feature);

    /// <summary>Re-reads the monitor. Its own menu can change settings without telling anyone.</summary>
    public Task RefreshAsync(IEnumerable<Feature>? features = null)
    {
        RefreshWindowsHdr();
        return Session.RefreshAsync(features);
    }

    public void RefreshWindowsHdr()
    {
        WindowsHdr = Core.Monitors.WindowsHdr.GetState(Session.Id);
        OnPropertyChanged(nameof(IsHdrActive));
    }

    public void Dispose() => Session.ValueChanged -= OnValueChanged;

    private void OnValueChanged(object? sender, FeatureValueChangedEventArgs e) =>
        _dispatcher.BeginInvoke(() => this[e.Value.Feature]?.Apply(e.Value));
}
