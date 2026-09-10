using System.Windows.Input;

namespace SchoolTimetableWidget.Desktop.Features.TimetableImport;

public static class TimetableImportCommands
{
    public static RoutedUICommand School { get; } = new("학교 시간표 가져오기", nameof(School), typeof(TimetableImportCommands));
    public static RoutedUICommand Canonical { get; } = new("표준 양식 가져오기", nameof(Canonical), typeof(TimetableImportCommands));
    public static RoutedUICommand CopyTemplate { get; } = new("표준 양식 복사", nameof(CopyTemplate), typeof(TimetableImportCommands));
}
