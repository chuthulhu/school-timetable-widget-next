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
    public MainWindow(CurrentStatusHeaderViewModel headerViewModel, WeeklyTimetableViewModel timetableViewModel, PeriodScheduleEditor? scheduleEditor = null, string persistenceNotice = "", RuntimeDisplaySettings? display = null, Action<DisplaySettingsWindow>? showDisplaySettings = null)
    {
        ArgumentNullException.ThrowIfNull(headerViewModel);
        ArgumentNullException.ThrowIfNull(timetableViewModel);
        InitializeComponent();
        PersistenceNotice.Text = persistenceNotice;
        PersistenceNotice.Visibility = persistenceNotice.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        if (display is not null) StatusHeader.Fonts = display.Fonts;
        StatusHeader.DataContext = headerViewModel;
        Timetable.DataContext = timetableViewModel;
        Timetable.ScheduleEditor = scheduleEditor;
        var settingsWindow = display is null ? null : new DisplaySettingsWindowOwner(this, display, showDisplaySettings);
        CommandBindings.Add(new CommandBinding(DisplaySettingsCommands.Open,
            (_, e) => { settingsWindow?.Open(); e.Handled = true; },
            (_, e) => { e.CanExecute = settingsWindow is not null; e.Handled = true; }));
    }
}
