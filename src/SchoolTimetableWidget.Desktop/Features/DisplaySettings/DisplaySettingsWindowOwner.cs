using System.Windows;

namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

/// <summary>Owns the single modal display window across all settings command entry points.</summary>
internal sealed class DisplaySettingsWindowOwner(Window owner, RuntimeDisplaySettings display,
    Action<DisplaySettingsWindow>? showDialog = null)
{
    private DisplaySettingsWindow? _window;
    private readonly Action<DisplaySettingsWindow> _showDialog = showDialog ?? (window =>
    {
        window.Owner = owner;
        window.ShowDialog();
    });

    public void Open()
    {
        if (_window is { } existing)
        {
            if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal;
            existing.Activate();
            return;
        }

        var session = display.Open();
        DisplaySettingsWindow? window = null;
        try
        {
            window = new DisplaySettingsWindow(session);
            _window = window; // Publish before ShowDialog enters its nested dispatcher loop.
            window.Closed += OnClosed;
            _showDialog(window);
        }
        finally
        {
            if (window is not null) window.Closed -= OnClosed;
            if (ReferenceEquals(_window, window)) _window = null;
            session.Cancel();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (ReferenceEquals(_window, sender)) _window = null;
    }
}
