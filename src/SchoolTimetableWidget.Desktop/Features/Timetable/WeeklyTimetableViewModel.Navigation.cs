using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Desktop.Features.Timetable;

public sealed partial class WeeklyTimetableViewModel
{
    private Func<DateOnly, DateSpecificOverride?> _getOverride = _ => null;
    private DateOnly? _actualDate;
    private (SchoolDay Day, int PeriodNumber)? _actualSlot;
    public DateOnly? ViewedWeekStart { get; private set; }
    public ReadOnlyCollection<TimetableDateColumn> Columns { get; private set; } = Array.AsReadOnly(Array.Empty<TimetableDateColumn>());
    public IReadOnlyList<DateOnly> DisplayedDates => Columns.Select(c => c.Date).ToArray();
    public RelayCommand PreviousWeekCommand { get; }
    public RelayCommand NextWeekCommand { get; }

    public static DateOnly MondayOf(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    public void ConfigureDateOverrides(Func<DateOnly, DateSpecificOverride?> getOverride)
    {
        ArgumentNullException.ThrowIfNull(getOverride);
        _getOverride = getOverride;
        RefreshDisplayedWeek();
    }

    /// <summary>Receives shared clock facts; ticks never navigate or rebuild contents.</summary>
    public void UpdateCurrent(DateOnly date, (SchoolDay Day, int PeriodNumber)? slot)
    {
        _actualDate = date;
        _actualSlot = slot;
        if (ViewedWeekStart is null) PublishWeek(MondayOf(date));
        foreach (var column in Columns) column.UpdateToday(date);
        UpdateVisibleHighlight();
    }

    private bool CanNavigate(int days) => !_publishing && ViewedWeekStart is { } start &&
        start.DayNumber + days >= DateOnly.MinValue.DayNumber &&
        start.DayNumber + days + 4 <= DateOnly.MaxValue.DayNumber;

    private void Navigate(int days)
    {
        if (!CanNavigate(days)) return;
        PublishWeek(ViewedWeekStart!.Value.AddDays(days));
    }

    public void RefreshDisplayedWeek()
    {
        if (ViewedWeekStart is { } start) PublishWeek(start);
    }

    private void PublishWeek(DateOnly start)
    {
        if (_publishing) throw new InvalidOperationException("Cannot navigate during publication.");
        var columns = Enumerable.Range(0, 5).Select(i =>
        {
            var date = start.AddDays(i);
            var entry = _getOverride(date);
            if (entry is not null && entry.Date != date)
                throw new InvalidOperationException("Override date does not match displayed date.");
            return new TimetableDateColumn(date, WeekdayHeaders[i], entry,
                Enumerable.Range(1, 7).Select(p => _cellsBySlot[((SchoolDay)i, p)]), _actualDate);
        }).ToArray();
        var presentation = ProjectViewedWeek(CommittedTimetable, columns);
        Publish(presentation, () =>
        {
            ViewedWeekStart = start;
            Columns = Array.AsReadOnly(columns);
        });
        UpdateVisibleHighlight();
        OnPropertyChanged(nameof(ViewedWeekStart));
        OnPropertyChanged(nameof(Columns));
        OnPropertyChanged(nameof(DisplayedDates));
        PreviousWeekCommand.NotifyCanExecuteChanged();
        NextWeekCommand.NotifyCanExecuteChanged();
    }

    private WeeklyTimetable ProjectViewedWeek(WeeklyTimetable source) => ProjectViewedWeek(source, Columns);

    private static WeeklyTimetable ProjectViewedWeek(WeeklyTimetable source, IReadOnlyList<TimetableDateColumn> columns)
    {
        var projected = source;
        // Each DateOnly selects its own day; schedules never determine timetable provenance.
        foreach (var column in columns)
            projected = EffectiveDayResolver.ProjectTimetable(projected, column.DateOverride);
        return projected;
    }

    private void UpdateVisibleHighlight() => SetCurrentCell(
        Columns.Any(c => c.Date == _actualDate) ? _actualSlot : null);
}
