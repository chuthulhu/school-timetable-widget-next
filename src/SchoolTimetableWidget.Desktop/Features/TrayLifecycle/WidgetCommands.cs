using System.Windows.Input;

namespace SchoolTimetableWidget.Desktop.Features.TrayLifecycle;

/// <summary>Widget visibility commands offered by the right-click menu (ADR 0033).</summary>
public static class WidgetCommands
{
    /// <summary>Hides the widget through the same close-to-tray path as X and Alt+F4 (ADR 0020).</summary>
    public static RoutedUICommand Hide { get; } = new("위젯 숨기기", nameof(Hide), typeof(WidgetCommands));
}
