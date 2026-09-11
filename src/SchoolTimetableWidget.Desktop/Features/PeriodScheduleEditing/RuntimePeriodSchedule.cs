using SchoolTimetableWidget.Core.Features.Periods;

namespace SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;

/// <summary>UI-thread-owned base schedule; injected persistence runs before reference replacement.</summary>
public sealed class RuntimePeriodSchedule
{
    private readonly Func<PeriodSchedule, string?>? _persist;
    public string? CommitError { get; private set; }
    public RuntimePeriodSchedule(PeriodSchedule initial, Func<PeriodSchedule, string?>? persist = null)
    {
        _persist = persist;
        ArgumentNullException.ThrowIfNull(initial);
        Current = initial;
    }

    public PeriodSchedule Current { get; private set; }

    public bool TryReplace(PeriodSchedule baseline, PeriodSchedule replacement)
    {
        CommitError = null;
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(replacement);
        if (!ReferenceEquals(Current, baseline)) return false;
        if ((CommitError = _persist?.Invoke(replacement)) is not null) return false;
        Current = replacement;
        return true;
    }
}
