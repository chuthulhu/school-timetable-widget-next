using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

internal interface IWindowWorkAreaProvider
{
    double GetAvailableHeight(Window window);
    double GetTopAdjustment(Window window, double targetHeight);
}

internal sealed class MonitorWindowWorkAreaProvider : IWindowWorkAreaProvider
{
    private const uint MonitorDefaultToNearest = 2;
    public static MonitorWindowWorkAreaProvider Instance { get; } = new();

    private MonitorWindowWorkAreaProvider() { }

    public double GetAvailableHeight(Window window)
    {
        if (!TryGet(window, out _, out var workArea, out var pixelsPerDip)) return double.PositiveInfinity;
        return PixelsToDips(workArea.Bottom - workArea.Top, pixelsPerDip);
    }

    public double GetTopAdjustment(Window window, double targetHeight)
    {
        if (!TryGet(window, out var bounds, out var workArea, out var pixelsPerDip)) return 0;
        var targetPixels = Math.Min(workArea.Bottom - workArea.Top, DipsToPixelsCeiling(targetHeight, pixelsPerDip));
        var maximumTop = workArea.Bottom - targetPixels;
        var targetTop = Math.Clamp(bounds.Top, workArea.Top, maximumTop);
        return (targetTop - bounds.Top) / pixelsPerDip;
    }

    internal static double PixelsToDips(int pixels, double pixelsPerDip) => pixels / pixelsPerDip;

    internal static int DipsToPixelsCeiling(double dips, double pixelsPerDip) =>
        Math.Max(1, (int)Math.Ceiling(dips * pixelsPerDip));

    private static bool TryGet(Window window, out NativeRect bounds, out NativeRect workArea, out double pixelsPerDip)
    {
        bounds = default;
        workArea = default;
        pixelsPerDip = 1;
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero || !GetWindowRect(handle, out bounds)) return false;
        var monitor = MonitorFromWindow(handle, MonitorDefaultToNearest);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info)) return false;
        var dpi = GetDpiForWindow(handle);
        if (dpi == 0) return false;
        pixelsPerDip = dpi / 96d;
        workArea = info.WorkArea;
        return true;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr window, out NativeRect bounds);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr window);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }
}
