using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Core.Features.TimetableImport;

public enum TimetableImportMode { School, Canonical }

/// <summary>A fully validated week plus display-only provenance; no teacher identity or UI state.</summary>
public sealed class TimetableImportCandidate
{
    public TimetableImportCandidate(string label, string evidence, WeeklyTimetable timetable)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(timetable);
        Label = label;
        Evidence = evidence;
        Timetable = timetable;
    }

    public string Label { get; }
    public string Evidence { get; }
    public WeeklyTimetable Timetable { get; }
}
