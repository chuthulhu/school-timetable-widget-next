using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;

namespace SchoolTimetableWidget.Tests.PeriodScheduleEditing;

public class PeriodScheduleContractTests
{
    internal static PeriodSchedule Default() => new(DefaultPeriodSchedule.Periods);
    internal static PeriodSchedule AtOne() => new(DefaultPeriodSchedule.Periods.Select(p => p.PeriodNumber == 5
        ? new PeriodDefinition(5, new TimeOnly(13, 0), new TimeOnly(13, 50)) : p));

    [Fact]
    public void CompleteScheduleCopiesInputAndExposesReadOnlyPeriods()
    {
        var input = DefaultPeriodSchedule.Periods.ToArray();
        var schedule = new PeriodSchedule(input);
        Assert.Equal(Enumerable.Range(1, 7), schedule.Periods.Select(p => p.PeriodNumber));
        input[0] = new(1, new TimeOnly(8, 0), new TimeOnly(8, 50));
        Assert.Equal(new TimeOnly(9, 0), schedule.Periods[0].Start);
        Assert.Throws<NotSupportedException>(() => ((IList<PeriodDefinition>)schedule.Periods)[0] = input[0]);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(6)] [InlineData(8)]
    public void IncompleteOrExtraPeriodsCannotBeCommitted(int count) =>
        Assert.Throws<ArgumentException>(() => new PeriodSchedule(DefaultPeriodSchedule.Periods.Concat(DefaultPeriodSchedule.Periods).Take(count)));

    [Theory]
    [InlineData("null")] [InlineData("duplicate")] [InlineData("reordered")]
    [InlineData("overlap")] [InlineData("reversed-time")]
    public void InvalidFullSchedulesRejectWithoutReorderingCaller(string kind)
    {
        var periods = DefaultPeriodSchedule.Periods.ToArray();
        switch (kind)
        {
            case "null": periods[3] = null!; break;
            case "duplicate": periods[3] = periods[2]; break;
            case "reordered": (periods[0], periods[1]) = (periods[1], periods[0]); break;
            case "overlap": periods[1] = new(2, new TimeOnly(9, 40), new TimeOnly(10, 30)); break;
            case "reversed-time":
                periods[0] = new(1, new TimeOnly(10, 0), new TimeOnly(10, 50));
                periods[1] = new(2, new TimeOnly(9, 0), new TimeOnly(9, 50)); break;
        }
        var before = periods.ToArray();
        Assert.Throws<ArgumentException>(() => new PeriodSchedule(periods));
        Assert.Equal(before, periods);
    }

    [Theory]
    [InlineData(9, 0)] [InlineData(8, 59)]
    public void EqualOrEarlierEndCannotCreateAnInterval(int hour, int minute) =>
        Assert.Throws<ArgumentException>(() => new PeriodDefinition(1, new TimeOnly(9, 0), new TimeOnly(hour, minute)));

    [Theory]
    [InlineData(9, 50)] [InlineData(10, 0)]
    public void TouchingAndGapAreValid(int hour, int minute)
    {
        var periods = DefaultPeriodSchedule.Periods.ToArray();
        periods[1] = new(2, new TimeOnly(hour, minute), new TimeOnly(10, 40));
        Assert.Equal(new TimeOnly(hour, minute), new PeriodSchedule(periods).Periods[1].Start);
    }

    [Fact]
    public void ReplacementSwapsOneCompleteSnapshotRejectsStaleAndNewRuntimeStartsAtDefaults()
    {
        var initial = Default();
        var runtime = new RuntimePeriodSchedule(initial);
        var replacement = AtOne();
        Assert.True(runtime.TryReplace(initial, replacement));
        Assert.Same(replacement, runtime.Current);
        Assert.Equal(new TimeOnly(14, 0), initial.Periods[4].Start);
        Assert.False(runtime.TryReplace(initial, Default()));
        Assert.Same(replacement, runtime.Current);
        Assert.Equal(new TimeOnly(14, 0), new RuntimePeriodSchedule(Default()).Current.Periods[4].Start);
    }
}
