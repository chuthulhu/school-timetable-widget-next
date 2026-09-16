using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Core.Features.Timetable;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.CurrentStatus;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Tests.Time;

namespace SchoolTimetableWidget.Tests.CurrentStatus;

public class CurrentStatusHeaderRefreshContractTests
{
    [Fact]
    public void ViewModelStartsEmptyAndExposesIndependentReadOnlyTextsAndDisplay()
    {
        var model = new CurrentStatusHeaderViewModel();
        Assert.Equal(string.Empty, model.CurrentDateText);
        Assert.Equal(string.Empty, model.CurrentTimeText);
        Assert.Equal(string.Empty, model.StatusText);
        var properties = typeof(CurrentStatusHeaderViewModel).GetProperties();
        Assert.Equal(new[] { "AmPmText", "CurrentDateText", "CurrentTimeText", "Display", "StatusText", "WeekdayText" }, properties.Select(p => p.Name).Order());
        Assert.All(properties, p => Assert.False(p.SetMethod?.IsPublic ?? false));
        Assert.Throws<ArgumentNullException>("text", () => model.Apply(null!));
    }

    [Fact]
    public void EqualTextFromDifferentResultsDoesNotNotify()
    {
        var model = new CurrentStatusHeaderViewModel();
        model.Apply(TextAt(9, 49, 58));
        var changes = Changes(model);
        model.Apply(TextAt(9, 49, 58));
        Assert.Empty(changes);
    }

    [Fact]
    public void OneSecondWithinTheSameStatusOnlyNotifiesTime()
    {
        var model = new CurrentStatusHeaderViewModel();
        model.Apply(TextAt(9, 49, 58));
        var changes = Changes(model);
        model.Apply(TextAt(9, 49, 59));
        Assert.Equal(new[] { "CurrentTimeText" }, changes);
        Assert.Equal("09:49:59", model.CurrentTimeText);
        Assert.Equal("1교시 · 종료까지 1분 미만", model.StatusText);
    }

    [Fact]
    public void CrossingPeriodBoundaryNotifiesBothChangedTexts()
    {
        var model = new CurrentStatusHeaderViewModel();
        model.Apply(TextAt(9, 49, 59));
        var changes = Changes(model);
        model.Apply(TextAt(9, 50, 0));
        Assert.Equal(new[] { "CurrentTimeText", "StatusText" }, changes);
        Assert.Equal("09:50:00", model.CurrentTimeText);
        Assert.Equal("쉬는시간 · 2교시까지 10분", model.StatusText);
    }

    [Fact]
    public void StartRefreshesOnceBeforeEnablingTheOneSecondTimer() => OnDispatcher(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(9, 49, 59));
        var model = new CurrentStatusHeaderViewModel();
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, model, new WeeklyTimetableViewModel(WeeklyTimetable.Empty()));
        var timer = TimerOf(loop);
        Assert.Equal(TimeSpan.FromSeconds(1), timer.Interval);
        Assert.False(timer.IsEnabled);
        Assert.False(loop.IsRunning);
        Assert.Equal(0, clock.ReadCount);
        model.PropertyChanged += (_, _) => Assert.False(timer.IsEnabled);

        loop.Start();

        Assert.Equal(1, clock.ReadCount);
        Assert.Equal("09:49:59", model.CurrentTimeText);
        Assert.Equal("1교시 · 종료까지 1분 미만", model.StatusText);
        Assert.True(timer.IsEnabled);
        Assert.True(loop.IsRunning);
    });

    [Fact]
    public void RunningStartIsANoOpEvenIfClockHasAdvanced() => OnDispatcher(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(9, 49, 59));
        var model = new CurrentStatusHeaderViewModel();
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, model, new WeeklyTimetableViewModel(WeeklyTimetable.Empty()));
        loop.Start();
        var changes = Changes(model);
        clock.CurrentSnapshot = Snapshot(9, 50, 0);
        loop.Start();
        loop.Start();
        Assert.Equal(1, clock.ReadCount);
        Assert.Empty(changes);
        Assert.Equal("09:49:59", model.CurrentTimeText);
    });

    [Fact]
    public void EachManualRefreshUsesOneSnapshotAcrossTheBoundary() => OnDispatcher(() =>
    {
        // Any accidental second read would cross into the next period state.
        var clock = new SwitchingClock(Snapshot(9, 49, 59), Snapshot(9, 50, 0));
        var model = new CurrentStatusHeaderViewModel();
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, model, new WeeklyTimetableViewModel(WeeklyTimetable.Empty()));
        loop.RefreshNow();
        Assert.Equal(1, clock.ReadCount);
        Assert.Equal("09:49:59", model.CurrentTimeText);
        Assert.Equal("1교시 · 종료까지 1분 미만", model.StatusText);
        loop.RefreshNow();
        Assert.Equal(2, clock.ReadCount);
        Assert.Equal("09:50:00", model.CurrentTimeText);
        Assert.Equal("쉬는시간 · 2교시까지 10분", model.StatusText);
        Assert.False(loop.IsRunning);
        Assert.False(TimerOf(loop).IsEnabled);
    });

    [Fact]
    public void JumpedClockAppliesOnlyTheCurrentStateWithoutReplay() => OnDispatcher(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(9, 49, 58));
        var model = new CurrentStatusHeaderViewModel();
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, model, new WeeklyTimetableViewModel(WeeklyTimetable.Empty()));
        loop.Start();
        var times = new List<string>();
        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(model.CurrentTimeText)) times.Add(model.CurrentTimeText);
        };
        clock.CurrentSnapshot = Snapshot(9, 50, 3);
        loop.RefreshNow();
        Assert.Equal(2, clock.ReadCount);
        Assert.Equal(new[] { "09:50:03" }, times);
        Assert.Equal("쉬는시간 · 2교시까지 9분", model.StatusText);
    });

    [Fact]
    public void RepeatedStopAndRestartRefreshOnlyAtTheNewStart() => OnDispatcher(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(9, 49, 58));
        var model = new CurrentStatusHeaderViewModel();
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, model, new WeeklyTimetableViewModel(WeeklyTimetable.Empty()));
        loop.Stop();
        loop.Start();
        loop.Stop();
        loop.Stop();
        Assert.False(loop.IsRunning);
        Assert.False(TimerOf(loop).IsEnabled);
        Assert.Equal(1, clock.ReadCount);
        clock.CurrentSnapshot = Snapshot(9, 50, 3);
        loop.Start();
        Assert.True(loop.IsRunning);
        Assert.True(TimerOf(loop).IsEnabled);
        Assert.Equal(2, clock.ReadCount);
        Assert.Equal("09:50:03", model.CurrentTimeText);
        Assert.Equal("쉬는시간 · 2교시까지 9분", model.StatusText);
    });

    [Fact]
    public void ManualRefreshWhileStoppedDoesNotRestartTimer() => OnDispatcher(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(9, 49, 58));
        var model = new CurrentStatusHeaderViewModel();
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, model, new WeeklyTimetableViewModel(WeeklyTimetable.Empty()));
        loop.Start();
        loop.Stop();
        clock.CurrentSnapshot = Snapshot(17, 0, 0);
        loop.RefreshNow();
        Assert.Equal(2, clock.ReadCount);
        Assert.Equal("17:00:00", model.CurrentTimeText);
        Assert.Equal("오늘 수업 종료", model.StatusText);
        Assert.False(loop.IsRunning);
        Assert.False(TimerOf(loop).IsEnabled);
    });

    [Fact]
    public void TickHandlerRefreshesOnceAndIgnoresInactiveCallbacks() => OnDispatcher(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(9, 49, 58));
        var model = new CurrentStatusHeaderViewModel();
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, model, new WeeklyTimetableViewModel(WeeklyTimetable.Empty()));
        // Invoke our handler directly: this is object/event evidence, not native timer delivery.
        var tick = typeof(CurrentStatusRefreshLoop).GetMethod("OnTick",
            BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<EventHandler>(loop);
        tick(null, EventArgs.Empty);
        Assert.Equal(0, clock.ReadCount);
        loop.Start();
        clock.CurrentSnapshot = Snapshot(9, 50, 3);
        tick(null, EventArgs.Empty);
        Assert.Equal(2, clock.ReadCount);
        Assert.Equal("09:50:03", model.CurrentTimeText);
        Assert.Equal("쉬는시간 · 2교시까지 9분", model.StatusText);
        loop.Stop();
        tick(null, EventArgs.Empty);
        loop.Dispose();
        tick(null, EventArgs.Empty);
        Assert.Equal(2, clock.ReadCount);
    });

    [Fact]
    public void DisposeStopsTimerAndRejectsFutureRefreshOrStart() => OnDispatcher(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(9, 49, 58));
        var model = new CurrentStatusHeaderViewModel();
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, model, new WeeklyTimetableViewModel(WeeklyTimetable.Empty()));
        loop.Start();
        loop.Dispose();
        loop.Dispose();
        loop.Stop();
        Assert.False(loop.IsRunning);
        Assert.False(TimerOf(loop).IsEnabled);
        Assert.Throws<ObjectDisposedException>(loop.Start);
        Assert.Throws<ObjectDisposedException>(loop.RefreshNow);
        Assert.Equal(1, clock.ReadCount);
    });

    [Fact]
    public void InjectedScheduleIsCapturedAndUsedInsteadOfDefaults() => OnDispatcher(() =>
    {
        PeriodDefinition[] periods = [new(4, new TimeOnly(8, 0), new TimeOnly(8, 50))];
        var clock = new FakeApplicationClock(Snapshot(8, 20, 0));
        var model = new CurrentStatusHeaderViewModel();
        using var loop = new CurrentStatusRefreshLoop(clock, periods, model, new WeeklyTimetableViewModel(WeeklyTimetable.Empty()));
        periods[0] = new(2, new TimeOnly(10, 0), new TimeOnly(10, 50));
        loop.RefreshNow();
        Assert.Equal("4교시 · 종료까지 30분", model.StatusText);
        Assert.Equal(1, clock.ReadCount);
    });

    [Theory]
    [InlineData(9)]
    [InlineData(0)]
    [InlineData(-7)]
    public void SourceRevisionOffsetChangesWithSameLocalTimeDoNotNotify(int offsetHours) => OnDispatcher(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(9, 49, 59, offsetHours: offsetHours));
        var model = new CurrentStatusHeaderViewModel();
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, model, new WeeklyTimetableViewModel(WeeklyTimetable.Empty()));
        loop.RefreshNow();
        var changes = Changes(model);
        clock.CurrentSnapshot = Snapshot(9, 49, 59, source: ApplicationTimeSource.SynchronizedStandardTime, revision: 42);
        loop.RefreshNow();
        clock.CurrentSnapshot = Snapshot(9, 49, 59, source: ApplicationTimeSource.SynchronizedStandardTime, revision: 99);
        loop.RefreshNow();
        Assert.Equal(3, clock.ReadCount);
        Assert.Equal("09:49:59", model.CurrentTimeText);
        Assert.Equal("1교시 · 종료까지 1분 미만", model.StatusText);
        Assert.Empty(changes);
    });

    [Fact]
    public void FailedStartDoesNotRunTimerOrPublishPartialText() => OnDispatcher(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(9, 49, 59));
        var model = new CurrentStatusHeaderViewModel();
        model.Apply(TextAt(17, 0, 0));
        using var loop = new CurrentStatusRefreshLoop(clock, [], model, new WeeklyTimetableViewModel(WeeklyTimetable.Empty()));
        var changes = Changes(model);
        Assert.Throws<ArgumentException>(loop.Start);
        Assert.False(loop.IsRunning);
        Assert.False(TimerOf(loop).IsEnabled);
        Assert.Equal(1, clock.ReadCount);
        Assert.Empty(changes);
        Assert.Equal("17:00:00", model.CurrentTimeText);
        Assert.Equal("오늘 수업 종료", model.StatusText);
    });

    [Theory]
    [InlineData("start")]
    [InlineData("stop")]
    [InlineData("dispose")]
    public void LifecycleCalledFromInitialNotificationDoesNotDuplicateOrReviveTimer(string action) => OnDispatcher(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(9, 49, 59));
        var model = new CurrentStatusHeaderViewModel();
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, model, new WeeklyTimetableViewModel(WeeklyTimetable.Empty()));
        model.PropertyChanged += (_, _) =>
        {
            if (action == "start") loop.Start();
            else if (action == "stop") loop.Stop();
            else loop.Dispose();
        };
        loop.Start();
        Assert.Equal(1, clock.ReadCount);
        Assert.Equal(action == "start", loop.IsRunning);
        Assert.Equal(action == "start", TimerOf(loop).IsEnabled);
    });

    [Fact]
    public void OtherThreadsCannotOperateLoopOrReadClock() => OnDispatcher(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(9, 49, 59));
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, new(), new WeeklyTimetableViewModel(WeeklyTimetable.Empty()));
        Exception?[] failures = new Exception?[4];
        var other = new Thread(() =>
        {
            failures[0] = Record.Exception(loop.Start);
            failures[1] = Record.Exception(loop.RefreshNow);
            failures[2] = Record.Exception(loop.Stop);
            failures[3] = Record.Exception(loop.Dispose);
        });
        other.Start();
        other.Join();
        Assert.All(failures, e => Assert.IsType<InvalidOperationException>(e));
        Assert.Equal(0, clock.ReadCount);
        Assert.False(loop.IsRunning);
    });

    [Fact]
    public void NullDependenciesAreRejectedBeforeActivation() => OnDispatcher(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(9, 49, 59));
        Assert.Throws<ArgumentNullException>("clock", () => new CurrentStatusRefreshLoop(null!, [], new(), new WeeklyTimetableViewModel(WeeklyTimetable.Empty())));
        Assert.Throws<ArgumentNullException>("periods", () => new CurrentStatusRefreshLoop(clock, (IEnumerable<PeriodDefinition>)null!, new(), new WeeklyTimetableViewModel(WeeklyTimetable.Empty())));
        Assert.Throws<ArgumentNullException>("viewModel", () => new CurrentStatusRefreshLoop(clock, [], null!, new WeeklyTimetableViewModel(WeeklyTimetable.Empty())));
        Assert.Equal(0, clock.ReadCount);
    });

    private static List<string?> Changes(CurrentStatusHeaderViewModel model)
    {
        var changes = new List<string?>();
        model.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        return changes;
    }

    // Inspect our owned timer without exposing a mutable timer as production public API.
    private static DispatcherTimer TimerOf(CurrentStatusRefreshLoop loop) =>
        (DispatcherTimer)typeof(CurrentStatusRefreshLoop)
            .GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(loop)!;

    private static CurrentStatusHeaderText TextAt(int hour, int minute, int second)
    {
        var snapshot = Snapshot(hour, minute, second);
        var status = CurrentStatusResolver.Resolve(snapshot, DefaultPeriodSchedule.Periods);
        return CurrentStatusHeaderFormatter.Format(snapshot, status,
            CurrentStatusCountdownCalculator.Calculate(snapshot, status));
    }

    private static ApplicationTimeSnapshot Snapshot(int hour, int minute, int second,
        int offsetHours = 9, ApplicationTimeSource source = ApplicationTimeSource.PcLocalFallback, long revision = 0) =>
        new(new DateTimeOffset(2026, 9, 7, hour, minute, second, TimeSpan.FromHours(offsetHours)), source, revision);

    private sealed class SwitchingClock(ApplicationTimeSnapshot first, ApplicationTimeSnapshot next) : IApplicationClock
    {
        public int ReadCount { get; private set; }
        public ApplicationTimeSnapshot GetSnapshot() => ++ReadCount == 1 ? first : next;
    }

    private static void OnDispatcher(Action test) => SchoolTimetableWidget.Tests.Timetable.HighlightTestDispatcher.Run(test);
}
