using System.Windows;

namespace SchoolTimetableWidget.Desktop.Features.TimetableImport;

public partial class TimetableImportWindow : Window
{
    private readonly TimetableImportSession _session;
    private readonly Action _readClipboard;

    public TimetableImportWindow(TimetableImportSession session, Action readClipboard)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(readClipboard);
        if (session.IsClosed) throw new ArgumentException("The import session is closed.", nameof(session));
        _session = session;
        _readClipboard = readClipboard;
        InitializeComponent();
        DataContext = session;
        session.Completed += Session_Completed;
    }

    private void ReadClipboard_Click(object sender, RoutedEventArgs e) => _readClipboard();
    private void Session_Completed(object? sender, EventArgs e) => Close();
    protected override void OnClosed(EventArgs e)
    {
        _session.Completed -= Session_Completed;
        _session.Cancel();
        base.OnClosed(e);
    }
}
