using System.Collections.ObjectModel;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Desktop.Features.Persistence;

/// <summary>Only durable profile inputs; no mutable UI, clock or derived presentation state.</summary>
public sealed class ProfileSnapshot
{
    public ProfileSnapshot(WeeklyTimetable timetable, PeriodSchedule schedule,
        IEnumerable<DateSpecificOverride> overrides, bool showLunch)
    {
        ArgumentNullException.ThrowIfNull(timetable);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(overrides);
        var entries = overrides.ToArray();
        if (entries.Any(e => e is null) || entries.Select(e => e.Date).Distinct().Count() != entries.Length)
            throw new ArgumentException("Override dates must be unique and entries non-null.", nameof(overrides));
        Timetable = timetable;
        Schedule = schedule;
        Overrides = Array.AsReadOnly(entries.OrderBy(e => e.Date).ToArray());
        ShowLunch = showLunch;
    }

    public WeeklyTimetable Timetable { get; }
    public PeriodSchedule Schedule { get; }
    public ReadOnlyCollection<DateSpecificOverride> Overrides { get; }
    public bool ShowLunch { get; }
    public static ProfileSnapshot Defaults() => new(WeeklyTimetable.Empty(), new(DefaultPeriodSchedule.Periods), [], false);
}
