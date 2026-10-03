using System.Runtime.InteropServices;

namespace DisplayToolkit.Core.Native;

internal static unsafe partial class User32
{
    private const string Dll = "user32.dll";

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MonitorInfoEx
    {
        public uint Size;
        public Rect Monitor;
        public Rect WorkArea;
        public uint Flags;
        public fixed char DeviceName[32];
    }

    [LibraryImport(Dll)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumDisplayMonitors(
        nint hdc, Rect* clip, delegate* unmanaged<nint, nint, Rect*, nint, int> callback, nint data);

    [LibraryImport(Dll, EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetMonitorInfo(nint hMonitor, MonitorInfoEx* info);

    /// <summary>Returns the HMONITOR handles of all display monitors, in enumeration order.</summary>
    internal static List<nint> GetMonitorHandles()
    {
        var handles = new List<nint>();
        var gcHandle = GCHandle.Alloc(handles);
        try
        {
            EnumDisplayMonitors(0, null, &OnMonitor, GCHandle.ToIntPtr(gcHandle));
        }
        finally
        {
            gcHandle.Free();
        }
        return handles;
    }

    /// <summary>Returns the GDI device name (for example <c>\\.\DISPLAY1</c>) of a monitor.</summary>
    internal static string? GetDeviceName(nint hMonitor)
    {
        var info = new MonitorInfoEx { Size = (uint)sizeof(MonitorInfoEx) };
        return GetMonitorInfo(hMonitor, &info) ? new string(info.DeviceName) : null;
    }

    [UnmanagedCallersOnly]
    private static int OnMonitor(nint hMonitor, nint hdc, Rect* rect, nint data)
    {
        var handles = (List<nint>)GCHandle.FromIntPtr(data).Target!;
        handles.Add(hMonitor);
        return 1;
    }
}
