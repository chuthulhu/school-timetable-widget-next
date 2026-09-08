using SchoolTimetableWidget.Core.Time;

namespace SchoolTimetableWidget.Core.Features.Periods;

/// <summary>Resolves only the current period from one caller-supplied time snapshot.</summary>
public static class CurrentPeriodResolver
{
    /// <summary>
    /// Returns a period number on Monday-Friday in [Start, End), otherwise null.
    /// Definitions must have unique numbers and non-overlapping intervals. Input order
    /// is irrelevant; gaps and touching endpoints are supported. This calculation API
    /// does not validate completeness of a saved seven-period profile.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Definitions contain a null entry, duplicate number or overlapping intervals,
    /// even when the snapshot is outside those intervals or falls on a weekend.
    /// </exception>
    public static int? Resolve(ApplicationTimeSnapshot snapshot, IEnumerable<PeriodDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(definitions);

        var periods = definitions.ToArray();
        for (var i = 0; i < periods.Length; i++)
        {
            var period = periods[i];
            if (period is null)
            {
                throw new ArgumentException("Period definitions must not contain null entries.", nameof(definitions));
            }

            for (var j = 0; j < i; j++)
            {
                var other = periods[j];
                if (period.PeriodNumber == other.PeriodNumber)
                {
                    throw new ArgumentException("Period numbers must be unique.", nameof(definitions));
                }

                if (period.Start < other.End && other.Start < period.End)
                {
                    throw new ArgumentException("Current-period resolution requires non-overlapping intervals.", nameof(definitions));
                }
            }
        }

        if (snapshot.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return null;
        }

        var time = snapshot.TimeOfDay;
        foreach (var period in periods)
        {
            if (period.Start <= time && time < period.End)
            {
                return period.PeriodNumber;
            }
        }

        return null;
    }
}
