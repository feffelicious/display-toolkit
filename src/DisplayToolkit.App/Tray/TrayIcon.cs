using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using DisplayToolkit.App.Native;
using Microsoft.Win32;

namespace DisplayToolkit.App.Tray;

/// <summary>
/// The notification-area icon, plus the hidden window that receives its messages and the system broadcasts the app
/// cares about (display changes, resume from sleep, theme changes). Must be created on the UI thread.
/// </summary>
internal sealed unsafe class TrayIcon : IDisposable
{
    private const int CallbackMessage = 0x8000 + 1; // WM_APP + 1
    private const uint IconId = 1;
    private const int WheelNotch = 120;

    /// <summary>The mouse hook's callback is static; there is one tray icon.</summary>
    private static TrayIcon? s_instance;

    private readonly HwndSource _window;
    private readonly uint _taskbarCreatedMessage;
    private nint _icon;
    private string _tooltip = "Display Toolkit";

    /// <summary>A low-level mouse hook, installed only while the pointer is over the icon, to see the wheel there.</summary>
    private nint _mouseHook;
    private User32.Rect _iconBounds;
    private int _wheelRemainder;

    public TrayIcon()
    {
        _window = new HwndSource(new HwndSourceParameters("DisplayToolkitTray") { Width = 0, Height = 0, WindowStyle = 0 });
        _window.AddHook(WndProc);
        _taskbarCreatedMessage = User32.RegisterWindowMessage("TaskbarCreated");
        s_instance = this;
        RefreshIcon();
        Add();
    }

    /// <summary>Left click or keyboard selection of the icon.</summary>
    public event EventHandler? Invoked;

    /// <summary>Right click or the context-menu key. The argument is the anchor point in physical screen pixels.</summary>
    public event EventHandler<User32.Point>? ContextMenuRequested;

    /// <summary>The user clicked the notification shown with <see cref="ShowNotification"/>.</summary>
    public event EventHandler? NotificationClicked;

    /// <summary>The mouse wheel turned over the icon, by this many notches (positive is up, away from the user).</summary>
    public event EventHandler<int>? WheelScrolled;

    /// <summary>Monitors were added, removed or changed mode, or the PC resumed from sleep.</summary>
    public event EventHandler? DisplaysChanged;

    public string Tooltip
    {
        get => _tooltip;
        set
        {
            _tooltip = value;
            Update(Shell32.NifTip | Shell32.NifShowTip);
        }
    }

    public nint WindowHandle => _window.Handle;

    public void Dispose()
    {
        StopWatchingWheel();
        s_instance = null;
        Send(Shell32.NimDelete, 0);
        _window.RemoveHook(WndProc);
        _window.Dispose();
        if (_icon != 0)
        {
            User32.DestroyIcon(_icon);
        }
    }

    /// <summary>Shows a Windows notification from the tray icon.</summary>
    public void ShowNotification(string title, string text) => Send(Shell32.NimModify, Shell32.NifInfo, (title, text));

    private void Add()
    {
        Send(Shell32.NimAdd, Shell32.NifMessage | Shell32.NifIcon | Shell32.NifTip | Shell32.NifShowTip);
        Send(Shell32.NimSetVersion, 0);
    }

    private void Update(uint flags) => Send(Shell32.NimModify, flags);

    private unsafe void Send(uint message, uint flags, (string Title, string Text)? notification = null)
    {
        var data = new Shell32.NotifyIconData
        {
            Size = (uint)sizeof(Shell32.NotifyIconData),
            Window = _window.Handle,
            Id = IconId,
            Flags = flags,
            CallbackMessage = CallbackMessage,
            Icon = _icon,
            TimeoutOrVersion = Shell32.NotifyIconVersion4,
        };
        data.SetTip(_tooltip);
        if (notification is var (title, text))
        {
            data.SetInfo(title, text);
        }
        Shell32.ShellNotifyIcon(message, &data);
    }

    private void RefreshIcon()
    {
        var previous = _icon;
        _icon = TrayIconRenderer.Render(IsTaskbarLight());
        if (previous != 0)
        {
            Update(Shell32.NifIcon);
            User32.DestroyIcon(previous);
        }
    }

    private static bool IsTaskbarLight()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("SystemUsesLightTheme") is 1;
    }

    /// <summary>
    /// The notification area doesn't pass the wheel on, so a mouse hook watches it while the pointer is over the
    /// icon, and goes away as soon as the pointer leaves.
    /// </summary>
    private void StartWatchingWheel()
    {
        if (_mouseHook != 0 || WheelScrolled is null)
        {
            return;
        }
        var identifier = new Shell32.NotifyIconIdentifier { Size = (uint)sizeof(Shell32.NotifyIconIdentifier), Window = _window.Handle, Id = IconId };
        User32.Rect bounds;
        if (Shell32.ShellNotifyIconGetRect(&identifier, &bounds) != 0)
        {
            return;
        }
        _iconBounds = bounds;
        _wheelRemainder = 0;
        _mouseHook = User32.SetWindowsHookEx(User32.WhMouseLowLevel, &OnMouseHook, User32.GetModuleHandle(null), 0);
    }

    private void StopWatchingWheel()
    {
        if (_mouseHook != 0)
        {
            User32.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = 0;
        }
    }

    /// <summary>Runs inside the hook, which must return quickly: anything slow is posted to the dispatcher.</summary>
    private void OnMouse(int message, User32.LowLevelMouse* data)
    {
        var point = data->Point;
        var isOverIcon = point.X >= _iconBounds.Left && point.X < _iconBounds.Right && point.Y >= _iconBounds.Top && point.Y < _iconBounds.Bottom;
        if (!isOverIcon)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(StopWatchingWheel);
        }
        else if (message == User32.WmMouseWheel)
        {
            // Wheels report 120 per notch; precision touchpads report less, more often.
            _wheelRemainder += (short)(data->MouseData >> 16);
            var notches = _wheelRemainder / WheelNotch;
            _wheelRemainder %= WheelNotch;
            if (notches != 0)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(() => WheelScrolled?.Invoke(this, notches));
            }
        }
    }

    [UnmanagedCallersOnly]
    private static nint OnMouseHook(int code, nint wParam, nint lParam)
    {
        if (code >= 0 && s_instance is { } icon && (wParam == User32.WmMouseWheel || wParam == User32.WmMouseMove))
        {
            try
            {
                icon.OnMouse((int)wParam, (User32.LowLevelMouse*)lParam);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                // An exception can't cross back into Windows; a missed notch is harmless.
            }
        }
        return User32.CallNextHookEx(0, code, wParam, lParam);
    }

    private nint WndProc(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == CallbackMessage)
        {
            switch ((int)(lParam & 0xFFFF))
            {
                case Shell32.NinSelect:
                case Shell32.NinKeySelect:
                    Invoked?.Invoke(this, EventArgs.Empty);
                    break;
                case Shell32.WmMouseMove:
                    StartWatchingWheel();
                    break;
                case Shell32.NinBalloonUserClick:
                    NotificationClicked?.Invoke(this, EventArgs.Empty);
                    break;
                case Shell32.WmContextMenu:
                    // NOTIFYICON_VERSION_4 passes the anchor point in wParam.
                    var anchor = new User32.Point { X = (short)(wParam & 0xFFFF), Y = (short)((wParam >> 16) & 0xFFFF) };
                    ContextMenuRequested?.Invoke(this, anchor);
                    break;
            }
            handled = true;
        }
        else if (message == _taskbarCreatedMessage)
        {
            // Explorer restarted: the icon is gone and must be added again.
            Add();
        }
        else if (message == User32.WmSettingChange)
        {
            RefreshIcon();
        }
        else if (message == User32.WmDisplayChange
            || (message == User32.WmPowerBroadcast && wParam == User32.PbtApmResumeAutomatic))
        {
            DisplaysChanged?.Invoke(this, EventArgs.Empty);
        }
        return 0;
    }
}
