using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using System.Windows.Input;
using SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;
using System.Windows;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Timetable;

namespace SchoolTimetableWidget.Desktop;

/// <summary>Composes feature views without owning their calculation or timer lifecycle.</summary>
public partial class MainWindow : Window
{
    public MainWindow(CurrentStatusHeaderViewModel headerViewModel, WeeklyTimetableViewModel timetableViewModel, PeriodScheduleEditor? scheduleEditor = null, string persistenceNotice = "", RuntimeDisplaySettings? display = null)
    {
        ArgumentNullException.ThrowIfNull(headerViewModel);
        ArgumentNullException.ThrowIfNull(timetableViewModel);
        InitializeComponent();
        PersistenceNotice.Text = persistenceNotice;
        PersistenceNotice.Visibility = persistenceNotice.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        StatusHeader.DataContext = headerViewModel;
        Timetable.DataContext = timetableViewModel;
        Timetable.ScheduleEditor = scheduleEditor;
        CommandBindings.Add(new CommandBinding(DisplaySettingsCommands.Open, (_, _) =>
        {
            if (display is null) return;
            var session = display.Open();
            try { new DisplaySettingsWindow(session) { Owner = this }.ShowDialog(); }
            finally { session.Cancel(); }
        }, (_, e) => e.CanExecute = display is not null));
    }
}
