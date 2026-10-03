using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DisplayToolkit.App.ViewModels.Main;

namespace DisplayToolkit.App.Views.Pages;

/// <summary>Profiles &amp; automation. View plumbing only: menus, the glyph picker, focus and the strip's rounded clip.</summary>
public partial class AutomationPage
{
    private AutomationPageViewModel? _viewModel;

    public AutomationPage()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += (_, _) => Watch(null);
    }

    /// <summary>"…" and Pause buttons open their menu under the button, like a drop-down.</summary>
    private void OnMoreButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }

    private void OnGlyphButtonClick(object sender, RoutedEventArgs e) => GlyphPopup.IsOpen = true;

    private void OnGlyphChosen(object sender, RoutedEventArgs e) => GlyphPopup.IsOpen = false;

    /// <summary>Rounds the strip's corners: segments are drawn edge to edge and would otherwise poke out.</summary>
    private void OnStripSizeChanged(object sender, SizeChangedEventArgs e) =>
        ((UIElement)sender).Clip = new RectangleGeometry(new Rect(e.NewSize), 6, 6);

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) => Watch(DataContext as AutomationPageViewModel);

    private void Watch(AutomationPageViewModel? viewModel)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        _viewModel = viewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    /// <summary>A new profile opens with its name selected, ready to type over.</summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AutomationPageViewModel.Editor) && _viewModel?.IsNewProfile == true)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Keyboard.Focus(NameBox);
                NameBox.SelectAll();
            }, DispatcherPriority.Input);
        }
    }
}
