using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.App.Services;
using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels.Main;

/// <summary>A choice in the app theme combo box.</summary>
public sealed record ThemeChoice(AppTheme Theme, string Name);

/// <summary>App settings, plus the monitor's own system options (key lock, power LED).</summary>
public sealed partial class SettingsPageViewModel : MainPageViewModel
{
    private const string RepositoryUrl = "https://github.com/feffelicious/display-toolkit";

    private readonly AppSettings _settings;
    private readonly LocationService _location;

    internal SettingsPageViewModel(AppSettings settings, LocationService location, MonitorViewModel? monitor)
        : base("Settings")
    {
        _settings = settings;
        _location = location;
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

    /// <summary>Show a small overlay when a shortcut switches a profile.</summary>
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

    public override void Close()
    {
        _location.Changed -= OnLocationChanged;
        base.Close();
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

    public string Version { get; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "dev";

    public override Task OnShownAsync() => Monitor?.RefreshAsync(
        [FeatureCatalog.PowerIndicator, FeatureCatalog.KeyLock, FeatureCatalog.PowerKeyLock, FeatureCatalog.InputAutoDetection])
        ?? Task.CompletedTask;

    [RelayCommand]
    private static void OpenRepository() => Process.Start(new ProcessStartInfo(RepositoryUrl) { UseShellExecute = true });

    [RelayCommand]
    private static void OpenLogFolder() => Process.Start(new ProcessStartInfo(AppPaths.DataDirectory) { UseShellExecute = true });
}
