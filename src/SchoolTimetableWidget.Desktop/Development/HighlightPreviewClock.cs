using SchoolTimetableWidget.Core.Time;

namespace SchoolTimetableWidget.Desktop.Development;

/// <summary>Explicit --highlight-preview only: a repeating 70-second synthetic-time fixture.</summary>
public sealed class HighlightPreviewClock : IApplicationClock
{
    private readonly TimeProvider _elapsedTime;
    private readonly long _started;

    // Each stage runs for ten real seconds. Normal application schedules are unchanged.
    private static readonly DateTimeOffset[] StageStarts =
    [
        At(7, 9, 10, 0),     // Monday 1: non-empty
        At(7, 9, 49, 55),    // Monday 1 -> break at elapsed stage second 5
        At(7, 9, 59, 55),    // Break -> Monday 2 at second 5
        At(11, 9, 10, 0),    // Friday 1: empty
        At(9, 10, 10, 0),    // Wednesday 2: whitespace-only
        At(11, 16, 49, 55),  // Friday 7 -> after last at second 5
        At(12, 9, 10, 0)     // Saturday: no highlight
    ];

    public HighlightPreviewClock(TimeProvider? elapsedTime = null)
    {
        _elapsedTime = elapsedTime ?? TimeProvider.System;
        _started = _elapsedTime.GetTimestamp();
    }

    public ApplicationTimeSnapshot GetSnapshot()
    {
        var elapsed = _elapsedTime.GetElapsedTime(_started);
        var cycleTicks = elapsed.Ticks % (StageStarts.Length * 10L * TimeSpan.TicksPerSecond);
        var stage = (int)(cycleTicks / (10L * TimeSpan.TicksPerSecond));
        var withinStage = cycleTicks % (10L * TimeSpan.TicksPerSecond);
        // Source/revision are fixture metadata, not a claim of actual PC/KRISS time.
        return new(StageStarts[stage].AddTicks(withinStage), ApplicationTimeSource.PcLocalFallback, stage);
    }

    private static DateTimeOffset At(int day, int hour, int minute, int second) =>
        new(2026, 9, day, hour, minute, second, TimeSpan.FromHours(9));
}
