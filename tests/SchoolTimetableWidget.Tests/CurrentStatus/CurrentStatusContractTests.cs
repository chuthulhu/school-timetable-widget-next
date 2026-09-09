using SchoolTimetableWidget.Core.Features.CurrentStatus;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Tests.Time;

namespace SchoolTimetableWidget.Tests.CurrentStatus;

public class CurrentStatusContractTests
{
    private static IReadOnlyList<PeriodDefinition> Defaults => DefaultPeriodSchedule.Periods;

    [Theory]
    [InlineData(8, 30, CurrentStatusKind.BeforeFirstPeriod, null, 1, 9, 0)]
    [InlineData(9, 0, CurrentStatusKind.InPeriod, 1, null, 9, 50)]
    [InlineData(9, 50, CurrentStatusKind.Break, null, 2, 10, 0)]
    [InlineData(10, 0, CurrentStatusKind.InPeriod, 2, null, 10, 50)]
    [InlineData(13, 0, CurrentStatusKind.Break, null, 5, 14, 0)]
    [InlineData(14, 0, CurrentStatusKind.InPeriod, 5, null, 14, 50)]
    [InlineData(16, 50, CurrentStatusKind.AfterLastPeriod, null, null, null, null)]
    public void ApprovedDefaultExamplesExposeOnlyStatusFacts(
        int hour, int minute, CurrentStatusKind kind, int? current, int? next,
        int? transitionHour, int? transitionMinute)
    {
        TimeOnly? transition = transitionHour.HasValue
            ? new TimeOnly(transitionHour.Value, transitionMinute!.Value)
            : null;
        AssertStatus(Resolve(new TimeOnly(hour, minute)), kind, current, next, transition);
    }

    [Theory]
    [InlineData(1, 9)]
    [InlineData(2, 10)]
    [InlineData(3, 11)]
    [InlineData(4, 12)]
    [InlineData(5, 14)]
    [InlineData(6, 15)]
    [InlineData(7, 16)]
    public void EveryDefaultBoundaryUsesFullTickPrecision(int number, int hour)
    {
        var start = new TimeOnly(hour, 0);
        var end = new TimeOnly(hour, 50);
        AssertStatus(Resolve(start.Add(TimeSpan.FromTicks(-1))),
            number == 1 ? CurrentStatusKind.BeforeFirstPeriod : CurrentStatusKind.Break,
            null, number, start);
        AssertStatus(Resolve(start), CurrentStatusKind.InPeriod, number, null, end);
        AssertStatus(Resolve(end.Add(TimeSpan.FromTicks(-1))),
            CurrentStatusKind.InPeriod, number, null, end);
        if (number == 7)
        {
            AssertStatus(Resolve(end), CurrentStatusKind.AfterLastPeriod, null, null, null);
        }
        else
        {
            AssertStatus(Resolve(end), CurrentStatusKind.Break, null, number + 1, Defaults[number].Start);
        }
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    public void AllWeekdaysUseTheSameSchoolSchedule(int septemberDay)
    {
        AssertStatus(CurrentStatusResolver.Resolve(Snapshot(new TimeOnly(9, 20), septemberDay), Defaults),
            CurrentStatusKind.InPeriod, 1, null, new TimeOnly(9, 50));
    }

    [Theory]
    [InlineData(12)]
    [InlineData(13)]
    public void WeekendHasNoCurrentNextOrTransitionThroughoutTheDay(int septemberDay)
    {
        foreach (var time in RepresentativeTimes())
        {
            AssertStatus(CurrentStatusResolver.Resolve(Snapshot(time, septemberDay), Defaults),
                CurrentStatusKind.Weekend, null, null, null);
        }
    }

    [Fact]
    public void TouchingPeriodsImmediatelyEnterTheNextPeriod()
    {
        PeriodDefinition[] periods =
        [
            new(2, new TimeOnly(9, 50), new TimeOnly(10, 40)),
            new(1, new TimeOnly(9, 0), new TimeOnly(9, 50)),
        ];
        var boundary = new TimeOnly(9, 50);
        AssertStatus(CurrentStatusResolver.Resolve(Snapshot(boundary.Add(TimeSpan.FromTicks(-1))), periods),
            CurrentStatusKind.InPeriod, 1, null, boundary);
        AssertStatus(CurrentStatusResolver.Resolve(Snapshot(boundary), periods),
            CurrentStatusKind.InPeriod, 2, null, new TimeOnly(10, 40));
        Assert.Equal(2, CurrentPeriodResolver.Resolve(Snapshot(boundary), periods));
    }

    [Fact]
    public void InputOrderDoesNotChangeAnyStatusOrMutateTheCallerArray()
    {
        PeriodDefinition[] unordered =
            [Defaults[2], Defaults[0], Defaults[1], Defaults[6], Defaults[4], Defaults[5], Defaults[3]];
        var original = unordered.ToArray();
        foreach (var time in RepresentativeTimes())
        {
            Assert.Equal(Facts(Resolve(time)), Facts(CurrentStatusResolver.Resolve(Snapshot(time), unordered)));
        }
        Assert.Equal(original, unordered);
    }

    [Fact]
    public void PartialScheduleUsesTimeOrderRatherThanPeriodNumberOrderOrDefaults()
    {
        PeriodDefinition[] periods =
        [
            new(2, new TimeOnly(11, 0), new TimeOnly(11, 50)),
            new(4, new TimeOnly(9, 0), new TimeOnly(9, 50)),
        ];
        AssertStatus(CurrentStatusResolver.Resolve(Snapshot(new TimeOnly(8, 30)), periods),
            CurrentStatusKind.BeforeFirstPeriod, null, 4, new TimeOnly(9, 0));
        AssertStatus(CurrentStatusResolver.Resolve(Snapshot(new TimeOnly(9, 0)), periods),
            CurrentStatusKind.InPeriod, 4, null, new TimeOnly(9, 50));
        AssertStatus(CurrentStatusResolver.Resolve(Snapshot(new TimeOnly(10, 0)), periods),
            CurrentStatusKind.Break, null, 2, new TimeOnly(11, 0));
        AssertStatus(CurrentStatusResolver.Resolve(Snapshot(new TimeOnly(11, 0)), periods),
            CurrentStatusKind.InPeriod, 2, null, new TimeOnly(11, 50));
        AssertStatus(CurrentStatusResolver.Resolve(Snapshot(new TimeOnly(11, 50)), periods),
            CurrentStatusKind.AfterLastPeriod, null, null, null);
    }

    [Fact]
    public void SingleSubsecondPeriodPreservesTransitionTicksAndHasNoBreak()
    {
        var start = new TimeOnly(9, 0).Add(TimeSpan.FromTicks(1234567));
        var end = start.Add(TimeSpan.FromTicks(2));
        PeriodDefinition[] periods = [new(4, start, end)];
        AssertStatus(CurrentStatusResolver.Resolve(Snapshot(start.Add(TimeSpan.FromTicks(-1))), periods),
            CurrentStatusKind.BeforeFirstPeriod, null, 4, start);
        AssertStatus(CurrentStatusResolver.Resolve(Snapshot(start), periods),
            CurrentStatusKind.InPeriod, 4, null, end);
        AssertStatus(CurrentStatusResolver.Resolve(Snapshot(end.Add(TimeSpan.FromTicks(-1))), periods),
            CurrentStatusKind.InPeriod, 4, null, end);
        AssertStatus(CurrentStatusResolver.Resolve(Snapshot(end), periods),
            CurrentStatusKind.AfterLastPeriod, null, null, null);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(0)]
    [InlineData(-7)]
    public void SourceRevisionAndOffsetAreIndependentOfStatusForIdenticalLocalDateAndTime(int offsetHours)
    {
        foreach (var day in new[] { 7, 12, 13 })
        {
            foreach (var time in RepresentativeTimes())
            {
                var fallback = Snapshot(time, day, offsetHours: offsetHours);
                var synchronized = Snapshot(time, day, ApplicationTimeSource.SynchronizedStandardTime, revision: 42);
                var laterRevision = Snapshot(time, day, ApplicationTimeSource.SynchronizedStandardTime, revision: 99);
                Assert.Equal(Facts(CurrentStatusResolver.Resolve(fallback, Defaults)),
                    Facts(CurrentStatusResolver.Resolve(synchronized, Defaults)));
                Assert.Equal(Facts(CurrentStatusResolver.Resolve(synchronized, Defaults)),
                    Facts(CurrentStatusResolver.Resolve(laterRevision, Defaults)));
            }
        }
    }

    [Theory]
    [InlineData(7, CurrentStatusKind.InPeriod)]
    [InlineData(12, CurrentStatusKind.Weekend)]
    public void WeekdayClassificationUsesLocalDateEvenWhenUtcDateDiffers(int septemberDay, CurrentStatusKind expected)
    {
        var snapshot = Snapshot(new TimeOnly(0, 20), septemberDay);
        PeriodDefinition[] periods = [new(3, new TimeOnly(0, 0), new TimeOnly(0, 50))];
        Assert.NotEqual(snapshot.Date.DayOfWeek, snapshot.LocalTime.UtcDateTime.DayOfWeek);
        AssertStatus(CurrentStatusResolver.Resolve(snapshot, periods), expected,
            expected == CurrentStatusKind.InPeriod ? 3 : null, null,
            expected == CurrentStatusKind.InPeriod ? new TimeOnly(0, 50) : null);
    }

    [Fact]
    public void CapturedSnapshotIsReusedAcrossClockDateAndSourceChanges()
    {
        var clock = new FakeApplicationClock(Snapshot(new TimeOnly(9, 20), 11));
        var captured = clock.GetSnapshot();
        clock.CurrentSnapshot = Snapshot(new TimeOnly(9, 20), 12,
            ApplicationTimeSource.SynchronizedStandardTime, revision: 1);

        AssertStatus(CurrentStatusResolver.Resolve(captured, Defaults),
            CurrentStatusKind.InPeriod, 1, null, new TimeOnly(9, 50));
        Assert.Equal(1, CurrentPeriodResolver.Resolve(captured, Defaults));
        Assert.Equal(1, clock.ReadCount);
        AssertStatus(CurrentStatusResolver.Resolve(clock.GetSnapshot(), Defaults),
            CurrentStatusKind.Weekend, null, null, null);
        Assert.Equal(2, clock.ReadCount);
    }

    [Fact]
    public void BothResolversAgreeAcrossAllDefaultTransitionsAndWeekdays()
    {
        foreach (var day in Enumerable.Range(7, 7))
        {
            foreach (var time in RepresentativeTimes())
            {
                var snapshot = Snapshot(time, day);
                var status = CurrentStatusResolver.Resolve(snapshot, Defaults);
                Assert.Equal(CurrentPeriodResolver.Resolve(snapshot, Defaults), status.CurrentPeriodNumber);
                Assert.Equal(status.Kind == CurrentStatusKind.InPeriod, status.CurrentPeriodNumber.HasValue);
            }
        }
    }

    [Theory]
    [InlineData(7)]
    [InlineData(12)]
    public void EmptyScheduleIsRejectedBeforeWeekendClassification(int septemberDay)
    {
        var snapshot = Snapshot(new TimeOnly(9, 20), septemberDay);
        var exception = Assert.Throws<ArgumentException>(() => CurrentStatusResolver.Resolve(snapshot, []));
        Assert.Equal("definitions", exception.ParamName);
        Assert.Null(CurrentPeriodResolver.Resolve(snapshot, []));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(12)]
    public void MalformedSchedulesAreRejectedEvenOutsideTheirIntervalsAndOnWeekends(int septemberDay)
    {
        PeriodDefinition[][] invalidSchedules =
        [
            [Defaults[0], null!],
            [Defaults[0], new(1, new TimeOnly(10, 0), new TimeOnly(10, 50))],
            [Defaults[0], new(2, new TimeOnly(9, 30), new TimeOnly(10, 20))],
            [Defaults[0], new(2, new TimeOnly(9, 10), new TimeOnly(9, 40))],
            [Defaults[0], new(2, new TimeOnly(9, 0), new TimeOnly(9, 50))],
        ];
        var snapshot = Snapshot(new TimeOnly(17, 0), septemberDay);
        foreach (var periods in invalidSchedules)
        {
            Assert.Throws<ArgumentException>(() => CurrentStatusResolver.Resolve(snapshot, periods));
            Assert.Throws<ArgumentException>(() => CurrentStatusResolver.Resolve(snapshot, periods.Reverse()));
        }
    }

    [Fact]
    public void NullInputsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => CurrentStatusResolver.Resolve(null!, Defaults));
        Assert.Throws<ArgumentNullException>(() => CurrentStatusResolver.Resolve(Snapshot(new TimeOnly(9, 0)), null!));
    }

    [Fact]
    public void ScheduleIsEnumeratedOnlyOnce()
    {
        var enumerationCount = 0;
        IEnumerable<PeriodDefinition> Enumerate()
        {
            enumerationCount++;
            if (enumerationCount > 1)
            {
                throw new InvalidOperationException("Schedule must be captured once.");
            }
            yield return Defaults[1];
            yield return Defaults[0];
        }

        AssertStatus(CurrentStatusResolver.Resolve(Snapshot(new TimeOnly(9, 50)), Enumerate()),
            CurrentStatusKind.Break, null, 2, new TimeOnly(10, 0));
        Assert.Equal(1, enumerationCount);
    }

    [Fact]
    public void ResultFactoriesEnforceTheFiveKindsAndTheirFieldInvariants()
    {
        Assert.Equal(
            new[] { CurrentStatusKind.BeforeFirstPeriod, CurrentStatusKind.InPeriod, CurrentStatusKind.Break,
                CurrentStatusKind.AfterLastPeriod, CurrentStatusKind.Weekend },
            Enum.GetValues<CurrentStatusKind>());
        var period = new PeriodDefinition(3, new TimeOnly(8, 0), new TimeOnly(8, 50));
        AssertStatus(CurrentStatusResult.BeforeFirstPeriod(period),
            CurrentStatusKind.BeforeFirstPeriod, null, 3, period.Start);
        AssertStatus(CurrentStatusResult.InPeriod(period),
            CurrentStatusKind.InPeriod, 3, null, period.End);
        AssertStatus(CurrentStatusResult.Break(period),
            CurrentStatusKind.Break, null, 3, period.Start);
        AssertStatus(CurrentStatusResult.AfterLastPeriod(), CurrentStatusKind.AfterLastPeriod, null, null, null);
        AssertStatus(CurrentStatusResult.Weekend(), CurrentStatusKind.Weekend, null, null, null);
        Assert.Throws<ArgumentNullException>(() => CurrentStatusResult.BeforeFirstPeriod(null!));
        Assert.Throws<ArgumentNullException>(() => CurrentStatusResult.InPeriod(null!));
        Assert.Throws<ArgumentNullException>(() => CurrentStatusResult.Break(null!));
    }

    [Fact]
    public void ResultCannotBeConstructedOrMutatedIntoInvalidFieldCombinations()
    {
        var type = typeof(CurrentStatusResult);
        Assert.True(type.IsSealed);
        Assert.Empty(type.GetConstructors());
        Assert.Empty(type.GetFields());
        Assert.All(type.GetProperties(), property => Assert.Null(property.SetMethod));
    }

    private static IEnumerable<TimeOnly> RepresentativeTimes()
    {
        yield return TimeOnly.MinValue;
        yield return new TimeOnly(8, 30);
        foreach (var period in Defaults)
        {
            yield return period.Start.Add(TimeSpan.FromTicks(-1));
            yield return period.Start;
            yield return period.End.Add(TimeSpan.FromTicks(-1));
            yield return period.End;
        }
        yield return new TimeOnly(13, 0);
        yield return TimeOnly.MaxValue;
    }

    private static CurrentStatusResult Resolve(TimeOnly time) =>
        CurrentStatusResolver.Resolve(Snapshot(time), Defaults);

    private static (CurrentStatusKind, int?, int?, TimeOnly?) Facts(CurrentStatusResult result) =>
        (result.Kind, result.CurrentPeriodNumber, result.NextPeriodNumber, result.TransitionTime);

    private static void AssertStatus(CurrentStatusResult result, CurrentStatusKind kind,
        int? current, int? next, TimeOnly? transition) =>
        Assert.Equal((kind, current, next, transition), Facts(result));

    private static ApplicationTimeSnapshot Snapshot(
        TimeOnly time,
        int septemberDay = 7,
        ApplicationTimeSource source = ApplicationTimeSource.PcLocalFallback,
        int offsetHours = 9,
        long revision = 0) =>
        new(new DateTimeOffset(2026, 9, septemberDay, 0, 0, 0, TimeSpan.FromHours(offsetHours))
            .AddTicks(time.Ticks), source, revision);
}
