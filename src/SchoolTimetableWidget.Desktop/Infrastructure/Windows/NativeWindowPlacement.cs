using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using SchoolTimetableWidget.Desktop.Features.WindowPlacement;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

internal interface IWindowPlacementDesktop
{
    WindowWorkArea PrepareMonitor(string? hint);
    PreferredWindowBounds? Observe();
    void ApplyPosition(AppliedWindowBounds bounds);
}

/// <summary>Physical virtual-desktop coordinates never pass through WPF Left/Top conversion.</summary>
internal sealed class NativeWindowPlacement(Window window) : IWindowPlacementDesktop
{
    private nint Handle => new WindowInteropHelper(window).Handle;

    public WindowWorkArea PrepareMonitor(string? hint)
    {
        var monitors = new List<(nint Handle, MonitorInfo Info)>();
        EnumDisplayMonitors(0, 0, (nint handle, nint dc, ref NativeRect rectangle, nint data) =>
        {
            if (TryInfo(handle, out var info)) monitors.Add((handle, info));
            return true;
        }, 0);
        var chosen = WindowBoundsCalculator.SelectMonitor(monitors.Select(m => WorkArea(m.Info, 1)).ToArray(), hint);
        if (chosen is null)
        {
            var fallback = SystemParameters.WorkArea;
            var fallbackScale = VisualTreeHelper.GetDpi(window).DpiScaleX;
            return new("", fallback.Left * fallbackScale, fallback.Top * fallbackScale,
                fallback.Width * fallbackScale, fallback.Height * fallbackScale, fallbackScale, true);
        }
        var selected = monitors.First(m => m.Info.Device == chosen.Value.Key);
        // Relocate the HWND before reading its target DPI. At startup this runs before Show.
        // GetDpiForWindow is authoritative; no GetDpiForMonitor/system-DPI approximation.
        if (MonitorFromWindow(Handle, 2) != selected.Handle)
        {
            var area = selected.Info.WorkArea;
            GetWindowRect(Handle, out var current);
            Check(SetWindowPos(Handle, 0, area.Left, area.Top,
                Math.Max(1, Math.Min(current.Right - current.Left, area.Right - area.Left)),
                Math.Max(1, Math.Min(current.Bottom - current.Top, area.Bottom - area.Top)),
                0x0014)); // no activate, no z-order; keep/cap actual size while switching DPI
        }
        var dpi = GetDpiForWindow(Handle);
        return WorkArea(selected.Info, dpi > 0 ? dpi / 96d : VisualTreeHelper.GetDpi(window).DpiScaleX);
    }

    public PreferredWindowBounds? Observe()
    {
        if (window.WindowState != WindowState.Normal || !GetWindowRect(Handle, out var bounds) ||
            !TryInfo(MonitorFromWindow(Handle, 2), out var info)) return null;
        var dpi = GetDpiForWindow(Handle);
        if (dpi == 0) return null;
        var scale = dpi / 96d;
        return new((bounds.Right - bounds.Left) / scale, (bounds.Bottom - bounds.Top) / scale,
            (bounds.Left - info.WorkArea.Left) / scale, (bounds.Top - info.WorkArea.Top) / scale, info.Device);
    }

    public void ApplyPosition(AppliedWindowBounds bounds)
    {
        var dpi = GetDpiForWindow(Handle) / 96d;
        if (dpi <= 0) return;
        // Floor the extents so rounding cannot extend outside the work area.
        Check(SetWindowPos(Handle, 0, (int)Math.Round(bounds.LeftPixels), (int)Math.Round(bounds.TopPixels),
            Math.Max(1, (int)Math.Floor(bounds.Width * dpi)), Math.Max(1, (int)Math.Floor(bounds.Height * dpi)), 0x0014));
    }

    private static WindowWorkArea WorkArea(MonitorInfo info, double scale) => new(info.Device,
        info.WorkArea.Left, info.WorkArea.Top, info.WorkArea.Right - info.WorkArea.Left,
        info.WorkArea.Bottom - info.WorkArea.Top, scale, (info.Flags & 1) != 0);

    private static bool TryInfo(nint monitor, out MonitorInfo info)
    {
        info = new() { Size = Marshal.SizeOf<MonitorInfo>(), Device = "" };
        return monitor != 0 && GetMonitorInfo(monitor, ref info);
    }
    private static void Check(bool success)
    {
        if (!success) System.Diagnostics.Debug.WriteLine(new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()));
    }

    private delegate bool MonitorCallback(nint monitor, nint dc, ref NativeRect rect, nint data);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(nint dc, nint clip, MonitorCallback callback, nint data);
    [DllImport("user32.dll")] private static extern nint MonitorFromWindow(nint window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out NativeRect rect);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint window, nint insertAfter, int x, int y, int width, int height, uint flags);
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor, WorkArea;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Device;
    }
}
