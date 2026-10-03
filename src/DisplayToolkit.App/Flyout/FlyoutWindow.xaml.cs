using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DisplayToolkit.App.Native;
using DisplayToolkit.App.Services;
using DisplayToolkit.App.ViewModels;
using Microsoft.Win32;

namespace DisplayToolkit.App.Flyout;

/// <summary>
/// The quick-settings flyout above the tray. Borderless, Acrylic, closes when it loses focus. Window plumbing only;
/// everything else lives in <see cref="FlyoutViewModel"/>.
/// </summary>
internal sealed partial class FlyoutWindow : Window
{
    /// <summary>Gap between the flyout and the screen edge or taskbar, in DIPs.</summary>
    private const double EdgeMargin = 12;

    private static readonly Duration OpenDuration = TimeSpan.FromMilliseconds(200);

    private readonly FlyoutViewModel _viewModel;
    private nint _hwnd;

    public FlyoutWindow(FlyoutViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateState();
    }

    /// <summary>When the flyout last closed. Used to ignore the tray click that caused the close.</summary>
    public DateTime LastHiddenAt { get; private set; }

    public void ShowFlyout()
    {
        _hwnd = new WindowInteropHelper(this).EnsureHandle();
        ApplyBackdrop();
        Reposition();
        Show();
        Activate();
        User32.SetForegroundWindow(_hwnd);
        AnimateOpen();
        _ = _viewModel.OnOpenedAsync();
    }

    public void HideFlyout()
    {
        if (!IsVisible)
        {
            return;
        }
        Hide();
        LastHiddenAt = DateTime.UtcNow;
        _viewModel.OnClosed();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        ApplyBackdrop();
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        HideFlyout();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var goBack = e.Key == Key.Back || (e.Key == Key.Left && Keyboard.Modifiers == ModifierKeys.Alt);
        if (e.Key == Key.Escape || goBack)
        {
            if (_viewModel.Page is not null)
            {
                _viewModel.BackCommand.Execute(null);
            }
            else if (e.Key == Key.Escape)
            {
                HideFlyout();
            }
            e.Handled = true;
        }
    }

    /// <summary>Keeps the bottom edge anchored above the taskbar when the content (and so the height) changes.</summary>
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        if (IsVisible && sizeInfo.HeightChanged)
        {
            Reposition();
        }
    }

    private void ApplyBackdrop()
    {
        if (_hwnd == 0)
        {
            return;
        }
        if (HwndSource.FromHwnd(_hwnd) is { CompositionTarget: { } target })
        {
            target.BackgroundColor = Colors.Transparent;
        }
        Dwm.SetDarkMode(_hwnd, IsDarkTheme());
        Dwm.SetBackdrop(_hwnd, Dwm.Backdrop.Acrylic);
        Dwm.SetRoundCorners(_hwnd);
    }

    /// <summary>
    /// Places the flyout in the corner next to the notification area of the monitor that was clicked, on whichever
    /// side the taskbar is.
    /// </summary>
    private void Reposition()
    {
        User32.GetCursorPos(out var cursor);
        var (bounds, work, scale) = User32.GetMonitorAt(cursor);

        var height = IsVisible ? ActualHeight : MeasureHeight();
        var widthPx = (int)Math.Round(Width * scale);
        var heightPx = (int)Math.Round(height * scale);
        var marginPx = (int)Math.Round(EdgeMargin * scale);

        var taskbarAtLeft = work.Left > bounds.Left;
        var taskbarAtTop = work.Top > bounds.Top;

        var x = taskbarAtLeft ? work.Left + marginPx : work.Right - widthPx - marginPx;
        var y = taskbarAtTop ? work.Top + marginPx : work.Bottom - heightPx - marginPx;

        const uint noSize = 0x0001;
        User32.SetWindowPos(_hwnd, 0, x, y, 0, 0, noSize | User32.SwpNoZOrder | User32.SwpNoActivate);
    }

    private double MeasureHeight()
    {
        Root.Measure(new Size(Width, double.PositiveInfinity));
        return Root.DesiredSize.Height;
    }

    private void AnimateOpen()
    {
        var ease = new QuarticEase { EasingMode = EasingMode.EaseOut };
        Root.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, OpenDuration) { EasingFunction = ease });
        if (SystemParameters.ClientAreaAnimation)
        {
            RootOffset.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, new DoubleAnimation(12, 0, OpenDuration) { EasingFunction = ease });
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FlyoutViewModel.State) or nameof(FlyoutViewModel.Monitor))
        {
            UpdateState();
        }
    }

    private void UpdateState()
    {
        var state = _viewModel.Monitor is not null ? MonitorDiscoveryState.Ready : _viewModel.State;
        SearchingState.Visibility = state == MonitorDiscoveryState.Searching ? Visibility.Visible : Visibility.Collapsed;
        ReadyState.Visibility = state == MonitorDiscoveryState.Ready ? Visibility.Visible : Visibility.Collapsed;
        EmptyState.Visibility = state is MonitorDiscoveryState.NoMonitors or MonitorDiscoveryState.NotResponding
            ? Visibility.Visible
            : Visibility.Collapsed;
        EmptyTitle.Text = state == MonitorDiscoveryState.NoMonitors ? "No monitor found" : "Can't reach the monitor";
    }

    private static bool IsDarkTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is 0;
    }
}
