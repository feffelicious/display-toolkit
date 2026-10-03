using System.Windows;
using System.Windows.Controls.Primitives;

namespace DisplayToolkit.App.Views.Pages;

public partial class DisplayPage
{
    public DisplayPage() => InitializeComponent();

    /// <summary>The input menu drops down from the button, like a split-button menu.</summary>
    private void OnSwitchInputClick(object sender, RoutedEventArgs e)
    {
        var menu = SwitchInputButton.ContextMenu;
        menu.PlacementTarget = SwitchInputButton;
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
    }
}
