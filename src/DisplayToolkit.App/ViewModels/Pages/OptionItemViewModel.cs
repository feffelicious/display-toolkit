using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DisplayToolkit.App.ViewModels.Pages;

/// <summary>One selectable row in a sub-page list.</summary>
public sealed partial class OptionItemViewModel(string name, uint value, Action<uint> select) : ObservableObject
{
    public string Name { get; } = name;

    public uint Value { get; } = value;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [RelayCommand]
    private void Select() => select(Value);
}
