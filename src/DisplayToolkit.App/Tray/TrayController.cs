using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    MonitorContext context,
    ShortcutService shortcuts,
    TargetMode targetMode,
    AutomationService automation,
    AppSettings settings,
    UpdateService updates,
    ILogger<TrayController> logger)
    : IDisposable
{
    /// <summary>
    /// Clicking the tray icon while the flyout is open first deactivates (and hides) the flyout, then delivers the
    /// click. A click this soon after hiding means "close", not "open again".
    /// </summary>
    private static readonly TimeSpan ReopenGuard = TimeSpan.FromMilliseconds(400);

    private TrayIcon? _icon;
    private Views.HudWindow? _hud;

    public void Start()
    {
        _icon = new TrayIcon();
        _icon.Invoked += (_, _) => ToggleFlyout();
        _icon.ContextMenuRequested += (_, _) => ShowMenu();
        _icon.DisplaysChanged += (_, _) => OnDisplaysChanged();
        monitors.Changed += (_, _) => UpdateTooltip();
        _icon.WheelScrolled += (_, notches) =>
        {
            if (settings.Current.ScrollOverTrayIcon)
            {
                shortcuts.StepBrightness(notches);
            }
        };
        automation.ShortcutUsed += (_, feedback) => ShowHud(feedback);
        shortcuts.Used += (_, feedback) => ShowHud(feedback);
        shortcuts.QuickSettingsRequested += (_, _) => ToggleFlyout(fromKeyboard: true);
        updates.Changed += (_, _) => NotifyAboutUpdate();
        _icon.NotificationClicked += (_, _) => updates.OpenReleasePage();
        shortcuts.Start();
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
        targetMode.OnDisplaysChanged();
        await monitors.RescanAsync();
        await flyoutViewModel.OnDisplaysChangedAsync();
    }

    /// <summary>Tells the user about a new version once, with a Windows notification from the tray icon.</summary>
    private void NotifyAboutUpdate()
    {
        if (updates.Available is not { } update || settings.Current.NotifiedUpdateVersion == update.Version.ToString(3))
        {
            return;
        }
        settings.Update(current => current with { NotifiedUpdateVersion = update.Version.ToString(3) });
        _icon?.ShowNotification($"Display Toolkit {update.Version.ToString(3)} is available", "Click to see what's new and download it.");
    }

    /// <summary>Confirms a shortcut on screen, unless the user turned that off or quick settings already shows it.</summary>
    private void ShowHud(ShortcutFeedback feedback)
    {
        if (settings.Current.ShowShortcutOverlay && !flyout.IsVisible)
        {
            _hud ??= new Views.HudWindow();
            _hud.Show(feedback.Glyph, feedback.Text, feedback.Level);
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
        var quickSettingsShortcut = shortcuts.Get(ShortcutService.QuickSettings);
        menu.Items.Add(Item("Quick settings", () => flyout.ShowFlyout(),
            gesture: quickSettingsShortcut is null ? null : ViewModels.Automation.AutomationText.Shortcut(quickSettingsShortcut)));

        // With several monitors: which one quick settings, the main window and automation control.
        if (context.Monitors.Count > 1)
        {
            var monitorsItem = new MenuItem { Header = "Monitor" };
            foreach (var monitor in context.Monitors)
            {
                var item = Item(context.NameOf(monitor), () => context.Select(monitor));
                item.IsChecked = context.Current?.Session == monitor;
                monitorsItem.Items.Add(item);
            }
            menu.Items.Add(monitorsItem);
        }

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
        var targetModeItem = Item("Target mode", targetMode.Toggle);
        targetModeItem.IsChecked = targetMode.IsOn;
        menu.Items.Add(targetModeItem);

        menu.Items.Add(new Separator());
        if (updates.Available is { } update)
        {
            menu.Items.Add(Item($"Update available: {update.Version.ToString(3)}…", updates.OpenReleasePage));
        }
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
