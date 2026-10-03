using System.Windows.Interop;
using DisplayToolkit.App.Native;
using Microsoft.Win32;

namespace DisplayToolkit.App.Tray;

/// <summary>
/// The notification-area icon, plus the hidden window that receives its messages and the system broadcasts the app
/// cares about (display changes, resume from sleep, theme changes). Must be created on the UI thread.
/// </summary>
internal sealed class TrayIcon : IDisposable
{
    private const int CallbackMessage = 0x8000 + 1; // WM_APP + 1
    private const uint IconId = 1;

    private readonly HwndSource _window;
    private readonly uint _taskbarCreatedMessage;
    private nint _icon;
    private string _tooltip = "Display Toolkit";

    public TrayIcon()
    {
        _window = new HwndSource(new HwndSourceParameters("DisplayToolkitTray") { Width = 0, Height = 0, WindowStyle = 0 });
        _window.AddHook(WndProc);
        _taskbarCreatedMessage = User32.RegisterWindowMessage("TaskbarCreated");
        RefreshIcon();
        Add();
    }

    /// <summary>Left click or keyboard selection of the icon.</summary>
    public event EventHandler? Invoked;

    /// <summary>Right click or the context-menu key. The argument is the anchor point in physical screen pixels.</summary>
    public event EventHandler<User32.Point>? ContextMenuRequested;

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
        Send(Shell32.NimDelete, 0);
        _window.RemoveHook(WndProc);
        _window.Dispose();
        if (_icon != 0)
        {
            User32.DestroyIcon(_icon);
        }
    }

    private void Add()
    {
        Send(Shell32.NimAdd, Shell32.NifMessage | Shell32.NifIcon | Shell32.NifTip | Shell32.NifShowTip);
        Send(Shell32.NimSetVersion, 0);
    }

    private void Update(uint flags) => Send(Shell32.NimModify, flags);

    private unsafe void Send(uint message, uint flags)
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
