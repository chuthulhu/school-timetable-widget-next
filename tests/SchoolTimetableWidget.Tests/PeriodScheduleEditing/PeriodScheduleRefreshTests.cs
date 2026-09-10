using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Tests.Time;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.PeriodScheduleEditing;

public class PeriodScheduleRefreshTests
{
    [Fact]
    public void ApplyImmediatelyRefreshesOneClockAndScheduleWithoutChangingTimetableContent() => HighlightTestDispatcher.Run(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(7, 13, 10));
        var runtime = new RuntimePeriodSchedule(PeriodScheduleContractTests.Default());
        var reads = 0;
        var header = new CurrentStatusHeaderViewModel();
        var week = WeeklyTimetable.Empty().WithCellValue(SchoolDay.Monday, 5, new("물리\n ", "3-1"));
        var timetable = new WeeklyTimetableViewModel(week);
        using var loop = new CurrentStatusRefreshLoop(clock, () => { reads++; return runtime.Current.Periods; }, header, timetable);
        loop.RefreshNow();
        Assert.Equal("쉬는시간 · 5교시까지 50분", header.StatusText);
        Assert.DoesNotContain(timetable.Cells, cell => cell.IsCurrent);
        var editor = new PeriodScheduleEditor(runtime, loop.RefreshNow);
        var session = editor.CreateSession();
        session.Rows[4].StartText = "13:00";
        session.Rows[4].EndText = "13:50";
        Assert.Equal(1, reads);
        Assert.True(session.TryApply());
        Assert.Equal(2, reads);
        Assert.Equal(2, clock.ReadCount);
        Assert.Equal("5교시 · 종료까지 40분", header.StatusText);
        Assert.Equal("2026년 09월 07일", header.CurrentDateText);
        Assert.Equal("13:10:00", header.CurrentTimeText);
        Assert.Same(timetable.Cells[20], Assert.Single(timetable.Cells, cell => cell.IsCurrent));
        Assert.Same(week, timetable.CommittedTimetable);
        Assert.Equal(new TimetableCellValue("물리\n ", "3-1"), timetable.Cells[20].Value);
        Assert.False(loop.IsRunning); // Explicit refresh does not need a timer tick.
    });

    [Fact]
    public void SwitchingScheduleSourceCannotSplitHeaderAndHighlightWithinOneCycle() => HighlightTestDispatcher.Run(() =>
    {
        var first = PeriodScheduleContractTests.Default();
        var next = PeriodScheduleContractTests.AtOne();
        var reads = 0;
        var clock = new FakeApplicationClock(Snapshot(7, 13, 10));
        var header = new CurrentStatusHeaderViewModel();
        var timetable = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        using var loop = new CurrentStatusRefreshLoop(clock, () => ++reads == 1 ? first.Periods : next.Periods, header, timetable);
        loop.RefreshNow();
        Assert.Equal(1, reads); Assert.Equal(1, clock.ReadCount);
        Assert.StartsWith("쉬는시간", header.StatusText);
        Assert.DoesNotContain(timetable.Cells, c => c.IsCurrent);
        loop.RefreshNow();
        Assert.Equal(2, reads); Assert.Equal(2, clock.ReadCount);
        Assert.StartsWith("5교시", header.StatusText);
        Assert.Same(timetable.Cells[20], Assert.Single(timetable.Cells, c => c.IsCurrent));
    });

    [Theory]
    [InlineData(7, 8, 59, "1교시까지 1분", -1)]
    [InlineData(7, 9, 0, "1교시 · 종료까지 50분", 0)]
    [InlineData(7, 9, 50, "2교시 · 종료까지 50분", 5)] // Touching: no fake Break.
    [InlineData(7, 10, 40, "쉬는시간 · 3교시까지 20분", -1)]
    [InlineData(7, 13, 0, "5교시 · 종료까지 50분", 20)]
    [InlineData(7, 13, 50, "쉬는시간 · 6교시까지 1시간 10분", -1)]
    [InlineData(7, 16, 50, "오늘 수업 종료", -1)]
    [InlineData(12, 13, 10, "오늘은 수업이 없습니다", -1)]
    public void ReplacedScheduleHonorsBoundaries(int day, int hour, int minute, string text, int currentIndex) =>
        HighlightTestDispatcher.Run(() =>
        {
            var runtime = new RuntimePeriodSchedule(PeriodScheduleContractTests.Default());
            var periods = PeriodScheduleContractTests.AtOne().Periods.ToArray();
            periods[1] = new(2, new TimeOnly(9, 50), new TimeOnly(10, 40));
            Assert.True(runtime.TryReplace(runtime.Current, new PeriodSchedule(periods)));
            var clock = new FakeApplicationClock(Snapshot(day, hour, minute));
            var header = new CurrentStatusHeaderViewModel();
            var timetable = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
            using var loop = new CurrentStatusRefreshLoop(clock, () => runtime.Current.Periods, header, timetable);
            loop.RefreshNow();
            Assert.Equal(text, header.StatusText);
            if (currentIndex < 0) Assert.DoesNotContain(timetable.Cells, c => c.IsCurrent);
            else Assert.Same(timetable.Cells[currentIndex], Assert.Single(timetable.Cells, c => c.IsCurrent));
            Assert.Equal(1, clock.ReadCount);
        });

    private static ApplicationTimeSnapshot Snapshot(int day, int hour, int minute) => new(
        new DateTimeOffset(2026, 9, day, hour, minute, 0, TimeSpan.FromHours(9)), ApplicationTimeSource.PcLocalFallback, 0);
}
