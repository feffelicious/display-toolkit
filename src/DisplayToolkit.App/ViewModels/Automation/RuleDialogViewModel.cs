using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.App.Services;
using DisplayToolkit.Automation.Conditions;
using DisplayToolkit.Automation.Profiles;
using DisplayToolkit.Automation.Rules;
using DayFlags = DisplayToolkit.Automation.Rules.Days;

namespace DisplayToolkit.App.ViewModels.Automation;

public enum TriggerKind
{
    App,
    FullscreenGame,
    Time,
    Sun,
    Power,
    Hdr,
}

/// <summary>A card in the first step of the Add rule dialog.</summary>
public sealed record TriggerChoice(TriggerKind Kind, string Title, string Description, string Glyph);

/// <summary>A day chip ("M", "T", …) for schedule rules.</summary>
public sealed partial class DayChip(DayOfWeek day, string label) : ObservableObject
{
    public DayOfWeek Day { get; } = day;

    public string Label { get; } = label;

    public string Name => CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(Day);

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = true;

    [RelayCommand]
    private void Toggle() => IsSelected = !IsSelected;
}

/// <summary>
/// The Add rule dialog (design spec §5.6): step 1 picks what triggers the rule, step 2 its details and the profile.
/// Editing a rule opens straight on step 2.
/// </summary>
internal sealed partial class RuleDialogViewModel : ObservableObject
{
    private static readonly int[] OffsetChoices = [-120, -90, -60, -45, -30, -15, 0, 15, 30, 45, 60, 90, 120];

    private readonly AutomationService _automation;
    private readonly LocationService _location;
    private readonly Action _close;
    private readonly Guid _ruleId;
    private readonly bool _isEnabled;
    private bool _appsLoaded;

    public RuleDialogViewModel(AutomationService automation, LocationService location, Rule? rule, Action close)
    {
        _automation = automation;
        _location = location;
        _close = close;
        _ruleId = rule?.Id ?? Guid.NewGuid();
        _isEnabled = rule?.IsEnabled ?? true;
        IsEditing = rule is not null;

        Triggers =
        [
            new(TriggerKind.App, "An app is open", "Pick a running app or browse for one", ""),
            new(TriggerKind.FullscreenGame, "A game is in full screen", "Any game, detected automatically", ""),
            new(TriggerKind.Time, "At a time", "Daily or on chosen days", ""),
            new(TriggerKind.Sun, "At sunrise or sunset", "Follows your location, with an offset", ""),
            .. SystemSampler.HasBattery() ? [new TriggerChoice(TriggerKind.Power, "Power source changes", "Plugged in or on battery", "")] : Array.Empty<TriggerChoice>(),
            new(TriggerKind.Hdr, "HDR turns on or off", "Windows HDR for this monitor", ""),
        ];
        Profiles = automation.Profiles;
        SelectedProfile = Profiles.FirstOrDefault(profile => profile.Id == rule?.ProfileId) ?? (Profiles.Count > 0 ? Profiles[0] : null);
        Days = [.. Enum.GetValues<DayOfWeek>().OrderBy(day => ((int)day + 6) % 7)
            .Select(day => new DayChip(day, CultureInfo.CurrentCulture.DateTimeFormat.GetShortestDayName(day)))];
        foreach (var day in Days)
        {
            day.PropertyChanged += (_, _) => OnDetailsChanged();
        }
        Offsets = [.. OffsetChoices.Select(minutes => new OffsetChoice(minutes))];
        Offset = Offsets.First(offset => offset.Minutes == 0);
        Hours = [.. Enumerable.Range(0, 24)];
        Minutes = [.. Enumerable.Range(0, 12).Select(step => step * 5)];

        if (rule is not null)
        {
            Load(rule.Trigger);
        }
    }

    public bool IsEditing { get; }

    public string Title => IsEditing ? "Edit rule" : "Add a rule";

    public IReadOnlyList<TriggerChoice> Triggers { get; }

    /// <summary>Null on step 1; the chosen trigger on step 2.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChoosingTrigger), nameof(DetailsTitle), nameof(IsApp), nameof(IsTime), nameof(IsSun),
        nameof(IsPower), nameof(IsHdr), nameof(IsFullscreenGame), nameof(HasDays), nameof(CanSave), nameof(SunSummary))]
    public partial TriggerKind? Kind { get; set; }

    public bool IsChoosingTrigger => Kind is null;

    public string DetailsTitle => Triggers.FirstOrDefault(trigger => trigger.Kind == Kind)?.Title ?? string.Empty;

    public bool IsApp => Kind == TriggerKind.App;

    public bool IsFullscreenGame => Kind == TriggerKind.FullscreenGame;

    public bool IsTime => Kind == TriggerKind.Time;

    public bool IsSun => Kind == TriggerKind.Sun;

    public bool IsPower => Kind == TriggerKind.Power;

    public bool IsHdr => Kind == TriggerKind.Hdr;

    public bool HasDays => Kind is TriggerKind.Time or TriggerKind.Sun;

    public IReadOnlyList<Profile> Profiles { get; }

    public bool HasProfiles => Profiles.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial Profile? SelectedProfile { get; set; }

    public IReadOnlyList<DayChip> Days { get; }

    // App
    public ObservableCollection<AppChoice> Apps { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial AppChoice? SelectedApp { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingApps { get; private set; }

    // Time
    public IReadOnlyList<int> Hours { get; }

    public IReadOnlyList<int> Minutes { get; }

    [ObservableProperty]
    public partial int Hour { get; set; } = 8;

    [ObservableProperty]
    public partial int Minute { get; set; }

    // Sun
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSunrise), nameof(IsSunset), nameof(SunSummary))]
    public partial SunEvent SunEvent { get; set; } = SunEvent.Sunset;

    public bool IsSunrise => SunEvent == SunEvent.Sunrise;

    public bool IsSunset => SunEvent == SunEvent.Sunset;

    public IReadOnlyList<OffsetChoice> Offsets { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SunSummary))]
    public partial OffsetChoice Offset { get; set; }

    /// <summary>"From Windows location. Today this runs at 17:51."</summary>
    public string SunSummary
    {
        get
        {
            if (_location.Current is not { } location)
            {
                return _location.IsWindowsLocationUnavailable
                    ? "Windows didn't share your location. Turn on location for desktop apps, or enter it in Settings."
                    : "Your location isn't known yet. Set it in Settings.";
            }
            var source = _location.Source == LocationSource.Windows ? "From Windows location." : "From the location in Settings.";
            var trigger = new SunTrigger(SunEvent, Offset.Minutes, Days: DayFlags.Every);
            return trigger.OccurrenceOn(DateOnly.FromDateTime(DateTime.Now), TimeZoneInfo.Local, location) is { } today
                ? $"{source} Today this runs at {AutomationText.Time(today)}."
                : $"{source} The sun doesn't {(SunEvent == SunEvent.Sunrise ? "rise" : "set")} here today.";
        }
    }

    // Power and HDR
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PluggedIn))]
    public partial bool OnBattery { get; set; } = true;

    public bool PluggedIn => !OnBattery;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HdrOff))]
    public partial bool HdrOn { get; set; } = true;

    public bool HdrOff => !HdrOn;

    public bool CanSave => Kind is { } kind && SelectedProfile is not null
        && (kind != TriggerKind.App || SelectedApp is not null)
        && (!HasDays || Days.Any(day => day.IsSelected));

    [RelayCommand]
    private async Task ChooseTrigger(TriggerKind kind)
    {
        Kind = kind;
        if (kind == TriggerKind.App && !_appsLoaded)
        {
            await LoadAppsAsync();
        }
    }

    [RelayCommand]
    private void Back() => Kind = null;

    [RelayCommand]
    private void SetSunEvent(SunEvent sunEvent) => SunEvent = sunEvent;

    [RelayCommand]
    private void SetOnBattery(bool onBattery) => OnBattery = onBattery;

    [RelayCommand]
    private void SetHdrOn(bool on) => HdrOn = on;

    [RelayCommand]
    private void Cancel() => _close();

    [RelayCommand]
    private void Browse()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Apps (*.exe)|*.exe", Title = "Choose an app" };
        if (dialog.ShowDialog() == true)
        {
            var app = RunningApps.FromFile(dialog.FileName);
            if (Apps.FirstOrDefault(existing => string.Equals(existing.ProcessName, app.ProcessName, StringComparison.OrdinalIgnoreCase)) is { } existing)
            {
                app = existing;
            }
            else
            {
                Apps.Insert(0, app);
            }
            SelectedApp = app;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        _automation.SaveRule(new Rule { Id = _ruleId, ProfileId = SelectedProfile!.Id, Trigger = BuildTrigger(), IsEnabled = _isEnabled });
        _close();
    }

    private Trigger BuildTrigger()
    {
        var days = Days.Where(day => day.IsSelected).Aggregate(DayFlags.None, (all, day) => all | DaysExtensions.Of(day.Day));
        return Kind switch
        {
            TriggerKind.App => new AppTrigger(SelectedApp!.ProcessName, SelectedApp.DisplayName),
            TriggerKind.FullscreenGame => new FullscreenGameTrigger(),
            TriggerKind.Time => new TimeTrigger(new TimeOnly(Hour, Minute), days),
            TriggerKind.Sun => new SunTrigger(SunEvent, Offset.Minutes, days),
            TriggerKind.Power => new PowerTrigger(OnBattery ? PowerSource.Battery : PowerSource.PluggedIn),
            _ => new HdrTrigger(HdrOn),
        };
    }

    private void Load(Trigger trigger)
    {
        switch (trigger)
        {
            case AppTrigger app:
                Kind = TriggerKind.App;
                SelectedApp = new AppChoice(app.ProcessName, app.DisplayName, null);
                Apps.Add(SelectedApp);
                _ = LoadAppsAsync();
                break;
            case FullscreenGameTrigger:
                Kind = TriggerKind.FullscreenGame;
                break;
            case TimeTrigger time:
                Kind = TriggerKind.Time;
                Hour = time.Time.Hour;
                Minute = time.Time.Minute - (time.Time.Minute % 5);
                LoadDays(time.Days);
                break;
            case SunTrigger sun:
                Kind = TriggerKind.Sun;
                SunEvent = sun.Event;
                Offset = Offsets.FirstOrDefault(offset => offset.Minutes == sun.OffsetMinutes) ?? Offset;
                LoadDays(sun.Days);
                break;
            case PowerTrigger power:
                Kind = TriggerKind.Power;
                OnBattery = power.Source == PowerSource.Battery;
                break;
            case HdrTrigger hdr:
                Kind = TriggerKind.Hdr;
                HdrOn = hdr.IsOn;
                break;
        }
    }

    private void LoadDays(DayFlags days)
    {
        foreach (var day in Days)
        {
            day.IsSelected = days.Includes(day.Day);
        }
    }

    private async Task LoadAppsAsync()
    {
        _appsLoaded = true;
        IsLoadingApps = true;
        var running = await Task.Run(RunningApps.List);
        foreach (var app in running)
        {
            var existing = Apps.FirstOrDefault(choice => string.Equals(choice.ProcessName, app.ProcessName, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                Apps.Add(app);
            }
            else if (existing.Icon is null)
            {
                // The rule's own app is running: show it with its icon, and keep it selected.
                var index = Apps.IndexOf(existing);
                Apps[index] = app with { DisplayName = existing.DisplayName };
                if (SelectedApp == existing)
                {
                    SelectedApp = Apps[index];
                }
            }
        }
        IsLoadingApps = false;
    }

    private void OnDetailsChanged() => OnPropertyChanged(nameof(CanSave));

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(CanSave))
        {
            SaveCommand.NotifyCanExecuteChanged();
        }
    }
}

/// <summary>A sun offset: "30 min before", "At the moment", "1 h after".</summary>
public sealed record OffsetChoice(int Minutes)
{
    public string Name => Minutes switch
    {
        0 => "Exactly",
        < 0 => $"{Format(-Minutes)} before",
        _ => $"{Format(Minutes)} after",
    };

    private static string Format(int minutes) => minutes % 60 == 0 ? $"{minutes / 60} h" : minutes > 60 ? $"{minutes / 60} h {minutes % 60} min" : $"{minutes} min";
}
