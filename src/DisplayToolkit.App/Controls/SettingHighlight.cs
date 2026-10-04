using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DisplayToolkit.App.Controls;

/// <summary>
/// Points at a setting found by search: opens the expanders around it, scrolls it into view and draws an accent
/// outline around it that fades away.
/// </summary>
public static class SettingHighlight
{
    private static readonly Duration FadeDuration = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// Finds the card, expander or heading titled <paramref name="title"/> inside <paramref name="root"/> and points at
    /// it. Returns false if the page has no such element (the page itself is still open).
    /// </summary>
    public static bool Reveal(DependencyObject root, string title)
    {
        // Realized elements first (this also covers cards generated from lists), then the logical tree, which also
        // has the rows of collapsed expanders. The page is built from a template, so the logical search starts inside
        // the page view itself.
        var page = FindVisual(root, element => element is UserControl) ?? root;
        var target = FindVisual(root, element => IsCard(element, title))
            ?? FindLogical(page, element => IsCard(element, title))
            ?? FindVisual(root, element => element is TextBlock text && text.Text == title);
        if (target is null)
        {
            return false;
        }

        for (var parent = LogicalTreeHelper.GetParent(target); parent is not null; parent = LogicalTreeHelper.GetParent(parent))
        {
            if (parent is SettingsExpander expander)
            {
                expander.IsExpanded = true;
            }
        }

        // Expanding changes the layout: point at the element once it's arranged.
        target.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            target.BringIntoView();
            if (AdornerLayer.GetAdornerLayer(target) is { } layer)
            {
                var outline = new Outline(target);
                layer.Add(outline);
                var fade = new DoubleAnimation(1, 0, FadeDuration) { BeginTime = TimeSpan.FromSeconds(0.8) };
                fade.Completed += (_, _) => layer.Remove(outline);
                outline.BeginAnimation(UIElement.OpacityProperty, fade);
            }
        });
        return true;
    }

    private static bool IsCard(DependencyObject element, string title) =>
        element is SettingsCard card && card.Header == title || element is SettingsExpander expander && expander.Header == title;

    private static FrameworkElement? FindVisual(DependencyObject parent, Func<DependencyObject, bool> match)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement { IsVisible: true } element && match(element))
            {
                return element;
            }
            if (child is UIElement { Visibility: not Visibility.Visible })
            {
                continue;
            }
            if (FindVisual(child, match) is { } found)
            {
                return found;
            }
        }
        return null;
    }

    private static FrameworkElement? FindLogical(DependencyObject parent, Func<DependencyObject, bool> match)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            if (child is UIElement { Visibility: Visibility.Collapsed })
            {
                continue; // Hidden on purpose: not supported by this monitor, or not available right now.
            }
            if (child is FrameworkElement element && match(element))
            {
                return element;
            }
            if (FindLogical(child, match) is { } found)
            {
                return found;
            }
        }
        return null;
    }

    private sealed class Outline(UIElement element) : Adorner(element)
    {
        protected override void OnRender(DrawingContext drawingContext)
        {
            var brush = TryFindResource("AccentFillColorDefaultBrush") as Brush ?? SystemColors.HighlightBrush;
            var bounds = new Rect(AdornedElement.RenderSize);
            bounds.Inflate(1, 1);
            drawingContext.DrawRoundedRectangle(null, new Pen(brush, 2), bounds, 6, 6);
        }
    }
}
