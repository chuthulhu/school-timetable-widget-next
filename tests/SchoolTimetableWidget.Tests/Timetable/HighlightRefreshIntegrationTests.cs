using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Tests.Time;

namespace SchoolTimetableWidget.Tests.Timetable;

public class HighlightRefreshIntegrationTests
{
    [Fact]
    public void EachCycleReadsOneSnapshotAndPublishesMatchingHeaderAndCurrentSlot() => HighlightTestDispatcher.Run(() =>
    {
        var clock = new SequenceClock(Snapshot(7, 9, 49, 59), Snapshot(7, 9, 50, 0), Snapshot(7, 10, 0, 0));
        var header = new CurrentStatusHeaderViewModel();
        var timetable = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, header, timetable);
        loop.Start();
        Assert.Equal(1, clock.ReadCount);
        Assert.Equal("09:49:59", header.CurrentTimeText);
        Assert.Equal("1교시 · 종료까지 1분 미만", header.StatusText);
        AssertCurrent(timetable, 0);
        loop.RefreshNow();
        Assert.Equal(2, clock.ReadCount);
        Assert.Equal("09:50:00", header.CurrentTimeText);
        Assert.Equal("쉬는시간 · 2교시까지 10분", header.StatusText);
        AssertCurrent(timetable, null);
        loop.RefreshNow();
        Assert.Equal(3, clock.ReadCount);
        Assert.Equal("10:00:00", header.CurrentTimeText);
        Assert.Equal("2교시 · 종료까지 50분", header.StatusText);
        AssertCurrent(timetable, 5);
    });

    [Fact]
    public void LargeTimeAndDateJumpsPublishOnlyThePresentSlotAndClearWeekend() => HighlightTestDispatcher.Run(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(7, 9, 10, 0));
        var header = new CurrentStatusHeaderViewModel();
        var timetable = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, header, timetable);
        loop.RefreshNow();
        var selected = new List<int>();
        for (var i = 0; i < 35; i++)
        {
            var index = i;
            timetable.Cells[i].PropertyChanged += (_, _) =>
            {
                if (timetable.Cells[index].IsCurrent) selected.Add(index);
            };
        }
        clock.CurrentSnapshot = Snapshot(11, 16, 10, 0);
        loop.RefreshNow();
        Assert.Equal("7교시 · 종료까지 40분", header.StatusText);
        AssertCurrent(timetable, 34);
        clock.CurrentSnapshot = Snapshot(12, 16, 10, 0);
        loop.RefreshNow();
        Assert.Equal("오늘은 수업이 없습니다", header.StatusText);
        AssertCurrent(timetable, null);
        clock.CurrentSnapshot = Snapshot(14, 9, 10, 0);
        loop.RefreshNow();
        AssertCurrent(timetable, 0);
        Assert.Equal(new[] { 34, 0 }, selected);
        Assert.Equal(4, clock.ReadCount);
    });

    [Fact]
    public void AdjacentPeriodsSwitchDirectlyAtExactEndUsingInjectedSchedule() => HighlightTestDispatcher.Run(() =>
    {
        PeriodDefinition[] periods = [new(7, new(9, 0), new(10, 0)), new(2, new(10, 0), new(11, 0))];
        var clock = new FakeApplicationClock(Snapshot(7, 9, 59, 59));
        var header = new CurrentStatusHeaderViewModel();
        var timetable = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        using var loop = new CurrentStatusRefreshLoop(clock, periods.Reverse(), header, timetable);
        loop.RefreshNow();
        AssertCurrent(timetable, 30);
        clock.CurrentSnapshot = Snapshot(7, 10, 0, 0);
        loop.RefreshNow();
        AssertCurrent(timetable, 5);
        Assert.Equal("2교시 · 종료까지 1시간", header.StatusText);
        Assert.Equal(2, clock.ReadCount);
    });

    [Fact]
    public void SamePeriodAcrossWeekdaysMovesByDateWithoutChangingHeaderStatusText() => HighlightTestDispatcher.Run(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(7, 9, 10, 0));
        var header = new CurrentStatusHeaderViewModel();
        var timetable = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, header, timetable);
        loop.RefreshNow();
        var text = header.StatusText;
        clock.CurrentSnapshot = Snapshot(8, 9, 10, 0);
        loop.RefreshNow();
        Assert.Equal(text, header.StatusText);
        AssertCurrent(timetable, 1);
    });

    [Theory]
    [InlineData(9, ApplicationTimeSource.PcLocalFallback)]
    [InlineData(-7, ApplicationTimeSource.PcLocalFallback)]
    [InlineData(9, ApplicationTimeSource.SynchronizedStandardTime)]
    public void SlotUsesLocalDateAndDoesNotDependOnSourceOrRevision(int offset, ApplicationTimeSource source) =>
        HighlightTestDispatcher.Run(() =>
        {
            // Monday local / Sunday UTC at +09; this custom schedule tests date ownership.
            var snapshot = new ApplicationTimeSnapshot(
                new DateTimeOffset(2026, 9, 7, 0, 10, 0, TimeSpan.FromHours(offset)), source, 42);
            var clock = new FakeApplicationClock(snapshot);
            var header = new CurrentStatusHeaderViewModel();
            var timetable = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
            using var loop = new CurrentStatusRefreshLoop(clock, [new(3, new(0, 0), new(0, 50))], header, timetable);
            loop.RefreshNow();
            AssertCurrent(timetable, 10);
            Assert.Equal("3교시 · 종료까지 40분", header.StatusText);
            Assert.Equal(1, clock.ReadCount);
        });

    [Fact]
    public void FailedCalculationLeavesBothPreviouslyPublishedViewsIntact() => HighlightTestDispatcher.Run(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(7, 9, 10, 0));
        var header = new CurrentStatusHeaderViewModel();
        var timetable = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        using (var valid = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, header, timetable))
            valid.RefreshNow();
        var previous = header.StatusText;
        using var invalid = new CurrentStatusRefreshLoop(clock, [], header, timetable);
        clock.CurrentSnapshot = Snapshot(7, 9, 50, 0);
        Assert.Throws<ArgumentException>(invalid.Start);
        Assert.Equal(previous, header.StatusText);
        AssertCurrent(timetable, 0);
        Assert.False(invalid.IsRunning);
    });

    [Fact]
    public void NewTimetableDependencyIsRequired() => HighlightTestDispatcher.Run(() =>
        Assert.Throws<ArgumentNullException>("timetableViewModel", () =>
            new CurrentStatusRefreshLoop(new FakeApplicationClock(Snapshot(7, 9, 10, 0)), [], new(), null!)));

    private static void AssertCurrent(WeeklyTimetableViewModel model, int? index)
    {
        if (index is { } selected)
            Assert.Same(model.Cells[selected], Assert.Single(model.Cells, cell => cell.IsCurrent));
        else
            Assert.DoesNotContain(model.Cells, cell => cell.IsCurrent);
    }

    private static ApplicationTimeSnapshot Snapshot(int day, int hour, int minute, int second) =>
        new(new DateTimeOffset(2026, 9, day, hour, minute, second, TimeSpan.FromHours(9)),
            ApplicationTimeSource.PcLocalFallback, 0);

    private sealed class SequenceClock(params ApplicationTimeSnapshot[] snapshots) : IApplicationClock
    {
        public int ReadCount { get; private set; }
        public ApplicationTimeSnapshot GetSnapshot() => snapshots[ReadCount++];
    }
}
