using System.Windows;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Timetable;

namespace SchoolTimetableWidget.Desktop;

/// <summary>Composes feature views without owning their calculation or timer lifecycle.</summary>
public partial class MainWindow : Window
{
    public MainWindow(CurrentStatusHeaderViewModel headerViewModel, WeeklyTimetableViewModel timetableViewModel)
    {
        ArgumentNullException.ThrowIfNull(headerViewModel);
        ArgumentNullException.ThrowIfNull(timetableViewModel);
        InitializeComponent();
        StatusHeader.DataContext = headerViewModel;
        Timetable.DataContext = timetableViewModel;
    }
}
