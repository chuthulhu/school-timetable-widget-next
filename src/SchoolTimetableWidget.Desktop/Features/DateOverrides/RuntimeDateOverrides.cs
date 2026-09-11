using SchoolTimetableWidget.Core.Features.SchoolDays;

namespace SchoolTimetableWidget.Desktop.Features.DateOverrides;

/// <summary>Owned UI-dispatcher date map; injected persistence accepts the full candidate before mutation.</summary>
public sealed class RuntimeDateOverrides
{
    private readonly Dictionary<DateOnly, DateSpecificOverride> _entries = new();
    private readonly Func<IReadOnlyCollection<DateSpecificOverride>, string?>? _persist;
    public RuntimeDateOverrides(IEnumerable<DateSpecificOverride>? initial = null,
        Func<IReadOnlyCollection<DateSpecificOverride>, string?>? persist = null)
    {
        _persist = persist;
        foreach (var entry in initial ?? []) _entries.Add(entry.Date, entry);
    }
    public string? CommitError { get; private set; }
    public DateSpecificOverride? Get(DateOnly date) => _entries.GetValueOrDefault(date);

    public bool TryReplace(DateOnly date, DateSpecificOverride? expected, DateSpecificOverride? candidate)
    {
        CommitError = null;
        if (candidate is not null && candidate.Date != date)
            throw new ArgumentException("Candidate belongs to a different date.", nameof(candidate));
        if (!ReferenceEquals(Get(date), expected)) return false;
        var next = _entries.Values.Where(e => e.Date != date).ToList();
        if (candidate is not null) next.Add(candidate);
        if ((CommitError = _persist?.Invoke(next.AsReadOnly())) is not null) return false;
        if (candidate is null) _entries.Remove(date);
        else _entries[date] = candidate;
        return true;
    }
}
