using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Semesters;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Core.Features.DataInterchange;

/// <summary>Portable Base components only, independent of the receiving semester.</summary>
public sealed class TimetableDataPackage
{
    public TimetableDataPackage(WeeklyTimetable? timetable, PeriodSchedule? schedule)
    {
        if (timetable is null && schedule is null) throw new ArgumentException("시간표 또는 일과 시각이 필요합니다.");
        Timetable = timetable;
        Schedule = schedule;
    }
    public WeeklyTimetable? Timetable { get; }
    public PeriodSchedule? Schedule { get; }

    public SemesterSet ApplyTo(SemesterSet target, bool useTimetable, bool useSchedule)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!useTimetable && !useSchedule) throw new ArgumentException("가져올 항목을 선택해 주세요.");
        if (useTimetable && Timetable is null || useSchedule && Schedule is null)
            throw new ArgumentException("선택한 항목이 가져올 데이터에 없습니다.");
        return new(target.SemesterId, target.DisplayName, useTimetable ? Timetable! : target.Timetable,
            useSchedule ? Schedule! : target.Schedule, target.Overrides);
    }
}
