using System.Diagnostics;
using System.Reflection;
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

    internal SettingsPageViewModel(AppSettings settings, MonitorViewModel? monitor)
        : base("Settings")
    {
        _settings = settings;
        Monitor = monitor;
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
