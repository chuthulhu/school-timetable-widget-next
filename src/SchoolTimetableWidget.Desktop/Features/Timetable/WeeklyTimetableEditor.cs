using SchoolTimetableWidget.Desktop.Features.DateOverrides;

namespace SchoolTimetableWidget.Desktop.Features.Timetable;

/// <summary>Adapts a weekly slot to a cell-edit session; owns no timer or persistence.</summary>
public sealed class WeeklyTimetableEditor
{
    private readonly WeeklyTimetableViewModel _owner;

    internal WeeklyTimetableEditor(WeeklyTimetableViewModel owner) => _owner = owner;

    public DateOverrideEditor? DateEditor { get; set; }

    public CellEditSession? ActiveSession { get; private set; }

    public CellEditSession BeginEdit(TimetableCellViewModel cell)
    {
        if (ActiveSession is not null) throw new InvalidOperationException("A cell edit is already open.");
        var slot = _owner.GetSlot(cell);
        CellEditSession session;
        var label = $"{_owner.WeekdayHeaders[(int)slot.Day]}요일 {slot.PeriodNumber}교시";
        if (_owner.DisplayedOverride is { Timetable: not null } dateOverride && dateOverride.Day == slot.Day)
        {
            if (DateEditor is null) throw new InvalidOperationException("Date editing is not configured.");
            session = DateEditor.CreateCellSession(dateOverride, slot.PeriodNumber, label);
        }
        else
        {
            var baseline = _owner.CommittedTimetable[slot.Day, slot.PeriodNumber];
            session = new CellEditSession($"편집 대상: 기본 시간표\n{label}", baseline.Value,
                value => _owner.TryCommitCell(baseline, value));
        }
        ActiveSession = session;
        session.Completed += OnCompleted;
        return session;
    }

    private void OnCompleted(object? sender, EventArgs e)
    {
        if (ReferenceEquals(ActiveSession, sender)) ActiveSession = null;
    }
}
