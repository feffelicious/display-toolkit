using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels.Tiles;

/// <summary>A plain on/off tile, for switches and bitmask flags.</summary>
public sealed class SwitchTileViewModel(string id, string glyph, FeatureState state, string? name = null)
    : FeatureTileViewModel(id, TileKind.Toggle, glyph, state, name)
{
    protected override void OnMain()
    {
        var turnOn = !State.IsOn;
        State.Write(State.Feature is SwitchFeature switchFeature
            ? turnOn ? switchFeature.OnValue : switchFeature.OffValue
            : turnOn ? 1u : 0u);
    }
}
