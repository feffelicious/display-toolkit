using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using DisplayToolkit.App.Flyout;
using DisplayToolkit.App.Native;
using DisplayToolkit.App.Services;
using DisplayToolkit.App.ViewModels;
using Microsoft.Extensions.Logging;

namespace DisplayToolkit.App.Tray;

/// <summary>Connects the tray icon to the flyout and the context menu, and display changes to monitor rescans.</summary>
internal sealed class TrayController(MonitorService monitors, FlyoutWindow flyout, FlyoutViewModel flyoutViewModel, ILogger<TrayController> logger)
    : IDisposable
{
    /// <summary>
    /// Clicking the tray icon while the flyout is open first deactivates (and hides) the flyout, then delivers the
    /// click. A click this soon after hiding means "close", not "open again".
    /// </summary>
    private static readonly TimeSpan ReopenGuard = TimeSpan.FromMilliseconds(400);

    private const int OpenFlyoutHotkey = 1;

    private TrayIcon? _icon;
    private GlobalHotkeys? _hotkeys;
    private ContextMenu? _menu;

    public void Start()
    {
        _icon = new TrayIcon();
        _icon.Invoked += (_, _) => ToggleFlyout();
        _icon.ContextMenuRequested += (_, _) => ShowMenu();
        _icon.DisplaysChanged += (_, _) => OnDisplaysChanged();
        monitors.Changed += (_, _) => UpdateTooltip();

        _hotkeys = new GlobalHotkeys();
        _hotkeys.Pressed += (_, id) =>
        {
            if (id == OpenFlyoutHotkey)
            {
                ToggleFlyout(fromKeyboard: true);
            }
        };
        if (!_hotkeys.Register(OpenFlyoutHotkey, ModifierKeys.Control | ModifierKeys.Alt, Key.D))
        {
            logger.LogWarning("Ctrl+Alt+D is already used by another app");
        }
    }

    /// <param name="fromKeyboard">Opened with a shortcut: focus the brightness band so the arrow keys work at once.</param>
    public void ToggleFlyout(bool fromKeyboard = false)
    {
        if (flyout.IsVisible)
        {
            flyout.HideFlyout();
        }
        else if (fromKeyboard || DateTime.UtcNow - flyout.LastHiddenAt > ReopenGuard)
        {
            flyout.ShowFlyout(fromKeyboard);
        }
    }

    public void Dispose()
    {
        _hotkeys?.Dispose();
        _icon?.Dispose();
    }

    private async void OnDisplaysChanged()
    {
        logger.LogInformation("Displays changed");
        await monitors.RescanAsync();
        await flyoutViewModel.OnDisplaysChangedAsync();
    }

    private void ShowMenu()
    {
        _menu ??= CreateMenu();

        // The menu only closes on an outside click if our process owns the foreground window.
        User32.SetForegroundWindow(_icon!.WindowHandle);
        _menu.Placement = PlacementMode.MousePoint;
        _menu.IsOpen = true;
    }

    private ContextMenu CreateMenu()
    {
        var quickSettings = new MenuItem { Header = "Quick settings" };
        quickSettings.Click += (_, _) => flyout.ShowFlyout();

        var exit = new MenuItem { Header = "Exit" };
        exit.Click += (_, _) => Application.Current.Shutdown();

        return new ContextMenu { Items = { quickSettings, new Separator(), exit } };
    }

    private void UpdateTooltip()
    {
        if (_icon is null)
        {
            return;
        }
        _icon.Tooltip = monitors.Sessions.Count == 0
            ? "Display Toolkit"
            : string.Join(Environment.NewLine, monitors.Sessions.Select(session => session.Name));
    }
}
