namespace SchoolTimetableWidget.Desktop.Features.CurrentStatus;

/// <summary>Immutable, separate header texts without UI styling or layout types.</summary>
public sealed class CurrentStatusHeaderText
{
    internal CurrentStatusHeaderText(string currentDateText, string currentTimeText, string statusText)
    {
        CurrentDateText = currentDateText;
        CurrentTimeText = currentTimeText;
        StatusText = statusText;
    }

    public string CurrentDateText { get; }

    public string CurrentTimeText { get; }

    public string StatusText { get; }
}
