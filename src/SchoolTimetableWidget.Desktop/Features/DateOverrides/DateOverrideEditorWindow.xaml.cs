using System.Windows;
namespace SchoolTimetableWidget.Desktop.Features.DateOverrides;

public partial class DateOverrideEditorWindow : Window
{
    private readonly DateOverrideEditor _editor;
    public DateOverrideEditSession? Session { get; private set; }
    public DateOverrideEditorWindow(DateOverrideEditor editor, DateOnly initialDate)
    {
        _editor = editor;
        InitializeComponent();
        DateInput.SelectedDate = initialDate.ToDateTime(TimeOnly.MinValue);
    }
    private void Begin_Click(object sender, RoutedEventArgs e)
    {
        if (DateInput.SelectedDate is not { } selected)
        { DateError.Text = "편집할 날짜를 선택해 주세요."; return; }
        var date = DateOnly.FromDateTime(selected);
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        { DateError.Text = "주말은 수업이 없는 날입니다. 월요일부터 금요일 중 선택해 주세요."; return; }
        Session = _editor.CreateSession(date);
        DataContext = Session;
        DateError.Text = "";
        SetEditing(true);
    }
    private void SetEditing(bool editing)
    {
        DateInput.IsEnabled = BeginButton.IsEnabled = !editing;
        EditPanel.IsEnabled = ApplyButton.IsEnabled = RemoveButton.IsEnabled = ChangeDateButton.IsEnabled = editing;
    }
    private void ChangeDate_Click(object sender, RoutedEventArgs e)
    {
        Session?.Cancel(); Session = null; DataContext = null; SetEditing(false);
    }
    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (Session is null) return;
        Session.UseTimetable = false; Session.UseSchedule = false;
        DateError.Text = "전체 예외 해제를 준비했습니다. 적용하면 이 날짜의 기본 시간표와 일과로 돌아갑니다.";
    }
    private void Apply_Click(object sender, RoutedEventArgs e) { if (Session?.TryApply() == true) Close(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    protected override void OnClosed(EventArgs e) { Session?.Cancel(); base.OnClosed(e); }
}
