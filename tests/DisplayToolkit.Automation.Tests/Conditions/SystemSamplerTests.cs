using DisplayToolkit.Automation.Conditions;
using DisplayToolkit.Automation.Native;

namespace DisplayToolkit.Automation.Tests.Conditions;

public sealed class SystemSamplerTests
{
    private const long Popup = 0x80000000;
    private const long OverlappedWindow = 0x00CF0000; // Title bar, system menu, sizing border, minimize and maximize.

    private static readonly Win32.Rect Screen = new() { Left = 0, Top = 0, Right = 3840, Bottom = 2160 };

    [Fact]
    public void A_borderless_window_covering_the_screen_is_full_screen() =>
        Assert.True(SystemSampler.IsBorderlessFullscreen(Popup, isMaximized: false, Screen, Screen));

    [Fact]
    public void A_maximized_window_with_the_taskbar_hidden_is_not()
    {
        // Maximized windows overhang the screen by their border.
        var bounds = new Win32.Rect { Left = -8, Top = -8, Right = 3848, Bottom = 2168 };

        Assert.False(SystemSampler.IsBorderlessFullscreen(OverlappedWindow, isMaximized: true, bounds, Screen));
    }

    [Fact]
    public void A_maximized_borderless_window_is_not()
    {
        // Apps that draw their own title bar (browsers, Electron apps) when maximized.
        Assert.False(SystemSampler.IsBorderlessFullscreen(Popup, isMaximized: true, Screen, Screen));
    }

    [Fact]
    public void A_borderless_window_smaller_than_the_screen_is_not()
    {
        var bounds = new Win32.Rect { Left = 100, Top = 100, Right = 1380, Bottom = 820 };

        Assert.False(SystemSampler.IsBorderlessFullscreen(Popup, isMaximized: false, bounds, Screen));
    }
}
