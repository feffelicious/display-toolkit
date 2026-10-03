using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.App.Services;
using DisplayToolkit.App.ViewModels.Main;
using DisplayToolkit.Automation.Profiles;

namespace DisplayToolkit.App.ViewModels.Automation;

/// <summary>
/// The detail side of the profiles card: name, glyph, shortcut and the settings the profile includes. Every edit is
/// saved at once, as in Windows Settings.
/// </summary>
internal sealed partial class ProfileEditorViewModel : ObservableObject
{
    /// <summary>The glyphs a profile can have: monitor, sun, moon, game, video, music, color, bolt, home, work, cup, leaf.</summary>
    public static IReadOnlyList<string> GlyphChoices { get; } =
        ["", "", "", "", "", "", "", "", "", "", "", ""];

    private readonly AutomationService _automation;
    private readonly IMainWindowHost _host;
    private readonly Action<Profile> _select;
    private Profile _profile;
    private readonly bool _isLoading = true;

    public ProfileEditorViewModel(Profile profile, AutomationService automation, MonitorViewModel monitor, IMainWindowHost host, Action<Profile> select)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _profile = profile;
        _automation = automation;
        _host = host;
        _select = select;

        Name = profile.Name;
        Glyph = profile.Glyph;
        ShowInQuickSettings = profile.ShowInQuickSettings;

        var current = automation.CaptureCurrent(AutomationService.SettingsFor(monitor).Select(setting => setting.Id));
        var rows = AutomationService.SettingsFor(monitor)
            .Select(setting => new ProfileSettingRowViewModel(
                setting,
                monitor,
                profile.Settings.ContainsKey(setting.Id),
                profile.Settings.TryGetValue(setting.Id, out var value) ? value : current.GetValueOrDefault(setting.Id),
                Save))
            .ToList();
        CommonSettings = [.. rows.Where(row => row.Setting.IsCommon)];
        MoreSettings = [.. rows.Where(row => !row.Setting.IsCommon)];
        _isLoading = false;
    }

    public Guid Id => _profile.Id;

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string Glyph { get; set; }

    [ObservableProperty]
    public partial bool ShowInQuickSettings { get; set; }

    public Shortcut? Shortcut => _profile.Shortcut;

    public IReadOnlyList<string> ShortcutKeys => Shortcut is { } shortcut ? AutomationText.ShortcutKeys(shortcut) : [];

    public bool HasShortcut => Shortcut is not null;

    /// <summary>Windows or another app already uses the shortcut, so it doesn't work.</summary>
    public bool IsShortcutTaken => _automation.ShortcutConflicts.Contains(Id);

    /// <summary>The settings most profiles use, always shown.</summary>
    public IReadOnlyList<ProfileSettingRowViewModel> CommonSettings { get; }

    /// <summary>The rest, behind "Show N more settings".</summary>
    public IReadOnlyList<ProfileSettingRowViewModel> MoreSettings { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowMoreText))]
    public partial bool ShowAllSettings { get; set; }

    public string ShowMoreText => ShowAllSettings
        ? "Show fewer settings"
        : string.Create(CultureInfo.CurrentCulture, $"Show {MoreSettings.Count} more settings");

    /// <summary>Records a new shortcut, or removes it with null.</summary>
    public void SetShortcut(Shortcut? shortcut)
    {
        _profile = _profile with { Shortcut = shortcut };
        Save();
        OnPropertyChanged(nameof(Shortcut));
        OnPropertyChanged(nameof(ShortcutKeys));
        OnPropertyChanged(nameof(HasShortcut));
        OnPropertyChanged(nameof(IsShortcutTaken));
    }

    partial void OnNameChanged(string value) => Save();

    partial void OnGlyphChanged(string value) => Save();

    partial void OnShowInQuickSettingsChanged(bool value) => Save();

    [RelayCommand]
    private void SetGlyph(string glyph) => Glyph = glyph;

    [RelayCommand]
    private void RecordShortcut(Shortcut? shortcut) => SetShortcut(shortcut);

    [RelayCommand]
    private void ToggleAllSettings() => ShowAllSettings = !ShowAllSettings;

    /// <summary>Overwrites the included settings with what the monitor uses right now.</summary>
    [RelayCommand]
    private void CaptureCurrent()
    {
        var rows = CommonSettings.Concat(MoreSettings).Where(row => row.IsIncluded).ToList();
        var current = _automation.CaptureCurrent(rows.Select(row => row.Setting.Id));
        foreach (var row in rows)
        {
            if (current.TryGetValue(row.Setting.Id, out var value))
            {
                row.Load(value);
            }
        }
        Save();
    }

    [RelayCommand]
    private Task ApplyNow() => _automation.ApplyAsync(_profile);

    [RelayCommand]
    private void Duplicate()
    {
        var copy = _profile with { Id = Guid.NewGuid(), Name = $"{_profile.Name} copy", Shortcut = null };
        _automation.SaveProfile(copy);
        _select(copy);
    }

    [RelayCommand]
    private async Task Delete()
    {
        var rules = _automation.Rules.Count(rule => rule.ProfileId == Id);
        var confirmed = await _host.ConfirmAsync(new ConfirmationViewModel(
            $"Delete {_profile.Name}?",
            rules switch
            {
                0 => "The monitor keeps its current settings.",
                1 => "The rule that uses it is deleted too. The monitor keeps its current settings.",
                _ => $"The {rules} rules that use it are deleted too. The monitor keeps its current settings.",
            },
            "Delete"));
        if (confirmed)
        {
            _automation.DeleteProfile(Id);
        }
    }

    private void Save()
    {
        if (_isLoading)
        {
            return;
        }

        // An emptied name box keeps the old name rather than leaving a nameless profile.
        var name = string.IsNullOrWhiteSpace(Name) ? _profile.Name : Name.Trim();
        _profile = _profile with
        {
            Name = name,
            Glyph = Glyph,
            ShowInQuickSettings = ShowInQuickSettings,
            Settings = CommonSettings.Concat(MoreSettings).Where(row => row.IsIncluded).ToDictionary(row => row.Setting.Id, row => row.Value),
        };
        _automation.SaveProfile(_profile);
    }
}
