using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.App.Services;
using DisplayToolkit.Core.Features;
using DisplayToolkit.Core.Monitors;

namespace DisplayToolkit.App.ViewModels.Main;

/// <summary>
/// GamePlus: overlays the monitor draws itself, so they work with any input and never show in captures. Also Target
/// mode, the app's own focus helper.
/// </summary>
public sealed partial class GamePlusPageViewModel : MainPageViewModel
{
    private readonly TargetMode _targetMode;
    private readonly AppSettings _settings;

    internal GamePlusPageViewModel(MonitorViewModel monitor, TargetMode targetMode, AppSettings settings)
        : base("GamePlus")
    {
        Monitor = monitor;
        _targetMode = targetMode;
        _settings = settings;
        Observe(targetMode, (_, _) => OnPropertyChanged(nameof(IsTargetModeOn)));
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

    public bool IsTargetModeOn
    {
        get => _targetMode.IsOn;
        set => _targetMode.SetOn(value);
    }

    /// <summary>How dark everything but the window in use gets, in percent.</summary>
    public double TargetModeDim
    {
        get => Math.Round(_settings.Current.TargetModeDim * 100);
        set
        {
            _settings.Update(current => current with { TargetModeDim = Math.Clamp(value, 10, 95) / 100 });
            OnPropertyChanged();
        }
    }

    public override Task OnShownAsync() => Monitor.RefreshAsync([FeatureCatalog.Crosshair, FeatureCatalog.FpsCounter, FeatureCatalog.Timer, FeatureCatalog.DisplayAlignment]);
}
