using SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;
using System.Windows;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Timetable;

namespace SchoolTimetableWidget.Desktop;

/// <summary>Composes feature views without owning their calculation or timer lifecycle.</summary>
public partial class MainWindow : Window
{
    public MainWindow(CurrentStatusHeaderViewModel headerViewModel, WeeklyTimetableViewModel timetableViewModel, PeriodScheduleEditor? scheduleEditor = null)
    {
        ArgumentNullException.ThrowIfNull(headerViewModel);
        ArgumentNullException.ThrowIfNull(timetableViewModel);
        InitializeComponent();
        StatusHeader.DataContext = headerViewModel;
        Timetable.DataContext = timetableViewModel;
        Timetable.ScheduleEditor = scheduleEditor;
    }
}
