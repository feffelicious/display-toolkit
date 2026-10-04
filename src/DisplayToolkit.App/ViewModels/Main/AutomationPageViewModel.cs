using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.App.Services;
using DisplayToolkit.App.ViewModels.Automation;
using DisplayToolkit.Automation.Engine;
using DisplayToolkit.Automation.Profiles;
using DisplayToolkit.Automation.Rules;

namespace DisplayToolkit.App.ViewModels.Main;

/// <summary>A profile in the list on the left of the profiles card.</summary>
public sealed partial class ProfileListItem(Profile profile, string subtitle) : ObservableObject
{
    [ObservableProperty]
    public partial Profile Profile { get; set; } = profile;

    [ObservableProperty]
    public partial string Subtitle { get; set; } = subtitle;
}

/// <summary>
/// Profiles &amp; automation (design spec §5.6): the profiles card (list and editor), today's day strip with pause, and
/// the rules in priority order.
/// </summary>
internal sealed partial class AutomationPageViewModel : MainPageViewModel
{
    private readonly AutomationService _automation;
    private readonly LocationService _location;
    private readonly IMainWindowHost _host;

    public AutomationPageViewModel(MonitorViewModel monitor, AutomationService automation, LocationService location, IMainWindowHost host)
        : base("Profiles & automation")
    {
        Monitor = monitor;
        _automation = automation;
        _location = location;
        _host = host;
        SunCycle = new SunCycleViewModel(automation, location);
        automation.Changed += OnAutomationChanged;
        location.Changed += OnAutomationChanged;
        Refresh();
        SelectedProfile = Profiles.FirstOrDefault(item => item.Profile.Id == automation.State.ProfileId) ?? Profiles.FirstOrDefault();
    }

    public MonitorViewModel Monitor { get; }

    public ObservableCollection<ProfileListItem> Profiles { get; } = [];

    public bool HasProfiles => Profiles.Count > 0;

    [ObservableProperty]
    public partial ProfileListItem? SelectedProfile { get; set; }

    /// <summary>The editor for <see cref="SelectedProfile"/>.</summary>
    [ObservableProperty]
    public partial ProfileEditorViewModel? Editor { get; private set; }

    /// <summary>The editor belongs to a profile that was just created: the view focuses its name.</summary>
    public bool IsNewProfile { get; private set; }

    [ObservableProperty]
    public partial DayStripViewModel? Day { get; private set; }

    /// <summary>"Night is active, set by the sunset rule at 18:21".</summary>
    [ObservableProperty]
    public partial string Status { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPaused { get; private set; }

    /// <summary>A profile picked by hand is in effect; "Back to automatic" ends it now.</summary>
    [ObservableProperty]
    public partial bool IsManual { get; private set; }

    public SunCycleViewModel SunCycle { get; }

    public ObservableCollection<RuleItemViewModel> Rules { get; } = [];

    public bool HasRules => Rules.Count > 0;

    public override void Close()
    {
        _automation.Changed -= OnAutomationChanged;
        _location.Changed -= OnAutomationChanged;
        base.Close();
    }

    partial void OnSelectedProfileChanged(ProfileListItem? value)
    {
        if (value is null)
        {
            Editor = null;
        }
        else if (Editor?.Id != value.Profile.Id)
        {
            Editor = new ProfileEditorViewModel(value.Profile, _automation, Monitor, _host, Select);
        }
    }

    [RelayCommand]
    private void NewProfile()
    {
        // Start from what the monitor shows now. HDR stays out: switching it blanks the screen, so it's opt-in.
        var settings = AutomationService.SettingsFor(Monitor)
            .Where(setting => setting.IsCommon && setting.Id != ProfileSettings.WindowsHdrId)
            .Select(setting => setting.Id);
        var profile = new Profile { Id = Guid.NewGuid(), Name = UniqueName(), Settings = _automation.CaptureCurrent(settings) };
        _automation.SaveProfile(profile);
        IsNewProfile = true;
        Select(profile);
        IsNewProfile = false;
    }

    [RelayCommand]
    private void AddRule() => OpenRuleDialog(null);

    [RelayCommand]
    private void PauseForAnHour() => _automation.Pause(TimeSpan.FromHours(1));

    [RelayCommand]
    private void PauseUntilTomorrow() => _automation.PauseUntilMorning();

    [RelayCommand]
    private void PauseUntilResumed() => _automation.Pause(null);

    [RelayCommand]
    private void Resume() => _automation.Resume();

    [RelayCommand]
    private Task BackToAutomatic() => _automation.ResumeAutomaticAsync();

    private void OpenRuleDialog(Rule? rule) =>
        _host.ShowDialog(new RuleDialogViewModel(_automation, _location, rule, _host.CloseDialog));

    private void Select(Profile profile)
    {
        Refresh();
        SelectedProfile = Profiles.FirstOrDefault(item => item.Profile.Id == profile.Id);
    }

    private void OnAutomationChanged(object? sender, EventArgs e) => Refresh();

    /// <summary>
    /// Brings the lists up to date in place, so a save from the editor doesn't replace the editor (and the focus
    /// inside it).
    /// </summary>
    private void Refresh()
    {
        var profiles = _automation.Profiles;
        for (var i = Profiles.Count - 1; i >= 0; i--)
        {
            if (profiles.All(profile => profile.Id != Profiles[i].Profile.Id))
            {
                Profiles.RemoveAt(i);
            }
        }
        for (var i = 0; i < profiles.Count; i++)
        {
            var subtitle = AutomationText.ProfileSubtitle(profiles[i], _automation.Rules);
            if (Profiles.FirstOrDefault(item => item.Profile.Id == profiles[i].Id) is { } item)
            {
                item.Profile = profiles[i];
                item.Subtitle = subtitle;
            }
            else
            {
                Profiles.Insert(i, new ProfileListItem(profiles[i], subtitle));
            }
        }
        if (SelectedProfile is not null && !Profiles.Contains(SelectedProfile))
        {
            SelectedProfile = Profiles.FirstOrDefault();
        }
        OnPropertyChanged(nameof(HasProfiles));

        Rules.Clear();
        for (var i = 0; i < _automation.Rules.Count; i++)
        {
            Rules.Add(new RuleItemViewModel(_automation.Rules[i], i, _automation, _location.Current, OpenRuleDialog));
        }
        OnPropertyChanged(nameof(HasRules));

        Day = new DayStripViewModel(_automation.Engine, _automation.Automation, DateTimeOffset.Now);
        IsPaused = _automation.Engine.IsPaused;
        IsManual = _automation.IsManual;
        Status = StatusLine();
        SunCycle.UpdateStatus();
    }

    private string StatusLine()
    {
        var state = _automation.State;
        var profile = _automation.ActiveProfile?.Name;
        return state.Reason switch
        {
            AutomationReason.Schedule => $"{profile} is active, set by the {RuleName(state.Rule!)} at {AutomationText.Time(state.Since!.Value)}",
            AutomationReason.Condition => $"{profile} is active: {AutomationText.Title(state.Rule!.Trigger).ToLower(System.Globalization.CultureInfo.CurrentCulture)}",
            AutomationReason.None when !HasRules => "Add a rule to switch profiles automatically.",
            AutomationReason.None => "No rule applies right now.",
            _ => AutomationText.Status(_automation.Engine, _automation.Automation),
        };
    }

    private static string RuleName(Rule rule) => rule.Trigger switch
    {
        SunTrigger { Event: SunEvent.Sunrise } => "sunrise rule",
        SunTrigger => "sunset rule",
        _ => "schedule",
    };

    private string UniqueName()
    {
        for (var number = 1; ; number++)
        {
            var name = number == 1 ? "New profile" : $"New profile {number}";
            if (_automation.Profiles.All(profile => profile.Name != name))
            {
                return name;
            }
        }
    }
}
