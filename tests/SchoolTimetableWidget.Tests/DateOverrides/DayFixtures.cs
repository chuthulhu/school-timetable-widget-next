using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Time;
namespace SchoolTimetableWidget.Tests.DateOverrides;
internal static class DayFixtures
{
    public static readonly DateOnly Monday = new(2026, 9, 7);
    public static WeeklyTimetable Week() => new(WeeklyTimetable.Empty().Cells.Select(c =>
        new TimetableCell(c.Day, c.PeriodNumber, new($"{c.Day}-{c.PeriodNumber}", "반"))));
    public static DayTimetable Day() => new(Enumerable.Range(1, 7).Select(i => new TimetableCellValue("반복", $"{i}")));
    public static PeriodSchedule Schedule() => new(DefaultPeriodSchedule.Periods);
    public static PeriodSchedule ShortSchedule()
    {
        var p = DefaultPeriodSchedule.Periods.ToArray();
        p[4] = new(5, new TimeOnly(13, 0), new TimeOnly(13, 50));
        return new(p);
    }
    public static ApplicationTimeSnapshot Time(int day = 7, int hour = 13, int minute = 10, int second = 0) => new(
        new DateTimeOffset(2026, 9, day, hour, minute, second, TimeSpan.FromHours(9)), ApplicationTimeSource.PcLocalFallback, 0);
}
