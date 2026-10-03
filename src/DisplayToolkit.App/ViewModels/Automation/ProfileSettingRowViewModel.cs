using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using DisplayToolkit.App.ViewModels.Pages;
using DisplayToolkit.Automation.Profiles;
using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels.Automation;

/// <summary>How a profile setting's stored value is edited.</summary>
public enum SettingControl
{
    Toggle,
    Levels,
    Slider,
    Choice,
}

/// <summary>
/// One row of "Settings in this profile": whether the profile includes the setting, and the value it stores. The row
/// only edits the profile; nothing is sent to the monitor until the profile is applied.
/// </summary>
public sealed partial class ProfileSettingRowViewModel : ObservableObject
{
    /// <summary>Ranges up to this maximum are edited as segments (Off, 1, 2, 3, 4).</summary>
    private const uint MaxLevels = 10;

    private readonly Action _changed;
    private List<OptionItemViewModel>? _levels;

    /// <summary>Set while values are loaded rather than edited, so they aren't saved back.</summary>
    private bool _isLoading = true;

    public ProfileSettingRowViewModel(ProfileSetting setting, MonitorViewModel monitor, bool isIncluded, uint value, Action changed)
    {
        ArgumentNullException.ThrowIfNull(setting);
        ArgumentNullException.ThrowIfNull(monitor);
        Setting = setting;
        _changed = changed;
        Maximum = setting.Feature is { } feature ? monitor[feature]?.Maximum ?? 100 : 1;
        Options = setting.Feature is EnumFeature enumFeature ? monitor.OptionsOf(enumFeature) : [];
        Control = setting.Feature switch
        {
            null or FlagFeature or SwitchFeature => SettingControl.Toggle,
            EnumFeature => SettingControl.Choice,
            _ when Maximum is > 0 and <= MaxLevels => SettingControl.Levels,
            _ => SettingControl.Slider,
        };
        IsIncluded = isIncluded;
        // A choice the monitor can't take (the HDR mode reads 0 without an HDR signal) starts on the first option.
        Value = Control == SettingControl.Choice && Options.Count > 0 && Options.All(option => option.Value != value) ? Options[0].Value : value;
        _isLoading = false;
    }

    public ProfileSetting Setting { get; }

    public string Name => Setting.Name;

    public SettingControl Control { get; }

    public uint Maximum { get; }

    public IReadOnlyList<FeatureOption> Options { get; }

    [ObservableProperty]
    public partial bool IsIncluded { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SliderValue), nameof(IsOn), nameof(SelectedOption), nameof(ValueText))]
    public partial uint Value { get; set; }

    public double SliderValue
    {
        get => Value;
        set => Value = (uint)Math.Round(Math.Clamp(value, 0, Maximum));
    }

    public string ValueText => Value.ToString(CultureInfo.CurrentCulture);

    public bool IsOn
    {
        get => Setting.Feature is SwitchFeature switchFeature ? switchFeature.IsOn(Value) : Value != 0;
        set => Value = Setting.Feature is SwitchFeature switchFeature
            ? value ? switchFeature.OnValue : switchFeature.OffValue
            : value ? 1u : 0u;
    }

    public FeatureOption? SelectedOption
    {
        get => Options.FirstOrDefault(option => option.Value == Value);
        set
        {
            if (value is not null)
            {
                Value = value.Value;
            }
        }
    }

    /// <summary>Off, 1, 2 … for small ranges such as the blue light filter.</summary>
    public IReadOnlyList<OptionItemViewModel> Levels => _levels ??=
    [
        .. Enumerable.Range(0, (int)Maximum + 1).Select(level => new OptionItemViewModel(
            level == 0 ? "Off" : level.ToString(CultureInfo.CurrentCulture), (uint)level, selected => Value = selected)
        {
            IsSelected = level == Value,
        }),
    ];

    partial void OnIsIncludedChanged(bool value)
    {
        if (!_isLoading)
        {
            _changed();
        }
    }

    partial void OnValueChanged(uint value)
    {
        foreach (var level in _levels ?? [])
        {
            level.IsSelected = level.Value == value;
        }
        if (IsIncluded && !_isLoading)
        {
            _changed();
        }
    }

    /// <summary>Sets the value without saving (after "Capture current", which saves once for all rows).</summary>
    public void Load(uint value)
    {
        _isLoading = true;
        Value = value;
        _isLoading = false;
    }
}
