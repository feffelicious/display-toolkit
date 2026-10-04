using System.Runtime.InteropServices;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DisplayToolkit.App.Native;

namespace DisplayToolkit.App.Services;

/// <summary>
/// Target mode: dims every screen except the window in use, so attention stays on it. A click-through, topmost black
/// window covers all screens with a hole cut where the foreground window is, and the hole follows the window as it
/// moves or as another window takes over. Lives on the UI thread.
/// </summary>
public sealed unsafe partial class TargetMode : ObservableObject, IDisposable
{
    private const string WindowClassName = "DisplayToolkitTargetMode";

    /// <summary>Windows 11 rounds window corners by 8 px (at 100 %); the hole follows the curve.</summary>
    private const double CornerRadius = 8;

    private static readonly TimeSpan FadeStep = TimeSpan.FromMilliseconds(15);
    private const int FadeSteps = 10;

    /// <summary>Windows that never become the target: the taskbar and the desktop. Clicking them keeps the last target.</summary>
    private static readonly HashSet<string> ShellClasses =
        ["Shell_TrayWnd", "Shell_SecondaryTrayWnd", "Progman", "WorkerW", "NotifyIconOverflowWindow", "TopLevelWindowForOverflowXamlIsland"];

    /// <summary>The window events arrive in static callbacks; there is one Target mode per app.</summary>
    private static TargetMode? s_instance;
    private static bool s_classRegistered;

    private readonly AppSettings _settings;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _fade = new() { Interval = FadeStep };
    private nint _overlay;
    private nint _systemHook;
    private nint _minimizeHook;
    private nint _locationHook;
    private nint _target;
    private User32.Rect _screen;
    private bool _updateQueued;
    private int _fadeStep;
    private bool _fadingOut;

    internal TargetMode(AppSettings settings, Dispatcher dispatcher)
    {
        _settings = settings;
        _dispatcher = dispatcher;
        _fade.Tick += (_, _) => OnFadeStep();
        settings.Changed += (_, _) =>
        {
            if (_overlay != 0 && !_fade.IsEnabled)
            {
                User32.SetLayeredWindowAttributes(_overlay, 0, Alpha(1), User32.LwaAlpha);
            }
        };
    }

    /// <summary>The overlay is showing (or fading in).</summary>
    [ObservableProperty]
    public partial bool IsOn { get; private set; }

    public void Toggle() => SetOn(!IsOn);

    public void SetOn(bool on)
    {
        if (on == IsOn)
        {
            return;
        }
        IsOn = on;
        if (on)
        {
            Show();
        }
        else
        {
            StartFade(fadingOut: true);
        }
    }

    /// <summary>Screens were added, removed or rearranged: cover them all again.</summary>
    public void OnDisplaysChanged()
    {
        if (_overlay != 0)
        {
            _screen = User32.VirtualScreen;
            User32.SetWindowPos(_overlay, User32.HwndTopmost, _screen.Left, _screen.Top, _screen.Width, _screen.Height, User32.SwpNoActivate);
            UpdateHole();
        }
    }

    public void Dispose()
    {
        _fade.Stop();
        Close();
    }

    private void Show()
    {
        if (_overlay == 0)
        {
            RegisterWindowClass();
            _screen = User32.VirtualScreen;
            _overlay = User32.CreateWindowEx(
                User32.WsExLayered | User32.WsExTransparentStyle | User32.WsExToolWindowStyle | User32.WsExNoActivateStyle | User32.WsExTopmost,
                WindowClassName, "Target mode", User32.WsPopup,
                _screen.Left, _screen.Top, _screen.Width, _screen.Height, 0, 0, User32.GetModuleHandle(null), 0);
            if (_overlay == 0)
            {
                IsOn = false;
                return;
            }

            s_instance = this;
            _systemHook = User32.SetWinEventHook(User32.EventSystemForeground, User32.EventSystemForeground, 0, &OnWinEvent, 0, 0, User32.WinEventOutOfContext);
            _minimizeHook = User32.SetWinEventHook(User32.EventSystemMinimizeStart, User32.EventSystemMinimizeEnd, 0, &OnWinEvent, 0, 0, User32.WinEventOutOfContext);
            User32.SetLayeredWindowAttributes(_overlay, 0, 0, User32.LwaAlpha);
            SetTarget(User32.GetForegroundWindow());
            User32.ShowWindow(_overlay, User32.SwShowNoActivate);
        }
        StartFade(fadingOut: false);
    }

    private void Close()
    {
        foreach (var hook in (ReadOnlySpan<nint>)[_systemHook, _minimizeHook, _locationHook])
        {
            if (hook != 0)
            {
                User32.UnhookWinEvent(hook);
            }
        }
        _systemHook = _minimizeHook = _locationHook = 0;
        if (_overlay != 0)
        {
            User32.DestroyWindow(_overlay);
            _overlay = 0;
        }
        _target = 0;
        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    private void StartFade(bool fadingOut)
    {
        // Continue from wherever a fade in the other direction got to.
        _fadeStep = _fade.IsEnabled ? FadeSteps - _fadeStep : 0;
        _fadingOut = fadingOut;
        _fade.Start();
    }

    private void OnFadeStep()
    {
        if (_overlay == 0)
        {
            _fade.Stop();
            return;
        }
        _fadeStep++;
        var progress = (double)_fadeStep / FadeSteps;
        User32.SetLayeredWindowAttributes(_overlay, 0, Alpha(_fadingOut ? 1 - progress : progress), User32.LwaAlpha);
        if (_fadeStep >= FadeSteps)
        {
            _fade.Stop();
            if (_fadingOut)
            {
                Close();
            }
        }
    }

    private byte Alpha(double progress) => (byte)Math.Round(Math.Clamp(_settings.Current.TargetModeDim, 0, 1) * 255 * progress);

    /// <summary>Follows a new foreground window, unless it's the taskbar, the desktop or something invisible.</summary>
    private void OnForegroundChanged(nint hwnd)
    {
        if (hwnd == 0 || hwnd == _overlay || !User32.IsWindowVisible(hwnd) || Dwm.GetFrameBounds(hwnd) is null
            || ShellClasses.Contains(User32.GetClassName(hwnd)))
        {
            QueueUpdate();
            return;
        }
        SetTarget(hwnd);
    }

    private void SetTarget(nint hwnd)
    {
        _target = hwnd;

        // Location changes are only watched in the target's process: far fewer events than watching everything.
        if (_locationHook != 0)
        {
            User32.UnhookWinEvent(_locationHook);
            _locationHook = 0;
        }
        if (hwnd != 0 && User32.GetWindowThreadProcessId(hwnd, out var processId) != 0)
        {
            _locationHook = User32.SetWinEventHook(
                User32.EventObjectLocationChange, User32.EventObjectLocationChange, 0, &OnWinEvent, processId, 0, User32.WinEventOutOfContext);
        }

        // Stay above windows that became topmost since (they're dimmed too, unless they're the target).
        User32.SetWindowPos(_overlay, User32.HwndTopmost, 0, 0, 0, 0, User32.SwpNoMove | User32.SwpNoSize | User32.SwpNoActivate);
        UpdateHole();
    }

    /// <summary>Moving a window raises many events; redraw once per frame.</summary>
    private void QueueUpdate()
    {
        if (_updateQueued)
        {
            return;
        }
        _updateQueued = true;
        _dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
        {
            _updateQueued = false;
            UpdateHole();
        });
    }

    /// <summary>The overlay's shape: everything, minus the target window (with rounded corners unless it fills its screen).</summary>
    private void UpdateHole()
    {
        if (_overlay == 0)
        {
            return;
        }

        var region = Gdi32.CreateRectRgn(0, 0, _screen.Width, _screen.Height);
        if (_target != 0 && User32.IsWindowVisible(_target) && !User32.IsIconic(_target) && !Dwm.IsCloaked(_target)
            && Dwm.GetFrameBounds(_target) is { } bounds)
        {
            var center = new User32.Point { X = bounds.Left + (bounds.Width / 2), Y = bounds.Top + (bounds.Height / 2) };
            var (monitor, _, scale) = User32.GetMonitorAt(center);
            var fillsScreen = User32.IsZoomed(_target)
                || (bounds.Left <= monitor.Left && bounds.Top <= monitor.Top && bounds.Right >= monitor.Right && bounds.Bottom >= monitor.Bottom);
            var diameter = fillsScreen ? 0 : (int)Math.Round(CornerRadius * 2 * scale);

            var (left, top) = (bounds.Left - _screen.Left, bounds.Top - _screen.Top);
            var hole = diameter > 0
                ? Gdi32.CreateRoundRectRgn(left, top, left + bounds.Width + 1, top + bounds.Height + 1, diameter, diameter)
                : Gdi32.CreateRectRgn(left, top, left + bounds.Width, top + bounds.Height);
            _ = Gdi32.CombineRgn(region, region, hole, Gdi32.RgnDiff);
            Gdi32.DeleteObject(hole);
        }
        User32.SetWindowRgn(_overlay, region, redraw: true);
    }

    private void OnWinEvent(uint @event, nint hwnd, int objectId)
    {
        switch (@event)
        {
            case User32.EventSystemForeground:
                OnForegroundChanged(hwnd);
                break;
            case User32.EventSystemMinimizeStart or User32.EventSystemMinimizeEnd:
                QueueUpdate();
                break;
            case User32.EventObjectLocationChange when hwnd == _target && objectId == 0:
                QueueUpdate();
                break;
        }
    }

    [UnmanagedCallersOnly]
    private static void OnWinEvent(nint hook, uint @event, nint hwnd, int objectId, int childId, uint thread, uint time)
    {
        try
        {
            s_instance?.OnWinEvent(@event, hwnd, objectId);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // An exception can't cross back into Windows; losing one update is harmless.
        }
    }

    /// <summary>A class with a black background and the default window procedure: the overlay only needs to be drawn.</summary>
    private static void RegisterWindowClass()
    {
        if (s_classRegistered)
        {
            return;
        }
        fixed (char* name = WindowClassName)
        {
            var windowClass = new User32.WindowClass
            {
                Size = (uint)sizeof(User32.WindowClass),
                WindowProcedure = User32.DefaultWindowProcedure,
                Instance = User32.GetModuleHandle(null),
                Background = Gdi32.GetStockObject(Gdi32.BlackBrush),
                ClassName = name,
            };
            s_classRegistered = User32.RegisterClassEx(&windowClass) != 0;
        }
    }
}
