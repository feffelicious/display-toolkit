namespace DisplayToolkit.App.ViewModels.Tiles;

/// <summary>A tile backed by one feature. Its status (pending, failed, available) comes straight from the feature.</summary>
public abstract class FeatureTileViewModel : TileViewModel
{
    /// <param name="name">Short tile label; defaults to the feature name.</param>
    protected FeatureTileViewModel(string id, TileKind kind, string glyph, FeatureState state, string? name = null)
        : base(id, kind, glyph, name ?? state.Feature.Name)
    {
        State = state;
        Track(state);
    }

    protected FeatureState State { get; }

    public override string Label => Name;

    public override bool IsOn => State.IsOn;

    public override bool IsPending => State.IsPending;

    public override bool HasFailed => State.HasFailed;

    public override bool IsVisible => State.IsAvailable;
}
