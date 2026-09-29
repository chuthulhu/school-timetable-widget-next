using System.Windows;

namespace SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;

/// <summary>Adapts editing to runtime ownership and an explicit application refresh callback.</summary>
public sealed class PeriodScheduleEditor(RuntimePeriodSchedule target, Action refreshAfterApply)
{
    public PeriodScheduleEditSession CreateSession()
    {
        var semesterId = target.GetSemesterId?.Invoke();
        var baseline = target.Current;
        return new(baseline, candidate =>
        {
            if (semesterId != target.GetSemesterId?.Invoke() || !target.TryReplace(baseline, candidate)) return false;
            refreshAfterApply();
            return true;
        }, () => target.CommitError);
    }

    public void ShowEditor(Window owner)
    {
        var session = CreateSession();
        try { new PeriodScheduleEditorWindow(session) { Owner = owner }.ShowDialog(); }
        finally { session.Cancel(); }
    }
}
