using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Desktop.Development;

/// <summary>Opt-in native smoke content, never a persisted or default user profile.</summary>
internal static class TimetablePreviewData
{
    public static WeeklyTimetable Create() => new(
        WeeklyTimetable.Empty().Cells.Select(cell => new TimetableCell(cell.Day, cell.PeriodNumber,
            (cell.Day, cell.PeriodNumber) switch
            {
                (SchoolDay.Monday, 1) => new("국어", ""),
                // Existing single-text fixture is carried whole, never split by guessing.
                (SchoolDay.Tuesday, 1) => new("물리학\n실험 A반!", ""),
                (SchoolDay.Wednesday, 1) => new("과학 탐구 프로젝트 발표 및 토론 수업!", ""),
                (SchoolDay.Thursday, 1) => new("<b>과목</b>", ""),
                (SchoolDay.Monday, 2) => new("국어", ""),
                (SchoolDay.Tuesday, 2) => new("  앞뒤 공백  ", ""),
                (SchoolDay.Wednesday, 2) => new("   ", ""),
                (SchoolDay.Thursday, 2) => new("한글 · Unicode Ω 🎵", ""),
                (SchoolDay.Monday, 3) => new("수학", "2-3"),
                (SchoolDay.Tuesday, 3) => new("", "반만 표시"),
                (SchoolDay.Friday, 7) => new("마지막 수업", ""),
                _ => new("", "")
            })));
}
