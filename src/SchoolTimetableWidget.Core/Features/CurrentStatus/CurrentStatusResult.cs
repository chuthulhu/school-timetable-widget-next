using SchoolTimetableWidget.Core.Features.Periods;

namespace SchoolTimetableWidget.Core.Features.CurrentStatus;

/// <summary>Immutable school status facts. Named factories preserve each kind's field invariants.</summary>
public sealed class CurrentStatusResult
{
    private CurrentStatusResult(
        CurrentStatusKind kind,
        int? currentPeriodNumber,
        int? nextPeriodNumber,
        TimeOnly? transitionTime)
    {
        Kind = kind;
        CurrentPeriodNumber = currentPeriodNumber;
        NextPeriodNumber = nextPeriodNumber;
        TransitionTime = transitionTime;
    }

    public CurrentStatusKind Kind { get; }

    public int? CurrentPeriodNumber { get; }

    public int? NextPeriodNumber { get; }

    /// <summary>The scheduled local school time of the next state transition, not a duration.</summary>
    public TimeOnly? TransitionTime { get; }

    public static CurrentStatusResult BeforeFirstPeriod(PeriodDefinition firstPeriod)
    {
        ArgumentNullException.ThrowIfNull(firstPeriod);
        return new(CurrentStatusKind.BeforeFirstPeriod, null, firstPeriod.PeriodNumber, firstPeriod.Start);
    }

    public static CurrentStatusResult InPeriod(PeriodDefinition currentPeriod)
    {
        ArgumentNullException.ThrowIfNull(currentPeriod);
        return new(CurrentStatusKind.InPeriod, currentPeriod.PeriodNumber, null, currentPeriod.End);
    }

    public static CurrentStatusResult Break(PeriodDefinition nextPeriod)
    {
        ArgumentNullException.ThrowIfNull(nextPeriod);
        return new(CurrentStatusKind.Break, null, nextPeriod.PeriodNumber, nextPeriod.Start);
    }

    public static CurrentStatusResult AfterLastPeriod() =>
        new(CurrentStatusKind.AfterLastPeriod, null, null, null);

    public static CurrentStatusResult Weekend() =>
        new(CurrentStatusKind.Weekend, null, null, null);
}
