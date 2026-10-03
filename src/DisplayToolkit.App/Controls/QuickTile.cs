using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DisplayToolkit.App.ViewModels.Tiles;

namespace DisplayToolkit.App.Controls;

/// <summary>
/// A quick-settings tile: a 48 px button with a caption underneath. <see cref="Kind"/> decides the layout: a single
/// toggle, a toggle plus a chevron (split), or a button that shows the current value (picker).
/// </summary>
public sealed class QuickTile : Control
{
    public static readonly DependencyProperty KindProperty = Register(nameof(Kind), TileKind.Toggle);
    public static readonly DependencyProperty GlyphProperty = Register(nameof(Glyph), string.Empty);
    public static readonly DependencyProperty LabelProperty = Register(nameof(Label), string.Empty);
    public static readonly DependencyProperty IsOnProperty = Register(nameof(IsOn), false);
    public static readonly DependencyProperty IsPendingProperty = Register(nameof(IsPending), false);
    public static readonly DependencyProperty HasFailedProperty = Register(nameof(HasFailed), false);
    public static readonly DependencyProperty CommandProperty = Register<ICommand?>(nameof(Command), null);
    public static readonly DependencyProperty SecondaryCommandProperty = Register<ICommand?>(nameof(SecondaryCommand), null);
    public static readonly DependencyProperty IsEditingProperty = Register(nameof(IsEditing), false);
    public static readonly DependencyProperty RemoveCommandProperty = Register<ICommand?>(nameof(RemoveCommand), null);

    static QuickTile()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(QuickTile), new FrameworkPropertyMetadata(typeof(QuickTile)));
        FocusableProperty.OverrideMetadata(typeof(QuickTile), new FrameworkPropertyMetadata(false));
    }

    public TileKind Kind
    {
        get => (TileKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <summary>A Segoe Fluent Icons glyph, or <c>"HDR"</c> for the outlined text glyph.</summary>
    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
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

    /// <summary>The main area: toggle for toggle/split tiles, open the page for pickers.</summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>The chevron of a split tile.</summary>
    public ICommand? SecondaryCommand
    {
        get => (ICommand?)GetValue(SecondaryCommandProperty);
        set => SetValue(SecondaryCommandProperty, value);
    }

    /// <summary>Edit mode: the tile shows an unpin badge and its own buttons stop reacting, so it can be dragged.</summary>
    public bool IsEditing
    {
        get => (bool)GetValue(IsEditingProperty);
        set => SetValue(IsEditingProperty, value);
    }

    /// <summary>The unpin badge in edit mode.</summary>
    public ICommand? RemoveCommand
    {
        get => (ICommand?)GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }

    private static DependencyProperty Register<T>(string name, T defaultValue) =>
        DependencyProperty.Register(name, typeof(T), typeof(QuickTile), new PropertyMetadata(defaultValue));
}
