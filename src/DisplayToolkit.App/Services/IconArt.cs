using System.Windows;
using System.Windows.Media;

namespace DisplayToolkit.App.Services;

/// <summary>
/// The Display Toolkit icon, drawn from geometry so it's sharp at every size: a rounded screen holding a half-filled
/// disc (the "adjust" symbol). The tray uses the symbol alone in the taskbar's text color; the app icon puts it in white
/// on a blue-to-violet tile.
/// </summary>
internal static class IconArt
{
    /// <summary>The symbol is designed on a 16 x 16 grid.</summary>
    private const double Grid = 16;

    private static readonly Color TileTop = Color.FromRgb(56, 120, 255);
    private static readonly Color TileBottom = Color.FromRgb(124, 58, 237);

    /// <summary>The app icon as an image, for the title bar.</summary>
    public static ImageSource AppIcon { get; } = CreateAppIconImage();

    /// <summary>Draws the symbol into a <paramref name="size"/>-pixel square.</summary>
    public static void DrawSymbol(DrawingContext context, Brush brush, double size)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scale = size / Grid;
        context.PushTransform(new ScaleTransform(scale, scale));

        // The screen: a rounded rectangle, a little wider than tall.
        context.DrawRoundedRectangle(null, new Pen(brush, 1.3), new Rect(1, 2.5, 14, 11), 2.2, 2.2);

        // The disc: outlined, with its left half filled.
        var center = new Point(8, 8);
        const double Radius = 3.3;
        context.DrawEllipse(null, new Pen(brush, 1.2), center, Radius, Radius);
        var half = new PathGeometry([
            new PathFigure(new Point(center.X, center.Y - Radius), [
                new ArcSegment(new Point(center.X, center.Y + Radius), new Size(Radius, Radius), 0, false, SweepDirection.Counterclockwise, true),
            ], closed: true),
        ]);
        context.DrawGeometry(brush, null, half);

        context.Pop();
    }

    /// <summary>Draws the app icon (tile and white symbol) into a <paramref name="size"/>-pixel square.</summary>
    public static void DrawAppIcon(DrawingContext context, double size)
    {
        ArgumentNullException.ThrowIfNull(context);
        var tile = new LinearGradientBrush(TileTop, TileBottom, new Point(0, 0), new Point(1, 1));
        context.DrawRoundedRectangle(tile, null, new Rect(0, 0, size, size), size * 0.225, size * 0.225);

        // Small icons get a bigger symbol, so its strokes stay readable.
        var inset = size * (size >= 48 ? 0.16 : 0.08);
        context.PushTransform(new TranslateTransform(inset, inset));
        DrawSymbol(context, Brushes.White, size - (2 * inset));
        context.Pop();
    }

    private static DrawingImage CreateAppIconImage()
    {
        var group = new DrawingGroup();
        using (var context = group.Open())
        {
            DrawAppIcon(context, 256);
        }
        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }
}
