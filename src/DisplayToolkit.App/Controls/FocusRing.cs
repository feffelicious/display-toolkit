using System.Windows;
using System.Windows.Input;

namespace DisplayToolkit.App.Controls;

/// <summary>
/// Keyboard-only focus rings, decided by the app rather than WPF. Templates draw the ring themselves and show it when
/// <see cref="IsVisibleProperty"/> is true.
/// </summary>
/// <remarks>
/// WPF's FocusVisualStyle draws in an adorner layer that lags behind animated transforms (the flyout's opening slide),
/// doesn't count a global hotkey as keyboard input, and shows after mouse clicks when Windows is set to always show
/// focus indicators. Here the rule is simple: keyboard focus shows the ring, a mouse press hides it.
/// </remarks>
public static class FocusRing
{
    /// <summary>Set to true (usually from a style) to track keyboard focus on the element.</summary>
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(FocusRing), new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyPropertyKey IsVisiblePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "IsVisible", typeof(bool), typeof(FocusRing), new PropertyMetadata(false));

    /// <summary>True while the element should show its focus ring. Read-only; bind template triggers to it.</summary>
    public static readonly DependencyProperty IsVisibleProperty = IsVisiblePropertyKey.DependencyProperty;

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsVisible(DependencyObject element) => (bool)element.GetValue(IsVisibleProperty);

    /// <summary>Focuses the element and shows its ring, as if it had been reached with the keyboard.</summary>
    public static void FocusFromKeyboard(UIElement element)
    {
        Keyboard.Focus(element);
        SetIsVisible(element, element.IsKeyboardFocused);
    }

    private static void SetIsVisible(DependencyObject element, bool value) => element.SetValue(IsVisiblePropertyKey, value);

    private static void OnIsEnabledChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not UIElement target)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            target.GotKeyboardFocus += OnGotKeyboardFocus;
            target.LostKeyboardFocus += OnLostKeyboardFocus;
            target.PreviewMouseDown += OnPreviewMouseDown;
            target.PreviewKeyDown += OnPreviewKeyDown;
        }
        else
        {
            target.GotKeyboardFocus -= OnGotKeyboardFocus;
            target.LostKeyboardFocus -= OnLostKeyboardFocus;
            target.PreviewMouseDown -= OnPreviewMouseDown;
            target.PreviewKeyDown -= OnPreviewKeyDown;
            SetIsVisible(target, false);
        }
    }

    private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) =>
        SetIsVisible((DependencyObject)sender, InputManager.Current.MostRecentInputDevice is KeyboardDevice);

    private static void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) =>
        SetIsVisible((DependencyObject)sender, false);

    private static void OnPreviewMouseDown(object sender, MouseButtonEventArgs e) =>
        SetIsVisible((DependencyObject)sender, false);

    /// <summary>Using the keyboard on a focused element (for example arrows after a click) brings the ring back.</summary>
    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is UIElement { IsKeyboardFocused: true } element)
        {
            SetIsVisible(element, true);
        }
    }
}
