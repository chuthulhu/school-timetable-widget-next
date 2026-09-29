using Microsoft.Win32;
using SchoolTimetableWidget.Desktop.Infrastructure.Time;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

/// <summary>Owns the static Windows event subscription; no time-setting API is used.</summary>
internal sealed class WindowsResumeSignal : IResumeSignal
{
    private bool _disposed;
    public event EventHandler? Resumed;
    public WindowsResumeSignal() => SystemEvents.PowerModeChanged += OnPowerModeChanged;
    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (!_disposed && e.Mode == PowerModes.Resume) Resumed?.Invoke(this, EventArgs.Empty);
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
    }
}
