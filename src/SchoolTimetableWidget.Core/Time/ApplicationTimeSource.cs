namespace SchoolTimetableWidget.Core.Time;

public enum ApplicationTimeSource
{
    /// <summary>The PC wall clock, retaining its local UTC offset.</summary>
    PcLocalFallback,

    /// <summary>Synchronized standard time represented in KST (UTC+09:00).</summary>
    SynchronizedStandardTime,
}
