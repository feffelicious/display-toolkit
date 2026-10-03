using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using DisplayToolkit.App.Native;
using DisplayToolkit.App.Services;
using DisplayToolkit.App.ViewModels.Main;

namespace DisplayToolkit.App.Views;

/// <summary>The settings window. Window plumbing only; everything else lives in <see cref="MainWindowViewModel"/>.</summary>
internal sealed partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly ThemeService _theme;
    private IInputElement? _focusBeforeDialog;

    public MainWindow(MainWindowViewModel viewModel, ThemeService theme)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _theme = theme;
        DataContext = viewModel;
        theme.Changed += OnThemeChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>Shows the window, or brings it to the front if it's already open.</summary>
    public void ShowAndActivate()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Show();
        Activate();
        _ = _viewModel.OnShownAsync();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyBackdrop();
    }

    /// <summary>Esc closes the confirmation, then a dialog; Alt+Left goes back from a sub-page.</summary>
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.Escape && _viewModel.Confirmation is not null)
        {
            _viewModel.ConfirmCancelCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _viewModel.Dialog is not null && !IsDropDownOpen(e.OriginalSource))
        {
            _viewModel.CloseDialogCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.SystemKey == Key.Left && Keyboard.Modifiers == ModifierKeys.Alt)
        {
            _viewModel.GoToParentCommand.Execute(null);
            e.Handled = true;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _theme.Changed -= OnThemeChanged;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        base.OnClosed(e);
    }

    private void OnSettingsClick(object sender, MouseButtonEventArgs e) => _viewModel.OpenSettingsCommand.Execute(null);

    /// <summary>
    /// A confirmation opens with Cancel focused, so Enter can't trigger the action by accident. A dialog takes the
    /// focus to its first control, and gives it back to the page when it closes.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.Confirmation) && _viewModel.Confirmation is not null)
        {
            Dispatcher.BeginInvoke(() => Keyboard.Focus(CancelButton), System.Windows.Threading.DispatcherPriority.Input);
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.Dialog))
        {
            if (_viewModel.Dialog is not null)
            {
                _focusBeforeDialog = Keyboard.FocusedElement;
                Dispatcher.BeginInvoke(
                    () => DialogHost.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)),
                    System.Windows.Threading.DispatcherPriority.Input);
            }
            else
            {
                _focusBeforeDialog?.Focus();
                _focusBeforeDialog = null;
            }
        }
    }

    /// <summary>Esc in an open combo box closes the drop-down, not the dialog.</summary>
    private static bool IsDropDownOpen(object source) =>
        source is DependencyObject element && ItemsControl.ItemsControlFromItemContainer(element) is System.Windows.Controls.ComboBox { IsDropDownOpen: true }
        || source is System.Windows.Controls.ComboBox { IsDropDownOpen: true };

    private void OnThemeChanged(object? sender, EventArgs e) => ApplyBackdrop();

    private void ApplyBackdrop()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == 0)
        {
            return;
        }
        if (HwndSource.FromHwnd(hwnd) is { CompositionTarget: { } target })
        {
            target.BackgroundColor = Colors.Transparent;
        }
        Dwm.SetDarkMode(hwnd, _theme.IsDark);
        Dwm.SetBackdrop(hwnd, Dwm.Backdrop.Mica);
    }
}
