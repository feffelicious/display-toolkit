using System.ComponentModel;
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

    /// <summary>The picture mode in use before HDR was turned on, to restore afterwards (see <see cref="RestorePictureModeAfterHdrAsync"/>).</summary>
    private uint? _lastSdrPictureMode;
    private bool _leftHdr;

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

        if (this[FeatureCatalog.PictureMode] is { } pictureMode)
        {
            pictureMode.PropertyChanged += OnPictureModeChanged;
            RememberSdrPictureMode(pictureMode);
        }

        session.ValueChanged += OnValueChanged;
        RefreshWindowsHdr();
    }

    public MonitorSession Session { get; }

    public string Name => Session.Name;

    /// <summary>Windows HDR state ("Use HDR"), or null if Windows can't drive this monitor in HDR.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHdrActive))]
    public partial HdrState? WindowsHdr { get; private set; }

    public bool IsHdrActive => WindowsHdr?.IsEnabled == true;

    /// <summary>True while Windows switches HDR on or off (the screen blanks for a second or two).</summary>
    [ObservableProperty]
    public partial bool IsHdrSwitching { get; private set; }

    /// <summary>The state of a feature, or null if this monitor doesn't support it.</summary>
    public FeatureState? this[Feature feature] => _features.GetValueOrDefault(feature);

    public IReadOnlyList<FeatureOption> OptionsOf(EnumFeature feature) => Session.OptionsOf(feature);

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
            await RefreshAsync();
        }
        finally
        {
            IsHdrSwitching = false;
        }
    }

    /// <summary>Re-reads the monitor. Its own menu, and Windows (Win+Alt+B), can change things without telling us.</summary>
    public async Task RefreshAsync(IEnumerable<Feature>? features = null)
    {
        RefreshWindowsHdr();
        await Session.RefreshAsync(features);
        await RestorePictureModeAfterHdrAsync();
    }

    public void Dispose()
    {
        Session.ValueChanged -= OnValueChanged;
        if (this[FeatureCatalog.PictureMode] is { } pictureMode)
        {
            pictureMode.PropertyChanged -= OnPictureModeChanged;
        }
    }

    private void RefreshWindowsHdr()
    {
        var wasHdr = IsHdrActive;
        WindowsHdr = Core.Monitors.WindowsHdr.GetState(Session.Id);
        _leftHdr |= wasHdr && !IsHdrActive;
    }

    /// <summary>
    /// The PG32UCWM comes out of HDR in the Racing picture mode, whatever was used before. Put the previous mode back.
    /// Only after an observed HDR-to-SDR switch, so a mode chosen in the monitor's own menu is never overridden.
    /// </summary>
    private async Task RestorePictureModeAfterHdrAsync()
    {
        if (!_leftHdr || IsHdrActive)
        {
            return;
        }
        _leftHdr = false;

        if (this[FeatureCatalog.PictureMode] is { } pictureMode && _lastSdrPictureMode is { } previous && pictureMode.Value != previous)
        {
            // Give the monitor a moment: it reports stale modes right after the signal changes.
            await Task.Delay(HdrSettleTime);
            pictureMode.Write(previous);
        }
    }

    private void OnPictureModeChanged(object? sender, PropertyChangedEventArgs e) => RememberSdrPictureMode((FeatureState)sender!);

    /// <summary>Records the mode while in SDR. In HDR the register is locked and reads a meaningless value.</summary>
    private void RememberSdrPictureMode(FeatureState pictureMode)
    {
        if (!IsHdrActive && !_leftHdr && pictureMode.IsKnown && pictureMode.Status == FeatureStatus.Confirmed)
        {
            _lastSdrPictureMode = pictureMode.Value;
        }
    }

    private void OnValueChanged(object? sender, FeatureValueChangedEventArgs e) =>
        _dispatcher.BeginInvoke(() => this[e.Value.Feature]?.Apply(e.Value));
}
