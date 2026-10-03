using System.Windows;
using System.Windows.Controls;

namespace DisplayToolkit.App.Controls;

/// <summary>
/// A settings card that expands to show nested rows (put <see cref="SettingsCard"/>s with <c>IsNested="True"</c> in a
/// panel as the content). <see cref="HeaderContent"/> is shown on the right of the header, for example a summary or a
/// master switch.
/// </summary>
public sealed class SettingsExpander : ContentControl
{
    public static readonly DependencyProperty HeaderProperty = Register(nameof(Header), string.Empty);
    public static readonly DependencyProperty DescriptionProperty = Register<string?>(nameof(Description), null);
    public static readonly DependencyProperty HeaderIconProperty = Register<string?>(nameof(HeaderIcon), null);
    public static readonly DependencyProperty HeaderContentProperty = Register<object?>(nameof(HeaderContent), null);

    public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
        nameof(IsExpanded), typeof(bool), typeof(SettingsExpander), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    static SettingsExpander()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SettingsExpander), new FrameworkPropertyMetadata(typeof(SettingsExpander)));
        FocusableProperty.OverrideMetadata(typeof(SettingsExpander), new FrameworkPropertyMetadata(false));
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

    public string? HeaderIcon
    {
        get => (string?)GetValue(HeaderIconProperty);
        set => SetValue(HeaderIconProperty, value);
    }

    public object? HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    private static DependencyProperty Register<T>(string name, T defaultValue) =>
        DependencyProperty.Register(name, typeof(T), typeof(SettingsExpander), new PropertyMetadata(defaultValue));
}
