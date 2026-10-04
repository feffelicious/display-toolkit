using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.App.Services;
using DisplayToolkit.App.ViewModels.Automation;
using DisplayToolkit.Automation.Profiles;

namespace DisplayToolkit.App.ViewModels.Main;

/// <summary>One app shortcut in Settings: what it does, its keys, and whether another app has taken them.</summary>
public sealed partial class ShortcutRowViewModel : ObservableObject
{
    private readonly ShortcutAction _action;
    private readonly ShortcutService _service;

    internal ShortcutRowViewModel(ShortcutAction action, ShortcutService service, bool isNested = false)
    {
        _action = action;
        _service = service;
        IsNested = isNested;
    }

    public string Name => _action.Name;

    /// <summary>Nested rows (inside an expander) have no icon of their own.</summary>
    public string? Glyph => IsNested ? null : _action.Glyph;

    public bool IsNested { get; }

    public IReadOnlyList<string> Keys => _service.Get(_action.Id) is { } shortcut ? AutomationText.ShortcutKeys(shortcut) : [];

    public bool HasShortcut => _service.Get(_action.Id) is not null;

    public bool IsTaken => _service.IsTaken(_action.Id);

    /// <summary>Re-reads the shortcut after it changed.</summary>
    public void Refresh() => OnPropertyChanged(string.Empty);

    [RelayCommand]
    private void Record(Shortcut? shortcut) => _service.Set(_action.Id, shortcut);
}
