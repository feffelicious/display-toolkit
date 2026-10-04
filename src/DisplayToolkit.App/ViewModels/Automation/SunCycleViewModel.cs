using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using DisplayToolkit.App.Services;
using DisplayToolkit.Automation.Engine;
using DisplayToolkit.Automation.Sun;

namespace DisplayToolkit.App.ViewModels.Automation;

/// <summary>A transition length in the Follow the sun card.</summary>
public sealed record TransitionChoice(int Minutes, string Name);

/// <summary>How the sun cycle changes over the day.</summary>
public sealed record SunCycleModeChoice(SunCycleMode Mode, string Name);

/// <summary>The Follow the sun card: day and night brightness and warmth, and how long the fade takes.</summary>
internal sealed partial class SunCycleViewModel : ObservableObject
{
    private readonly AutomationService _automation;
    private readonly LocationService _location;
    private readonly bool _isLoading = true;

    public SunCycleViewModel(AutomationService automation, LocationService location)
    {
        _automation = automation;
        _location = location;
        var cycle = automation.Automation.SunCycle;
        IsEnabled = cycle.IsEnabled;
        ControlsBrightness = cycle.ControlsBrightness;
        ControlsWarmth = cycle.ControlsWarmth;
        DayBrightness = cycle.DayBrightness;
        NightBrightness = cycle.NightBrightness;
        DayKelvin = cycle.DayKelvin;
        NightKelvin = cycle.NightKelvin;
        Transition = Transitions.FirstOrDefault(choice => choice.Minutes == cycle.TransitionMinutes) ?? Transitions[1];
        Mode = Modes.First(choice => choice.Mode == cycle.Mode);
        _isLoading = false;
        UpdateStatus();
    }

    public IReadOnlyList<TransitionChoice> Transitions { get; } =
    [
        new(30, "30 minutes"),
        new(60, "1 hour"),
        new(90, "1.5 hours"),
        new(120, "2 hours"),
        new(180, "3 hours"),
    ];

    public IReadOnlyList<SunCycleModeChoice> Modes { get; } =
    [
        new(SunCycleMode.SunriseAndSunset, "Around sunrise and sunset"),
        new(SunCycleMode.SunHeight, "All day, with the sun's height"),
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UsesFade))]
    public partial SunCycleModeChoice Mode { get; set; }

    /// <summary>The fade length only matters around sunrise and sunset.</summary>
    public bool UsesFade => Mode.Mode == SunCycleMode.SunriseAndSunset;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial bool ControlsBrightness { get; set; }

    [ObservableProperty]
    public partial bool ControlsWarmth { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DayBrightnessText))]
    public partial double DayBrightness { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NightBrightnessText))]
    public partial double NightBrightness { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DayKelvinText))]
    public partial double DayKelvin { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NightKelvinText))]
    public partial double NightKelvin { get; set; }

    [ObservableProperty]
    public partial TransitionChoice Transition { get; set; }

    public string DayBrightnessText => Format(DayBrightness);

    public string NightBrightnessText => Format(NightBrightness);

    public string DayKelvinText => string.Create(CultureInfo.CurrentCulture, $"{DayKelvin:0} K");

    public string NightKelvinText => string.Create(CultureInfo.CurrentCulture, $"{NightKelvin:0} K");

    /// <summary>"Now: brightness 55, 4450 K", or why it's waiting.</summary>
    [ObservableProperty]
    public partial string Status { get; private set; } = string.Empty;

    public void UpdateStatus()
    {
        Status = !IsEnabled ? "Brightness and warmth change gradually at sunrise and sunset."
            : _location.Current is null ? "Waiting for your location. Set it in Settings."
            : _automation.Monitor?.IsHdrActive == true ? "Paused while HDR is on: the monitor sets brightness and color itself."
            : _automation.State.Reason == AutomationReason.Condition ? $"Paused while {_automation.ActiveProfile?.Name} applies."
            : _automation.State.Reason == AutomationReason.Manual ? "Paused after your own choice, until the next rule event."
            : _automation.State.Reason == AutomationReason.Paused ? "Paused with automation."
            : _automation.SunTarget is { } target ? Now(target)
            : string.Empty;
    }

    partial void OnIsEnabledChanged(bool value) => Save();

    partial void OnControlsBrightnessChanged(bool value) => Save();

    partial void OnControlsWarmthChanged(bool value) => Save();

    partial void OnDayBrightnessChanged(double value) => Save();

    partial void OnNightBrightnessChanged(double value) => Save();

    partial void OnDayKelvinChanged(double value) => Save();

    partial void OnNightKelvinChanged(double value) => Save();

    partial void OnTransitionChanged(TransitionChoice value) => Save();

    partial void OnModeChanged(SunCycleModeChoice value) => Save();

    private static string Now(SunCycleTarget target)
    {
        var parts = new List<string>();
        if (target.Brightness is { } brightness)
        {
            parts.Add(string.Create(CultureInfo.CurrentCulture, $"brightness {brightness}"));
        }
        if (target.Kelvin is { } kelvin)
        {
            parts.Add(string.Create(CultureInfo.CurrentCulture, $"{kelvin} K"));
        }
        return parts.Count == 0 ? "Choose brightness, warmth or both." : $"Now: {string.Join(", ", parts)}.";
    }

    private static string Format(double value) => Math.Round(value).ToString(CultureInfo.CurrentCulture);

    private void Save()
    {
        if (_isLoading)
        {
            return;
        }
        _automation.SaveSunCycle(new SunCycle
        {
            IsEnabled = IsEnabled,
            Mode = Mode.Mode,
            ControlsBrightness = ControlsBrightness,
            ControlsWarmth = ControlsWarmth,
            DayBrightness = (uint)Math.Round(DayBrightness),
            NightBrightness = (uint)Math.Round(NightBrightness),
            DayKelvin = (int)Math.Round(DayKelvin / 100) * 100,
            NightKelvin = (int)Math.Round(NightKelvin / 100) * 100,
            TransitionMinutes = Transition.Minutes,
        });
        UpdateStatus();
    }
}
