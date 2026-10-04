using DisplayToolkit.App.Services;
using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels.Tiles;

/// <summary>Turns Target mode on or off. Not a monitor feature: it works with any monitor.</summary>
public sealed class TargetModeTileViewModel : TileViewModel
{
    private readonly TargetMode _targetMode;

    public TargetModeTileViewModel(TargetMode targetMode)
        : base("target-mode", TileKind.Toggle, "\uE7C4", "Target mode")
    {
        _targetMode = targetMode;
        Track(targetMode);
    }

    public override string Label => Name;

    public override bool IsOn => _targetMode.IsOn;

    public override bool IsPending => false;

    public override bool HasFailed => false;

    public override bool IsVisible => true;

    public override IEnumerable<Feature> Features => [];

    protected override void OnMain() => _targetMode.Toggle();
}
