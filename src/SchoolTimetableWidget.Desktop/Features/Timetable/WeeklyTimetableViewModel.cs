using System.Collections.ObjectModel;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Desktop.Features.Timetable;

/// <summary>Read-only presentation; the Core week already owns slot ordering.</summary>
public sealed class WeeklyTimetableViewModel
{
    public WeeklyTimetableViewModel(WeeklyTimetable timetable)
    {
        ArgumentNullException.ThrowIfNull(timetable);
        Cells = Array.AsReadOnly(timetable.Cells
            .Select(cell => new TimetableCellViewModel(cell.Content)).ToArray());
    }

    public ReadOnlyCollection<string> WeekdayHeaders { get; } =
        Array.AsReadOnly(new[] { "월", "화", "수", "목", "금" });

    public ReadOnlyCollection<string> PeriodHeaders { get; } =
        Array.AsReadOnly(new[] { "1", "2", "3", "4", "5", "6", "7" });

    public ReadOnlyCollection<TimetableCellViewModel> Cells { get; }
}
