using SchoolTimetableWidget.Core.Features.SchoolDays;

namespace SchoolTimetableWidget.Desktop.Features.DateOverrides;

/// <summary>Owned UI-dispatcher runtime state. No persistence, public mutable map or global singleton.</summary>
public sealed class RuntimeDateOverrides
{
    private readonly Dictionary<DateOnly, DateSpecificOverride> _entries = new();
    public DateSpecificOverride? Get(DateOnly date) => _entries.GetValueOrDefault(date);

    public bool TryReplace(DateOnly date, DateSpecificOverride? expected, DateSpecificOverride? candidate)
    {
        if (candidate is not null && candidate.Date != date)
            throw new ArgumentException("Candidate belongs to a different date.", nameof(candidate));
        if (!ReferenceEquals(Get(date), expected)) return false;
        if (candidate is null) _entries.Remove(date);
        else _entries[date] = candidate;
        return true;
    }
}
