using System.Collections.ObjectModel;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Core.Features.Semesters;

/// <summary>Immutable instructional data. Names are labels, never calendar rules.</summary>
public sealed class SemesterSet
{
    public const int MaximumNameLength = 80;
    public SemesterSet(Guid semesterId, string displayName, WeeklyTimetable timetable,
        PeriodSchedule schedule, IEnumerable<DateSpecificOverride> overrides)
    {
        if (semesterId == Guid.Empty) throw new ArgumentException("Invalid semester ID.");
        ArgumentNullException.ThrowIfNull(displayName);
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > MaximumNameLength || displayName != displayName.Trim())
            throw new ArgumentException("학기 이름은 앞뒤 공백 없이 1~80자로 입력해 주세요.");
        ArgumentNullException.ThrowIfNull(timetable);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(overrides);
        var entries = overrides.ToArray();
        if (entries.Any(e => e is null) || entries.Select(e => e.Date).Distinct().Count() != entries.Length)
            throw new ArgumentException("Override dates must be unique and entries non-null.");
        SemesterId = semesterId; DisplayName = displayName; Timetable = timetable; Schedule = schedule;
        Overrides = Array.AsReadOnly(entries.OrderBy(e => e.Date).ToArray());
    }
    public Guid SemesterId { get; }
    public string DisplayName { get; }
    public WeeklyTimetable Timetable { get; }
    public PeriodSchedule Schedule { get; }
    public ReadOnlyCollection<DateSpecificOverride> Overrides { get; }
    public SemesterSet WithName(string name) => new(SemesterId, name, Timetable, Schedule, Overrides);
    public SemesterSet WithTimetable(WeeklyTimetable value) => new(SemesterId, DisplayName, value, Schedule, Overrides);
    public SemesterSet WithSchedule(PeriodSchedule value) => new(SemesterId, DisplayName, Timetable, value, Overrides);
    public SemesterSet WithOverrides(IEnumerable<DateSpecificOverride> value) => new(SemesterId, DisplayName, Timetable, Schedule, value);
}
