using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Desktop.Development;

/// <summary>Opt-in native smoke content, never a persisted or default user profile.</summary>
internal static class TimetablePreviewData
{
    public static WeeklyTimetable Create() => new(
        WeeklyTimetable.Empty().Cells.Select(cell => new TimetableCell(cell.Day, cell.PeriodNumber,
            (cell.Day, cell.PeriodNumber) switch
            {
                (SchoolDay.Monday, 1) => "국어",
                (SchoolDay.Tuesday, 1) => "물리학\n실험 A반!",
                (SchoolDay.Wednesday, 1) => "과학 탐구 프로젝트 발표 및 토론 수업!",
                (SchoolDay.Thursday, 1) => "<b>과목</b>",
                (SchoolDay.Monday, 2) => "국어",
                (SchoolDay.Tuesday, 2) => "  앞뒤 공백  ",
                (SchoolDay.Wednesday, 2) => "   ",
                (SchoolDay.Thursday, 2) => "한글 · Unicode Ω 🎵",
                (SchoolDay.Friday, 7) => "마지막 수업",
                _ => string.Empty
            })));
}
