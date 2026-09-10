using SchoolTimetableWidget.Core.Features.CurrentStatus;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Time;

namespace SchoolTimetableWidget.Desktop.Features.Timetable;

/// <summary>Projects already resolved status into a slot; does not read time or recalculate status.</summary>
public static class CurrentTimetableSlot
{
    public static (SchoolDay Day, int PeriodNumber)? From(
        ApplicationTimeSnapshot snapshot, CurrentStatusResult status)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(status);
        if (status.Kind != CurrentStatusKind.InPeriod) return null;
        SchoolDay? day = snapshot.Date.DayOfWeek switch
        {
            DayOfWeek.Monday => SchoolDay.Monday,
            DayOfWeek.Tuesday => SchoolDay.Tuesday,
            DayOfWeek.Wednesday => SchoolDay.Wednesday,
            DayOfWeek.Thursday => SchoolDay.Thursday,
            DayOfWeek.Friday => SchoolDay.Friday,
            DayOfWeek.Saturday or DayOfWeek.Sunday => null,
            _ => throw new ArgumentOutOfRangeException(nameof(snapshot))
        };
        return day is { } schoolDay ? (schoolDay, status.CurrentPeriodNumber!.Value) : null;
    }
}
