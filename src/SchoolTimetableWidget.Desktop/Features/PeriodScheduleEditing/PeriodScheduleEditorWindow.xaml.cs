using System.Windows;

namespace SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;

public partial class PeriodScheduleEditorWindow : Window
{
    private readonly PeriodScheduleEditSession _session;
    public PeriodScheduleEditorWindow(PeriodScheduleEditSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.IsClosed) throw new ArgumentException("The edit session is already closed.", nameof(session));
        _session = session;
        InitializeComponent();
        DataContext = session;
    }

    private void Apply_Click(object sender, RoutedEventArgs e) { if (_session.TryApply()) Close(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    protected override void OnClosed(EventArgs e) { _session.Cancel(); base.OnClosed(e); }
}
