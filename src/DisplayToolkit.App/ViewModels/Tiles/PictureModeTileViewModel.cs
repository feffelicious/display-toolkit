using DisplayToolkit.App.ViewModels.Pages;
using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels.Tiles;

/// <summary>
/// Shows the active picture mode, or the HDR preset while HDR is on (the monitor ignores picture modes in HDR).
/// </summary>
public sealed class PictureModeTileViewModel : TileViewModel
{
    private readonly MonitorViewModel _monitor;
    private readonly Action<PageViewModel> _navigate;

    public PictureModeTileViewModel(MonitorViewModel monitor, Action<PageViewModel> navigate)
        : base("picture-mode", TileKind.Picker, "", "Picture mode")
    {
        _monitor = monitor;
        _navigate = navigate;
        Track(monitor);
        if (PictureMode is { } pictureMode)
        {
            Track(pictureMode);
        }
        if (HdrMode is { } hdrMode)
        {
            Track(hdrMode);
        }
    }

    private FeatureState? PictureMode => _monitor[FeatureCatalog.PictureMode];

    private FeatureState? HdrMode => _monitor[FeatureCatalog.HdrMode];

    /// <summary>
    /// Windows' HDR state decides, not the monitor's registers: right after a switch the monitor briefly reports no HDR
    /// preset, and in HDR its picture-mode register reads 5, which would show as "Racing".
    /// </summary>
    private FeatureState? Active => _monitor.IsHdrActive && HdrMode is { } hdr ? hdr : PictureMode;

    public override string Label => Active is { } state && state.Feature is EnumFeature feature
        ? feature.FindOption(state.Value)?.Name ?? (state == HdrMode ? "HDR" : Name)
        : Name;

    public override bool IsOn => false;

    public override bool IsPending => Active?.IsPending == true;

    public override bool HasFailed => Active?.HasFailed == true;

    public override bool IsVisible => Active?.IsKnown == true;

    public override IEnumerable<Feature> Features => [FeatureCatalog.PictureMode, FeatureCatalog.HdrMode];

    protected override void OnMain()
    {
        if (Active is { } state && state.Feature is EnumFeature feature)
        {
            _navigate(state == HdrMode
                ? new HdrPageViewModel(_monitor)
                : new OptionsPageViewModel("Picture mode", state, _monitor.OptionsOf(feature)));
        }
    }
}
