using System.Windows.Input;
namespace SchoolTimetableWidget.Desktop.Features.DateOverrides;
public static class DateOverrideCommands
{
    public static RoutedUICommand Edit { get; } = new("날짜별 예외 설정", nameof(Edit), typeof(DateOverrideCommands));
    public static RoutedUICommand Lunch { get; } = new("점심시간 표시", nameof(Lunch), typeof(DateOverrideCommands));
}
