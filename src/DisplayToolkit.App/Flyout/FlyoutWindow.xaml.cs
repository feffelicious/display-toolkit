using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DisplayToolkit.App.Native;
using DisplayToolkit.App.Services;
using DisplayToolkit.App.ViewModels;
using DisplayToolkit.App.ViewModels.Tiles;
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

    /// <summary>Pointer travel before a press on a tile becomes a drag, in DIPs.</summary>
    private const double DragThreshold = 4;

    private readonly FlyoutViewModel _viewModel;
    private nint _hwnd;

    // Edit-mode drag state.
    private TileViewModel? _dragTile;
    private ContentPresenter? _dragContainer;
    private Point _dragStart;
    private bool _isDragging;

    public FlyoutWindow(FlyoutViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        TileGrid.PreviewMouseLeftButtonDown += OnTilePointerDown;
        TileGrid.PreviewMouseMove += OnTilePointerMove;
        TileGrid.PreviewMouseLeftButtonUp += (_, _) => EndDrag();
        TileGrid.LostMouseCapture += (_, _) => EndDrag();
        UpdateState();
    }

    /// <summary>When the flyout last closed. Used to ignore the tray click that caused the close.</summary>
    public DateTime LastHiddenAt { get; private set; }

    /// <param name="focusBand">Put keyboard focus on the brightness band (when opened with a shortcut).</param>
    public void ShowFlyout(bool focusBand = false)
    {
        _hwnd = new WindowInteropHelper(this).EnsureHandle();
        ApplyBackdrop();
        Reposition();
        Show();
        Activate();
        User32.SetForegroundWindow(_hwnd);
        AnimateOpen();
        if (focusBand)
        {
            Dispatcher.BeginInvoke(Band.FocusFromKeyboard, System.Windows.Threading.DispatcherPriority.Input);
        }
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
        if (_viewModel.IsEditing && _viewModel.Page is null && HandleEditKey(e))
        {
            e.Handled = true;
            return;
        }

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

    /// <summary>
    /// Edit mode keyboard: Delete unpins the focused tile, Ctrl+Left/Right moves it, Enter finishes, Esc cancels.
    /// </summary>
    private bool HandleEditKey(KeyEventArgs e)
    {
        var focused = (Keyboard.FocusedElement as FrameworkElement)?.DataContext as TileViewModel;
        var index = focused is null ? -1 : _viewModel.Tiles.IndexOf(focused);

        switch (e.Key)
        {
            case Key.Escape:
                _viewModel.CancelEditCommand.Execute(null);
                return true;
            case Key.Enter:
                _viewModel.DoneCommand.Execute(null);
                return true;
            case Key.Delete when focused is not null:
                focused.RemoveCommand.Execute(null);
                return true;
            case Key.Left or Key.Right when index >= 0 && Keyboard.Modifiers == ModifierKeys.Control:
                _viewModel.MoveTile(index, index + (e.Key == Key.Left ? -1 : 1));
                return true;
            default:
                return false;
        }
    }

    private void OnTilePointerDown(object sender, MouseButtonEventArgs e)
    {
        // The unpin badge is the only live button on a tile in edit mode; let its click through.
        if (!_viewModel.IsEditing || IsInsideButton(e.OriginalSource as DependencyObject))
        {
            return;
        }
        if (ItemsControl.ContainerFromElement(TileGrid, (DependencyObject)e.OriginalSource) is not ContentPresenter container)
        {
            return;
        }

        _dragTile = container.Content as TileViewModel;
        _dragContainer = container;
        _dragStart = e.GetPosition(TileGrid);
        TileGrid.CaptureMouse();
        e.Handled = true;
    }

    private void OnTilePointerMove(object sender, MouseEventArgs e)
    {
        if (_dragTile is null || _dragContainer is null)
        {
            return;
        }

        var position = e.GetPosition(TileGrid);
        if (!_isDragging)
        {
            if ((position - _dragStart).Length < DragThreshold)
            {
                return;
            }
            _isDragging = true;
            Panel.SetZIndex(_dragContainer, 1);
            _dragContainer.Opacity = 0.85;
            _dragContainer.RenderTransform = new ScaleTransform(1.04, 1.04);
        }

        _viewModel.MoveTile(_viewModel.Tiles.IndexOf(_dragTile), TileIndexAt(position));
    }

    private void EndDrag()
    {
        if (_dragContainer is not null)
        {
            Panel.SetZIndex(_dragContainer, 0);
            _dragContainer.Opacity = 1;
            _dragContainer.RenderTransform = Transform.Identity;
        }
        _dragTile = null;
        _dragContainer = null;
        _isDragging = false;
        if (TileGrid.IsMouseCaptured)
        {
            TileGrid.ReleaseMouseCapture();
        }
    }

    /// <summary>Which grid slot a point falls in. Every tile is shown in edit mode, so slots map 1:1 to the list.</summary>
    private int TileIndexAt(Point position)
    {
        const int columns = 3;
        var count = _viewModel.Tiles.Count;
        var rows = Math.Max(1, (count + columns - 1) / columns);
        var column = Math.Clamp((int)(position.X / (TileGrid.ActualWidth / columns)), 0, columns - 1);
        var row = Math.Clamp((int)(position.Y / (TileGrid.ActualHeight / rows)), 0, rows - 1);
        return Math.Min((row * columns) + column, count - 1);
    }

    private static bool IsInsideButton(DependencyObject? element)
    {
        for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is Button)
            {
                return true;
            }
            if (current is ContentPresenter { Content: TileViewModel })
            {
                return false;
            }
        }
        return false;
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
        Footer.Visibility = state == MonitorDiscoveryState.Ready ? Visibility.Visible : Visibility.Collapsed;
        EmptyTitle.Text = state == MonitorDiscoveryState.NoMonitors ? "No monitor found" : "Can't reach the monitor";
    }

    private static bool IsDarkTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is 0;
    }
}
