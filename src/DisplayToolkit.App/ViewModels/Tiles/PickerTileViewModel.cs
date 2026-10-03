using DisplayToolkit.App.ViewModels.Pages;
using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels.Tiles;

/// <summary>A tile whose label is the current choice (for example "Bar" for the FPS counter); click opens the list.</summary>
public sealed class PickerTileViewModel(
    string id, string glyph, FeatureState state, IReadOnlyList<FeatureOption> options, Action<PageViewModel> navigate, string? name = null)
    : FeatureTileViewModel(id, TileKind.Picker, glyph, state, name)
{
    public override string Label => ((EnumFeature)State.Feature).FindOption(State.Value)?.Name ?? Name;

    public override bool IsOn => false;

    protected override void OnMain() => navigate(new OptionsPageViewModel(Name, State, options));
}
