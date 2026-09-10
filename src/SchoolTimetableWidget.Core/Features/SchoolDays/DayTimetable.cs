using System.Collections.ObjectModel;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Core.Features.SchoolDays;

/// <summary>A complete immutable 1–7 snapshot, reusable independently of date/profile ownership.</summary>
public sealed class DayTimetable
{
    public DayTimetable(IEnumerable<TimetableCellValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copy = values.ToArray();
        if (copy.Length != 7 || copy.Any(value => value is null))
            throw new ArgumentException("Exactly seven non-null cell values are required.", nameof(values));
        Values = Array.AsReadOnly(copy);
    }

    public ReadOnlyCollection<TimetableCellValue> Values { get; }
    public TimetableCellValue this[int period] => period is >= 1 and <= 7
        ? Values[period - 1] : throw new ArgumentOutOfRangeException(nameof(period));
    public DayTimetable WithCell(int period, TimetableCellValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (this[period] == value) return this;
        var copy = Values.ToArray();
        copy[period - 1] = value;
        return new(copy);
    }
    public static DayTimetable FromBase(WeeklyTimetable week, SchoolDay day) =>
        new(Enumerable.Range(1, 7).Select(period => week[day, period].Value));
}
