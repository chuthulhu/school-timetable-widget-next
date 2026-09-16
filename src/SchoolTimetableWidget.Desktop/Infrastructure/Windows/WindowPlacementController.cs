using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using SchoolTimetableWidget.Desktop.Features.WindowPlacement;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

public static class WindowPlacementCommands
{
    public static RoutedUICommand Reset { get; } = new("창 위치/크기 초기화", nameof(Reset), typeof(WindowPlacementCommands));
}

internal sealed class WindowPlacementController
{
    private readonly Window _window;
    private readonly IWindowPlacementDesktop _desktop;
    private HwndSource? _source;
    private bool _applying;
    private bool _closed;
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
        window.CommandBindings.Add(new CommandBinding(WindowPlacementCommands.Reset, (_, _) => Reset()));
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
    { ProcessMessage(message, (int)wParam); return 0; }

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
    }
}
