#if DEBUG
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DisplayToolkit.App.Services;

/// <summary>
/// Development aid: <c>DisplayToolkit.exe --snapshot &lt;folder&gt;</c> renders a window's content to a PNG on a solid
/// theme-colored background (the Acrylic backdrop can't be captured), so layout can be reviewed without clicking.
/// </summary>
internal static class DebugSnapshot
{
    public static string? RequestedFolder(string[] args)
    {
        var index = Array.IndexOf(args, "--snapshot");
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    public static void Save(FrameworkElement element, string folder, string name)
    {
        var dpi = VisualTreeHelper.GetDpi(element);
        var width = (int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX);
        var height = (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY);
        if (width == 0 || height == 0)
        {
            return;
        }

        var background = Application.Current.TryFindResource("SolidBackgroundFillColorBaseBrush") as Brush ?? Brushes.White;
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var size = new Size(element.ActualWidth, element.ActualHeight);
            context.DrawRectangle(background, null, new Rect(size));
            // Absolute viewbox, no stretch: keep the element's padding instead of cropping to its content.
            var brush = new VisualBrush(element)
            {
                Stretch = Stretch.None,
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(size),
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
            };
            context.DrawRectangle(brush, null, new Rect(size));
        }

        var bitmap = new RenderTargetBitmap(width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        Directory.CreateDirectory(folder);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(folder, $"{name}.png"));
        encoder.Save(stream);
    }
}
#endif
