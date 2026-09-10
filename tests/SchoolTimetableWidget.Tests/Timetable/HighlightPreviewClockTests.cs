using SchoolTimetableWidget.Core.Features.CurrentStatus;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop.Development;
using SchoolTimetableWidget.Desktop.Features.Timetable;

namespace SchoolTimetableWidget.Tests.Timetable;

public class HighlightPreviewClockTests
{
    [Theory]
    [InlineData(0, 7, 9, 10, 0, CurrentStatusKind.InPeriod, 0)]
    [InlineData(14, 7, 9, 49, 59, CurrentStatusKind.InPeriod, 0)]
    [InlineData(15, 7, 9, 50, 0, CurrentStatusKind.Break, -1)]
    [InlineData(24, 7, 9, 59, 59, CurrentStatusKind.Break, -1)]
    [InlineData(25, 7, 10, 0, 0, CurrentStatusKind.InPeriod, 5)]
    [InlineData(30, 11, 9, 10, 0, CurrentStatusKind.InPeriod, 4)]
    [InlineData(40, 9, 10, 10, 0, CurrentStatusKind.InPeriod, 7)]
    [InlineData(54, 11, 16, 49, 59, CurrentStatusKind.InPeriod, 34)]
    [InlineData(55, 11, 16, 50, 0, CurrentStatusKind.AfterLastPeriod, -1)]
    [InlineData(60, 12, 9, 10, 0, CurrentStatusKind.Weekend, -1)]
    [InlineData(70, 7, 9, 10, 0, CurrentStatusKind.InPeriod, 0)]
    public void PreviewStagesUseElapsedTimeAndTheRealResolver(
        int elapsed, int day, int hour, int minute, int second, CurrentStatusKind kind, int index)
    {
        var time = new ElapsedProvider();
        var clock = new HighlightPreviewClock(time);
        time.Seconds = elapsed;
        var snapshot = clock.GetSnapshot();
        Assert.Equal(new DateTimeOffset(2026, 9, day, hour, minute, second, TimeSpan.FromHours(9)),
            snapshot.LocalTime);
        var status = CurrentStatusResolver.Resolve(snapshot, DefaultPeriodSchedule.Periods);
        Assert.Equal(kind, status.Kind);
        var slot = CurrentTimetableSlot.From(snapshot, status);
        if (index < 0) Assert.Null(slot);
        else Assert.Equal(((SchoolDay)(index % 5), index / 5 + 1), slot);
        Assert.Equal(snapshot.LocalTime, clock.GetSnapshot().LocalTime); // reading does not advance fixture time
    }

    [Fact]
    public void DelayedPreviewReadsJumpToCurrentStageWithoutTickReplay()
    {
        var time = new ElapsedProvider();
        var clock = new HighlightPreviewClock(time);
        _ = clock.GetSnapshot();
        time.Seconds = 5 * 70 + 60;
        Assert.Equal(DayOfWeek.Saturday, clock.GetSnapshot().Date.DayOfWeek);
    }

    private sealed class ElapsedProvider : TimeProvider
    {
        public long Seconds { get; set; }
        public override long TimestampFrequency => 1;
        public override long GetTimestamp() => Seconds;
        public override DateTimeOffset GetUtcNow() => throw new InvalidOperationException("No wall-clock read allowed.");
    }
}
