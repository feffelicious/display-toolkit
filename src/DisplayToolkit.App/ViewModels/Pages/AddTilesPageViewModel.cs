using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.App.ViewModels.Tiles;

namespace DisplayToolkit.App.ViewModels.Pages;

public sealed record AddTileItem(string Name, string KindLabel, IRelayCommand AddCommand);

public sealed record AddTileGroup(string Name, IReadOnlyList<AddTileItem> Items);

/// <summary>
/// Edit mode's Add list: tiles this monitor supports that aren't in the flyout yet, grouped. Adding keeps the page open
/// so several can be added in a row.
/// </summary>
public sealed class AddTilesPageViewModel : PageViewModel
{
    private readonly Func<IReadOnlyList<TileDefinition>> _available;
    private readonly Action<TileDefinition> _add;

    public AddTilesPageViewModel(Func<IReadOnlyList<TileDefinition>> available, Action<TileDefinition> add)
        : base("Add to quick settings")
    {
        _available = available;
        _add = add;
        Refresh();
    }

    public IReadOnlyList<AddTileGroup> Groups { get; private set; } = [];

    public bool IsEmpty => Groups.Count == 0;

    private void Refresh()
    {
        Groups = [.. _available()
            .GroupBy(definition => definition.Group)
            .Select(group => new AddTileGroup(group.Key, [.. group.Select(definition =>
                new AddTileItem(definition.Name, definition.KindLabel, new RelayCommand(() => Add(definition))))]))];
        OnPropertyChanged(nameof(Groups));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void Add(TileDefinition definition)
    {
        _add(definition);
        Refresh();
    }
}
