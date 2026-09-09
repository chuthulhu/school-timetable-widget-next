using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Time;

namespace SchoolTimetableWidget.Core.Features.CurrentStatus;

/// <summary>Resolves school status from one caller-supplied snapshot and schedule.</summary>
public static class CurrentStatusResolver
{
    /// <summary>
    /// Uses the snapshot's local date/time and [Start, End) intervals. First/next/last
    /// follow Start order, regardless of input order or period numbers. A nonempty
    /// partial schedule is supported; persisted profile completeness is a separate policy.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Definitions are empty or contain null entries, duplicate numbers or overlapping
    /// intervals. Validation precedes weekend classification.
    /// </exception>
    public static CurrentStatusResult Resolve(
        ApplicationTimeSnapshot snapshot,
        IEnumerable<PeriodDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var periods = PeriodScheduleValidator.CopyAndValidate(definitions);
        if (periods.Length == 0)
        {
            throw new ArgumentException("Current status resolution requires at least one period.", nameof(definitions));
        }

        Array.Sort(periods, (left, right) => left.Start.CompareTo(right.Start));

        if (snapshot.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return CurrentStatusResult.Weekend();
        }

        var time = snapshot.TimeOfDay;
        for (var i = 0; i < periods.Length; i++)
        {
            var period = periods[i];
            if (time < period.Start)
            {
                return i == 0
                    ? CurrentStatusResult.BeforeFirstPeriod(period)
                    : CurrentStatusResult.Break(period);
            }

            if (time < period.End)
            {
                return CurrentStatusResult.InPeriod(period);
            }
        }

        return CurrentStatusResult.AfterLastPeriod();
    }
}
