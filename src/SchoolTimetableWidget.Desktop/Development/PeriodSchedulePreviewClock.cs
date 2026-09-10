using SchoolTimetableWidget.Core.Time;

namespace SchoolTimetableWidget.Desktop.Development;

/// <summary>Explicit --period-preview only; Monday 13:10 plus process-local elapsed time.</summary>
public sealed class PeriodSchedulePreviewClock : IApplicationClock
{
    private readonly TimeProvider _elapsedTime;
    private readonly long _started;

    public PeriodSchedulePreviewClock(TimeProvider? elapsedTime = null)
    {
        _elapsedTime = elapsedTime ?? TimeProvider.System;
        _started = _elapsedTime.GetTimestamp();
    }

    public ApplicationTimeSnapshot GetSnapshot() => new(
        new DateTimeOffset(2026, 9, 7, 13, 10, 0, TimeSpan.FromHours(9)) + _elapsedTime.GetElapsedTime(_started),
        ApplicationTimeSource.PcLocalFallback, 0);
}
