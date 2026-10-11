using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using SchoolTimetableWidget.Desktop.Features.WindowPlacement;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

public static class WindowPlacementCommands
{
    public static RoutedUICommand Settings { get; } = new("창 설정...", nameof(Settings), typeof(WindowPlacementCommands));
    public static RoutedUICommand Reset { get; } = new("창 위치/크기 초기화", nameof(Reset), typeof(WindowPlacementCommands));
}

internal sealed class WindowPlacementController
{
    private readonly Window _window;
    private readonly IWindowPlacementDesktop _desktop;
    private HwndSource? _source;
    private bool _applying;
    private bool _closed;
    private Point? _dragOrigin;
    private DependencyObject? _dragSource;
    private WindowSettingsWindow? _settingsWindow;
    internal WindowPlacementSession Session { get; }
    internal bool IsInteracting => Session.IsInteracting;

    public WindowPlacementController(Window window, IWindowStateStore? store, IWindowPlacementDesktop? desktop = null)
    {
        _window = window;
        _desktop = desktop ?? new NativeWindowPlacement(window);
        Session = new(store);
        WindowContentMinimum.SetPlacement(window, this);
        window.Width = Session.Preferred.Width;
        window.Height = Session.Preferred.Height;
        window.SourceInitialized += SourceInitialized;
        window.StateChanged += StateChanged;
        window.Closed += Closed;
        window.Activated += Activated;
        window.PreviewMouseLeftButtonDown += PointerDown;
        window.PreviewMouseMove += PointerMove;
        window.PreviewMouseLeftButtonUp += PointerUp;
        window.LostMouseCapture += PointerCaptureLost;
        window.CommandBindings.Add(new CommandBinding(WindowPlacementCommands.Settings, (_, e) => { OpenSettings(); e.Handled = true; }));
        window.CommandBindings.Add(new CommandBinding(WindowPlacementCommands.Reset, (_, _) => Reset()));
    }

    internal bool TrySetPreferences(double width, double height, bool moveLocked)
    {
        if (!Session.TrySetPreferences(width, height, moveLocked)) return false;
        Apply(_window.MinWidth, _window.MinHeight);
        WindowContentMinimum.Refresh(_window);
        return true;
    }
    private void OpenSettings()
    {
        if (_settingsWindow is { IsVisible: true }) { _settingsWindow.Activate(); return; }
        if (!_window.IsVisible) return;
        _settingsWindow = new(this) { Owner = _window };
        try { _settingsWindow.ShowDialog(); }
        finally { _settingsWindow = null; NativeWindowPlacement.KeepBelowApplications(_window); }
    }
    private void Activated(object? sender, EventArgs e)
    { if (_window is SchoolTimetableWidget.Desktop.MainWindow) NativeWindowPlacement.KeepBelowApplications(_window); }
    private void PointerDown(object sender, MouseButtonEventArgs e)
    {
        _dragOrigin = null; _dragSource = null;
        // Let a stationary single click focus the cell and a double click open its editor.
        if (e.ClickCount != 1 || Session.Preferred.MoveLocked) return;
        var point = e.GetPosition(_window);
        if (e.OriginalSource is DependencyObject source && !Excluded(source) && ResizeHit(point) == 0)
        { _dragOrigin = point; _dragSource = source; }
    }
    private void PointerUp(object sender, MouseButtonEventArgs e) { _dragOrigin = null; _dragSource = null; }
    private void PointerCaptureLost(object sender, MouseEventArgs e) { _dragOrigin = null; _dragSource = null; }
    private void PointerMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) { _dragOrigin = null; _dragSource = null; return; }
        if (_dragOrigin is not { } start || _dragSource is null || !CanStartDrag(_dragSource, start, e.GetPosition(_window))) return;
        _dragOrigin = null; _dragSource = null;
        e.Handled = true;
        _window.DragMove(); // Existing native enter/move/exit captures one completed DIP preference.
    }
    internal bool CanStartDrag(DependencyObject source, Point start, Point current) =>
        !Session.Preferred.MoveLocked && !Session.IsInteracting && !Excluded(source) && ResizeHit(start) == 0 &&
        (Math.Abs(current.X - start.X) >= SystemParameters.MinimumHorizontalDragDistance ||
         Math.Abs(current.Y - start.Y) >= SystemParameters.MinimumVerticalDragDistance);
    private static bool Excluded(DependencyObject source)
    {
        for (DependencyObject? current = source; current is not null;)
        {
            if (current is ButtonBase or TextBoxBase or PasswordBox or Selector or Slider or ScrollBar or Thumb or MenuItem)
                return true;
            current = current is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(current)
                : current is FrameworkContentElement content ? content.Parent : LogicalTreeHelper.GetParent(current);
        }
        return false;
    }
    private int ResizeHit(Point point)
    {
        if (_window.ResizeMode is not (ResizeMode.CanResize or ResizeMode.CanResizeWithGrip) ||
            _window.WindowState != WindowState.Normal || _window.ActualWidth <= 0 || _window.ActualHeight <= 0) return 0;
        const double edge = 6; // DIPs, independent of the monitor's scale.
        var left = point.X >= 0 && point.X < edge;
        var right = point.X <= _window.ActualWidth && point.X > _window.ActualWidth - edge;
        var top = point.Y >= 0 && point.Y < edge;
        var bottom = point.Y <= _window.ActualHeight && point.Y > _window.ActualHeight - edge;
        if (top) return left ? 13 : right ? 14 : 12;
        if (bottom) return left ? 16 : right ? 17 : 15;
        return left ? 10 : right ? 11 : 0;
    }

    private void SourceInitialized(object? sender, EventArgs e)
    {
        _source = HwndSource.FromHwnd(new WindowInteropHelper(_window).Handle);
        _source?.AddHook(Message);
        Apply(0, 0);
    }

    internal void PrepareForShow()
    {
        // Refit the existing HWND before showing it after a monitor/work-area change.
        Apply(_window.MinWidth, _window.MinHeight);
        WindowContentMinimum.Refresh(_window);
    }

    internal void Reset()
    {
        Session.Reset(_desktop.Observe()?.MonitorHint);
        if (_window.WindowState != WindowState.Normal) _window.WindowState = WindowState.Normal;
        WindowContentMinimum.Refresh(_window);
    }

    private void StateChanged(object? sender, EventArgs e)
    {
        if (_window.WindowState == WindowState.Normal) WindowContentMinimum.Refresh(_window);
    }

    internal void ProcessMessage(int message, int parameter = 0)
    {
        if (_applying || _closed) return;
        switch (message)
        {
            case 0x0231: // WM_ENTERSIZEMOVE
                if (_desktop.Observe() is { } observed) Session.Begin(observed);
                break;
            case 0x0216: Session.Moving(); break; // WM_MOVING
            case 0x0214: Session.Sizing(parameter); break; // WM_SIZING
            case 0x0232: // WM_EXITSIZEMOVE: sample the actual accepted normal rectangle, not a proposed drag rect.
                Session.End(_desktop.Observe());
                WindowContentMinimum.Refresh(_window);
                break;
            case 0x02E0: // WPF applies its DPI update after the hook; the existing coalesced layout runs afterward.
            case 0x007E:
            case 0x001A:
                WindowContentMinimum.Refresh(_window);
                break;
        }
    }

    private nint Message(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (_closed) return 0;
        if (message == 0x0084 && _window.WindowStyle == WindowStyle.None) // WM_NCHITTEST
        {
            var packed = lParam.ToInt64();
            var point = _window.PointFromScreen(new Point((short)(packed & 0xffff), (short)((packed >> 16) & 0xffff)));
            var hit = ResizeHit(point);
            if (hit != 0) { handled = true; return new nint(hit); }
        }
        if (message == 0x0112 && (wParam.ToInt64() & 0xfff0) == 0xf010 && Session.Preferred.MoveLocked)
        { handled = true; return 0; } // SC_MOVE; resizing remains enabled
        if (message == 0x0046 && _window is SchoolTimetableWidget.Desktop.MainWindow)
            NativeWindowPlacement.KeepPositionBelowApplications(_window, lParam); // WM_WINDOWPOSCHANGING
        if (message == 0x000A && wParam != 0 && _window is SchoolTimetableWidget.Desktop.MainWindow)
            _window.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            { if (!_closed) NativeWindowPlacement.KeepBelowApplications(_window); })); // WM_ENABLE after a modal closes
        ProcessMessage(message, (int)wParam);
        return 0;
    }

    internal void Apply(double minimumWidth, double minimumHeight)
    {
        if (_applying || _closed || IsInteracting || _window.WindowState != WindowState.Normal) return;
        _applying = true;
        try
        {
            var area = _desktop.PrepareMonitor(Session.Preferred.MonitorHint);
            var bounds = WindowBoundsCalculator.Fit(Session.Preferred, area, minimumWidth, minimumHeight);
            // Release old limits before a smaller/larger monitor's pair is installed.
            _window.MinWidth = 0; _window.MinHeight = 0;
            _window.MaxWidth = area.Width; _window.MaxHeight = area.Height;
            _window.MinWidth = Math.Min(minimumWidth, area.Width);
            _window.MinHeight = Math.Min(minimumHeight, area.Height);
            _window.Width = bounds.Width; _window.Height = bounds.Height;
            _desktop.ApplyPosition(bounds);
        }
        finally { _applying = false; }
    }

    private void Closed(object? sender, EventArgs e)
    {
        _closed = true;
        Session.Flush(); // Retry only dirty intent, never capture applied geometry at shutdown.
        _source?.RemoveHook(Message);
        _source = null;
        _window.SourceInitialized -= SourceInitialized;
        _window.StateChanged -= StateChanged;
        _window.Closed -= Closed;
        _window.Activated -= Activated;
        _window.PreviewMouseLeftButtonDown -= PointerDown;
        _window.PreviewMouseMove -= PointerMove;
        _window.PreviewMouseLeftButtonUp -= PointerUp;
        _window.LostMouseCapture -= PointerCaptureLost;
    }
}
