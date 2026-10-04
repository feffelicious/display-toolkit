using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.Core.Features;
using DisplayToolkit.Core.Monitors;

namespace DisplayToolkit.App.ViewModels.Main;

/// <summary>GamePlus: overlays the monitor draws itself, so they work with any input and never show in captures.</summary>
public sealed partial class GamePlusPageViewModel : MainPageViewModel
{
    public GamePlusPageViewModel(MonitorViewModel monitor)
        : base("GamePlus")
    {
        Monitor = monitor;
    }

    public MonitorViewModel Monitor { get; }

    public FeatureState? Crosshair => Monitor[FeatureCatalog.Crosshair];

    public FeatureState? FpsCounter => Monitor[FeatureCatalog.FpsCounter];

    public FeatureState? Timer => Monitor[FeatureCatalog.Timer];

    public FeatureState? DisplayAlignment => Monitor[FeatureCatalog.DisplayAlignment];

    /// <summary>The FPS counter and timer can be moved around the screen.</summary>
    public bool CanMoveOverlays => Monitor.Session.CanMoveOverlays;

    /// <summary>Moves the overlays one step; the arrows repeat this while held, so steps may overlap.</summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task<bool> MoveOverlays(OverlayDirection direction) => Monitor.Session.MoveOverlaysAsync(direction);

    public override Task OnShownAsync() => Monitor.RefreshAsync([FeatureCatalog.Crosshair, FeatureCatalog.FpsCounter, FeatureCatalog.Timer, FeatureCatalog.DisplayAlignment]);
}
