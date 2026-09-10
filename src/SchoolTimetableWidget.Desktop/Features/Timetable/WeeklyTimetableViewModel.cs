using System.Collections.ObjectModel;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Desktop.Features.Timetable;

/// <summary>Owns the accepted in-memory week and its stable cell presentations.</summary>
public sealed class WeeklyTimetableViewModel
{
    private readonly Dictionary<(SchoolDay Day, int PeriodNumber), TimetableCellViewModel> _cellsBySlot;
    private TimetableCellViewModel? _currentCell;
    private bool _publishing;

    public WeeklyTimetableViewModel(WeeklyTimetable timetable)
    {
        ArgumentNullException.ThrowIfNull(timetable);
        CommittedTimetable = timetable;
        var cells = timetable.Cells.Select(cell => new TimetableCellViewModel(
            cell.Value, $"{WeekdayHeaders[(int)cell.Day]}요일 {cell.PeriodNumber}교시")).ToArray();
        Cells = Array.AsReadOnly(cells);
        _cellsBySlot = timetable.Cells.Select((cell, index) => (cell, index))
            .ToDictionary(entry => (entry.cell.Day, entry.cell.PeriodNumber), entry => cells[entry.index]);
        Editor = new WeeklyTimetableEditor(this);
    }

    public ReadOnlyCollection<string> WeekdayHeaders { get; } =
        Array.AsReadOnly(new[] { "월", "화", "수", "목", "금" });

    public ReadOnlyCollection<string> PeriodHeaders { get; } =
        Array.AsReadOnly(new[] { "1", "2", "3", "4", "5", "6", "7" });

    public ReadOnlyCollection<TimetableCellViewModel> Cells { get; }
    public WeeklyTimetable CommittedTimetable { get; private set; }
    public WeeklyTimetableEditor Editor { get; }

    internal (SchoolDay Day, int PeriodNumber) GetSlot(TimetableCellViewModel cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        foreach (var pair in _cellsBySlot)
            if (ReferenceEquals(pair.Value, cell)) return pair.Key;
        throw new ArgumentException("The cell does not belong to this timetable.", nameof(cell));
    }

    internal bool TryCommitCell(TimetableCell baseline, TimetableCellValue value)
    {
        if (_publishing || !ReferenceEquals(CommittedTimetable[baseline.Day, baseline.PeriodNumber], baseline)) return false;
        var next = CommittedTimetable.WithCellValue(baseline.Day, baseline.PeriodNumber, value);
        CommittedTimetable = next;
        _cellsBySlot[(baseline.Day, baseline.PeriodNumber)].SetValue(value);
        return true;
    }

    /// <summary>UI-dispatcher transaction: prepare every projection before a single accepted-week swap.</summary>
    public bool TryReplaceTimetable(WeeklyTimetable baseline, WeeklyTimetable replacement)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(replacement);
        if (_publishing || Editor.ActiveSession is not null || !ReferenceEquals(CommittedTimetable, baseline)) return false;
        var values = replacement.Cells.Select(cell => cell.Value).ToArray();
        var displays = values.Select(TimetableCellFormatter.Format).ToArray();
        var changed = Cells.Select((cell, i) => cell.Value != values[i]).ToArray();
        var displayChanged = Cells.Select((cell, i) => cell.DisplayText != displays[i]).ToArray();
        _publishing = true;
        try
        {
            CommittedTimetable = replacement;
            for (var i = 0; i < Cells.Count; i++) Cells[i].SetValueWithoutNotification(values[i], displays[i]);
            // All 35 canonical values and projections are already coherent at the first notification.
            for (var i = 0; i < Cells.Count; i++)
                if (changed[i]) Cells[i].NotifyValueChanged(displayChanged[i]);
            return true;
        }
        finally { _publishing = false; }
    }

    /// <summary>Null clears the current slot. Invalid identities fail before changing state.</summary>
    public void SetCurrentCell((SchoolDay Day, int PeriodNumber)? slot)
    {
        TimetableCellViewModel? next = null;
        if (slot is { } key && !_cellsBySlot.TryGetValue(key, out next))
            throw new ArgumentOutOfRangeException(nameof(slot), "The slot must be Monday-Friday, period 1-7.");
        if (ReferenceEquals(_currentCell, next)) return;
        var previous = _currentCell;
        _currentCell = next;
        previous?.SetIsCurrent(false);
        // A notification observer may select a different slot while the old one clears.
        if (ReferenceEquals(_currentCell, next)) next?.SetIsCurrent(true);
    }
}
