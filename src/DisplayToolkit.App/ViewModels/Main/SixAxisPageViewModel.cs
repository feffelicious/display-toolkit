using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels.Main;

/// <summary>One color of the six-axis adjustment: its name, swatch and saturation.</summary>
public sealed record SixAxisColor(string Name, string Swatch, FeatureState Saturation);

/// <summary>Per-color saturation (and hue, on monitors that have it) for the active picture mode.</summary>
public sealed class SixAxisPageViewModel : MainPageViewModel
{
    public SixAxisPageViewModel(MonitorViewModel monitor)
        : base("Six-axis color")
    {
        Monitor = monitor;
        (string Name, string Swatch, Feature Feature)[] colors =
        [
            ("Red", "#E5484D", FeatureCatalog.SaturationRed),
            ("Yellow", "#F5D90A", FeatureCatalog.SaturationYellow),
            ("Green", "#46A758", FeatureCatalog.SaturationGreen),
            ("Cyan", "#05A2C2", FeatureCatalog.SaturationCyan),
            ("Blue", "#3E63DD", FeatureCatalog.SaturationBlue),
            ("Magenta", "#D6409F", FeatureCatalog.SaturationMagenta),
        ];
        Colors = [.. colors
            .Where(color => monitor[color.Feature] is not null)
            .Select(color => new SixAxisColor(color.Name, color.Swatch, monitor[color.Feature]!))];
    }

    public MonitorViewModel Monitor { get; }

    public IReadOnlyList<SixAxisColor> Colors { get; }

    /// <summary>The picture mode these settings belong to: each mode keeps its own.</summary>
    public string ModeName => Monitor[FeatureCatalog.PictureMode]?.SelectedOption?.Name ?? "the current picture mode";

    public override Task OnShownAsync() => Monitor.RefreshAsync(Colors.Select(color => color.Saturation.Feature));
}
