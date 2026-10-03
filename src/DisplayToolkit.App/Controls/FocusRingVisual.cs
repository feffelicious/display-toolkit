using System.Windows;
using System.Windows.Controls;

namespace DisplayToolkit.App.Controls;

/// <summary>
/// The Fluent focus ring: a 2 px outer stroke 3 px outside the control and a 1 px inner stroke just outside it. Place it
/// as the last child of a template's root panel, collapsed, and show it with a trigger on <see cref="FocusRing.IsVisibleProperty"/>.
/// </summary>
public sealed class FocusRingVisual : Control
{
    /// <summary>The outer ring's radius: the control's corner radius plus 3.</summary>
    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(double), typeof(FocusRingVisual), new PropertyMetadata(7.0, OnCornerRadiusChanged));

    private static readonly DependencyPropertyKey InnerCornerRadiusPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(InnerCornerRadius), typeof(CornerRadius), typeof(FocusRingVisual), new PropertyMetadata(new CornerRadius(5)));

    public static readonly DependencyProperty InnerCornerRadiusProperty = InnerCornerRadiusPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey OuterCornerRadiusPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(OuterCornerRadius), typeof(CornerRadius), typeof(FocusRingVisual), new PropertyMetadata(new CornerRadius(7)));

    public static readonly DependencyProperty OuterCornerRadiusProperty = OuterCornerRadiusPropertyKey.DependencyProperty;

    static FocusRingVisual()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(FocusRingVisual), new FrameworkPropertyMetadata(typeof(FocusRingVisual)));
        FocusableProperty.OverrideMetadata(typeof(FocusRingVisual), new FrameworkPropertyMetadata(false));
        IsHitTestVisibleProperty.OverrideMetadata(typeof(FocusRingVisual), new FrameworkPropertyMetadata(false));
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public CornerRadius OuterCornerRadius => (CornerRadius)GetValue(OuterCornerRadiusProperty);

    public CornerRadius InnerCornerRadius => (CornerRadius)GetValue(InnerCornerRadiusProperty);

    private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var radius = (double)e.NewValue;
        d.SetValue(OuterCornerRadiusPropertyKey, new CornerRadius(radius));
        d.SetValue(InnerCornerRadiusPropertyKey, new CornerRadius(Math.Max(0, radius - 2)));
    }
}
