using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using DisplayToolkit.App.ViewModels.Pages;
using DisplayToolkit.Core.Features;
using DisplayToolkit.Core.Monitors;

namespace DisplayToolkit.App.ViewModels;

/// <summary>
/// UI-thread mirror of one feature on one monitor. Controls bind to it directly: <see cref="SliderValue"/> for
/// sliders, <see cref="Toggled"/> for switches, <see cref="SelectedOption"/> for combo boxes and <see cref="Choices"/>
/// for segmented controls. Every setter writes to the monitor through <see cref="Write"/>.
/// </summary>
public sealed partial class FeatureState : ObservableObject
{
    /// <summary>Ranges up to this maximum are small enough to offer as segments (Off, 1, 2, 3, 4).</summary>
    private const uint MaxSegmentedRange = 10;

    private readonly MonitorSession _session;
    private uint? _lastRequested;
    private List<OptionItemViewModel>? _choices;

    /// <param name="options">The choices the monitor supports, for enum features; empty otherwise.</param>
    public FeatureState(MonitorSession session, Feature feature, IReadOnlyList<FeatureOption> options)
    {
        _session = session;
        Feature = feature;
        Options = options;
    }

    public Feature Feature { get; }

    /// <summary>The choices this monitor supports, for enum features.</summary>
    public IReadOnlyList<FeatureOption> Options { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SliderValue), nameof(IsOn), nameof(Toggled), nameof(SelectedOption))]
    public partial uint Value { get; private set; }

    [ObservableProperty]
    public partial uint Maximum { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPending), nameof(HasFailed), nameof(IsLocked), nameof(IsAvailable))]
    public partial FeatureStatus Status { get; private set; }

    /// <summary>False until the first value arrives from the monitor.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAvailable))]
    public partial bool IsKnown { get; private set; }

    public bool IsPending => Status == FeatureStatus.Pending;

    public bool HasFailed => Status == FeatureStatus.Failed;

    public bool IsLocked => Status == FeatureStatus.Locked;

    /// <summary>Known and usable in the monitor's current mode. Unavailable features are hidden, not disabled.</summary>
    public bool IsAvailable => IsKnown && !IsLocked;

    /// <summary>On/off reading for switches, flags and "0 means off" levels.</summary>
    public bool IsOn => Feature is SwitchFeature switchFeature ? switchFeature.IsOn(Value) : Value != 0;

    /// <summary>Two-way switch binding.</summary>
    public bool Toggled
    {
        get => IsOn;
        set => SetOn(value);
    }

    /// <summary>Two-way slider binding.</summary>
    public double SliderValue
    {
        get => Value;
        set => Write((uint)Math.Round(Math.Clamp(value, 0, Maximum)));
    }

    /// <summary>Two-way combo box binding, for enum features.</summary>
    public FeatureOption? SelectedOption
    {
        get => Options.FirstOrDefault(option => option.Value == Value);
        set
        {
            if (value is not null)
            {
                Write(value.Value);
            }
        }
    }

    /// <summary>
    /// Segments for a segmented control: the enum options, or Off/1…max for a small range such as blue light 0–4.
    /// </summary>
    public IReadOnlyList<OptionItemViewModel> Choices => _choices ??= CreateChoices();

    /// <summary>Turns a switch, flag or level on or off.</summary>
    public void SetOn(bool on)
    {
        if (on == IsOn)
        {
            return;
        }
        Write(Feature switch
        {
            SwitchFeature switchFeature => on ? switchFeature.OnValue : switchFeature.OffValue,
            _ => on ? 1u : 0u,
        });
    }

    public void Write(uint value)
    {
        if (value == Value && Status != FeatureStatus.Failed)
        {
            return;
        }

        // Optimistic: the session also reports Pending, but updating here keeps a dragged slider glued to the pointer.
        _lastRequested = value;
        Value = value;
        Status = FeatureStatus.Pending;
        _ = _session.WriteAsync(Feature, value);
    }

    /// <summary>Sends the last requested value again, after a failed write.</summary>
    public void Retry()
    {
        if (_lastRequested is { } value)
        {
            Status = FeatureStatus.Pending;
            Value = value;
            _ = _session.WriteAsync(Feature, value);
        }
    }

    /// <summary>Applies a value reported by the session. Call on the UI thread.</summary>
    public void Apply(FeatureValue value)
    {
        Value = value.Value;
        if (value.Maximum != 0)
        {
            Maximum = value.Maximum;
        }
        Status = value.Status;
        IsKnown = true;
    }

    partial void OnValueChanged(uint value)
    {
        foreach (var choice in _choices ?? [])
        {
            choice.IsSelected = choice.Value == value;
        }
    }

    private List<OptionItemViewModel> CreateChoices()
    {
        IEnumerable<(string Name, uint Value)> choices = Options.Count > 0
            ? Options.Select(option => (option.Name, option.Value))
            : Maximum is > 0 and <= MaxSegmentedRange
                ? Enumerable.Range(0, (int)Maximum + 1).Select(level => (level == 0 ? "Off" : level.ToString(CultureInfo.CurrentCulture), (uint)level))
                : [];

        return [.. choices.Select(choice => new OptionItemViewModel(choice.Name, choice.Value, Write) { IsSelected = choice.Value == Value })];
    }
}
