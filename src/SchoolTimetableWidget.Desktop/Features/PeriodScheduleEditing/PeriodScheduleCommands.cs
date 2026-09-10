using System.Windows.Input;

namespace SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;

public static class PeriodScheduleCommands
{
    public static RoutedUICommand Edit { get; } = new("일과 시간 편집", nameof(Edit), typeof(PeriodScheduleCommands));
}
