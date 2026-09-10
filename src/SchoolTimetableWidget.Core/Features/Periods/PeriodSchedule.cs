using System.Collections.ObjectModel;

namespace SchoolTimetableWidget.Core.Features.Periods;

/// <summary>A complete user-committable schedule, in period and chronological order.</summary>
public sealed class PeriodSchedule
{
    public PeriodSchedule(IEnumerable<PeriodDefinition> periods)
    {
        ArgumentNullException.ThrowIfNull(periods);
        var copy = periods.ToArray();
        if (copy.Length != 7)
            throw new ArgumentException("A schedule must contain exactly seven periods.", nameof(periods));
        for (var i = 0; i < copy.Length; i++)
        {
            if (copy[i] is null || copy[i].PeriodNumber != i + 1)
                throw new ArgumentException("Periods must be numbered 1 through 7 in order.", nameof(periods));
            // PeriodDefinition enforces Start < End. Never sort or renumber caller input.
            if (i > 0 && copy[i - 1].End > copy[i].Start)
                throw new ArgumentException("Periods must progress chronologically without overlap.", nameof(periods));
        }
        Periods = Array.AsReadOnly(copy);
    }

    public ReadOnlyCollection<PeriodDefinition> Periods { get; }
}
