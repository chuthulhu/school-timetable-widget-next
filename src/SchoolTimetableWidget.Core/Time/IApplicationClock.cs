namespace SchoolTimetableWidget.Core.Time;

/// <summary>The shared source of time for application features.</summary>
public interface IApplicationClock
{
    /// <summary>
    /// Captures time and its reference identity together. Read once per related
    /// state calculation and pass that snapshot to every participating calculation.
    /// </summary>
    ApplicationTimeSnapshot GetSnapshot();
}
