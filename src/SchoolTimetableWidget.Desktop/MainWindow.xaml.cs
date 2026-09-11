using SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;
using System.Windows;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Timetable;

namespace SchoolTimetableWidget.Desktop;

/// <summary>Composes feature views without owning their calculation or timer lifecycle.</summary>
public partial class MainWindow : Window
{
    public MainWindow(CurrentStatusHeaderViewModel headerViewModel, WeeklyTimetableViewModel timetableViewModel, PeriodScheduleEditor? scheduleEditor = null, string persistenceNotice = "")
    {
        ArgumentNullException.ThrowIfNull(headerViewModel);
        ArgumentNullException.ThrowIfNull(timetableViewModel);
        InitializeComponent();
        PersistenceNotice.Text = persistenceNotice;
        PersistenceNotice.Visibility = persistenceNotice.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        StatusHeader.DataContext = headerViewModel;
        Timetable.DataContext = timetableViewModel;
        Timetable.ScheduleEditor = scheduleEditor;
    }
}
