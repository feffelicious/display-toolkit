using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.App.Services;
using DisplayToolkit.Core.Monitors;

namespace DisplayToolkit.App.ViewModels.Main;

/// <summary>A navigation pane entry.</summary>
public sealed record NavItem(string Title, string Glyph, Func<MainPageViewModel> CreatePage);

/// <summary>A monitor in the pane's monitor switch.</summary>
public sealed record MonitorChoice(string Name, MonitorSession Session);

/// <summary>The main window: navigation pane, current page (or sub-page) and the in-window confirmation dialog.</summary>
internal sealed partial class MainWindowViewModel : ObservableObject, IMainWindowHost
{
    private readonly MonitorContext _context;
    private readonly AppSettings _settings;
    private readonly AutomationService _automation;
    private readonly LocationService _location;
    private readonly SettingsTransfer _transfer;
    private readonly UpdateService _updates;
    private readonly ShortcutService _shortcuts;
    private readonly TargetMode _targetMode;
    private TaskCompletionSource<bool>? _confirmation;
    private MonitorViewModel? _shownMonitor;

    public MainWindowViewModel(
        MonitorContext context, AppSettings settings, AutomationService automation, LocationService location, SettingsTransfer transfer,
        UpdateService updates, ShortcutService shortcuts, TargetMode targetMode)
    {
        _shortcuts = shortcuts;
        _targetMode = targetMode;
        _updates = updates;
        _transfer = transfer;
        _context = context;
        _settings = settings;
        _automation = automation;
        _location = location;
        context.Changed += (_, _) => OnMonitorChanged();
        SettingsItem = new NavItem("Settings", "", () => new SettingsPageViewModel(_settings, _location, _transfer, _updates, _shortcuts, _targetMode, this, Monitor));
        OnMonitorChanged();
    }

    public MonitorViewModel? Monitor => _context.Current;

    public MonitorDiscoveryState State => _context.State;

    /// <summary>Every connected monitor, for the switch under the monitor card (shown with two or more).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSeveralMonitors))]
    public partial IReadOnlyList<MonitorChoice> Monitors { get; private set; } = [];

    public bool HasSeveralMonitors => Monitors.Count > 1;

    public MonitorChoice? SelectedMonitor
    {
        get => Monitors.FirstOrDefault(choice => choice.Session == Monitor?.Session);
        set
        {
            if (value is not null && value.Session != Monitor?.Session)
            {
                _context.Select(value.Session);
            }
        }
    }

    /// <summary>Pages for the monitor's features, top of the pane. Empty while no monitor is connected.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<NavItem> NavItems { get; private set; } = [];

    /// <summary>Pinned to the bottom of the pane; available even without a monitor.</summary>
    public NavItem SettingsItem { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSettingsSelected), nameof(SelectedListItem))]
    public partial NavItem? SelectedNavItem { get; set; }

    /// <summary>
    /// The selection of the page list: nothing while Settings (outside the list) is open, so the list doesn't keep
    /// highlighting the page that was open before.
    /// </summary>
    public NavItem? SelectedListItem
    {
        get => SelectedNavItem is { } item && NavItems.Contains(item) ? item : null;
        set
        {
            if (value is not null)
            {
                SelectedNavItem = value;
            }
        }
    }

    /// <summary>Settings sits apart from the list at the bottom of the pane, so its selection is exposed separately.</summary>
    public bool IsSettingsSelected => SelectedNavItem == SettingsItem;

    [ObservableProperty]
    public partial MainPageViewModel? Page { get; private set; }

    [ObservableProperty]
    public partial ConfirmationViewModel? Confirmation { get; private set; }

    /// <summary>A dialog shown over the window, such as Add rule.</summary>
    [ObservableProperty]
    public partial object? Dialog { get; private set; }

    public void ShowDialog(object dialog) => Dialog = dialog;

    // ------------------------------------------------------------------ Find a setting

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    /// <summary>What the search box finds, best first; empty while it's empty.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSearchResults), nameof(HasNoSearchResults))]
    public partial IReadOnlyList<SearchEntry> SearchResults { get; private set; } = [];

    public bool HasSearchResults => SearchResults.Count > 0;

    /// <summary>Something was typed and nothing matches.</summary>
    public bool HasNoSearchResults => SearchResults.Count == 0 && !string.IsNullOrWhiteSpace(SearchText);

    /// <summary>A search result's page is open; the window points at the setting with this title.</summary>
    public event EventHandler<string>? RevealRequested;

    partial void OnSearchTextChanged(string value)
    {
        SearchResults = SettingsSearch.Find(value, Monitor);
        OnPropertyChanged(nameof(HasNoSearchResults));
    }

    /// <summary>Opens the result's page (the first result when none is given) and points at the setting.</summary>
    [RelayCommand]
    private void OpenSearchResult(SearchEntry? entry)
    {
        entry ??= SearchResults.Count > 0 ? SearchResults[0] : null;
        if (entry is null || NavItems.Append(SettingsItem).FirstOrDefault(item => item.Title == entry.Page) is not { } item)
        {
            return;
        }
        SearchText = string.Empty;
        ShowSetting(entry, item);
    }

    public void ShowSetting(SearchEntry setting)
    {
        if (NavItems.Append(SettingsItem).FirstOrDefault(item => item.Title == setting.Page) is { } item)
        {
            ShowSetting(setting, item);
        }
    }

    private void ShowSetting(SearchEntry entry, NavItem item)
    {
        // Open the page fresh, also when it's already showing (it may be on a sub-page, or scrolled away).
        SelectedNavItem = null;
        SelectedNavItem = item;
        if (entry.SubPage == SettingsSearch.SixAxis && Page is DisplayPageViewModel display)
        {
            display.OpenSixAxisCommand.Execute(null);
        }
        RevealRequested?.Invoke(this, entry.Card ?? entry.Title);
    }

    [RelayCommand]
    public void CloseDialog() => Dialog = null;

    public void Navigate(MainPageViewModel page)
    {
        // Keep a page alive while one of its sub-pages is open; close everything else that is left behind.
        if (Page is { } previous && page.Parent != previous)
        {
            previous.Close();
            if (previous.Parent is { } parent && parent != page)
            {
                parent.Close();
            }
        }
        Page = page;
        _ = page.OnShownAsync();
    }

    public Task<bool> ConfirmAsync(ConfirmationViewModel confirmation)
    {
        _confirmation?.TrySetResult(false);
        _confirmation = new TaskCompletionSource<bool>();
        Confirmation = confirmation;
        return _confirmation.Task;
    }

    /// <summary>The window was opened or brought back: re-read the current page.</summary>
    public Task OnShownAsync() => Page?.OnShownAsync() ?? Task.CompletedTask;

    [RelayCommand]
    private void GoToParent()
    {
        if (Page?.Parent is { } parent)
        {
            Navigate(parent);
        }
    }

    [RelayCommand]
    private void OpenSettings() => SelectedNavItem = SettingsItem;

    [RelayCommand]
    private void ConfirmPrimary() => CloseConfirmation(true);

    [RelayCommand]
    private void ConfirmCancel() => CloseConfirmation(false);

    [RelayCommand]
    private Task Retry() => _context.RescanAsync();

    partial void OnSelectedNavItemChanged(NavItem? value)
    {
        if (value is not null)
        {
            Navigate(value.CreatePage());
        }
    }

    private void CloseConfirmation(bool result)
    {
        Confirmation = null;
        _confirmation?.TrySetResult(result);
        _confirmation = null;
    }

    private void OnMonitorChanged()
    {
        var monitors = _context.Monitors;
        if (!monitors.SequenceEqual(Monitors.Select(choice => choice.Session)))
        {
            Monitors = [.. monitors.Select(monitor => new MonitorChoice(_context.NameOf(monitor), monitor))];
        }
        OnPropertyChanged(nameof(SelectedMonitor));

        // The context reports every rescan; rebuild the pages only when the monitor itself changed.
        if (Monitor == _shownMonitor && SelectedNavItem is not null && NavItems.Count > 0 == (Monitor is not null))
        {
            OnPropertyChanged(nameof(State));
            return;
        }
        _shownMonitor = Monitor;

        OnPropertyChanged(nameof(Monitor));
        OnPropertyChanged(nameof(State));

        // Stay on Settings only if the user went there while monitor pages existed; without a monitor it's the fallback.
        var keepSettings = SelectedNavItem == SettingsItem && NavItems.Count > 0;
        NavItems = Monitor is { } monitor
            ? [
                new("Display", "", () => new DisplayPageViewModel(monitor, this, () => _automation.Automation.SunCycle.IsEnabled)),
                new("Profiles & automation", "", () => new AutomationPageViewModel(monitor, _automation, _location, this)),
                new("OLED care", "", () => new OledCarePageViewModel(monitor, this)),
                new("GamePlus", "", () => new GamePlusPageViewModel(monitor)),
            ]
            : [];

        // Rebuild the page for the new monitor (or show Settings when there is none).
        SelectedNavItem = null;
        SelectedNavItem = keepSettings || NavItems.Count == 0 ? SettingsItem : NavItems[0];
    }
}
