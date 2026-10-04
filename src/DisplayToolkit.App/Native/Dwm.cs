using System.Runtime.InteropServices;

namespace DisplayToolkit.App.Native;

internal static partial class Dwm
{
    private const int ExtendedFrameBounds = 9;
    private const int Cloaked = 14;
    private const int UseImmersiveDarkMode = 20;
    private const int WindowCornerPreference = 33;
    private const int SystemBackdropType = 38;

    internal enum Backdrop
    {
        None = 1,
        Mica = 2,
        Acrylic = 3,
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

    [LibraryImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
    private static partial int DwmGetWindowRect(nint hwnd, int attribute, out User32.Rect value, int size);

    [LibraryImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
    private static partial int DwmGetWindowInt(nint hwnd, int attribute, out int value, int size);

    /// <summary>The visible bounds of a window in physical pixels, without the invisible resize borders.</summary>
    internal static User32.Rect? GetFrameBounds(nint hwnd) =>
        DwmGetWindowRect(hwnd, ExtendedFrameBounds, out var bounds, 16) == 0 && bounds.Width > 0 && bounds.Height > 0 ? bounds : null;

    /// <summary>Hidden by Windows although "visible", for example a suspended app or a window on another desktop.</summary>
    internal static bool IsCloaked(nint hwnd) => DwmGetWindowInt(hwnd, Cloaked, out var cloaked, sizeof(int)) == 0 && cloaked != 0;

    internal static void SetBackdrop(nint hwnd, Backdrop backdrop) => Set(hwnd, SystemBackdropType, (int)backdrop);

    internal static void SetDarkMode(nint hwnd, bool dark) => Set(hwnd, UseImmersiveDarkMode, dark ? 1 : 0);

    /// <summary>Rounded corners (DWMWCP_ROUND).</summary>
    internal static void SetRoundCorners(nint hwnd) => Set(hwnd, WindowCornerPreference, 2);

    private static void Set(nint hwnd, int attribute, int value) => DwmSetWindowAttribute(hwnd, attribute, ref value, sizeof(int));
}
