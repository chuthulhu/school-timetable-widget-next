using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Tests.Time;

namespace SchoolTimetableWidget.Tests.Periods;

public class CurrentPeriodContractTests
{
    private static IReadOnlyList<PeriodDefinition> Defaults => DefaultPeriodSchedule.Periods;

    [Fact]
    public void DefaultProfileHasTheSevenApprovedIntervals()
    {
        var expected = new (int Number, TimeOnly Start, TimeOnly End)[]
        {
            (1, new(9, 0), new(9, 50)),
            (2, new(10, 0), new(10, 50)),
            (3, new(11, 0), new(11, 50)),
            (4, new(12, 0), new(12, 50)),
            (5, new(14, 0), new(14, 50)),
            (6, new(15, 0), new(15, 50)),
            (7, new(16, 0), new(16, 50)),
        };

        Assert.Equal(expected, Defaults.Select(p => (p.PeriodNumber, p.Start, p.End)));
    }

    [Theory]
    [InlineData(1, 9)]
    [InlineData(2, 10)]
    [InlineData(3, 11)]
    [InlineData(4, 12)]
    [InlineData(5, 14)]
    [InlineData(6, 15)]
    [InlineData(7, 16)]
    public void EveryDefaultIntervalIncludesStartAndExcludesEndAtTickPrecision(int number, int hour)
    {
        var start = new TimeOnly(hour, 0);
        var end = new TimeOnly(hour, 50);

        Assert.Null(Resolve(start.Add(TimeSpan.FromTicks(-1))));
        Assert.Equal(number, Resolve(start));
        Assert.Equal(number, Resolve(end.Add(TimeSpan.FromTicks(-1))));
        Assert.Null(Resolve(end));
    }

    [Theory]
    [InlineData(8, 30, 0, null)]
    [InlineData(8, 59, 59, null)]
    [InlineData(9, 0, 0, 1)]
    [InlineData(9, 49, 59, 1)]
    [InlineData(9, 50, 0, null)]
    [InlineData(9, 55, 0, null)]
    [InlineData(10, 0, 0, 2)]
    [InlineData(13, 0, 0, null)]
    [InlineData(14, 0, 0, 5)]
    [InlineData(16, 50, 0, null)]
    [InlineData(17, 0, 0, null)]
    public void DefaultProfileResolvesOnlyAnActivePeriod(int hour, int minute, int second, int? expected)
    {
        Assert.Equal(expected, Resolve(new TimeOnly(hour, minute, second)));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    public void EachWeekdayAllowsCurrentPeriod(int septemberDay)
    {
        Assert.Equal(1, CurrentPeriodResolver.Resolve(Snapshot(new TimeOnly(9, 20), septemberDay), Defaults));
    }

    [Theory]
    [InlineData(12, 9, 20)]
    [InlineData(13, 14, 20)]
    public void WeekendHasNoCurrentPeriod(int septemberDay, int hour, int minute)
    {
        Assert.Null(CurrentPeriodResolver.Resolve(Snapshot(new TimeOnly(hour, minute), septemberDay), Defaults));
    }

    [Theory]
    [InlineData(9)]
    [InlineData(0)]
    [InlineData(-7)]
    public void SourceRevisionAndOffsetDoNotChangeTheResultForTheSameLocalDateAndTime(int fallbackOffset)
    {
        foreach (var time in new[] { new TimeOnly(9, 20), new TimeOnly(9, 50), new TimeOnly(14, 20) })
        {
            var fallback = Snapshot(time, source: ApplicationTimeSource.PcLocalFallback, offsetHours: fallbackOffset);
            var synchronized = Snapshot(time, source: ApplicationTimeSource.SynchronizedStandardTime, revision: 42);

            Assert.Equal(
                CurrentPeriodResolver.Resolve(fallback, Defaults),
                CurrentPeriodResolver.Resolve(synchronized, Defaults));
        }
    }

    [Fact]
    public void WeekdayUsesSnapshotLocalDateInsteadOfUtcDate()
    {
        var snapshot = Snapshot(new TimeOnly(0, 20), 7);
        PeriodDefinition[] earlyPeriod = [new(1, new TimeOnly(0, 0), new TimeOnly(0, 50))];

        Assert.Equal(DayOfWeek.Sunday, snapshot.LocalTime.UtcDateTime.DayOfWeek);
        Assert.Equal(DayOfWeek.Monday, snapshot.Date.DayOfWeek);
        Assert.Equal(1, CurrentPeriodResolver.Resolve(snapshot, earlyPeriod));
    }

    [Fact]
    public void CapturedSnapshotAloneDeterminesResultAfterClockDateAndSourceChange()
    {
        var clock = new FakeApplicationClock(Snapshot(new TimeOnly(9, 20), 11));
        var captured = clock.GetSnapshot();
        clock.CurrentSnapshot = Snapshot(new TimeOnly(9, 20), 12,
            ApplicationTimeSource.SynchronizedStandardTime, revision: 1);

        Assert.Equal(1, CurrentPeriodResolver.Resolve(captured, Defaults));
        Assert.Equal(1, CurrentPeriodResolver.Resolve(captured, Defaults));
        Assert.Equal(1, clock.ReadCount);
        Assert.Null(CurrentPeriodResolver.Resolve(clock.GetSnapshot(), Defaults));
        Assert.Equal(2, clock.ReadCount);
    }

    [Fact]
    public void CustomDefinitionsAreUsedWithoutSortingOrDefaultReplacement()
    {
        PeriodDefinition[] periods =
        [
            new(2, new TimeOnly(8, 0), new TimeOnly(8, 30)),
            new(1, new TimeOnly(7, 0), new TimeOnly(7, 30)),
        ];

        Assert.Equal(2, CurrentPeriodResolver.Resolve(Snapshot(new TimeOnly(8, 15)), periods));
        Assert.Equal(2, CurrentPeriodResolver.Resolve(Snapshot(new TimeOnly(8, 15)), periods.Reverse()));
        Assert.Null(CurrentPeriodResolver.Resolve(Snapshot(new TimeOnly(9, 20)), periods));
    }

    [Fact]
    public void TouchingIntervalsResolveTheStartingPeriodAtTheirSharedEndpoint()
    {
        PeriodDefinition[] periods =
        [
            new(2, new TimeOnly(9, 50), new TimeOnly(10, 40)),
            new(1, new TimeOnly(9, 0), new TimeOnly(9, 50)),
        ];

        Assert.Equal(1, CurrentPeriodResolver.Resolve(Snapshot(new TimeOnly(9, 50).Add(TimeSpan.FromTicks(-1))), periods));
        Assert.Equal(2, CurrentPeriodResolver.Resolve(Snapshot(new TimeOnly(9, 50)), periods));
    }

    [Fact]
    public void CustomSubsecondEndpointsRetainTickPrecision()
    {
        var start = new TimeOnly(9, 0).Add(TimeSpan.FromTicks(1234567));
        var end = start.Add(TimeSpan.FromTicks(2));
        PeriodDefinition[] periods = [new(1, start, end)];

        Assert.Null(CurrentPeriodResolver.Resolve(Snapshot(start.Add(TimeSpan.FromTicks(-1))), periods));
        Assert.Equal(1, CurrentPeriodResolver.Resolve(Snapshot(start), periods));
        Assert.Equal(1, CurrentPeriodResolver.Resolve(Snapshot(end.Add(TimeSpan.FromTicks(-1))), periods));
        Assert.Null(CurrentPeriodResolver.Resolve(Snapshot(end), periods));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void InvalidPeriodNumberIsRejected(int number)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PeriodDefinition(number, new TimeOnly(9, 0), new TimeOnly(9, 50)));
    }

    [Theory]
    [InlineData(9, 0)]
    [InlineData(8, 50)]
    public void EmptyOrReversedIntervalIsRejected(int endHour, int endMinute)
    {
        Assert.Throws<ArgumentException>(() => new PeriodDefinition(1, new TimeOnly(9, 0), new TimeOnly(endHour, endMinute)));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    public void DuplicateNumbersAreRejectedEvenOutsideClassOrOnWeekend(int septemberDay)
    {
        PeriodDefinition[] periods =
        [
            new(1, new TimeOnly(9, 0), new TimeOnly(9, 50)),
            new(1, new TimeOnly(10, 0), new TimeOnly(10, 50)),
        ];

        Assert.Throws<ArgumentException>(() => CurrentPeriodResolver.Resolve(Snapshot(new TimeOnly(17, 0), septemberDay), periods));
    }

    [Theory]
    [InlineData(9, 30, 10, 20)]
    [InlineData(9, 10, 9, 40)]
    [InlineData(9, 0, 9, 50)]
    public void OverlapsAreRejectedRegardlessOfOrderOrCurrentTime(int startHour, int startMinute, int endHour, int endMinute)
    {
        PeriodDefinition[] periods =
        [
            new(1, new TimeOnly(9, 0), new TimeOnly(9, 50)),
            new(2, new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute)),
        ];

        foreach (var snapshot in new[] { Snapshot(new TimeOnly(9, 35)), Snapshot(new TimeOnly(17, 0)), Snapshot(new TimeOnly(9, 35), 12) })
        {
            Assert.Throws<ArgumentException>(() => CurrentPeriodResolver.Resolve(snapshot, periods));
            Assert.Throws<ArgumentException>(() => CurrentPeriodResolver.Resolve(snapshot, periods.Reverse()));
        }
    }

    [Fact]
    public void NullInputsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => CurrentPeriodResolver.Resolve(null!, Defaults));
        Assert.Throws<ArgumentNullException>(() => CurrentPeriodResolver.Resolve(Snapshot(new TimeOnly(9, 20)), null!));
        Assert.Throws<ArgumentException>(() => CurrentPeriodResolver.Resolve(Snapshot(new TimeOnly(9, 20)), [Defaults[0], null!]));
    }

    [Fact]
    public void EmptyCalculationInputHasNoCurrentPeriod()
    {
        Assert.Null(CurrentPeriodResolver.Resolve(Snapshot(new TimeOnly(9, 20)), []));
    }

    private static int? Resolve(TimeOnly time) => CurrentPeriodResolver.Resolve(Snapshot(time), Defaults);

    private static ApplicationTimeSnapshot Snapshot(
        TimeOnly time,
        int septemberDay = 8,
        ApplicationTimeSource source = ApplicationTimeSource.PcLocalFallback,
        int offsetHours = 9,
        long revision = 0) =>
        new(new DateTimeOffset(2026, 9, septemberDay, 0, 0, 0, TimeSpan.FromHours(offsetHours)).AddTicks(time.Ticks), source, revision);
}
