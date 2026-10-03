using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels.Main;

/// <summary>GamePlus: overlays the monitor draws itself, so they work with any input and never show in captures.</summary>
public sealed class GamePlusPageViewModel(MonitorViewModel monitor) : MainPageViewModel("GamePlus")
{
    public MonitorViewModel Monitor { get; } = monitor;

    public FeatureState? Crosshair => Monitor[FeatureCatalog.Crosshair];

    public FeatureState? FpsCounter => Monitor[FeatureCatalog.FpsCounter];

    public FeatureState? Timer => Monitor[FeatureCatalog.Timer];

    public FeatureState? DisplayAlignment => Monitor[FeatureCatalog.DisplayAlignment];

    public override Task OnShownAsync() => Monitor.RefreshAsync([FeatureCatalog.Crosshair, FeatureCatalog.FpsCounter, FeatureCatalog.Timer, FeatureCatalog.DisplayAlignment]);
}
