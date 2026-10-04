using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels.Main;

/// <summary>OLED care: pixel cleaning, panel protection and the proximity sensor.</summary>
public sealed partial class OledCarePageViewModel : MainPageViewModel
{
    /// <summary>
    /// ASUS quotes about 6 minutes for a pixel-cleaning run on its OLED monitors. The monitor doesn't report when it's
    /// done, so the progress shown is an estimate.
    /// </summary>
    private static readonly TimeSpan CleaningDuration = TimeSpan.FromMinutes(6);

    private readonly IMainWindowHost _host;
    private readonly DispatcherTimer _cleaningTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private DateTime _cleaningEndsAt;

    public OledCarePageViewModel(MonitorViewModel monitor, IMainWindowHost host)
        : base("OLED care")
    {
        Monitor = monitor;
        _host = host;
        _cleaningTimer.Tick += (_, _) => UpdateCleaningProgress();
        if (ProximityDistance is { } distance)
        {
            Observe(distance, (_, _) => OnPropertyChanged(nameof(IsProximityOn)));
        }
        if (ProximitySensitivity is { } sensitivity)
        {
            Observe(sensitivity, (_, _) => OnPropertyChanged(nameof(SensitivityText)));
        }
    }

    public MonitorViewModel Monitor { get; }

    public FeatureState? PixelCleaning => Monitor[FeatureCatalog.PixelCleaning];

    public FeatureState? PixelCleaningReminder => Monitor[FeatureCatalog.PixelCleaningReminder];

    // Panel protection
    public FeatureState? ScreenDimming => Monitor[FeatureCatalog.ScreenDimming];

    public FeatureState? LogoDetection => Monitor[FeatureCatalog.LogoDetection];

    public FeatureState? TaskbarDetection => Monitor[FeatureCatalog.TaskbarDetection];

    public FeatureState? BoundaryDetection => Monitor[FeatureCatalog.BoundaryDetection];

    public FeatureState? OuterDimming => Monitor[FeatureCatalog.OuterDimming];

    public FeatureState? GlobalDimming => Monitor[FeatureCatalog.GlobalDimming];

    public FeatureState? UniformBrightness => Monitor[FeatureCatalog.UniformBrightness];

    public FeatureState? AntiFlicker => Monitor[FeatureCatalog.OledAntiFlicker];

    public FeatureState? ScreenMove => Monitor[FeatureCatalog.ScreenMove];

    // Proximity sensor
    public FeatureState? ProximityDistance => Monitor[FeatureCatalog.ProximityDistance];

    public FeatureState? ProximityScreenOff => Monitor[FeatureCatalog.ProximityScreenOff];

    public FeatureState? ProximitySensitivity => Monitor[FeatureCatalog.ProximitySensitivity];

    /// <summary>The menu offers levels 1–5; 0 is what the monitor reports after an app tried to change it.</summary>
    public string SensitivityText => ProximitySensitivity?.Value is { } level and > 0
        ? string.Create(CultureInfo.CurrentCulture, $"Level {level}")
        : "Not set";

    /// <summary>Screen-off time and sensitivity only matter while the sensor is on.</summary>
    public bool IsProximityOn => ProximityDistance?.Value is not (null or 0);

    [ObservableProperty]
    public partial bool IsCleaning { get; private set; }

    /// <summary>0–1, time-based: the monitor doesn't report progress (and may not answer at all) while cleaning.</summary>
    [ObservableProperty]
    public partial double CleaningProgress { get; private set; }

    [ObservableProperty]
    public partial string CleaningRemaining { get; private set; } = string.Empty;

    public override Task OnShownAsync() => Monitor.RefreshAsync();

    /// <summary>Leaving the page doesn't stop a running clean, but the countdown belongs to this page instance.</summary>
    public override void Close()
    {
        _cleaningTimer.Stop();
        base.Close();
    }

    [RelayCommand]
    private async Task RunPixelCleaning()
    {
        if (PixelCleaning is not { } pixelCleaning)
        {
            return;
        }

        var confirmed = await _host.ConfirmAsync(new ConfirmationViewModel(
            "Run pixel cleaning now?",
            "The screen turns off for several minutes while the panel refreshes. Leave the monitor plugged in and don't turn it off.",
            "Run pixel cleaning",
            "Display Toolkit can't change any settings until cleaning finishes."));
        if (!confirmed)
        {
            return;
        }

        pixelCleaning.Write(1);
        _cleaningEndsAt = DateTime.UtcNow + CleaningDuration;
        IsCleaning = true;
        UpdateCleaningProgress();
        _cleaningTimer.Start();
    }

    private void UpdateCleaningProgress()
    {
        var remaining = _cleaningEndsAt - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            _cleaningTimer.Stop();
            IsCleaning = false;
            _ = Monitor.RefreshAsync();
            return;
        }

        CleaningProgress = 1 - (remaining / CleaningDuration);
        var minutes = (int)Math.Ceiling(remaining.TotalMinutes);
        CleaningRemaining = minutes <= 1 ? "Less than a minute left" : $"About {minutes} minutes left";
    }
}
