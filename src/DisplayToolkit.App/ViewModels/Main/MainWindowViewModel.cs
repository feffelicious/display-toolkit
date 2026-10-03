using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.App.Services;

namespace DisplayToolkit.App.ViewModels.Main;

/// <summary>A navigation pane entry.</summary>
public sealed record NavItem(string Title, string Glyph, Func<MainPageViewModel> CreatePage);

/// <summary>The main window: navigation pane, current page (or sub-page) and the in-window confirmation dialog.</summary>
internal sealed partial class MainWindowViewModel : ObservableObject, IMainWindowHost
{
    private readonly MonitorContext _context;
    private readonly AppSettings _settings;
    private readonly AutomationService _automation;
    private readonly LocationService _location;
    private TaskCompletionSource<bool>? _confirmation;

    public MainWindowViewModel(MonitorContext context, AppSettings settings, AutomationService automation, LocationService location)
    {
        _context = context;
        _settings = settings;
        _automation = automation;
        _location = location;
        context.Changed += (_, _) => OnMonitorChanged();
        SettingsItem = new NavItem("Settings", "", () => new SettingsPageViewModel(_settings, _location, Monitor));
        OnMonitorChanged();
    }

    public MonitorViewModel? Monitor => _context.Current;

    public MonitorDiscoveryState State => _context.State;

    /// <summary>Pages for the monitor's features, top of the pane. Empty while no monitor is connected.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<NavItem> NavItems { get; private set; } = [];

    /// <summary>Pinned to the bottom of the pane; available even without a monitor.</summary>
    public NavItem SettingsItem { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSettingsSelected))]
    public partial NavItem? SelectedNavItem { get; set; }

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
        OnPropertyChanged(nameof(Monitor));
        OnPropertyChanged(nameof(State));

        // Stay on Settings only if the user went there while monitor pages existed; without a monitor it's the fallback.
        var keepSettings = SelectedNavItem == SettingsItem && NavItems.Count > 0;
        NavItems = Monitor is { } monitor
            ? [
                new("Display", "", () => new DisplayPageViewModel(monitor, this)),
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
