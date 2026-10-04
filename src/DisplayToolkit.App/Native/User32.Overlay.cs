using System.Runtime.InteropServices;

namespace DisplayToolkit.App.Native;

/// <summary>Plain Win32 windows, regions and window events, for Target mode and the tray icon's mouse wheel.</summary>
internal static unsafe partial class User32
{
    internal const uint WsPopup = 0x80000000;
    internal const uint WsExLayered = 0x00080000;
    internal const uint WsExTransparentStyle = 0x20;
    internal const uint WsExToolWindowStyle = 0x80;
    internal const uint WsExNoActivateStyle = 0x08000000;
    internal const uint WsExTopmost = 0x8;

    internal const uint LwaAlpha = 0x2;
    internal const int SwShowNoActivate = 4;
    internal const int SwHide = 0;

    internal const uint SwpNoSize = 0x0001;
    internal const uint SwpNoMove = 0x0002;
    internal static readonly nint HwndTopmost = -1;

    internal const uint EventSystemForeground = 0x0003;
    internal const uint EventSystemMinimizeStart = 0x0016;
    internal const uint EventSystemMinimizeEnd = 0x0017;
    internal const uint EventObjectLocationChange = 0x800B;
    internal const uint WinEventOutOfContext = 0x0000;
    internal const uint WinEventSkipOwnProcess = 0x0002;

    internal const int WhMouseLowLevel = 14;
    internal const int WmMouseMove = 0x0200;
    internal const int WmMouseWheel = 0x020A;

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowClass
    {
        public uint Size;
        public uint Style;
        public nint WindowProcedure;
        public int ClassExtra;
        public int WindowExtra;
        public nint Instance;
        public nint Icon;
        public nint Cursor;
        public nint Background;
        public char* MenuName;
        public char* ClassName;
        public nint SmallIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LowLevelMouse
    {
        public Point Point;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    [LibraryImport("user32.dll", EntryPoint = "RegisterClassExW")]
    internal static partial ushort RegisterClassEx(WindowClass* windowClass);

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint CreateWindowEx(
        uint exStyle, string className, string windowName, uint style, int x, int y, int width, int height,
        nint parent, nint menu, nint instance, nint parameter);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyWindow(nint hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(nint hwnd, int command);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetLayeredWindowAttributes(nint hwnd, uint colorKey, byte alpha, uint flags);

    /// <summary>The window owns the region afterwards; don't delete it.</summary>
    [LibraryImport("user32.dll")]
    internal static partial int SetWindowRgn(nint hwnd, nint region, [MarshalAs(UnmanagedType.Bool)] bool redraw);

    [LibraryImport("user32.dll")]
    internal static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindowVisible(nint hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsIconic(nint hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsZoomed(nint hwnd);

    [LibraryImport("user32.dll", EntryPoint = "GetClassNameW")]
    private static partial int GetClassName(nint hwnd, char* name, int length);

    [LibraryImport("user32.dll")]
    internal static partial uint GetWindowThreadProcessId(nint hwnd, out uint processId);

    [LibraryImport("user32.dll")]
    internal static partial nint SetWinEventHook(
        uint eventMin, uint eventMax, nint module, delegate* unmanaged<nint, uint, nint, int, int, uint, uint, void> callback,
        uint processId, uint threadId, uint flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnhookWinEvent(nint hook);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowsHookExW")]
    internal static partial nint SetWindowsHookEx(int type, delegate* unmanaged<int, nint, nint, nint> callback, nint module, uint threadId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnhookWindowsHookEx(nint hook);

    [LibraryImport("user32.dll")]
    internal static partial nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint GetModuleHandle(string? name);

    /// <summary>SM_XVIRTUALSCREEN and friends: the rectangle around every screen, in physical pixels.</summary>
    internal static Rect VirtualScreen
    {
        get
        {
            var left = GetSystemMetrics(76);
            var top = GetSystemMetrics(77);
            return new Rect { Left = left, Top = top, Right = left + GetSystemMetrics(78), Bottom = top + GetSystemMetrics(79) };
        }
    }

    internal static string GetClassName(nint hwnd)
    {
        var buffer = stackalloc char[256];
        var length = GetClassName(hwnd, buffer, 256);
        return new string(buffer, 0, Math.Max(length, 0));
    }

    /// <summary>A window procedure that does nothing of its own, for windows that only need to be drawn.</summary>
    internal static nint DefaultWindowProcedure =>
        NativeLibrary.GetExport(NativeLibrary.Load("user32.dll"), "DefWindowProcW");
}

internal static partial class Gdi32
{
    internal const int RgnDiff = 4;
    internal const int BlackBrush = 4;

    [LibraryImport("gdi32.dll")]
    internal static partial nint CreateRectRgn(int left, int top, int right, int bottom);

    [LibraryImport("gdi32.dll")]
    internal static partial nint CreateRoundRectRgn(int left, int top, int right, int bottom, int widthEllipse, int heightEllipse);

    [LibraryImport("gdi32.dll")]
    internal static partial int CombineRgn(nint destination, nint source1, nint source2, int mode);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeleteObject(nint handle);

    [LibraryImport("gdi32.dll")]
    internal static partial nint GetStockObject(int index);
}
