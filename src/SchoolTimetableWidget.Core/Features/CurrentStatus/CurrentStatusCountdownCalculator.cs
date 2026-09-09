using SchoolTimetableWidget.Core.Time;

namespace SchoolTimetableWidget.Core.Features.CurrentStatus;

/// <summary>Normalizes time until the supplied status transition using A7 display semantics.</summary>
public static class CurrentStatusCountdownCalculator
{
    /// <summary>
    /// The caller must pass the same snapshot used to resolve status. Times belong to
    /// one local school day; no midnight wrap-around or status/schedule re-resolution.
    /// Returns null for AfterLastPeriod and Weekend, which have no transition.
    /// At an exact transition, resolve the new status before calling this method.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Status has a transition at or before the snapshot time: stale/inconsistent input,
    /// not a zero countdown. Full date/schedule provenance remains the caller's responsibility.
    /// </exception>
    public static CountdownDisplayValue? Calculate(
        ApplicationTimeSnapshot snapshot,
        CurrentStatusResult status)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(status);

        if (status.TransitionTime is not { } transition)
        {
            return null;
        }

        // Subtract ticks directly: TimeOnly subtraction would wrap a negative gap overnight.
        var remainingTicks = transition.Ticks - snapshot.TimeOfDay.Ticks;
        if (remainingTicks <= 0)
        {
            throw new ArgumentException("Status transition must be after the time in the same snapshot.", nameof(status));
        }

        return new CountdownDisplayValue(remainingTicks);
    }
}
