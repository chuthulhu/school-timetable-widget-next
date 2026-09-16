namespace SchoolTimetableWidget.Desktop.Features.WindowPlacement;

/// <summary>Machine-local intent. Position is in DIPs relative to a monitor's work-area origin.</summary>
internal sealed record PreferredWindowBounds(double Width, double Height, double Left, double Top, string? MonitorHint)
{
    public static PreferredWindowBounds Default { get; } = new(800, 600, 40, 40, null);

    public bool IsValid => ValidSize(Width) && ValidSize(Height) && ValidPosition(Left) && ValidPosition(Top) &&
        (MonitorHint is null || (MonitorHint.Length is > 0 and <= 128 && !MonitorHint.Any(char.IsControl)));
    private static bool ValidSize(double value) => double.IsFinite(value) && value > 0 && value <= 32768;
    private static bool ValidPosition(double value) => double.IsFinite(value) && Math.Abs(value) <= 1000000;
}

internal readonly record struct WindowWorkArea(string Key, double LeftPixels, double TopPixels,
    double WidthPixels, double HeightPixels, double Scale, bool Primary = false)
{
    public double Width => WidthPixels / Scale;
    public double Height => HeightPixels / Scale;
}

internal readonly record struct AppliedWindowBounds(double Width, double Height, double LeftPixels, double TopPixels);

internal static class WindowBoundsCalculator
{
    public static WindowWorkArea? SelectMonitor(IReadOnlyList<WindowWorkArea> monitors, string? hint)
    {
        foreach (var monitor in monitors)
            if (string.Equals(monitor.Key, hint, StringComparison.OrdinalIgnoreCase)) return monitor;
        foreach (var monitor in monitors)
            if (monitor.Primary) return monitor;
        return monitors.Count > 0 ? monitors[0] : null;
    }
    public static AppliedWindowBounds Fit(PreferredWindowBounds preferred, WindowWorkArea monitor,
        double minimumWidth, double minimumHeight)
    {
        var width = Math.Min(Math.Max(preferred.Width, minimumWidth), monitor.Width);
        var height = Math.Min(Math.Max(preferred.Height, minimumHeight), monitor.Height);
        return new(width, height,
            monitor.LeftPixels + Math.Clamp(preferred.Left, 0, Math.Max(0, monitor.Width - width)) * monitor.Scale,
            monitor.TopPixels + Math.Clamp(preferred.Top, 0, Math.Max(0, monitor.Height - height)) * monitor.Scale);
    }
}
