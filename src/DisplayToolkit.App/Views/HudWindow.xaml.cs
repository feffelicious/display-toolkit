using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DisplayToolkit.App.Native;

namespace DisplayToolkit.App.Views;

/// <summary>
/// The overlay that confirms a shortcut, such as a profile switch (design spec §4.9): centered 24 px above the
/// taskbar of the monitor under the pointer, shown for 1.5 s after the last change.
/// </summary>
internal sealed partial class HudWindow : Window
{
    private static readonly TimeSpan VisibleFor = TimeSpan.FromSeconds(1.5);
    private static readonly Duration FadeDuration = TimeSpan.FromMilliseconds(150);
    private const double TaskbarGap = 24;

    private readonly DispatcherTimer _hideTimer = new() { Interval = VisibleFor };

    public HudWindow()
    {
        InitializeComponent();
        _hideTimer.Tick += (_, _) => FadeOut();
        SourceInitialized += (_, _) => User32.MakeOverlay(new WindowInteropHelper(this).Handle);
    }

    public void Show(string glyph, string text)
    {
        Glyph.Text = glyph;
        Label.Text = text;

        if (!IsVisible)
        {
            Pill.Opacity = 0;
            base.Show();
            Reposition();
        }
        Pill.BeginAnimation(OpacityProperty, new DoubleAnimation(1, FadeDuration));
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    private void FadeOut()
    {
        _hideTimer.Stop();
        var fade = new DoubleAnimation(0, FadeDuration);
        fade.Completed += (_, _) =>
        {
            if (!_hideTimer.IsEnabled)
            {
                Hide();
            }
        };
        Pill.BeginAnimation(OpacityProperty, fade);
    }

    private void Reposition()
    {
        User32.GetCursorPos(out var cursor);
        var (_, work, scale) = User32.GetMonitorAt(cursor);
        var width = (int)Math.Round(Width * scale);
        var height = (int)Math.Round(Height * scale);

        // The window is taller than the pill to leave room for its shadow; the pill sits in the middle.
        var x = work.Left + ((work.Width - width) / 2);
        var y = work.Bottom - (int)Math.Round(TaskbarGap * scale) - ((height + (int)Math.Round(Pill.Height * scale)) / 2);

        const uint noSize = 0x0001;
        User32.SetWindowPos(new WindowInteropHelper(this).Handle, 0, x, y, 0, 0, noSize | User32.SwpNoZOrder | User32.SwpNoActivate);
    }
}
