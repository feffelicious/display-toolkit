using CommunityToolkit.Mvvm.ComponentModel;
using DisplayToolkit.Core.Features;
using DisplayToolkit.Core.Monitors;

namespace DisplayToolkit.App.ViewModels;

/// <summary>
/// UI-thread mirror of one feature on one monitor. Controls bind to it; user changes go through <see cref="Write"/>.
/// </summary>
public sealed partial class FeatureState(MonitorSession session, Feature feature) : ObservableObject
{
    private uint? _lastRequested;

    public Feature Feature { get; } = feature;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SliderValue), nameof(IsOn))]
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

    /// <summary>Two-way slider binding: setting it writes to the monitor.</summary>
    public double SliderValue
    {
        get => Value;
        set => Write((uint)Math.Round(Math.Clamp(value, 0, Maximum)));
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
        _ = session.WriteAsync(Feature, value);
    }

    /// <summary>Sends the last requested value again, after a failed write.</summary>
    public void Retry()
    {
        if (_lastRequested is { } value)
        {
            Status = FeatureStatus.Pending;
            Value = value;
            _ = session.WriteAsync(Feature, value);
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
}
