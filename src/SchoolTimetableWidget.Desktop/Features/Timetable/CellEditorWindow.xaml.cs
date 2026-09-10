using System.Windows;

namespace SchoolTimetableWidget.Desktop.Features.Timetable;

public partial class CellEditorWindow : Window
{
    private readonly CellEditSession _session;

    public CellEditorWindow(CellEditSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.IsClosed) throw new ArgumentException("The edit session is already closed.", nameof(session));
        _session = session;
        InitializeComponent();
        DataContext = session;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        SubjectInput.Focus();
        SubjectInput.CaretIndex = SubjectInput.Text.Length;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (_session.TryApply()) Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        _session.Cancel();
        base.OnClosed(e);
    }
}
