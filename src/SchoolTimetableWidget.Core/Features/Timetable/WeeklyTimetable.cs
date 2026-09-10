using System.Collections.ObjectModel;

namespace SchoolTimetableWidget.Core.Features.Timetable;

/// <summary>A complete week, ordered by period and then Monday through Friday.</summary>
public sealed class WeeklyTimetable
{
    public const int DayCount = 5;
    public const int PeriodCount = 7;
    public const int CellCount = DayCount * PeriodCount;

    public WeeklyTimetable(IEnumerable<TimetableCell> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        var slots = new TimetableCell[CellCount];
        var count = 0;
        foreach (var cell in cells)
        {
            ArgumentNullException.ThrowIfNull(cell);
            var index = IndexOf(cell.Day, cell.PeriodNumber);
            if (slots[index] is not null)
                throw new ArgumentException("Each timetable slot must occur exactly once.", nameof(cells));
            slots[index] = cell;
            count++;
        }
        if (count != CellCount)
            throw new ArgumentException("A week must contain all 35 slots.", nameof(cells));
        Cells = Array.AsReadOnly(slots);
    }

    public ReadOnlyCollection<TimetableCell> Cells { get; }

    public TimetableCell this[SchoolDay day, int periodNumber]
    {
        get
        {
            TimetableCell.ValidateSlot(day, periodNumber);
            return Cells[IndexOf(day, periodNumber)];
        }
    }

    public static WeeklyTimetable Empty() => new(
        from period in Enumerable.Range(1, PeriodCount)
        from day in Enum.GetValues<SchoolDay>()
        select new TimetableCell(day, period, new TimetableCellValue("", "")));

    /// <summary>Returns a complete new snapshot, preserving every other slot and the exact text.</summary>
    public WeeklyTimetable WithCellValue(SchoolDay day, int periodNumber, TimetableCellValue value)
    {
        var replacement = new TimetableCell(day, periodNumber, value);
        var index = IndexOf(day, periodNumber);
        if (Cells[index].Value == value) return this;
        var cells = Cells.ToArray();
        cells[index] = replacement;
        return new WeeklyTimetable(cells);
    }

    private static int IndexOf(SchoolDay day, int periodNumber) =>
        (periodNumber - 1) * DayCount + (int)day;
}
