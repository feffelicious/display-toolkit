using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.App.Services;
using DisplayToolkit.Core.Features;
using DisplayToolkit.Core.Monitors;

namespace DisplayToolkit.App.ViewModels.Main;

/// <summary>A choice in the app theme combo box.</summary>
public sealed record ThemeChoice(AppTheme Theme, string Name);

/// <summary>App settings, plus the monitor's own system options (key lock, power LED).</summary>
public sealed partial class SettingsPageViewModel : MainPageViewModel
{
    private const string RepositoryUrl = "https://github.com/feffelicious/display-toolkit";

    private readonly AppSettings _settings;
    private readonly LocationService _location;
    private readonly SettingsTransfer _transfer;
    private readonly IMainWindowHost _host;
    private readonly UpdateService _updates;
    private readonly ShortcutService _shortcuts;

    internal SettingsPageViewModel(
        AppSettings settings, LocationService location, SettingsTransfer transfer, UpdateService updates, ShortcutService shortcuts,
        IMainWindowHost host, MonitorViewModel? monitor)
        : base("Settings")
    {
        _shortcuts = shortcuts;
        var actions = shortcuts.Actions;
        Shortcuts = [.. actions.Where(action => !action.Id.StartsWith("input-", StringComparison.Ordinal)).Select(action => new ShortcutRowViewModel(action, shortcuts))];
        InputShortcuts = [.. actions.Where(action => action.Id.StartsWith("input-", StringComparison.Ordinal)).Select(action => new ShortcutRowViewModel(action, shortcuts, isNested: true))];
        shortcuts.Changed += OnShortcutsChanged;
        _updates = updates;
        updates.Changed += OnUpdatesChanged;
        _settings = settings;
        _location = location;
        _transfer = transfer;
        _host = host;
        Monitor = monitor;
        var manual = settings.Current.ManualLocation ?? settings.Current.LastWindowsLocation;
        Latitude = manual?.Latitude.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        Longitude = manual?.Longitude.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        location.Changed += OnLocationChanged;
    }

    public MonitorViewModel? Monitor { get; }

    public bool StartWithWindows
    {
        get => StartupRegistration.IsEnabled;
        set
        {
            StartupRegistration.SetEnabled(value);
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<ThemeChoice> Themes { get; } =
    [
        new(AppTheme.System, "Use system setting"),
        new(AppTheme.Light, "Light"),
        new(AppTheme.Dark, "Dark"),
    ];

    public ThemeChoice SelectedTheme
    {
        get => Themes.First(choice => choice.Theme == _settings.Current.Theme);
        set
        {
            _settings.Update(current => current with { Theme = value.Theme });
            OnPropertyChanged();
        }
    }

    /// <summary>Open quick settings, brightness, picture mode, HDR and Target mode.</summary>
    public IReadOnlyList<ShortcutRowViewModel> Shortcuts { get; }

    /// <summary>One per input of the monitor; empty if it can't switch inputs.</summary>
    public IReadOnlyList<ShortcutRowViewModel> InputShortcuts { get; }

    public bool HasInputShortcuts => InputShortcuts.Count > 0;

    public bool ScrollOverTrayIcon
    {
        get => _settings.Current.ScrollOverTrayIcon;
        set
        {
            _settings.Update(current => current with { ScrollOverTrayIcon = value });
            OnPropertyChanged();
        }
    }

    /// <summary>Show a small overlay when a shortcut switches a profile or changes a setting.</summary>
    public bool ShowShortcutOverlay
    {
        get => _settings.Current.ShowShortcutOverlay;
        set
        {
            _settings.Update(current => current with { ShowShortcutOverlay = value });
            OnPropertyChanged();
        }
    }

    // Location for sunrise and sunset rules

    public bool UseWindowsLocation
    {
        get => _settings.Current.UseWindowsLocation;
        set
        {
            if (value)
            {
                _ = _location.UseWindowsLocationAsync();
            }
            else
            {
                SaveLocation();
            }
            OnPropertyChanged();
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveLocationCommand))]
    public partial string Latitude { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveLocationCommand))]
    public partial string Longitude { get; set; }

    public bool IsLocationValid => ParseLocation() is not null;

    /// <summary>"59.33° N, 18.07° E, from Windows location."</summary>
    public string LocationSummary => _location.Current is { } location
        ? string.Create(CultureInfo.CurrentCulture,
            $"{Math.Abs(location.Latitude):0.00}° {(location.Latitude >= 0 ? "N" : "S")}, {Math.Abs(location.Longitude):0.00}° {(location.Longitude >= 0 ? "E" : "W")}, ")
            + (_location.Source == LocationSource.Windows ? "from Windows location." : "entered by you.")
        : _location.IsWindowsLocationUnavailable
            ? "Windows didn't share a location. Turn on location for desktop apps in Windows Settings, or enter it below."
            : "Not known yet.";

    public bool CheckForUpdates
    {
        get => _settings.Current.CheckForUpdates;
        set
        {
            _settings.Update(current => current with { CheckForUpdates = value });
            OnPropertyChanged();
        }
    }

    /// <summary>"You're up to date. Checked 10:42." or "Version 1.1.0 is available."</summary>
    public string UpdateStatus => _updates.IsChecking
        ? "Checking…"
        : _updates.Status is { Length: > 0 } status ? status : "Display Toolkit looks for new versions on GitHub once a day.";

    public bool IsUpdateAvailable => _updates.Available is not null;

    [RelayCommand]
    private Task CheckForUpdatesNow() => _updates.CheckNowAsync();

    [RelayCommand]
    private void DownloadUpdate() => _updates.OpenReleasePage();

    private void OnUpdatesChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(UpdateStatus));
        OnPropertyChanged(nameof(IsUpdateAvailable));
    }

    private void OnShortcutsChanged(object? sender, EventArgs e)
    {
        foreach (var row in Shortcuts.Concat(InputShortcuts))
        {
            row.Refresh();
        }
    }

    public override void Close()
    {
        _shortcuts.Changed -= OnShortcutsChanged;
        _updates.Changed -= OnUpdatesChanged;
        _location.Changed -= OnLocationChanged;
        base.Close();
    }

    /// <summary>The outcome of the last export or import, shown under the buttons.</summary>
    [ObservableProperty]
    public partial string? TransferStatus { get; private set; }

    [RelayCommand]
    private void ExportSettings()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export settings",
            FileName = "Display Toolkit settings.json",
            Filter = "Display Toolkit settings (*.json)|*.json",
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }
        try
        {
            _transfer.Export(dialog.FileName);
            TransferStatus = $"Exported to {System.IO.Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception exception) when (exception is System.IO.IOException or UnauthorizedAccessException)
        {
            TransferStatus = $"Couldn't save the file: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportSettings()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Import settings", Filter = "Display Toolkit settings (*.json)|*.json" };
        if (dialog.ShowDialog() != true)
        {
            return;
        }
        var confirmed = await _host.ConfirmAsync(new ConfirmationViewModel(
            "Replace your settings?",
            "Profiles, rules, the quick settings layout and preferences are replaced with the ones in the file.",
            "Replace",
            "Export first if you might want the current ones back."));
        if (!confirmed)
        {
            return;
        }
        try
        {
            TransferStatus = _transfer.Import(dialog.FileName)
                ? "Imported."
                : "That file isn't a Display Toolkit export.";
            OnPropertyChanged(string.Empty); // Everything on this page may have changed.
        }
        catch (Exception exception) when (exception is System.IO.IOException or UnauthorizedAccessException)
        {
            TransferStatus = $"Couldn't read the file: {exception.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(IsLocationValid))]
    private void SaveLocation()
    {
        if (ParseLocation() is { } location)
        {
            _location.UseManualLocation(location);
        }
    }

    [RelayCommand]
    private static void OpenLocationSettings() => Process.Start(new ProcessStartInfo("ms-settings:privacy-location") { UseShellExecute = true });

    private SavedLocation? ParseLocation() =>
        double.TryParse(Latitude, NumberStyles.Float, CultureInfo.CurrentCulture, out var latitude)
        && double.TryParse(Longitude, NumberStyles.Float, CultureInfo.CurrentCulture, out var longitude)
        && latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180
            ? new SavedLocation(latitude, longitude)
            : null;

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(UseWindowsLocation));
        OnPropertyChanged(nameof(LocationSummary));
    }

    // The monitor's own system setup (null when unsupported or no monitor)
    public FeatureState? PowerIndicator => Monitor?[FeatureCatalog.PowerIndicator];

    public FeatureState? KeyLock => Monitor?[FeatureCatalog.KeyLock];

    public FeatureState? PowerKeyLock => Monitor?[FeatureCatalog.PowerKeyLock];

    public FeatureState? InputAutoDetection => Monitor?[FeatureCatalog.InputAutoDetection];

    /// <summary>The monitor's own menu can be used from here.</summary>
    public bool CanUseMonitorMenu => Monitor?.Session.CanPressMenuKeys == true;

    /// <summary>Presses one of the monitor's menu keys.</summary>
    [RelayCommand]
    private Task PressMenuKey(MenuKey key) => Monitor?.Session.PressMenuKeyAsync(key) ?? Task.CompletedTask;

    public string Version { get; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "dev";

    public override Task OnShownAsync() => Monitor?.RefreshAsync(
        [FeatureCatalog.PowerIndicator, FeatureCatalog.KeyLock, FeatureCatalog.PowerKeyLock, FeatureCatalog.InputAutoDetection])
        ?? Task.CompletedTask;

    [RelayCommand]
    private static void OpenRepository() => Process.Start(new ProcessStartInfo(RepositoryUrl) { UseShellExecute = true });

    [RelayCommand]
    private static void OpenLogFolder() => Process.Start(new ProcessStartInfo(AppPaths.DataDirectory) { UseShellExecute = true });
}
