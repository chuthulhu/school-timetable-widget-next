namespace SchoolTimetableWidget.Desktop.Features.Timetable;

/// <summary>Adapts a weekly slot to a cell-edit session; owns no timer or persistence.</summary>
public sealed class WeeklyTimetableEditor
{
    private readonly WeeklyTimetableViewModel _owner;

    internal WeeklyTimetableEditor(WeeklyTimetableViewModel owner) => _owner = owner;

    public CellEditSession? ActiveSession { get; private set; }

    public CellEditSession BeginEdit(TimetableCellViewModel cell)
    {
        if (ActiveSession is not null) throw new InvalidOperationException("A cell edit is already open.");
        var slot = _owner.GetSlot(cell);
        var baseline = _owner.CommittedTimetable[slot.Day, slot.PeriodNumber];
        var session = new CellEditSession(
            $"{_owner.WeekdayHeaders[(int)slot.Day]}요일 {slot.PeriodNumber}교시",
            baseline.Value, value => _owner.TryCommitCell(baseline, value));
        ActiveSession = session;
        session.Completed += OnCompleted;
        return session;
    }

    private void OnCompleted(object? sender, EventArgs e)
    {
        if (ReferenceEquals(ActiveSession, sender)) ActiveSession = null;
    }
}
