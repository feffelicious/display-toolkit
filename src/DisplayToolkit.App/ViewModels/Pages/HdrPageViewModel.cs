using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels.Pages;

/// <summary>The Windows "Use HDR" switch, plus the monitor's presets for the HDR signal it is receiving.</summary>
public sealed class HdrPageViewModel : PageViewModel
{
    private readonly MonitorViewModel _monitor;

    public HdrPageViewModel(MonitorViewModel monitor)
        : base("HDR")
    {
        _monitor = monitor;
        Observe(monitor, Update);
        if (HdrMode is { } hdrMode)
        {
            Observe(hdrMode, Update);
        }
        Update();
    }

    public bool UseHdr
    {
        get => _monitor.IsHdrActive;
        set => _ = _monitor.SetWindowsHdrAsync(value);
    }

    public bool IsSwitching => _monitor.IsHdrSwitching;

    /// <summary>Presets of the active group only: HDR10 presets for HDR10, Dolby Vision presets for Dolby Vision.</summary>
    public IReadOnlyList<OptionItemViewModel> Presets { get; private set; } = [];

    public bool HasPresets => Presets.Count > 0;

    private FeatureState? HdrMode => _monitor[FeatureCatalog.HdrMode];

    private void Update()
    {
        var group = HdrMode is { } state ? HdrModeFeature.GroupOf(state.Value) : 0;
        Presets = HdrMode is { } hdrMode && _monitor.IsHdrActive && group != 0
            ? [.. _monitor.OptionsOf(FeatureCatalog.HdrMode)
                .Where(option => HdrModeFeature.GroupOf(option.Value) == group)
                .Select(option => new OptionItemViewModel(option.Name, option.Value, hdrMode.Write) { IsSelected = option.Value == hdrMode.Value })]
            : [];

        OnPropertyChanged(nameof(UseHdr));
        OnPropertyChanged(nameof(IsSwitching));
        OnPropertyChanged(nameof(Presets));
        OnPropertyChanged(nameof(HasPresets));
    }
}
