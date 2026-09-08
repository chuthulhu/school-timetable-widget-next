using SchoolTimetableWidget.Core.Time;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Time;

/// <summary>Reads the PC wall clock without networking or system clock mutation.</summary>
internal sealed class PcFallbackApplicationClock : IApplicationClock
{
    public ApplicationTimeSnapshot GetSnapshot()
    {
        // This adapter is the only production boundary allowed to read system time.
        var localTime = DateTimeOffset.Now;
        return new ApplicationTimeSnapshot(localTime, ApplicationTimeSource.PcLocalFallback, revision: 0);
    }
}
