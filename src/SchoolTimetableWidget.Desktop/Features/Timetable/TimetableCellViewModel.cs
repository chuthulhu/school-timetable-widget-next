namespace SchoolTimetableWidget.Desktop.Features.Timetable;

public sealed class TimetableCellViewModel
{
    public TimetableCellViewModel(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        Content = content;
    }

    public string Content { get; }
}
