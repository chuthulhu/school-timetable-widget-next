using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Core.Features.SchoolDays;

public sealed class DateSpecificOverride
{
    public DateSpecificOverride(DateOnly date, DayTimetable? timetable, PeriodSchedule? schedule)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            throw new ArgumentException("Weekend overrides are not supported.", nameof(date));
        if (timetable is null && schedule is null)
            throw new ArgumentException("An override must contain at least one component.");
        Date = date;
        Timetable = timetable;
        Schedule = schedule;
    }
    public DateOnly Date { get; }
    public SchoolDay Day => (SchoolDay)((int)Date.DayOfWeek - 1);
    public DayTimetable? Timetable { get; }
    public PeriodSchedule? Schedule { get; }
}
