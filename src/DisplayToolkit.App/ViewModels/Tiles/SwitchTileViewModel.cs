namespace DisplayToolkit.App.ViewModels.Tiles;

/// <summary>A plain on/off tile, for switches and bitmask flags.</summary>
public sealed class SwitchTileViewModel(string id, string glyph, FeatureState state, string? name = null)
    : FeatureTileViewModel(id, TileKind.Toggle, glyph, state, name)
{
    protected override void OnMain() => State.SetOn(!State.IsOn);
}
