namespace SchoolTimetableWidget.Core.Time;

/// <summary>Immutable time facts from one application clock reference.</summary>
public sealed class ApplicationTimeSnapshot
{
    public ApplicationTimeSnapshot(
        DateTimeOffset localTime,
        ApplicationTimeSource source,
        long revision)
    {
        if (!Enum.IsDefined(source))
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(revision);

        if (source == ApplicationTimeSource.SynchronizedStandardTime
            && localTime.Offset != TimeSpan.FromHours(9))
        {
            throw new ArgumentException("Synchronized standard time must be represented in KST (UTC+09:00).", nameof(localTime));
        }

        LocalTime = localTime;
        Source = source;
        Revision = revision;
    }

    /// <summary>
    /// The instant and its source-local representation: PC local offset for
    /// fallback, KST for synchronized time. Never implicitly converted by consumers.
    /// </summary>
    public DateTimeOffset LocalTime { get; }

    public DateOnly Date => new(LocalTime.Year, LocalTime.Month, LocalTime.Day);

    public TimeOnly TimeOfDay => new(LocalTime.TimeOfDay.Ticks);

    public ApplicationTimeSource Source { get; }

    /// <summary>
    /// Generation of the authoritative time reference/source within this clock's
    /// lifetime. Not a tick counter or persisted profile revision. Initial fallback is 0.
    /// </summary>
    public long Revision { get; }
}
