using System.Windows.Media;
using System.Windows.Media.Imaging;
using DisplayToolkit.App.Native;
using DisplayToolkit.App.Services;

namespace DisplayToolkit.App.Tray;

/// <summary>Draws the tray icon from <see cref="IconArt"/>, so it is crisp at any DPI and matches the taskbar theme.</summary>
internal static class TrayIconRenderer
{
    /// <summary>Returns an HICON the caller must destroy with <see cref="User32.DestroyIcon"/>.</summary>
    public static nint Render(bool lightTaskbar)
    {
        var size = User32.SmallIconSize;
        var brush = lightTaskbar ? Brushes.Black : Brushes.White;

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            IconArt.DrawSymbol(context, brush, size);
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var pixels = new byte[size * size * 4];
        bitmap.CopyPixels(pixels, size * 4, 0);
        Unpremultiply(pixels);

        return User32.CreateIcon(size, size, pixels);
    }

    /// <summary>Icons use straight alpha; WPF renders premultiplied.</summary>
    private static void Unpremultiply(Span<byte> bgra)
    {
        for (var i = 0; i < bgra.Length; i += 4)
        {
            var alpha = bgra[i + 3];
            if (alpha is 0 or 255)
            {
                continue;
            }
            for (var channel = 0; channel < 3; channel++)
            {
                bgra[i + channel] = (byte)Math.Min(255, bgra[i + channel] * 255 / alpha);
            }
        }
    }
}
