using SchoolTimetableWidget.Core.Features.Periods;

namespace SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;

/// <summary>UI-thread-owned accepted base schedule. No persistence or timetable content.</summary>
public sealed class RuntimePeriodSchedule
{
    public RuntimePeriodSchedule(PeriodSchedule initial)
    {
        ArgumentNullException.ThrowIfNull(initial);
        Current = initial;
    }

    public PeriodSchedule Current { get; private set; }

    public bool TryReplace(PeriodSchedule baseline, PeriodSchedule replacement)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(replacement);
        if (!ReferenceEquals(Current, baseline)) return false;
        Current = replacement;
        return true;
    }
}
