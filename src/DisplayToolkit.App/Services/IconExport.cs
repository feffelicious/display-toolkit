#if DEBUG
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DisplayToolkit.App.Services;

/// <summary>
/// Development aid: <c>DisplayToolkit.exe --export-icon &lt;file.ico&gt;</c> writes the app icon from
/// <see cref="IconArt"/> as a multi-size .ico (the one in Assets is made this way).
/// </summary>
internal static class IconExport
{
    private static readonly int[] Sizes = [16, 20, 24, 32, 40, 48, 64, 256];

    public static string? RequestedPath(string[] args)
    {
        var index = Array.IndexOf(args, "--export-icon");
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    /// <summary>Writes an .ico with one PNG image per size (supported since Windows Vista).</summary>
    public static void Write(string path)
    {
        var images = Sizes.Select(size => (Size: size, Png: RenderPng(size))).ToList();

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write((short)0); // Reserved
        writer.Write((short)1); // Icon
        writer.Write((short)images.Count);

        var offset = 6 + (16 * images.Count);
        foreach (var (size, png) in images)
        {
            writer.Write((byte)(size >= 256 ? 0 : size)); // 0 means 256
            writer.Write((byte)(size >= 256 ? 0 : size));
            writer.Write((byte)0); // No palette
            writer.Write((byte)0); // Reserved
            writer.Write((short)1); // Color planes
            writer.Write((short)32); // Bits per pixel
            writer.Write(png.Length);
            writer.Write(offset);
            offset += png.Length;
        }
        foreach (var (_, png) in images)
        {
            writer.Write(png);
        }
    }

    private static byte[] RenderPng(int size)
    {
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            IconArt.DrawAppIcon(context, size);
        }
        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var memory = new MemoryStream();
        encoder.Save(memory);
        return memory.ToArray();
    }
}
#endif
