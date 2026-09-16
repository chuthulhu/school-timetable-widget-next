using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using SchoolTimetableWidget.Desktop.Features.TrayLifecycle;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

internal sealed class WpfWidgetWindow : IWidgetWindow
{
    private readonly Window _window;
    private readonly WindowPlacementController _placement;
    private readonly Action _refresh;
    public bool IsVisible => _window.IsVisible;
    public event Action? VisibilityChanged;
    public event Func<bool>? CloseRequested;

    public WpfWidgetWindow(Window window, WindowPlacementController placement, Action refresh)
    {
        _window = window; _placement = placement; _refresh = refresh;
        window.Closing += Closing;
        window.IsVisibleChanged += VisibleChanged;
    }
    private void Closing(object? sender, CancelEventArgs e)
    {
        if (CloseRequested?.Invoke() == true) e.Cancel = true;
    }
    private void VisibleChanged(object sender, DependencyPropertyChangedEventArgs e) => VisibilityChanged?.Invoke();

    internal static Window? FindDialog(Window owner, Func<Window, bool> isVisible)
    {
        var child = owner.OwnedWindows.Cast<Window>().LastOrDefault(isVisible);
        return child is null ? null : FindDialog(child, isVisible) ?? child;
    }

    public bool TryActivateDialog()
    {
        var dialog = FindDialog(_window, w => w.IsVisible);
        var owner = dialog ?? _window;
        var handle = new WindowInteropHelper(owner).Handle;
        // Native Open/Save/MessageBox dialogs do not appear in WPF OwnedWindows.
        if (handle != 0 && !IsWindowEnabled(handle))
        {
            var popup = GetLastActivePopup(handle);
            if (popup != 0 && IsWindowVisible(popup)) SetForegroundWindow(popup);
            return true;
        }
        if (dialog is null) return false;
        if (dialog.WindowState == WindowState.Minimized) dialog.WindowState = WindowState.Normal;
        dialog.Activate();
        return true;
    }

    public void ShowAndActivate()
    {
        _window.Dispatcher.VerifyAccess();
        if (_window.WindowState != WindowState.Normal) _window.WindowState = WindowState.Normal;
        _refresh();
        _placement.PrepareForShow();
        _window.Show();
        WindowContentMinimum.Refresh(_window);
        _window.Activate(); // Best effort; no topmost or OS focus-policy workaround.
    }
    public void Hide() => _window.Hide();
    public void Dispose()
    {
        _window.Closing -= Closing;
        _window.IsVisibleChanged -= VisibleChanged;
    }
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(nint window);
    [DllImport("user32.dll")] private static extern nint GetLastActivePopup(nint window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint window);
}
