namespace SchoolTimetableWidget.Desktop.Features.CurrentStatus;

/// <summary>Immutable, separate header texts without UI styling or layout types.</summary>
public sealed class CurrentStatusHeaderText
{
    internal CurrentStatusHeaderText(string currentTimeText, string statusText)
    {
        CurrentTimeText = currentTimeText;
        StatusText = statusText;
    }

    public string CurrentTimeText { get; }

    public string StatusText { get; }
}
