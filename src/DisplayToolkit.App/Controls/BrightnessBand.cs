using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace DisplayToolkit.App.Controls;

/// <summary>
/// The flyout's signature control: a full-width filled bar. Click or drag anywhere to set the value; arrows step by 1,
/// Page Up/Down by 10, the mouse wheel by 2.
/// </summary>
[TemplatePart(Name = FillPartName, Type = typeof(FrameworkElement))]
public sealed class BrightnessBand : RangeBase
{
    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(BrightnessBand), new PropertyMetadata(""));

    public static readonly DependencyProperty IsPendingProperty =
        DependencyProperty.Register(nameof(IsPending), typeof(bool), typeof(BrightnessBand));

    public static readonly DependencyProperty HasFailedProperty =
        DependencyProperty.Register(nameof(HasFailed), typeof(bool), typeof(BrightnessBand));

    private const string FillPartName = "PART_Fill";

    /// <summary>The fill never gets narrower than this, so the glyph always sits on the accent color.</summary>
    private const double MinimumFillWidth = 40;

    private const double WheelStep = 2;

    private FrameworkElement? _fill;

    static BrightnessBand()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BrightnessBand), new FrameworkPropertyMetadata(typeof(BrightnessBand)));
        FocusableProperty.OverrideMetadata(typeof(BrightnessBand), new FrameworkPropertyMetadata(true));
        SmallChangeProperty.OverrideMetadata(typeof(BrightnessBand), new FrameworkPropertyMetadata(1.0));
        LargeChangeProperty.OverrideMetadata(typeof(BrightnessBand), new FrameworkPropertyMetadata(10.0));
    }

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public bool IsPending
    {
        get => (bool)GetValue(IsPendingProperty);
        set => SetValue(IsPendingProperty, value);
    }

    public bool HasFailed
    {
        get => (bool)GetValue(HasFailedProperty);
        set => SetValue(HasFailedProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _fill = GetTemplateChild(FillPartName) as FrameworkElement;
        UpdateFill();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new BrightnessBandAutomationPeer(this);

    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);
        UpdateFill();
    }

    protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
    {
        base.OnMaximumChanged(oldMaximum, newMaximum);
        UpdateFill();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateFill();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        CaptureMouse();
        SetValueFromPointer(e);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (IsMouseCaptured)
        {
            SetValueFromPointer(e);
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        ReleaseMouseCapture();
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        Step(Math.Sign(e.Delta) * WheelStep);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        e.Handled = true;
        switch (e.Key)
        {
            case Key.Right or Key.Up:
                Step(SmallChange);
                break;
            case Key.Left or Key.Down:
                Step(-SmallChange);
                break;
            case Key.PageUp:
                Step(LargeChange);
                break;
            case Key.PageDown:
                Step(-LargeChange);
                break;
            case Key.Home:
                Value = Minimum;
                break;
            case Key.End:
                Value = Maximum;
                break;
            default:
                e.Handled = false;
                break;
        }
    }

    private void Step(double delta) => Value = Math.Clamp(Math.Round(Value + delta), Minimum, Maximum);

    private void SetValueFromPointer(MouseEventArgs e)
    {
        if (ActualWidth <= 0)
        {
            return;
        }
        var fraction = Math.Clamp(e.GetPosition(this).X / ActualWidth, 0, 1);
        Value = Math.Round(Minimum + (fraction * (Maximum - Minimum)));
    }

    private void UpdateFill()
    {
        if (_fill is null || ActualWidth <= 0)
        {
            return;
        }
        var range = Maximum - Minimum;
        var fraction = range > 0 ? (Value - Minimum) / range : 0;
        _fill.Width = Math.Max(MinimumFillWidth, fraction * ActualWidth);
    }

    private sealed class BrightnessBandAutomationPeer(BrightnessBand owner) : RangeBaseAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BrightnessBand);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;

        protected override string GetHelpTextCore() => owner.HasFailed
            ? "Didn't change. The monitor didn't respond."
            : owner.IsPending ? "Sending to monitor" : base.GetHelpTextCore();
    }
}
