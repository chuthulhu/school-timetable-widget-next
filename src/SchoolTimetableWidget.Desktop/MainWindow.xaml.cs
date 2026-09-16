using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using System.Windows.Input;
using SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;
using System.Windows;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;

namespace SchoolTimetableWidget.Desktop;

/// <summary>Composes feature views without owning their calculation or timer lifecycle.</summary>
public partial class MainWindow : Window
{
    public MainWindow(CurrentStatusHeaderViewModel headerViewModel, WeeklyTimetableViewModel timetableViewModel, PeriodScheduleEditor? scheduleEditor = null, string persistenceNotice = "", RuntimeDisplaySettings? display = null, Action<DisplaySettingsWindow>? showDisplaySettings = null, ProfileRuntime? runtime = null, IApplicationClock? clock = null, IBackupDialogs? backupDialogs = null)
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
        if (runtime is not null && clock is not null)
        {
            var actions = new ProfileBackupActions(runtime, clock, backupDialogs ?? new WindowsBackupDialogs(this),
                () => OwnedWindows.Cast<Window>().Any(w => w.IsVisible));
            CommandBindings.Add(new CommandBinding(BackupCommands.Backup, (_, _) => actions.Backup(), (_, e) => e.CanExecute = actions.CanBackup));
            CommandBindings.Add(new CommandBinding(BackupCommands.Restore, (_, _) => actions.Restore(), (_, e) => e.CanExecute = actions.CanRestore));
            CommandBindings.Add(new CommandBinding(BackupCommands.Recover, (_, _) => actions.Recover(), (_, e) => e.CanExecute = actions.CanRecover));
            void UpdateState()
            {
                PersistenceNotice.Text = runtime.Session.LoadResult.Notice;
                PersistenceNotice.Visibility = PersistenceNotice.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
                RecoveryButton.Visibility = runtime.Session.IsRecoveryRequired ? Visibility.Visible : Visibility.Collapsed;
                Timetable.IsEnabled = !runtime.Session.IsRecoveryRequired;
                WindowContentMinimum.Refresh(this);
                CommandManager.InvalidateRequerySuggested();
            }
            EventHandler changed = (_, _) => UpdateState();
            runtime.Session.StateChanged += changed;
            Closed += (_, _) => runtime.Session.StateChanged -= changed;
            UpdateState();
        }
        var settingsWindow = display is null ? null : new DisplaySettingsWindowOwner(this, display, showDisplaySettings);
        CommandBindings.Add(new CommandBinding(DisplaySettingsCommands.Open,
            (_, e) => { settingsWindow?.Open(); e.Handled = true; },
            (_, e) => { e.CanExecute = settingsWindow is not null && runtime?.Session.IsRecoveryRequired != true; e.Handled = true; }));
    }
}
