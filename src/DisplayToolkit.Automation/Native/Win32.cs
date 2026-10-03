using System.Runtime.InteropServices;

namespace DisplayToolkit.Automation.Native;

internal static partial class Win32
{
    /// <summary><c>QUNS_RUNNING_D3D_FULL_SCREEN</c>: an exclusive full-screen Direct3D app (almost always a game).</summary>
    public const int QunsRunningD3DFullScreen = 3;

    /// <summary><c>BATTERY_FLAG_NO_BATTERY</c>.</summary>
    public const byte BatteryFlagNoBattery = 128;

    /// <summary><c>WS_CAPTION</c>: a title bar (border and dialog frame bits together).</summary>
    public const long WsCaption = 0x00C00000;

    private const uint MonitorDefaultToNearest = 2;
    private const int GwlStyle = -16;

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SystemPowerStatus
    {
        public byte AcLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    /// <summary>The bounds of the monitor a window is on, or null if Windows can't tell.</summary>
    public static Rect? MonitorBoundsOf(nint window)
    {
        var monitor = MonitorFromWindow(window, MonitorDefaultToNearest);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        return monitor != 0 && GetMonitorInfoW(monitor, ref info) ? info.Monitor : null;
    }

    public static long GetWindowStyle(nint window) => GetWindowLongPtrW(window, GwlStyle);

    [LibraryImport("user32.dll")]
    public static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsZoomed(nint window);

    [LibraryImport("user32.dll")]
    private static partial nint GetWindowLongPtrW(nint window, int index);

    [LibraryImport("user32.dll")]
    public static partial nint GetShellWindow();

    [LibraryImport("user32.dll")]
    public static partial uint GetWindowThreadProcessId(nint window, out uint processId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowRect(nint window, out Rect rect);

    [LibraryImport("shell32.dll")]
    public static partial int SHQueryUserNotificationState(out int state);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetSystemPowerStatus(out SystemPowerStatus status);

    [LibraryImport("user32.dll")]
    private static partial nint MonitorFromWindow(nint window, uint flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorInfoW(nint monitor, ref MonitorInfo info);
}
