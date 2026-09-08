namespace SchoolTimetableWidget.Core.Features.Periods;

/// <summary>An immutable, date-free period interval [Start, End) within one day.</summary>
public sealed class PeriodDefinition
{
    public PeriodDefinition(int periodNumber, TimeOnly start, TimeOnly end)
    {
        if (periodNumber is < 1 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(periodNumber));
        }

        if (start >= end)
        {
            throw new ArgumentException("A period must start before it ends within the same day.", nameof(end));
        }

        PeriodNumber = periodNumber;
        Start = start;
        End = end;
    }

    public int PeriodNumber { get; }

    public TimeOnly Start { get; }

    public TimeOnly End { get; }
}
