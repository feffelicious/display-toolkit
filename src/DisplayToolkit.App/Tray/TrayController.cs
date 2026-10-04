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
internal sealed class TrayController(
    MonitorService monitors,
    FlyoutWindow flyout,
    FlyoutViewModel flyoutViewModel,
    MainWindowLauncher mainWindow,
    GlobalHotkeys hotkeys,
    AutomationService automation,
    AppSettings settings,
    ILogger<TrayController> logger)
    : IDisposable
{
    /// <summary>
    /// Clicking the tray icon while the flyout is open first deactivates (and hides) the flyout, then delivers the
    /// click. A click this soon after hiding means "close", not "open again".
    /// </summary>
    private static readonly TimeSpan ReopenGuard = TimeSpan.FromMilliseconds(400);

    private const int OpenFlyoutHotkey = 1;

    private TrayIcon? _icon;
    private Views.HudWindow? _hud;

    public void Start()
    {
        _icon = new TrayIcon();
        _icon.Invoked += (_, _) => ToggleFlyout();
        _icon.ContextMenuRequested += (_, _) => ShowMenu();
        _icon.DisplaysChanged += (_, _) => OnDisplaysChanged();
        monitors.Changed += (_, _) => UpdateTooltip();
        automation.ShortcutUsed += (_, feedback) => ShowHud(feedback.Glyph, feedback.Text);

        hotkeys.Pressed += (_, id) =>
        {
            if (id == OpenFlyoutHotkey)
            {
                ToggleFlyout(fromKeyboard: true);
            }
        };
        if (!hotkeys.Register(OpenFlyoutHotkey, ModifierKeys.Control | ModifierKeys.Alt, Key.D))
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
        _icon?.Dispose();
        _hud?.Close();
    }

    private async void OnDisplaysChanged()
    {
        logger.LogInformation("Displays changed");
        await monitors.RescanAsync();
        await flyoutViewModel.OnDisplaysChangedAsync();
    }

    /// <summary>Confirms a shortcut on screen, unless the user turned that off or quick settings already shows it.</summary>
    private void ShowHud(string glyph, string text)
    {
        if (settings.Current.ShowShortcutOverlay && !flyout.IsVisible)
        {
            _hud ??= new Views.HudWindow();
            _hud.Show(glyph, text);
        }
    }

    private void ShowMenu()
    {
        // Rebuilt every time: the profiles and the automation state change.
        var menu = CreateMenu();

        // The menu only closes on an outside click if our process owns the foreground window.
        User32.SetForegroundWindow(_icon!.WindowHandle);
        menu.Placement = PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    private ContextMenu CreateMenu()
    {
        var menu = new ContextMenu();
        menu.Items.Add(Item("Open Display Toolkit", mainWindow.Show, bold: true));
        menu.Items.Add(Item("Quick settings", () => flyout.ShowFlyout(), gesture: "Ctrl+Alt+D"));

        if (automation.Profiles.Count > 0)
        {
            menu.Items.Add(new Separator());
            var profiles = new MenuItem { Header = "Profiles" };
            foreach (var profile in automation.Profiles)
            {
                var item = Item(profile.Name, () => _ = automation.ApplyAsync(profile));
                item.IsChecked = automation.State.ProfileId == profile.Id;
                profiles.Items.Add(item);
            }
            menu.Items.Add(profiles);
        }

        if (automation.IsManual)
        {
            menu.Items.Add(Item("Back to automatic", () => _ = automation.ResumeAutomaticAsync()));
        }

        if (automation.Rules.Count > 0)
        {
            if (automation.Engine.IsPaused)
            {
                menu.Items.Add(Item("Resume automation", automation.Resume));
            }
            else
            {
                menu.Items.Add(new MenuItem
                {
                    Header = "Pause automation",
                    Items =
                    {
                        Item("For 1 hour", () => automation.Pause(TimeSpan.FromHours(1))),
                        Item("Until tomorrow", automation.PauseUntilMorning),
                        Item("Until I resume", () => automation.Pause(null)),
                    },
                });
            }
        }

        menu.Items.Add(new Separator());
        menu.Items.Add(Item("Exit", () => Application.Current.Shutdown()));
        return menu;
    }

    private static MenuItem Item(string header, Action action, bool bold = false, string? gesture = null)
    {
        var item = new MenuItem { Header = header, InputGestureText = gesture ?? string.Empty };
        if (bold)
        {
            item.FontWeight = FontWeights.SemiBold;
        }
        item.Click += (_, _) => action();
        return item;
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
