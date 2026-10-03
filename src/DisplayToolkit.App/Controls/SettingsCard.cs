using System.Windows;
using System.Windows.Controls.Primitives;

namespace DisplayToolkit.App.Controls;

/// <summary>
/// A row in a settings page, as in Windows 11 Settings: icon, title and description on the left, the control
/// (<see cref="System.Windows.Controls.ContentControl.Content"/>) on the right. With <see cref="IsClickEnabled"/> the
/// whole card is a button with a chevron (navigation cards).
/// </summary>
public sealed class SettingsCard : ButtonBase
{
    public static readonly DependencyProperty HeaderProperty = Register(nameof(Header), string.Empty);
    public static readonly DependencyProperty DescriptionProperty = Register<string?>(nameof(Description), null);
    public static readonly DependencyProperty HeaderIconProperty = Register<string?>(nameof(HeaderIcon), null);
    public static readonly DependencyProperty IsClickEnabledProperty = Register(nameof(IsClickEnabled), false);

    /// <summary>Drawn as a row inside a <see cref="SettingsExpander"/> rather than a standalone card.</summary>
    public static readonly DependencyProperty IsNestedProperty = Register(nameof(IsNested), false);

    static SettingsCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SettingsCard), new FrameworkPropertyMetadata(typeof(SettingsCard)));
        FocusableProperty.OverrideMetadata(typeof(SettingsCard), new FrameworkPropertyMetadata(false));
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>A Segoe Fluent Icons glyph.</summary>
    public string? HeaderIcon
    {
        get => (string?)GetValue(HeaderIconProperty);
        set => SetValue(HeaderIconProperty, value);
    }

    public bool IsClickEnabled
    {
        get => (bool)GetValue(IsClickEnabledProperty);
        set => SetValue(IsClickEnabledProperty, value);
    }

    public bool IsNested
    {
        get => (bool)GetValue(IsNestedProperty);
        set => SetValue(IsNestedProperty, value);
    }

    /// <summary>Only navigation cards behave as buttons; other cards just host their control.</summary>
    protected override void OnClick()
    {
        if (IsClickEnabled)
        {
            base.OnClick();
        }
    }

    private static DependencyProperty Register<T>(string name, T defaultValue) =>
        DependencyProperty.Register(name, typeof(T), typeof(SettingsCard), new PropertyMetadata(defaultValue));
}
