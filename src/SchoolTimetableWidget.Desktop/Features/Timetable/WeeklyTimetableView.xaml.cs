using System.Windows;
using System.Windows.Controls;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;

namespace SchoolTimetableWidget.Desktop.Features.Timetable;

public partial class WeeklyTimetableView : UserControl
{
    public WeeklyTimetableView() => InitializeComponent();

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
