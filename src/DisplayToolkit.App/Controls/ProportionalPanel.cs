using System.Windows;
using System.Windows.Controls;

namespace DisplayToolkit.App.Controls;

/// <summary>
/// Lays children out along the width by fraction: <see cref="StartProperty"/> 0–1 and <see cref="LengthProperty"/>
/// 0–1. A child with no length is a marker: centered on its start, but kept inside the panel. Used by the day strip.
/// </summary>
public sealed class ProportionalPanel : Panel
{
    public static readonly DependencyProperty StartProperty = DependencyProperty.RegisterAttached(
        "Start", typeof(double), typeof(ProportionalPanel), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsParentArrange));

    public static readonly DependencyProperty LengthProperty = DependencyProperty.RegisterAttached(
        "Length", typeof(double), typeof(ProportionalPanel), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

    public static double GetStart(DependencyObject element) => (double)element.GetValue(StartProperty);

    public static void SetStart(DependencyObject element, double value) => element.SetValue(StartProperty, value);

    public static double GetLength(DependencyObject element) => (double)element.GetValue(LengthProperty);

    public static void SetLength(DependencyObject element, double value) => element.SetValue(LengthProperty, value);

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
        var height = 0.0;
        foreach (UIElement child in InternalChildren)
        {
            var length = GetLength(child);
            child.Measure(new Size(length > 0 ? length * width : double.PositiveInfinity, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);
        }
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (UIElement child in InternalChildren)
        {
            var start = GetStart(child) * finalSize.Width;
            var length = GetLength(child);
            if (length > 0)
            {
                child.Arrange(new Rect(start, 0, length * finalSize.Width, finalSize.Height));
            }
            else
            {
                var width = child.DesiredSize.Width;
                var x = Math.Clamp(start - (width / 2), 0, Math.Max(0, finalSize.Width - width));
                child.Arrange(new Rect(x, 0, width, finalSize.Height));
            }
        }
        return finalSize;
    }
}
