namespace SchoolTimetableWidget.Desktop.Features.CurrentStatus;

/// <summary>Immutable, separate header texts without UI styling or layout types.</summary>
public sealed class CurrentStatusHeaderText
{
    internal CurrentStatusHeaderText(string currentDateText, string currentTimeText, string statusText, string weekdayText = "", string amPmText = "", string time12Text = "")
    {
        CurrentDateText = currentDateText;
        CurrentTimeText = currentTimeText;
        StatusText = statusText;
        WeekdayText = weekdayText;
        AmPmText = amPmText;
        Time12Text = time12Text;
    }

    public string CurrentDateText { get; }

    public string CurrentTimeText { get; }

    public string WeekdayText { get; }
    public string AmPmText { get; }
    public string Time12Text { get; }

    public string StatusText { get; }
}
