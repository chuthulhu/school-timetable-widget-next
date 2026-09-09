namespace SchoolTimetableWidget.Core.Features.CurrentStatus;

/// <summary>Immutable countdown meaning, without localized text or an exact duration.</summary>
public sealed class CountdownDisplayValue
{
    internal CountdownDisplayValue(long remainingTicks)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(remainingTicks);

        // Positive integer division floors the entire remaining duration to minutes.
        var wholeMinutes = remainingTicks / TimeSpan.TicksPerMinute;
        LessThanMinute = wholeMinutes == 0;
        Hours = (int)(wholeMinutes / 60);
        Minutes = (int)(wholeMinutes % 60);
    }

    /// <summary>Positive duration below one minute; Hours and Minutes are both zero.</summary>
    public bool LessThanMinute { get; }

    public int Hours { get; }

    /// <summary>Remainder minutes, 0-59. Zero with positive Hours means whole hours.</summary>
    public int Minutes { get; }
}
