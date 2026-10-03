namespace DisplayToolkit.Core.Monitors;

public sealed class FeatureValueChangedEventArgs(FeatureValue value) : EventArgs
{
    public FeatureValue Value { get; } = value;
}
