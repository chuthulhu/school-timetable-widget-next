using System.IO;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Desktop.Features.TimetableImport;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using System.Windows;
using SchoolTimetableWidget.Core.Features.Periods;
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
    private JsonProfileStore? _profileStore;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var effectivePreview = e.Args.Contains("--effective-preview", StringComparer.Ordinal);
        var periodPreview = effectivePreview || e.Args.Contains("--period-preview", StringComparer.Ordinal);
        var bulkPreview = e.Args.Contains("--bulk-preview", StringComparer.Ordinal);
        var highlightPreview = bulkPreview || e.Args.Contains("--highlight-preview", StringComparer.Ordinal);
        var preview = periodPreview || highlightPreview || e.Args.Contains("--timetable-preview", StringComparer.Ordinal);
        if (highlightPreview) ApplicationClock = new HighlightPreviewClock();
        if (periodPreview) ApplicationClock = new PeriodSchedulePreviewClock();
        var headerViewModel = new CurrentStatusHeaderViewModel();
        ProfileSession profile;
        try
        {
            var directory = DevelopmentProfileLocation.FromArguments(e.Args) ??
                (preview ? DevelopmentProfileLocation.CreateTemporary() : ProfileLocation.ForCurrentUser());
            _profileStore = new JsonProfileStore(directory);
            var seed = preview ? new ProfileSnapshot(TimetablePreviewData.Create(), new(DefaultPeriodSchedule.Periods), [], false) : null;
            profile = new ProfileSession(_profileStore, seed);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or System.Security.SecurityException)
        {
            System.Diagnostics.Debug.WriteLine(error);
            profile = ProfileSession.Unavailable("사용자 데이터 경로를 확인할 수 없습니다.");
        }
        var runtime = new ProfileRuntime(profile, () => _statusRefreshLoop!.RefreshNow(), date =>
        {
            if (_statusRefreshLoop!.CurrentDate == date) _statusRefreshLoop.RefreshNow();
        });
        headerViewModel.SetDisplay(runtime.Display.Current);
        runtime.Display.Changed += (_, _) => headerViewModel.SetDisplay(runtime.Display.Current);
        var timetableViewModel = runtime.Timetable;
        _statusRefreshLoop = new CurrentStatusRefreshLoop(ApplicationClock,
            runtime.Resolve, headerViewModel, timetableViewModel, () => runtime.Lunch.Enabled);
        try
        {
            MainWindow = new MainWindow(headerViewModel, timetableViewModel,
                runtime.ScheduleEditor, profile.LoadResult.Notice, runtime.Display);
            var timetableView = (WeeklyTimetableView)MainWindow.FindName("Timetable");
            timetableView.DateEditor = runtime.DateEditor;
            timetableView.GetCurrentDate = () => _statusRefreshLoop.CurrentDate;
            timetableView.LunchOption = runtime.Lunch;
            timetableViewModel.ContentChanged += (_, _) => WindowContentMinimum.Refresh(MainWindow);
            if (bulkPreview)
            {
                ((WeeklyTimetableView)MainWindow.FindName("Timetable")).ImportActions =
                    new TimetableImportActions(new WindowsSpreadsheetClipboard(), BulkImportPreviewData.Create);
                MainWindow.Title += " — 가져오기 개발 샘플";
            }
            if (effectivePreview) MainWindow.Title += " — 날짜 예외 검증 · 2026-09-07 월요일 13:10부터 모의 시각";
            else if (periodPreview) MainWindow.Title += " — 일과 편집 검증 · 월요일 13:10부터 모의 시각";
            else if (highlightPreview) MainWindow.Title += " — 강조 검증 · 모의 시각 (70초 순환)";
            else if (preview) MainWindow.Title += " — 개발용 시간표 미리보기";
            // Populate header and current slot before the first visible frame.
            _statusRefreshLoop.Start();
            MainWindow.Show();
        }
        catch
        {
            _statusRefreshLoop.Dispose();
            _profileStore?.Dispose();
            throw;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _statusRefreshLoop?.Dispose(); }
        finally { _profileStore?.Dispose(); base.OnExit(e); }
    }
}
