using SchoolTimetableWidget.Core.Time;

namespace SchoolTimetableWidget.Tests.Time;

internal sealed class FakeApplicationClock(ApplicationTimeSnapshot snapshot) : IApplicationClock
{
    public ApplicationTimeSnapshot CurrentSnapshot { get; set; } = snapshot;

    public int ReadCount { get; private set; }

    public ApplicationTimeSnapshot GetSnapshot()
    {
        ReadCount++;
        return CurrentSnapshot;
    }
}
