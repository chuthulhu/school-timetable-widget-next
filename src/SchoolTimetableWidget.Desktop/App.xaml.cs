using SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;
using SchoolTimetableWidget.Desktop.Features.TimetableImport;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using System.Windows;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop.Development;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Desktop.Infrastructure.Time;

namespace SchoolTimetableWidget.Desktop;

/// <summary>Owns the single application clock and shared status refresh lifetime.</summary>
public partial class App : Application
{
    internal IApplicationClock ApplicationClock { get; private set; } = new PcFallbackApplicationClock();
    private CurrentStatusRefreshLoop? _statusRefreshLoop;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var periodPreview = e.Args.Contains("--period-preview", StringComparer.Ordinal);
        var bulkPreview = e.Args.Contains("--bulk-preview", StringComparer.Ordinal);
        var highlightPreview = bulkPreview || e.Args.Contains("--highlight-preview", StringComparer.Ordinal);
        var preview = periodPreview || highlightPreview || e.Args.Contains("--timetable-preview", StringComparer.Ordinal);
        if (highlightPreview) ApplicationClock = new HighlightPreviewClock();
        if (periodPreview) ApplicationClock = new PeriodSchedulePreviewClock();
        var headerViewModel = new CurrentStatusHeaderViewModel();
        var timetable = preview ? TimetablePreviewData.Create() : WeeklyTimetable.Empty();
        var timetableViewModel = new WeeklyTimetableViewModel(timetable);
        var baseSchedule = new RuntimePeriodSchedule(new PeriodSchedule(DefaultPeriodSchedule.Periods));
        _statusRefreshLoop = new CurrentStatusRefreshLoop(
            ApplicationClock, () => baseSchedule.Current.Periods, headerViewModel, timetableViewModel);
        try
        {
            MainWindow = new MainWindow(headerViewModel, timetableViewModel,
                new PeriodScheduleEditor(baseSchedule, _statusRefreshLoop.RefreshNow));
            if (bulkPreview)
            {
                ((WeeklyTimetableView)MainWindow.FindName("Timetable")).ImportActions =
                    new TimetableImportActions(new WindowsSpreadsheetClipboard(), BulkImportPreviewData.Create);
                MainWindow.Title += " — 가져오기 개발 샘플";
            }
            if (periodPreview) MainWindow.Title += " — 일과 편집 검증 · 월요일 13:10부터 모의 시각";
            else if (highlightPreview) MainWindow.Title += " — 강조 검증 · 모의 시각 (70초 순환)";
            else if (preview) MainWindow.Title += " — 개발용 시간표 미리보기";
            // Populate header and current slot before the first visible frame.
            _statusRefreshLoop.Start();
            MainWindow.Show();
        }
        catch
        {
            _statusRefreshLoop.Dispose();
            throw;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _statusRefreshLoop?.Dispose(); }
        finally { base.OnExit(e); }
    }
}
