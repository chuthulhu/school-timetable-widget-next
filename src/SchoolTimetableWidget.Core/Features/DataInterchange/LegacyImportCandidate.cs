using System.Collections.ObjectModel;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Core.Features.DataInterchange;

public sealed record LegacyImportReport(string Code, string Path, string Message);

/// <summary>Complete conversion and explicit supplementation; no source I/O or commitment.</summary>
public sealed class LegacyImportCandidate
{
    internal LegacyImportCandidate(WeeklyTimetable timetable, PeriodSchedule schedule, IEnumerable<LegacyImportReport> reports)
    {
        Timetable = timetable;
        Schedule = schedule;
        Reports = Array.AsReadOnly(reports.ToArray());
    }
    public WeeklyTimetable Timetable { get; }
    public PeriodSchedule Schedule { get; }
    public ReadOnlyCollection<LegacyImportReport> Reports { get; }
}
