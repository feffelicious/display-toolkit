using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DisplayToolkit.Automation.Profiles;

namespace DisplayToolkit.App.Controls;

/// <summary>
/// Shows a shortcut as keycaps; click (or Enter) to record a new one. While recording, the next key pressed with
/// Ctrl, Alt or Win (or a function key on its own) becomes the shortcut. Esc cancels, Backspace removes the shortcut.
/// The result goes to <see cref="Command"/>: a <see cref="Shortcut"/>, or null to remove it.
/// </summary>
public sealed class ShortcutRecorder : Control
{
    public static readonly DependencyProperty KeysProperty =
        DependencyProperty.Register(nameof(Keys), typeof(IEnumerable<string>), typeof(ShortcutRecorder));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ShortcutRecorder));

    private static readonly DependencyPropertyKey IsRecordingPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(IsRecording), typeof(bool), typeof(ShortcutRecorder), new PropertyMetadata(false));

    public static readonly DependencyProperty IsRecordingProperty = IsRecordingPropertyKey.DependencyProperty;

    static ShortcutRecorder()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ShortcutRecorder), new FrameworkPropertyMetadata(typeof(ShortcutRecorder)));
        FocusableProperty.OverrideMetadata(typeof(ShortcutRecorder), new FrameworkPropertyMetadata(true));
    }

    /// <summary>The current shortcut's keys (["Ctrl", "Alt", "4"]), or empty for none.</summary>
    public IEnumerable<string>? Keys
    {
        get => (IEnumerable<string>?)GetValue(KeysProperty);
        set => SetValue(KeysProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public bool IsRecording
    {
        get => (bool)GetValue(IsRecordingProperty);
        private set => SetValue(IsRecordingPropertyKey, value);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        Focus();
        IsRecording = true;
        e.Handled = true;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (!IsRecording)
        {
            if (key is Key.Enter or Key.Space)
            {
                IsRecording = true;
                e.Handled = true;
            }
            return;
        }

        e.Handled = true;
        switch (key)
        {
            case Key.Escape:
                IsRecording = false;
                return;
            case Key.Back or Key.Delete:
                Record(null);
                return;
            case Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift
                or Key.LWin or Key.RWin or Key.Tab:
                // Waiting for the key that goes with the modifiers. Tab stays reserved for moving focus.
                e.Handled = key != Key.Tab;
                return;
        }

        var modifiers = ShortcutModifiers.None;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            modifiers |= ShortcutModifiers.Control;
        }
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            modifiers |= ShortcutModifiers.Alt;
        }
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            modifiers |= ShortcutModifiers.Shift;
        }
        if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
        {
            modifiers |= ShortcutModifiers.Windows;
        }

        // Plain letters would fire while typing anywhere: they need Ctrl, Alt or Win. Function keys may stand alone.
        var isFunctionKey = key is >= Key.F1 and <= Key.F24;
        if ((modifiers & ~ShortcutModifiers.Shift) == ShortcutModifiers.None && !isFunctionKey)
        {
            return;
        }
        Record(new Shortcut(modifiers, KeyInterop.VirtualKeyFromKey(key)));
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        IsRecording = false;
    }

    private void Record(Shortcut? shortcut)
    {
        IsRecording = false;
        if (Command?.CanExecute(shortcut) == true)
        {
            Command.Execute(shortcut);
        }
    }
}
