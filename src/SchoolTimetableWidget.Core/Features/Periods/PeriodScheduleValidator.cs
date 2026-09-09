namespace SchoolTimetableWidget.Core.Features.Periods;

/// <summary>Shared calculation preconditions, not persisted profile completeness policy.</summary>
internal static class PeriodScheduleValidator
{
    internal static PeriodDefinition[] CopyAndValidate(IEnumerable<PeriodDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        var periods = definitions.ToArray();
        for (var i = 0; i < periods.Length; i++)
        {
            var period = periods[i];
            if (period is null)
            {
                throw new ArgumentException("Period definitions must not contain null entries.", nameof(definitions));
            }

            // PeriodDefinition already guarantees a valid number and Start < End.
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

        return periods;
    }
}
