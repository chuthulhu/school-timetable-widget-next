using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Core.Features.SchoolDays;

public sealed record EffectiveDayConfiguration
{
    internal EffectiveDayConfiguration(DateOnly date, WeeklyTimetable timetable,
        PeriodSchedule schedule, DateSpecificOverride? dateOverride)
    { Date = date; Timetable = timetable; Schedule = schedule; DateOverride = dateOverride; }
    public DateOnly Date { get; }
    public WeeklyTimetable Timetable { get; }
    public PeriodSchedule Schedule { get; }
    public DateSpecificOverride? DateOverride { get; }
}

public static class EffectiveDayResolver
{
    public static EffectiveDayConfiguration Resolve(DateOnly date, WeeklyTimetable baseTimetable,
        PeriodSchedule baseSchedule, DateSpecificOverride? dateOverride)
    {
        ArgumentNullException.ThrowIfNull(baseTimetable);
        ArgumentNullException.ThrowIfNull(baseSchedule);
        if (dateOverride is not null && dateOverride.Date != date)
            throw new ArgumentException("Override date does not match the requested date.", nameof(dateOverride));
        return new(date, ProjectTimetable(baseTimetable, dateOverride),
            dateOverride?.Schedule ?? baseSchedule, dateOverride);
    }

    public static WeeklyTimetable ProjectTimetable(WeeklyTimetable baseTimetable, DateSpecificOverride? dateOverride)
    {
        ArgumentNullException.ThrowIfNull(baseTimetable);
        if (dateOverride?.Timetable is not { } day) return baseTimetable;
        return new(baseTimetable.Cells.Select(cell => cell.Day == dateOverride.Day
            ? new TimetableCell(cell.Day, cell.PeriodNumber, day[cell.PeriodNumber]) : cell));
    }
}
