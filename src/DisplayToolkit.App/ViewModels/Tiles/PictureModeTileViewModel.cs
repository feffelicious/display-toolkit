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

    /// <summary>The monitor reports an HDR preset only while it receives an HDR signal.</summary>
    private FeatureState? Active => HdrMode is { Value: not 0 } hdr ? hdr : PictureMode;

    public override string Label => Active is { } state && state.Feature is EnumFeature feature
        ? feature.FindOption(state.Value)?.Name ?? Name
        : Name;

    public override bool IsOn => false;

    public override bool IsPending => Active?.IsPending == true;

    public override bool HasFailed => Active?.HasFailed == true;

    public override bool IsVisible => Active?.IsKnown == true;

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
