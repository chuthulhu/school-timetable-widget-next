using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.Timetable;

namespace SchoolTimetableWidget.Desktop.Features.DateOverrides;

/// <summary>Date ownership adapter. Editors never consult a live clock or retarget on midnight.</summary>
public sealed class DateOverrideEditor(RuntimeDateOverrides target, Func<WeeklyTimetable> getBaseTimetable,
    Func<PeriodSchedule> getBaseSchedule, Action<DateOnly> refreshAfterApply)
{
    public DateOverrideEditSession CreateSession(DateOnly date)
    {
        var baseline = target.Get(date);
        return new(date, getBaseTimetable(), getBaseSchedule(), baseline, candidate =>
        {
            if (!target.TryReplace(date, baseline, candidate)) return false;
            refreshAfterApply(date);
            return true;
        });
    }

    public CellEditSession CreateCellSession(DateSpecificOverride displayed, int period, string slotLabel)
    {
        var baseline = displayed.Timetable ?? throw new ArgumentException("No timetable component.", nameof(displayed));
        var date = displayed.Date;
        var label = FormattableString.Invariant($"편집 대상: {date:yyyy년 MM월 dd일} 시간표\n{slotLabel}");
        return new(label, baseline[period], value =>
        {
            var current = target.Get(date);
            if (current is null || !ReferenceEquals(current.Timetable, baseline)) return false;
            var next = new DateSpecificOverride(date, baseline.WithCell(period, value), current.Schedule);
            if (!target.TryReplace(date, current, next)) return false;
            refreshAfterApply(date);
            return true;
        });
    }
}
