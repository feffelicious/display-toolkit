using DisplayToolkit.App.ViewModels.Pages;

namespace DisplayToolkit.App.ViewModels.Tiles;

/// <summary>
/// A feature where 0 means off and any other value is a level or style (blue light, shadow boost, crosshair). The main
/// area switches between off and the last value used; the chevron opens a page to pick one.
/// </summary>
public sealed class SplitTileViewModel : FeatureTileViewModel
{
    private readonly Func<PageViewModel> _page;
    private readonly Action<PageViewModel> _navigate;
    private uint _lastOnValue;

    public SplitTileViewModel(
        string id, string glyph, FeatureState state, uint defaultOnValue, Func<PageViewModel> page, Action<PageViewModel> navigate, string? name = null)
        : base(id, TileKind.Split, glyph, state, name)
    {
        _page = page;
        _navigate = navigate;
        _lastOnValue = state.IsOn ? state.Value : defaultOnValue;
        Observe(state, (_, e) =>
        {
            if (e.PropertyName == nameof(FeatureState.Value) && state.Value != 0)
            {
                _lastOnValue = state.Value;
            }
        });
    }

    protected override void OnMain() => State.Write(State.IsOn ? 0 : _lastOnValue);

    protected override void OnSecondary() => _navigate(_page());
}
