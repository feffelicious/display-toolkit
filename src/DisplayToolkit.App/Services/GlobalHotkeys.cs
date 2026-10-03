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
    private readonly HashSet<int> _registered = [];

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
    public bool Register(int id, ModifierKeys modifiers, Key key)
    {
        Unregister(id);
        var registered = RegisterHotKey(_window.Handle, id, (uint)modifiers | ModNoRepeat, (uint)KeyInterop.VirtualKeyFromKey(key));
        if (registered)
        {
            _registered.Add(id);
        }
        return registered;
    }

    public void Unregister(int id)
    {
        if (_registered.Remove(id))
        {
            UnregisterHotKey(_window.Handle, id);
        }
    }

    public void Dispose()
    {
        foreach (var id in _registered.ToList())
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
