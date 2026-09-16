using System.Collections.Concurrent;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.Persistence;
public class WpfResourceIsolationTests
{
    [Fact]
    public void ConcurrentTestCallersSerializeCompiledXamlResourceLifetime()
    {
        var failures = new ConcurrentBag<Exception>();
        Parallel.For(0, 6, worker =>
        {
            for (var i = 0; i < 10; i++)
                try { new WeeklyTimetableViewContractTests().SyntheticLoadedEventAppliesMeasuredContentMinimumWithoutShowingWindow(); }
                catch (Exception error) { failures.Add(error); }
        });
        if (!failures.IsEmpty) throw new AggregateException(failures);
    }
}
