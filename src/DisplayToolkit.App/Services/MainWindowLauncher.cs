using DisplayToolkit.App.ViewModels.Main;
using DisplayToolkit.App.Views;

namespace DisplayToolkit.App.Services;

/// <summary>
/// Opens the main window on demand. The window is created when needed and released when closed (the app lives in the
/// tray); its view model is kept, so it reopens where it was.
/// </summary>
internal sealed class MainWindowLauncher(MainWindowViewModel viewModel, ThemeService theme, AppSettings settings)
{
    private MainWindow? _window;

    public void Show()
    {
        if (_window is null)
        {
            _window = new MainWindow(viewModel, theme, settings);
            _window.Closed += (_, _) => _window = null;
        }
        _window.ShowAndActivate();
    }
}
