using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.App.ViewModels.Pages;
using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels.Main;

/// <summary>One entry of the "Switch input" menu.</summary>
public sealed record InputChoice(string Name, IRelayCommand SwitchCommand);

/// <summary>An Aura color: the choice, and the color its swatch shows.</summary>
public sealed record AuraSwatch(OptionItemViewModel Choice, string Color);

/// <summary>
/// The Display page: the everyday picture controls first, color tuning in an expander, six-axis color on a sub-page.
/// Properties are null when the monitor doesn't support the feature; the view hides those cards.
/// </summary>
public sealed partial class DisplayPageViewModel : MainPageViewModel
{
    private readonly IMainWindowHost _host;
    private readonly Func<bool> _isSunCycleOn;

    /// <param name="isSunCycleOn">Whether Follow the sun is on, which sets brightness and warmth again after a reset.</param>
    public DisplayPageViewModel(MonitorViewModel monitor, IMainWindowHost host, Func<bool> isSunCycleOn)
        : base("Display")
    {
        Monitor = monitor;
        _host = host;
        _isSunCycleOn = isSunCycleOn;
        Observe(monitor, (_, e) =>
        {
            if (e.PropertyName is nameof(MonitorViewModel.WindowsHdr) or nameof(MonitorViewModel.Link))
            {
                OnPropertyChanged(nameof(UseHdr));
                OnPropertyChanged(nameof(IsHdrActive));
                OnPropertyChanged(nameof(IsSdr));
                OnPropertyChanged(nameof(HdrPresets));
                OnPropertyChanged(nameof(LinkText));
            }
        });
        if (ColorTemperature is { } colorTemperature)
        {
            Observe(colorTemperature, (_, _) => OnPropertyChanged(nameof(ShowRgbGains)));
        }
        if (AuraEffect is { } auraEffect)
        {
            Observe(auraEffect, (_, _) =>
            {
                OnPropertyChanged(nameof(HasAuraColor));
                OnPropertyChanged(nameof(AuraDescription));
            });
        }
        if (PictureMode is { } pictureMode)
        {
            Observe(pictureMode, (_, _) => OnPropertyChanged(nameof(ResetModeTooltip)));
        }
        if (HdrMode is { } hdrMode)
        {
            Observe(hdrMode, (_, _) => OnPropertyChanged(nameof(HdrPresets)));
        }

        InputChoices = [.. monitor.OptionsOf(FeatureCatalog.InputSource)
            .Select(option => new InputChoice(option.Name, new RelayCommand(() => InputSource?.Write(option.Value))))];
    }

    public MonitorViewModel Monitor { get; }

    public string LinkText => Monitor.Link?.ToString() is { Length: > 0 } link ? link : "Connected";

    // Picture
    public FeatureState? Brightness => Monitor[FeatureCatalog.Brightness];

    public FeatureState? Contrast => Monitor[FeatureCatalog.Contrast];

    public FeatureState? PictureMode => Monitor[FeatureCatalog.PictureMode];

    public FeatureState? HdrMode => Monitor[FeatureCatalog.HdrMode];

    public bool CanUseHdr => Monitor.WindowsHdr?.IsSupported == true;

    public bool IsHdrActive => Monitor.IsHdrActive;

    /// <summary>Color settings only apply in SDR; in HDR the monitor manages color itself.</summary>
    public bool IsSdr => !Monitor.IsHdrActive;

    public bool UseHdr
    {
        get => Monitor.IsHdrActive;
        set => _ = Monitor.SetWindowsHdrAsync(value);
    }

    /// <summary>HDR presets for the signal being received (HDR10 or Dolby Vision).</summary>
    public IReadOnlyList<FeatureOption> HdrPresets => HdrMode is { } hdrMode && HdrModeFeature.GroupOf(hdrMode.Value) is var group and not 0
        ? [.. hdrMode.Options.Where(option => HdrModeFeature.GroupOf(option.Value) == group)]
        : [];

    // Color
    public FeatureState? ColorGamut => Monitor[FeatureCatalog.ColorGamut];

    public FeatureState? ColorTemperature => Monitor[FeatureCatalog.ColorTemperature];

    /// <summary>Red/green/blue only apply with the Custom color temperature (or always, without presets).</summary>
    public bool ShowRgbGains => ColorTemperature is null || ColorTemperature.Value == 11;

    /// <summary>The Color section has at least one setting on this monitor.</summary>
    public bool HasColorSettings =>
        new[] { ColorGamut, ColorTemperature, RedGain, GreenGain, BlueGain, Gamma, Saturation, Sharpness }.Any(state => state is not null);

    public FeatureState? RedGain => Monitor[FeatureCatalog.RedGain];

    public FeatureState? GreenGain => Monitor[FeatureCatalog.GreenGain];

    public FeatureState? BlueGain => Monitor[FeatureCatalog.BlueGain];

    public FeatureState? Gamma => Monitor[FeatureCatalog.Gamma];

    public FeatureState? Saturation => Monitor[FeatureCatalog.Saturation];

    public FeatureState? Sharpness => Monitor[FeatureCatalog.Sharpness];

    /// <summary>Sharpness is a standard setting; on ASUS monitors it also gets ASUS's name for it.</summary>
    public string SharpnessTitle => Monitor.Session.Id.Model.StartsWith("AUS", StringComparison.OrdinalIgnoreCase)
        ? "Sharpness (VividPixel)"
        : "Sharpness";

    public bool HasSixAxis => Monitor[FeatureCatalog.SaturationRed] is not null;

    /// <summary>The monitor can put the current picture mode back to factory settings.</summary>
    public bool CanResetMode => Monitor.Session.CanResetCurrentMode;

    private string ModeName => PictureMode?.SelectedOption?.Name ?? "this mode";

    /// <summary>"Reset User to factory settings": the reset button's tooltip and accessible name.</summary>
    public string ResetModeTooltip => $"Reset {ModeName} to factory settings";

    /// <summary>The reset is being sent and the monitor read again.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ResetModeCommand))]
    public partial bool IsResetting { get; private set; }

    // Gaming and comfort
    public FeatureState? BlueLight => Monitor[FeatureCatalog.BlueLightFilter];

    public FeatureState? ShadowBoost => Monitor[FeatureCatalog.ShadowBoost];

    public FeatureState? VariableRefreshRate => Monitor[FeatureCatalog.VariableRefreshRate];

    public FeatureState? FrameRateBoost => Monitor[FeatureCatalog.FrameRateBoost];

    public FeatureState? UpscalingSharpness => Monitor[FeatureCatalog.UpscalingSharpness];

    // Lighting
    public FeatureState? AuraEffect => Monitor[FeatureCatalog.AuraEffect];

    public FeatureState? AuraColor => Monitor[FeatureCatalog.AuraColor];

    /// <summary>Static, Breathing and Strobing have a color; the other effects don't.</summary>
    public bool HasAuraColor => AuraColor is not null && AuraEffect?.Value is 4 or 5 or 6;

    public string AuraDescription => AuraEffect?.Value == 1
        ? "Aura Sync: ASUS's Aura software drives the light over USB. Without it, the light stays dark."
        : "The monitor's RGB lighting.";

    /// <summary>The colors as swatches, in the monitor menu's order.</summary>
    public IReadOnlyList<AuraSwatch> AuraSwatches => AuraColor is { } color
        ? [.. color.Choices.Select(choice => new AuraSwatch(choice, SwatchColors.GetValueOrDefault(choice.Value, "#808080")))]
        : [];

    private static readonly Dictionary<uint, string> SwatchColors = new()
    {
        [1] = "#E5484D",
        [2] = "#46A758",
        [3] = "#3E63DD",
        [4] = "#05A2C2",
        [5] = "#D6409F",
        [6] = "#F5D90A",
    };

    // Sound and input
    public FeatureState? Volume => Monitor[FeatureCatalog.Volume];

    public FeatureState? Mute => Monitor[FeatureCatalog.Mute];

    public FeatureState? InputSource => Monitor[FeatureCatalog.InputSource];

    /// <summary>
    /// Inputs to switch to. Offered as actions rather than a selection: the monitor's input register isn't reliable
    /// while input auto-detection is on, so the "current" input can't be shown truthfully.
    /// </summary>
    public IReadOnlyList<InputChoice> InputChoices { get; }

    public override Task OnShownAsync() => Monitor.RefreshAsync();

    [RelayCommand(CanExecute = nameof(CanStartReset))]
    private async Task ResetMode()
    {
        var confirmed = await _host.ConfirmAsync(new ConfirmationViewModel(
            $"Reset {ModeName}?",
            $"Brightness, contrast, color temperature, gamma and the other settings of {ModeName} go back to their factory values. Other modes stay as they are.",
            "Reset",
            _isSunCycleOn() ? "Follow the sun sets brightness and warmth again at its next change." : null));
        if (!confirmed)
        {
            return;
        }

        IsResetting = true;
        try
        {
            await Monitor.Session.ResetCurrentModeAsync();
        }
        finally
        {
            IsResetting = false;
        }
    }

    private bool CanStartReset() => !IsResetting;

    [RelayCommand]
    private void OpenSixAxis() => _host.Navigate(new SixAxisPageViewModel(Monitor) { Parent = this });

    [RelayCommand]
    private void OpenMonitorInformation() => _host.Navigate(new MonitorInfoPageViewModel(Monitor) { Parent = this });
}
