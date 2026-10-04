using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace DisplayToolkit.App.Services;

/// <summary>
/// System-wide keyboard shortcuts through <c>RegisterHotKey</c>, delivered to a hidden message-only window.
/// Must be created on the UI thread; <see cref="Pressed"/> is raised there.
/// </summary>
internal sealed partial class GlobalHotkeys : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;
    private static readonly nint MessageOnlyParent = -3;

    private readonly HwndSource _window;

    /// <summary>Registered shortcuts by id, as passed to <c>RegisterHotKey</c>, so they can be paused and restored.</summary>
    private readonly Dictionary<int, (uint Flags, uint VirtualKey)> _registered = [];
    private bool _isSuspended;

    public GlobalHotkeys()
    {
        _window = new HwndSource(new HwndSourceParameters("DisplayToolkitHotkeys") { ParentWindow = MessageOnlyParent });
        _window.AddHook(WndProc);
    }

    /// <summary>Raised with the id passed to <see cref="Register"/>.</summary>
    public event EventHandler<int>? Pressed;

    /// <summary>
    /// Registers a shortcut. Returns false if Windows or another app already owns it; the caller should tell the user.
    /// </summary>
    /// <param name="repeats">Raise <see cref="Pressed"/> again while the keys are held, for stepping a level.</param>
    public bool Register(int id, ModifierKeys modifiers, Key key, bool repeats = false)
    {
        Unregister(id);
        var flags = (uint)modifiers | (repeats ? 0 : ModNoRepeat);
        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        var registered = _isSuspended || RegisterHotKey(_window.Handle, id, flags, virtualKey);
        if (registered)
        {
            _registered[id] = (flags, virtualKey);
        }
        return registered;
    }

    public void Unregister(int id)
    {
        if (_registered.Remove(id) && !_isSuspended)
        {
            UnregisterHotKey(_window.Handle, id);
        }
    }

    /// <summary>
    /// Pauses every shortcut (true) or brings them back (false), so a shortcut recorder receives the keys instead.
    /// </summary>
    public void SetSuspended(bool suspended)
    {
        if (suspended == _isSuspended)
        {
            return;
        }
        _isSuspended = suspended;
        foreach (var (id, (flags, virtualKey)) in _registered)
        {
            if (suspended)
            {
                UnregisterHotKey(_window.Handle, id);
            }
            else
            {
                RegisterHotKey(_window.Handle, id, flags, virtualKey);
            }
        }
    }

    public void Dispose()
    {
        foreach (var id in _registered.Keys.ToList())
        {
            Unregister(id);
        }
        _window.RemoveHook(WndProc);
        _window.Dispose();
    }

    private nint WndProc(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == WmHotkey)
        {
            Pressed?.Invoke(this, (int)wParam);
            handled = true;
        }
        return 0;
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(nint hwnd, int id, uint modifiers, uint virtualKey);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(nint hwnd, int id);
}
