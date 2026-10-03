using System.Runtime.InteropServices;

namespace DisplayToolkit.App.Native;

internal static unsafe partial class User32
{
    internal const int WmDisplayChange = 0x007E;
    internal const int WmPowerBroadcast = 0x0218;
    internal const int PbtApmResumeAutomatic = 0x12;
    internal const int WmSettingChange = 0x001A;

    internal const uint SwpNoZOrder = 0x0004;
    internal const uint SwpNoActivate = 0x0010;

    private const uint MonitorDefaultToNearest = 2;

    private const int GwlExStyle = -20;
    private const nint WsExTransparent = 0x20;
    private const nint WsExToolWindow = 0x80;
    private const nint WsExNoActivate = 0x08000000;

    [StructLayout(LayoutKind.Sequential)]
    internal struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly int Width => Right - Left;

        public readonly int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public uint Size;
        public Rect Monitor;
        public Rect WorkArea;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IconInfo
    {
        public int IsIcon;
        public int HotspotX;
        public int HotspotY;
        public nint MaskBitmap;
        public nint ColorBitmap;
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetCursorPos(out Point point);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetForegroundWindow(nint hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int width, int height, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint RegisterWindowMessage(string name);

    /// <summary>SM_CXSMICON: small icon size at the system DPI, which is what the notification area uses.</summary>
    internal static int SmallIconSize => GetSystemMetrics(49);

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int index);

    [LibraryImport("user32.dll")]
    private static partial nint MonitorFromPoint(Point point, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorInfo(nint monitor, MonitorInfo* info);

    [LibraryImport("shcore.dll")]
    private static partial int GetDpiForMonitor(nint monitor, int type, out uint dpiX, out uint dpiY);

    [LibraryImport("user32.dll")]
    private static partial nint CreateIconIndirect(IconInfo* info);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyIcon(nint icon);

    [LibraryImport("gdi32.dll")]
    private static partial nint CreateBitmap(int width, int height, uint planes, uint bitsPerPixel, void* bits);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(nint handle);

    /// <summary>Screen and work area (in physical pixels) and DPI scale of the monitor nearest to a point.</summary>
    internal static (Rect Bounds, Rect WorkArea, double Scale) GetMonitorAt(Point point)
    {
        var monitor = MonitorFromPoint(point, MonitorDefaultToNearest);
        var info = new MonitorInfo { Size = (uint)sizeof(MonitorInfo) };
        GetMonitorInfo(monitor, &info);
        var scale = GetDpiForMonitor(monitor, 0, out var dpi, out _) == 0 ? dpi / 96.0 : 1.0;
        return (info.Monitor, info.WorkArea, scale);
    }

    /// <summary>
    /// Turns a window into an overlay: clicks go through it, it never takes focus, and it stays out of Alt+Tab.
    /// </summary>
    internal static void MakeOverlay(nint hwnd) =>
        SetWindowLongPtr(hwnd, GwlExStyle, GetWindowLongPtr(hwnd, GwlExStyle) | WsExTransparent | WsExToolWindow | WsExNoActivate);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static partial nint GetWindowLongPtr(nint hwnd, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static partial nint SetWindowLongPtr(nint hwnd, int index, nint value);

    /// <summary>Creates an icon from premultiplied 32-bit BGRA pixels. The caller owns the handle.</summary>
    internal static nint CreateIcon(int width, int height, ReadOnlySpan<byte> bgraPixels)
    {
        fixed (byte* pixels = bgraPixels)
        {
            var color = CreateBitmap(width, height, 1, 32, pixels);
            var mask = CreateBitmap(width, height, 1, 1, null);
            try
            {
                var info = new IconInfo { IsIcon = 1, ColorBitmap = color, MaskBitmap = mask };
                return CreateIconIndirect(&info);
            }
            finally
            {
                DeleteObject(color);
                DeleteObject(mask);
            }
        }
    }
}
