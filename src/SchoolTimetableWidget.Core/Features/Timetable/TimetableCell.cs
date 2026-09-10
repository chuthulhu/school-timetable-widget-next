namespace SchoolTimetableWidget.Core.Features.Timetable;

/// <summary>One immutable slot. Empty text is valid; null is not text.</summary>
public sealed class TimetableCell
{
    public TimetableCell(SchoolDay day, int periodNumber, string content)
    {
        ValidateSlot(day, periodNumber);
        ArgumentNullException.ThrowIfNull(content);
        Day = day;
        PeriodNumber = periodNumber;
        Content = content;
    }

    public SchoolDay Day { get; }
    public int PeriodNumber { get; }
    public string Content { get; }

    internal static void ValidateSlot(SchoolDay day, int periodNumber)
    {
        if (day is < SchoolDay.Monday or > SchoolDay.Friday)
            throw new ArgumentOutOfRangeException(nameof(day));
        if (periodNumber is < 1 or > WeeklyTimetable.PeriodCount)
            throw new ArgumentOutOfRangeException(nameof(periodNumber));
    }
}
