using System.Runtime.InteropServices;

namespace DisplayToolkit.App.Native;

internal static partial class Dwm
{
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

    internal static void SetBackdrop(nint hwnd, Backdrop backdrop) => Set(hwnd, SystemBackdropType, (int)backdrop);

    internal static void SetDarkMode(nint hwnd, bool dark) => Set(hwnd, UseImmersiveDarkMode, dark ? 1 : 0);

    /// <summary>Rounded corners (DWMWCP_ROUND).</summary>
    internal static void SetRoundCorners(nint hwnd) => Set(hwnd, WindowCornerPreference, 2);

    private static void Set(nint hwnd, int attribute, int value) => DwmSetWindowAttribute(hwnd, attribute, ref value, sizeof(int));
}
