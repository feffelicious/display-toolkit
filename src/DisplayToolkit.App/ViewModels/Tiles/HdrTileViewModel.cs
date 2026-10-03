using DisplayToolkit.App.ViewModels.Pages;

namespace DisplayToolkit.App.ViewModels.Tiles;

/// <summary>Turns Windows HDR on or off; the chevron opens HDR presets.</summary>
public sealed class HdrTileViewModel : TileViewModel
{
    private readonly MonitorViewModel _monitor;
    private readonly Action<PageViewModel> _navigate;

    public HdrTileViewModel(MonitorViewModel monitor, Action<PageViewModel> navigate)
        : base("hdr", TileKind.Split, "HDR", "HDR")
    {
        _monitor = monitor;
        _navigate = navigate;
        Track(monitor);
    }

    public override string Label => Name;

    public override bool IsOn => _monitor.IsHdrActive;

    public override bool IsPending => _monitor.IsHdrSwitching;

    public override bool HasFailed => false;

    public override bool IsVisible => _monitor.WindowsHdr?.IsSupported == true;

    protected override void OnMain() => _ = _monitor.SetWindowsHdrAsync(!_monitor.IsHdrActive);

    protected override void OnSecondary() => _navigate(new HdrPageViewModel(_monitor));
}
