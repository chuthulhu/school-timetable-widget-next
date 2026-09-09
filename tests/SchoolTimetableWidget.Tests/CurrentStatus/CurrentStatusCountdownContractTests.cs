using SchoolTimetableWidget.Core.Features.CurrentStatus;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Tests.Time;

namespace SchoolTimetableWidget.Tests.CurrentStatus;

public class CurrentStatusCountdownContractTests
{
    [Theory]
    [InlineData(8, 42, 11, CurrentStatusKind.BeforeFirstPeriod, 0, 17, false)]
    [InlineData(9, 23, 18, CurrentStatusKind.InPeriod, 0, 26, false)]
    [InlineData(9, 49, 59, CurrentStatusKind.InPeriod, 0, 0, true)]
    [InlineData(9, 50, 0, CurrentStatusKind.Break, 0, 10, false)]
    [InlineData(9, 54, 7, CurrentStatusKind.Break, 0, 5, false)]
    [InlineData(12, 50, 0, CurrentStatusKind.Break, 1, 10, false)]
    [InlineData(13, 59, 0, CurrentStatusKind.Break, 0, 1, false)]
    [InlineData(13, 59, 1, CurrentStatusKind.Break, 0, 0, true)]
    public void ApprovedExamplesUseTheResolvedTransition(
        int hour, int minute, int second, CurrentStatusKind kind,
        int hours, int minutes, bool lessThanMinute)
    {
        var snapshot = Snapshot(new TimeOnly(hour, minute, second));
        var status = CurrentStatusResolver.Resolve(snapshot, DefaultPeriodSchedule.Periods);

        Assert.Equal(kind, status.Kind);
        AssertValue(CurrentStatusCountdownCalculator.Calculate(snapshot, status), hours, minutes, lessThanMinute);
    }

    [Theory]
    [InlineData(1L, 0, 0, true)]
    [InlineData(TimeSpan.TicksPerMinute - 1, 0, 0, true)]
    [InlineData(TimeSpan.TicksPerMinute, 0, 1, false)]
    [InlineData(2 * TimeSpan.TicksPerMinute - 1, 0, 1, false)]
    [InlineData(60 * TimeSpan.TicksPerMinute - TimeSpan.TicksPerSecond, 0, 59, false)]
    [InlineData(60 * TimeSpan.TicksPerMinute - 1, 0, 59, false)]
    [InlineData(60 * TimeSpan.TicksPerMinute, 1, 0, false)]
    [InlineData(60 * TimeSpan.TicksPerMinute + 59 * TimeSpan.TicksPerSecond, 1, 0, false)]
    [InlineData(61 * TimeSpan.TicksPerMinute, 1, 1, false)]
    [InlineData(120 * TimeSpan.TicksPerMinute, 2, 0, false)]
    public void PositiveDurationsFloorWholeMinutesAtTickPrecision(
        long remainingTicks, int hours, int minutes, bool lessThanMinute)
    {
        // A non-default, subsecond endpoint exercises both operands without string parsing.
        var start = new TimeOnly(7, 0).Add(TimeSpan.FromTicks(1234567));
        PeriodDefinition[] schedule = [new(3, start, start.Add(TimeSpan.FromTicks(remainingTicks)))];
        var snapshot = Snapshot(start);
        var status = CurrentStatusResolver.Resolve(snapshot, schedule);

        AssertValue(CurrentStatusCountdownCalculator.Calculate(snapshot, status), hours, minutes, lessThanMinute);
    }

    [Theory]
    [InlineData(7, 16, 50, CurrentStatusKind.AfterLastPeriod)]
    [InlineData(7, 23, 59, CurrentStatusKind.AfterLastPeriod)]
    [InlineData(12, 8, 30, CurrentStatusKind.Weekend)]
    [InlineData(13, 9, 20, CurrentStatusKind.Weekend)]
    public void FinishedDayAndWeekendHaveNoCountdown(int day, int hour, int minute, CurrentStatusKind kind)
    {
        var snapshot = Snapshot(new TimeOnly(hour, minute), day);
        var status = CurrentStatusResolver.Resolve(snapshot, DefaultPeriodSchedule.Periods);

        Assert.Equal(kind, status.Kind);
        Assert.Null(CurrentStatusCountdownCalculator.Calculate(snapshot, status));
    }

    [Theory]
    [InlineData(9)]
    [InlineData(0)]
    [InlineData(-7)]
    public void SourceRevisionAndOffsetDoNotChangeLocalCountdownMeaning(int offsetHours)
    {
        foreach (var day in new[] { 7, 12 })
        {
            foreach (var time in new[] { new TimeOnly(8, 42, 11), new TimeOnly(9, 23, 18),
                new TimeOnly(9, 49, 59), new TimeOnly(12, 50), new TimeOnly(16, 50) })
            {
                var fallback = Snapshot(time, day, offsetHours: offsetHours);
                var synchronized = Snapshot(time, day, ApplicationTimeSource.SynchronizedStandardTime, revision: 42);
                var laterRevision = Snapshot(time, day, ApplicationTimeSource.SynchronizedStandardTime, revision: 99);

                Assert.Equal(ResolveFacts(fallback), ResolveFacts(synchronized));
                Assert.Equal(ResolveFacts(synchronized), ResolveFacts(laterRevision));
            }
        }
    }

    [Theory]
    [InlineData(CurrentStatusKind.BeforeFirstPeriod)]
    [InlineData(CurrentStatusKind.InPeriod)]
    [InlineData(CurrentStatusKind.Break)]
    public void StaleStatusIsRejectedAtAndAfterTransitionWithoutMidnightWrap(CurrentStatusKind kind)
    {
        var originalTime = kind switch
        {
            CurrentStatusKind.BeforeFirstPeriod => new TimeOnly(8, 30),
            CurrentStatusKind.InPeriod => new TimeOnly(9, 20),
            _ => new TimeOnly(9, 55),
        };
        var original = Snapshot(originalTime);
        var status = CurrentStatusResolver.Resolve(original, DefaultPeriodSchedule.Periods);
        Assert.Equal(kind, status.Kind);
        var transition = status.TransitionTime!.Value;

        foreach (var time in new[] { transition, transition.Add(TimeSpan.FromTicks(1)), TimeOnly.MaxValue })
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                CurrentStatusCountdownCalculator.Calculate(Snapshot(time), status));
            Assert.Equal("status", exception.ParamName);
        }
    }

    [Fact]
    public void EveryDefaultTransitionUsesNewStatusWithoutAnIntermediateZero()
    {
        var schedule = DefaultPeriodSchedule.Periods;
        foreach (var period in schedule)
        {
            var beforeStart = Snapshot(period.Start.Add(TimeSpan.FromTicks(-1)));
            var waiting = CurrentStatusResolver.Resolve(beforeStart, schedule);
            Assert.Equal(period.PeriodNumber == 1 ? CurrentStatusKind.BeforeFirstPeriod : CurrentStatusKind.Break, waiting.Kind);
            Assert.Equal(period.PeriodNumber, waiting.NextPeriodNumber);
            AssertValue(CurrentStatusCountdownCalculator.Calculate(beforeStart, waiting), 0, 0, true);

            var atStart = Snapshot(period.Start);
            var started = CurrentStatusResolver.Resolve(atStart, schedule);
            Assert.Equal(CurrentStatusKind.InPeriod, started.Kind);
            Assert.Equal(period.PeriodNumber, started.CurrentPeriodNumber);
            AssertValue(CurrentStatusCountdownCalculator.Calculate(atStart, started), 0, 50, false);

            var beforeEnd = Snapshot(period.End.Add(TimeSpan.FromTicks(-1)));
            var ending = CurrentStatusResolver.Resolve(beforeEnd, schedule);
            Assert.Equal(CurrentStatusKind.InPeriod, ending.Kind);
            AssertValue(CurrentStatusCountdownCalculator.Calculate(beforeEnd, ending), 0, 0, true);

            var atEnd = Snapshot(period.End);
            var ended = CurrentStatusResolver.Resolve(atEnd, schedule);
            var countdown = CurrentStatusCountdownCalculator.Calculate(atEnd, ended);
            if (period.PeriodNumber == 7)
            {
                Assert.Equal(CurrentStatusKind.AfterLastPeriod, ended.Kind);
                Assert.Null(countdown);
            }
            else
            {
                Assert.Equal(CurrentStatusKind.Break, ended.Kind);
                Assert.Equal(period.PeriodNumber + 1, ended.NextPeriodNumber);
                AssertValue(countdown, period.PeriodNumber == 4 ? 1 : 0, 10, false);
            }
        }
    }

    [Fact]
    public void TouchingPeriodsImmediatelyCountDownToTheNewPeriodsEnd()
    {
        PeriodDefinition[] schedule =
        [
            new(1, new TimeOnly(9, 0), new TimeOnly(9, 50)),
            new(2, new TimeOnly(9, 50), new TimeOnly(10, 40)),
        ];
        var snapshot = Snapshot(new TimeOnly(9, 50));
        var status = CurrentStatusResolver.Resolve(snapshot, schedule);

        Assert.Equal(CurrentStatusKind.InPeriod, status.Kind);
        Assert.Equal(2, status.CurrentPeriodNumber);
        AssertValue(CurrentStatusCountdownCalculator.Calculate(snapshot, status), 0, 50, false);
    }

    [Fact]
    public void CapturedSnapshotIsReusedAfterClockDateAndSourceChange()
    {
        var clock = new FakeApplicationClock(Snapshot(new TimeOnly(9, 49, 59), 11));
        var captured = clock.GetSnapshot();
        var status = CurrentStatusResolver.Resolve(captured, DefaultPeriodSchedule.Periods);
        clock.CurrentSnapshot = Snapshot(new TimeOnly(9, 50), 12,
            ApplicationTimeSource.SynchronizedStandardTime, revision: 1);

        AssertValue(CurrentStatusCountdownCalculator.Calculate(captured, status), 0, 0, true);
        Assert.Equal(1, clock.ReadCount);
        Assert.Null(ResolveFacts(clock.GetSnapshot()));
    }

    [Fact]
    public void AllSameDayMinuteBoundariesPreserveResultInvariants()
    {
        PeriodDefinition[] schedule = [new(1, TimeOnly.MinValue, TimeOnly.MaxValue)];
        // Exercise both sides of every representable minute boundary, including almost 24 hours.
        for (var minute = 0; minute < 24 * 60; minute++)
        {
            foreach (var delta in new[] { -1L, 0L, 1L })
            {
                var remainingTicks = minute * TimeSpan.TicksPerMinute + delta;
                if (remainingTicks <= 0)
                {
                    continue;
                }
                var snapshot = Snapshot(new TimeOnly(TimeOnly.MaxValue.Ticks - remainingTicks));
                var status = CurrentStatusResolver.Resolve(snapshot, schedule);
                var value = CurrentStatusCountdownCalculator.Calculate(snapshot, status);
                Assert.NotNull(value);
                Assert.InRange(value.Hours, 0, 23);
                Assert.InRange(value.Minutes, 0, 59);
                if (remainingTicks < TimeSpan.TicksPerMinute)
                {
                    AssertValue(value, 0, 0, true);
                }
                else
                {
                    Assert.False(value.LessThanMinute);
                    var representedTicks = (value.Hours * 60L + value.Minutes) * TimeSpan.TicksPerMinute;
                    Assert.True(representedTicks > 0);
                    Assert.InRange(remainingTicks - representedTicks, 0, TimeSpan.TicksPerMinute - 1);
                }
            }
        }
        AssertValue(ResolveValue(Snapshot(TimeOnly.MinValue), schedule), 23, 59, false);
    }

    [Fact]
    public void ResultCannotBePubliclyConstructedOrMutatedIntoInvalidCombinations()
    {
        var type = typeof(CountdownDisplayValue);
        Assert.True(type.IsSealed);
        Assert.Empty(type.GetConstructors());
        Assert.Empty(type.GetFields());
        Assert.All(type.GetProperties(), property => Assert.Null(property.SetMethod));
    }

    [Fact]
    public void NullArgumentsAreRejectedEvenForANoCountdownStatus()
    {
        Assert.Throws<ArgumentNullException>("snapshot", () =>
            CurrentStatusCountdownCalculator.Calculate(null!, CurrentStatusResult.Weekend()));
        Assert.Throws<ArgumentNullException>("status", () =>
            CurrentStatusCountdownCalculator.Calculate(Snapshot(new TimeOnly(9, 0)), null!));
    }

    private static CountdownDisplayValue? ResolveValue(ApplicationTimeSnapshot snapshot, IEnumerable<PeriodDefinition> schedule) =>
        CurrentStatusCountdownCalculator.Calculate(snapshot, CurrentStatusResolver.Resolve(snapshot, schedule));

    private static (int, int, bool)? ResolveFacts(ApplicationTimeSnapshot snapshot)
    {
        var value = ResolveValue(snapshot, DefaultPeriodSchedule.Periods);
        return value is null ? null : (value.Hours, value.Minutes, value.LessThanMinute);
    }

    private static void AssertValue(CountdownDisplayValue? value, int hours, int minutes, bool lessThanMinute)
    {
        Assert.NotNull(value);
        Assert.Equal((hours, minutes, lessThanMinute), (value.Hours, value.Minutes, value.LessThanMinute));
    }

    private static ApplicationTimeSnapshot Snapshot(
        TimeOnly time,
        int septemberDay = 7,
        ApplicationTimeSource source = ApplicationTimeSource.PcLocalFallback,
        int offsetHours = 9,
        long revision = 0) =>
        new(new DateTimeOffset(2026, 9, septemberDay, 0, 0, 0, TimeSpan.FromHours(offsetHours))
            .AddTicks(time.Ticks), source, revision);
}
