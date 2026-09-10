using SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;
using SchoolTimetableWidget.Desktop.Features.DateOverrides;
using System.Windows.Input;
using SchoolTimetableWidget.Core.Features.TimetableImport;
using SchoolTimetableWidget.Desktop.Features.TimetableImport;
using System.Windows;
using System.Windows.Controls;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;

namespace SchoolTimetableWidget.Desktop.Features.Timetable;

public partial class WeeklyTimetableView : UserControl
{
    public WeeklyTimetableView() => InitializeComponent();

    public TimetableImportActions ImportActions { get; set; } = new(new WindowsSpreadsheetClipboard());

    public DateOverrideEditor? DateEditor { get; set; }
    public Func<DateOnly?>? GetCurrentDate { get; set; }
    public LunchPresentationOption? LunchOption { get; set; }

    private void DateOverride_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    { e.CanExecute = DateEditor is not null && GetCurrentDate?.Invoke() is not null; e.Handled = true; }
    private void DateOverride_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        e.Handled = true;
        if (DateEditor is not null && GetCurrentDate?.Invoke() is { } date && Window.GetWindow(this) is { IsVisible: true } owner)
            new DateOverrideEditorWindow(DateEditor, date) { Owner = owner }.ShowDialog();
    }
    private void Lunch_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    { e.CanExecute = LunchOption is not null; e.Handled = true; }
    private void Lunch_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        e.Handled = true;
        if (LunchOption is not null) LunchOption.Enabled = !LunchOption.Enabled;
    }

    public PeriodScheduleEditor? ScheduleEditor { get; set; }

    private void PeriodSchedule_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = ScheduleEditor is not null;
        e.Handled = true;
    }
    private void PeriodSchedule_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        e.Handled = true;
        if (Window.GetWindow(this) is { IsVisible: true } owner) ScheduleEditor?.ShowEditor(owner);
    }
    private void Import_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = DataContext is WeeklyTimetableViewModel { Editor.ActiveSession: null };
        e.Handled = true;
    }
    private void SchoolImport_Executed(object sender, ExecutedRoutedEventArgs e) => OpenImport(TimetableImportMode.School, e);
    private void CanonicalImport_Executed(object sender, ExecutedRoutedEventArgs e) => OpenImport(TimetableImportMode.Canonical, e);
    private void OpenImport(TimetableImportMode mode, ExecutedRoutedEventArgs e)
    {
        e.Handled = true;
        if (DataContext is WeeklyTimetableViewModel model && Window.GetWindow(this) is { IsVisible: true } owner)
            ImportActions.ShowImport(owner, model, mode);
    }
    private void CopyTemplate_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        e.Handled = true;
        var message = ImportActions.CopyTemplate();
        if (Window.GetWindow(this) is { IsVisible: true } owner)
            MessageBox.Show(owner, message, "표준 양식 복사", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Cell_EditRequested(object sender, RoutedEventArgs e)
    {
        if (sender is not TimetableCellControl { DataContext: TimetableCellViewModel cell } control ||
            DataContext is not WeeklyTimetableViewModel model ||
            Window.GetWindow(this) is not { IsVisible: true } owner) return;
        e.Handled = true;
        if (model.Editor.ActiveSession is not null) return;
        var session = model.Editor.BeginEdit(cell);
        try
        {
            var dialog = new CellEditorWindow(session) { Owner = owner };
            dialog.ShowDialog();
        }
        finally
        {
            session.Cancel();
            if (session.IsApplied) WindowContentMinimum.Refresh(owner);
            control.Focus();
        }
    }
}
