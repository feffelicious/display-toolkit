using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.App.Services;
using DisplayToolkit.App.ViewModels.Pages;
using DisplayToolkit.App.ViewModels.Tiles;
using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels;

/// <summary>The tray flyout: one monitor's brightness band, tile grid, sub-pages and edit mode.</summary>
internal sealed partial class FlyoutViewModel : ObservableObject
{
    /// <summary>Always shown above the tiles, so always refreshed on open.</summary>
    private static readonly Feature[] HeaderFeatures = [FeatureCatalog.Brightness, FeatureCatalog.HdrMode];

    private static readonly TimeSpan DisplayChangeSettleTime = TimeSpan.FromSeconds(2);

    private readonly MonitorService _monitors;
    private readonly LayoutStore _layouts;
    private readonly Dispatcher _dispatcher;

    /// <summary>The tile order when edit mode started, restored if editing is cancelled.</summary>
    private List<string>? _layoutBeforeEdit;

    public FlyoutViewModel(MonitorService monitors, LayoutStore layouts, Dispatcher dispatcher)
    {
        _monitors = monitors;
        _layouts = layouts;
        _dispatcher = dispatcher;
        monitors.Changed += (_, _) => OnMonitorsChanged();
        OnMonitorsChanged();
    }

    public MonitorDiscoveryState State => _monitors.State;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Brightness), nameof(Subtitle))]
    public partial MonitorViewModel? Monitor { get; private set; }

    public FeatureState? Brightness => Monitor?[FeatureCatalog.Brightness];

    /// <summary>
    /// The link under the monitor name, for example "DisplayPort, 240 Hz". From Windows, not the monitor's input
    /// register, which is unreliable while input auto-detection is on.
    /// </summary>
    public string Subtitle => Monitor?.Link?.ToString() ?? string.Empty;

    public ObservableCollection<TileViewModel> Tiles { get; } = [];

    /// <summary>The open sub-page, or null for the main page.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddTiles))]
    public partial PageViewModel? Page { get; private set; }

    /// <summary>Edit mode: tiles show unpin badges, can be dragged to reorder, and the footer offers Add and Done.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddTiles))]
    public partial bool IsEditing { get; private set; }

    /// <summary>The footer's Add button: in edit mode, but not while the Add page itself is open.</summary>
    public bool CanAddTiles => IsEditing && Page is null;

    [ObservableProperty]
    public partial bool IsConflictingAppRunning { get; private set; }

    /// <summary>A setting whose last write failed, shown in an info bar with a Retry button.</summary>
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
        await Monitor.RefreshAsync(VisibleFeatures());
    }

    /// <summary>Closing the flyout leaves sub-pages and keeps any layout edits.</summary>
    public void OnClosed()
    {
        Back();
        if (IsEditing)
        {
            Done();
        }
    }

    /// <summary>
    /// Displays changed: possibly an HDR switch from Windows (Win+Alt+B). Re-read once things have settled, which also
    /// restores the picture mode the monitor forgets when it leaves HDR.
    /// </summary>
    public async Task OnDisplaysChangedAsync()
    {
        await Task.Delay(DisplayChangeSettleTime);
        if (Monitor is { } monitor)
        {
            await monitor.RefreshAsync(VisibleFeatures());
        }
    }

    /// <summary>Moves a tile during a drag or keyboard reorder in edit mode.</summary>
    public void MoveTile(int from, int to)
    {
        if (IsEditing && from != to && from >= 0 && to >= 0 && from < Tiles.Count && to < Tiles.Count)
        {
            Tiles.Move(from, to);
        }
    }

    [RelayCommand]
    private void Back()
    {
        Page?.Close();
        Page = null;
    }

    [RelayCommand]
    private void Edit()
    {
        Back();
        _layoutBeforeEdit = [.. Tiles.Select(tile => tile.Id)];
        SetEditing(true);
    }

    [RelayCommand]
    private void Done()
    {
        Back();
        SetEditing(false);
        if (Monitor is { } monitor)
        {
            _layouts.Set(monitor.Session.Id.Model, [.. Tiles.Select(tile => tile.Id)]);
        }
    }

    /// <summary>Esc in edit mode: put the tiles back the way they were.</summary>
    [RelayCommand]
    private void CancelEdit()
    {
        Back();
        SetEditing(false);
        if (Monitor is { } monitor && _layoutBeforeEdit is { } layout)
        {
            LoadTiles(monitor, layout);
        }
    }

    [RelayCommand]
    private void OpenAddPage() => Navigate(new AddTilesPageViewModel(AvailableTiles, AddTile));

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

    private IEnumerable<Feature> VisibleFeatures() => HeaderFeatures.Concat(Tiles.SelectMany(tile => tile.Features)).Distinct();

    /// <summary>Tiles the monitor supports that aren't in the flyout yet.</summary>
    private List<TileDefinition> AvailableTiles() => Monitor is { } monitor
        ? [.. TileCatalog.All.Where(definition =>
            Tiles.All(tile => tile.Id != definition.Id) && definition.IsSupported(monitor))]
        : [];

    private void AddTile(TileDefinition definition)
    {
        if (Monitor is { } monitor && definition.Create(monitor, Navigate) is { } tile)
        {
            AttachTile(tile);
            Tiles.Add(tile);
        }
    }

    private void RemoveTile(TileViewModel tile)
    {
        if (IsEditing && Tiles.Remove(tile))
        {
            tile.Detach();
        }
    }

    private void SetEditing(bool editing)
    {
        IsEditing = editing;
        foreach (var tile in Tiles)
        {
            tile.IsEditing = editing;
        }
    }

    private void LoadTiles(MonitorViewModel monitor, IEnumerable<string> ids)
    {
        ClearTiles();
        foreach (var id in ids)
        {
            if (TileCatalog.Find(id)?.Create(monitor, Navigate) is { } tile)
            {
                AttachTile(tile);
                Tiles.Add(tile);
            }
        }
    }

    private void ClearTiles()
    {
        foreach (var tile in Tiles)
        {
            tile.Detach();
        }
        Tiles.Clear();
    }

    private void AttachTile(TileViewModel tile)
    {
        tile.IsEditing = IsEditing;
        tile.RemoveRequested += (_, _) => RemoveTile(tile);
    }

    private void OnMonitorsChanged()
    {
        var session = _monitors.Sessions.Count > 0 ? _monitors.Sessions[0] : null;
        if (Monitor?.Session != session)
        {
            Back();
            SetEditing(false);
            FailedFeature = null;
            Monitor?.Dispose();
            ClearTiles();
            Monitor = session is null ? null : new MonitorViewModel(session, _dispatcher);

            if (Monitor is { } monitor)
            {
                LoadTiles(monitor, _layouts.Get(monitor.Session.Id.Model) ?? TileCatalog.DefaultLayout);
                foreach (var feature in monitor.Session.Features.Select(feature => monitor[feature]!))
                {
                    feature.PropertyChanged += (_, e) =>
                    {
                        if (e.PropertyName == nameof(FeatureState.HasFailed) && feature.HasFailed)
                        {
                            FailedFeature = feature;
                        }
                    };
                }
                monitor.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Subtitle));
            }
        }
        OnPropertyChanged(nameof(State));
    }
}
