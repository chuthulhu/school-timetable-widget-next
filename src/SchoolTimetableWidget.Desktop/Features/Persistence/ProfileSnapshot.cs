using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using System.Collections.ObjectModel;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Features.Semesters;

namespace SchoolTimetableWidget.Desktop.Features.Persistence;

/// <summary>Only durable inputs; immutable semesters and global display/presentation settings.</summary>
public sealed class ProfileSnapshot
{
    public ProfileSnapshot(WeeklyTimetable timetable, PeriodSchedule schedule,
        IEnumerable<DateSpecificOverride> overrides, bool showLunch, DisplayConfiguration? display = null,
        UserDisplayPresetLibrary? displayPresets = null)
        : this([new SemesterSet(DefaultSemesterId, "기본 학기", timetable, schedule, overrides)],
            DefaultSemesterId, showLunch, display, displayPresets) { }

    // Deterministic identity for the one legacy/default dataset, independent of calendar/name.
    public static Guid DefaultSemesterId { get; } = new("7b189e46-3dd5-4710-97bc-3a6e08e3d942");
    public ProfileSnapshot(IEnumerable<SemesterSet> semesterSets, Guid activeSemesterId, bool showLunch,
        DisplayConfiguration? display = null, UserDisplayPresetLibrary? displayPresets = null)
    {
        ArgumentNullException.ThrowIfNull(semesterSets);
        var entries = semesterSets.ToArray();
        if (entries.Length == 0 || entries.Any(e => e is null) ||
            entries.Select(e => e.SemesterId).Distinct().Count() != entries.Length ||
            entries.Select(e => e.DisplayName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != entries.Length)
            throw new ArgumentException("학기는 하나 이상이어야 하며 ID와 이름이 중복될 수 없습니다.");
        ActiveSemester = entries.SingleOrDefault(e => e.SemesterId == activeSemesterId)
            ?? throw new ArgumentException("활성 학기를 찾을 수 없습니다.");
        SemesterSets = Array.AsReadOnly(entries);
        ActiveSemesterId = activeSemesterId;
        ShowLunch = showLunch;
        Display = display ?? Features.DisplaySettings.DisplayPresets.Create(DisplayPreset.Standard);
        DisplayPresets = displayPresets ?? UserDisplayPresetLibrary.Empty;
        DisplayPresets.ValidateReference(Display);
    }
    public ReadOnlyCollection<SemesterSet> SemesterSets { get; }
    public Guid ActiveSemesterId { get; }
    public SemesterSet ActiveSemester { get; }
    public WeeklyTimetable Timetable => ActiveSemester.Timetable;
    public PeriodSchedule Schedule => ActiveSemester.Schedule;
    public ReadOnlyCollection<DateSpecificOverride> Overrides => ActiveSemester.Overrides;
    public bool ShowLunch { get; }
    public DisplayConfiguration Display { get; }
    public UserDisplayPresetLibrary DisplayPresets { get; }
    public ProfileSnapshot ReplaceSemester(SemesterSet value)
    {
        if (!SemesterSets.Any(s => s.SemesterId == value.SemesterId)) throw new ArgumentException("Unknown semester.");
        return WithSemesters(SemesterSets.Select(s => s.SemesterId == value.SemesterId ? value : s), ActiveSemesterId);
    }
    public ProfileSnapshot WithSemesters(IEnumerable<SemesterSet> values, Guid activeId) => new(values, activeId, ShowLunch, Display, DisplayPresets);
    public static ProfileSnapshot Defaults() => new(WeeklyTimetable.Empty(), new(DefaultPeriodSchedule.Periods), [], false);
}
