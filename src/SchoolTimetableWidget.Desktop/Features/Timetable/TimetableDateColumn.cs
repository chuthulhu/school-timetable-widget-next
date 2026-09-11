using System.Collections.ObjectModel;
using System.Globalization;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Desktop.Features.Timetable;

/// <summary>One displayed date with typed provenance shared by its seven stable cells.</summary>
public sealed class TimetableDateColumn : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private bool _isToday;
    internal TimetableDateColumn(DateOnly date, string weekdayText,
        DateSpecificOverride? dateOverride, IEnumerable<TimetableCellViewModel> cells, DateOnly? today)
    {
        Date = date;
        Day = (SchoolDay)((int)date.DayOfWeek - 1);
        WeekdayText = weekdayText;
        DateOverride = dateOverride;
        Cells = Array.AsReadOnly(cells.ToArray());
        _isToday = date == today;
    }
    public DateOnly Date { get; }
    public SchoolDay Day { get; }
    public string DateText => Date.ToString("M/d", CultureInfo.InvariantCulture);
    public string WeekdayText { get; }
    public DateSpecificOverride? DateOverride { get; }
    public bool IsDateOverride => DateOverride?.Timetable is not null;
    public ReadOnlyCollection<TimetableCellViewModel> Cells { get; }
    public bool IsToday => _isToday;
    internal void UpdateToday(DateOnly today) => SetProperty(ref _isToday, Date == today, nameof(IsToday));
}
