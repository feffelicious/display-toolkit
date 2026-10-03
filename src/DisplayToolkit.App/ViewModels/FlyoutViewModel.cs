using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.App.Services;
using DisplayToolkit.App.ViewModels.Pages;
using DisplayToolkit.App.ViewModels.Tiles;
using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels;

/// <summary>The tray flyout: one monitor's brightness band, tile grid and sub-pages.</summary>
internal sealed partial class FlyoutViewModel : ObservableObject
{
    /// <summary>Features shown on the main page, refreshed each time the flyout opens.</summary>
    private static readonly Feature[] QuickFeatures =
    [
        FeatureCatalog.Brightness, FeatureCatalog.PictureMode, FeatureCatalog.HdrMode, FeatureCatalog.InputSource,
        FeatureCatalog.BlueLightFilter, FeatureCatalog.ShadowBoost, FeatureCatalog.Crosshair, FeatureCatalog.OledAntiFlicker,
    ];

    private readonly MonitorService _monitors;
    private readonly Dispatcher _dispatcher;

    public FlyoutViewModel(MonitorService monitors, Dispatcher dispatcher)
    {
        _monitors = monitors;
        _dispatcher = dispatcher;
        monitors.Changed += (_, _) => OnMonitorsChanged();
        OnMonitorsChanged();
    }

    public MonitorDiscoveryState State => _monitors.State;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Brightness), nameof(Subtitle))]
    public partial MonitorViewModel? Monitor { get; private set; }

    public FeatureState? Brightness => Monitor?[FeatureCatalog.Brightness];

    /// <summary>Input and signal under the monitor name, for example "DisplayPort".</summary>
    public string Subtitle => Monitor?[FeatureCatalog.InputSource] is { } input
        ? FeatureCatalog.InputSource.FindOption(input.Value)?.Name ?? string.Empty
        : string.Empty;

    public IReadOnlyList<TileViewModel> Tiles { get; private set; } = [];

    /// <summary>The open sub-page, or null for the main page.</summary>
    [ObservableProperty]
    public partial PageViewModel? Page { get; private set; }

    [ObservableProperty]
    public partial bool IsConflictingAppRunning { get; private set; }

    /// <summary>The name of a setting whose last write failed, shown in an info bar with a Retry button.</summary>
    [ObservableProperty]
    public partial FeatureState? FailedFeature { get; private set; }

    /// <summary>Call when the flyout opens: re-read what's visible, since the monitor's own menu may have changed it.</summary>
    public async Task OnOpenedAsync()
    {
        IsConflictingAppRunning = MonitorService.IsConflictingAppRunning();
        if (Monitor is null)
        {
            await _monitors.RescanAsync();
            return;
        }
        await Monitor.RefreshAsync(QuickFeatures);
    }

    public void OnClosed() => Back();

    [RelayCommand]
    private void Back()
    {
        Page?.Close();
        Page = null;
    }

    [RelayCommand]
    private Task Retry() => _monitors.RescanAsync();

    [RelayCommand]
    private void RetryFailedWrite()
    {
        if (FailedFeature is { } failed)
        {
            FailedFeature = null;
            failed.Retry();
        }
    }

    [RelayCommand]
    private void DismissFailure() => FailedFeature = null;

    [RelayCommand]
    private void CloseConflictingApp()
    {
        MonitorService.CloseConflictingApp();
        IsConflictingAppRunning = false;
    }

    private void Navigate(PageViewModel page)
    {
        Page?.Close();
        Page = page;
    }


    private void OnMonitorsChanged()
    {
        var session = _monitors.Sessions.Count > 0 ? _monitors.Sessions[0] : null;
        if (Monitor?.Session != session)
        {
            Page?.Close();
            Page = null;
            Monitor?.Dispose();
            Monitor = session is null ? null : new MonitorViewModel(session, _dispatcher);
            Tiles = Monitor is null ? [] : CreateTiles(Monitor);
            OnPropertyChanged(nameof(Tiles));

            if (Monitor is not null)
            {
                foreach (var feature in Monitor.Session.Features.Select(feature => Monitor[feature]!))
                {
                    feature.PropertyChanged += (_, e) =>
                    {
                        if (e.PropertyName == nameof(FeatureState.HasFailed) && feature.HasFailed)
                        {
                            FailedFeature = feature;
                        }
                    };
                }
                Monitor.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Subtitle));
                Monitor[FeatureCatalog.InputSource]?.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Subtitle));
            }
        }
        OnPropertyChanged(nameof(State));
    }

    /// <summary>The default tile layout from the design spec (§13), minus anything this monitor doesn't support.</summary>
    private List<TileViewModel> CreateTiles(MonitorViewModel monitor)
    {
        var tiles = new List<TileViewModel>();

        if (monitor[FeatureCatalog.PictureMode] is not null || monitor[FeatureCatalog.HdrMode] is not null)
        {
            tiles.Add(new PictureModeTileViewModel(monitor, Navigate));
        }

        tiles.Add(new HdrTileViewModel(monitor, Navigate));

        if (monitor[FeatureCatalog.BlueLightFilter] is { } blueLight)
        {
            tiles.Add(new SplitTileViewModel("blue-light", "", blueLight, defaultOnValue: 2,
                () => new LevelsPageViewModel(blueLight, "Level 4 matches TÜV low blue light."), Navigate, name: "Blue light"));
        }

        if (monitor[FeatureCatalog.ShadowBoost] is { } shadowBoost)
        {
            tiles.Add(new SplitTileViewModel("shadow-boost", "", shadowBoost, defaultOnValue: 1,
                () => new LevelsPageViewModel(shadowBoost), Navigate));
        }

        if (monitor[FeatureCatalog.Crosshair] is { } crosshair)
        {
            var styles = monitor.OptionsOf(FeatureCatalog.Crosshair);
            var firstStyle = styles.FirstOrDefault(option => option.Value != 0)?.Value ?? 0;
            tiles.Add(new SplitTileViewModel("crosshair", "", crosshair, firstStyle,
                () => new OptionsPageViewModel("Crosshair", crosshair, styles), Navigate));
        }

        if (monitor[FeatureCatalog.OledAntiFlicker] is { } antiFlicker)
        {
            tiles.Add(new SwitchTileViewModel("oled-anti-flicker", "", antiFlicker, name: "Anti-flicker"));
        }

        return tiles;
    }
}
