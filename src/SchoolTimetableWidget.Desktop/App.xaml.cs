using SchoolTimetableWidget.Desktop.Infrastructure.Fonts;
using SchoolTimetableWidget.Desktop.Features.TrayLifecycle;
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
    internal IApplicationClock ApplicationClock { get; private set; } =
        new SynchronizedApplicationClock(new PcFallbackApplicationClock(), TimeProvider.System);
    private ClockSynchronizationCoordinator? _clockSynchronization;
    private CurrentStatusRefreshLoop? _statusRefreshLoop;
    private JsonProfileStore? _profileStore;
    private WindowsSingleInstance? _instance;
    private WidgetTrayLifecycle? _trayLifecycle;
    private Features.Updates.UpdateCoordinator? _updates;
    private bool _restartForUpdate;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var developmentDirectory = DevelopmentProfileLocation.FromArguments(e.Args,
            Environment.GetEnvironmentVariable("STW_DEV_PROFILE_DIRECTORY"));
        var preview = e.Args.Any(arg => arg is "--effective-preview" or "--period-preview" or
            "--bulk-preview" or "--highlight-preview" or "--timetable-preview");
        _instance = new WindowsSingleInstance(WindowsSingleInstance.ScopeFor(developmentDirectory, preview));
        try
        {
            if (!SingleInstanceStartup.Run(_instance, () => InitializePrimary(e.Args, developmentDirectory)))
                Shutdown();
        }
        catch
        {
            DisposeResources();
            throw;
        }
    }

    private void InitializePrimary(string[] args, string? developmentDirectory)
    {
        var effectivePreview = args.Contains("--effective-preview", StringComparer.Ordinal);
        var periodPreview = effectivePreview || args.Contains("--period-preview", StringComparer.Ordinal);
        var bulkPreview = args.Contains("--bulk-preview", StringComparer.Ordinal);
        var highlightPreview = bulkPreview || args.Contains("--highlight-preview", StringComparer.Ordinal);
        var preview = periodPreview || highlightPreview || args.Contains("--timetable-preview", StringComparer.Ordinal);
        if (highlightPreview) ApplicationClock = new HighlightPreviewClock();
        if (periodPreview) ApplicationClock = new PeriodSchedulePreviewClock();
        var headerViewModel = new CurrentStatusHeaderViewModel();
        ProfileSession profile;
        JsonWindowStateStore? windowStateStore = null;
        JsonTrayNoticeStore? trayNoticeStore = null;
        var fonts = FontLibrary.LocalOnly;
        try
        {
            var directory = developmentDirectory ??
                (preview ? DevelopmentProfileLocation.CreateTemporary() : ProfileLocation.ForCurrentUser());
            windowStateStore = new JsonWindowStateStore(directory);
            trayNoticeStore = new JsonTrayNoticeStore(directory);
            _profileStore = new JsonProfileStore(directory);
            fonts = new FontLibrary(new DownloadedFontCache(directory));
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
        }, fonts);
        headerViewModel.SetDisplay(runtime.Display.Current);
        runtime.Display.Changed += (_, _) => headerViewModel.SetDisplay(runtime.Display.Current);
        var timetableViewModel = runtime.Timetable;
        _statusRefreshLoop = new CurrentStatusRefreshLoop(ApplicationClock,
            runtime.Resolve, headerViewModel, timetableViewModel, () => runtime.Lunch.Enabled);
        try
        {
            MainWindow = new MainWindow(headerViewModel, timetableViewModel,
                runtime.ScheduleEditor, profile.LoadResult.Notice, runtime.Display, runtime: runtime, clock: ApplicationClock);
            var placement = new WindowPlacementController(MainWindow, windowStateStore);
            var window = new WpfWidgetWindow(MainWindow, placement, () => _statusRefreshLoop.RefreshNow());
            var autostart = new Features.Autostart.AutoStartRegistration(
                new WindowsAutoStartRegistrationStore(), AutoStartCommand.ForCurrentProcess());
            var tray = new WindowsTrayIcon(registration: autostart);
            _trayLifecycle = new WidgetTrayLifecycle(window, tray, trayNoticeStore, () => Shutdown());
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
            _instance!.Listen(Dispatcher, _trayLifecycle.Show);
            if (!preview)
            {
                var clock = (SynchronizedApplicationClock)ApplicationClock;
                _clockSynchronization = new ClockSynchronizationCoordinator(clock,
                    new NtpClient(clock, TimeProvider.System, new UdpNtpNetwork(TimeProvider.System)),
                    TimeProvider.System, new WindowsResumeSignal());
                _ = _clockSynchronization.Start();
                StartUpdates(tray);
            }
        }
        catch
        {
            DisposeResources();
            throw;
        }
    }

    private void StartUpdates(WindowsTrayIcon tray)
    {
        _updates = new Features.Updates.UpdateCoordinator(new VelopackUpdateSource(), TimeProvider.System,
            SynchronizationContext.Current);
        _updates.Ready += version => { tray.SetUpdateReady(version); tray.ShowMessage(
            Features.Updates.UpdateCoordinator.Describe(new(Features.Updates.UpdateCheckOutcome.Ready, version))); };
        tray.CheckUpdatesRequested += async () =>
        {
            var result = await _updates.CheckAsync();
            // A newly ready version already announced itself through Ready.
            if (result.Outcome != Features.Updates.UpdateCheckOutcome.Ready) tray.ShowMessage(Features.Updates.UpdateCoordinator.Describe(result));
        };
        tray.ApplyUpdateRequested += () =>
        {
            // Same guard as 종료: an open editor keeps the app running, so the updater must not start.
            if (MainWindow.OwnedWindows.Cast<Window>().Any(w => w.IsVisible)) { tray.RequestEditorNotice(); return; }
            _restartForUpdate = true;
            tray.RequestExit();
        };
        _updates.Start();
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        _clockSynchronization?.Dispose();
        _trayLifecycle?.SessionEnding();
        base.OnSessionEnding(e);
        e.Cancel = false;
    }

    private void DisposeResources()
    {
        _clockSynchronization?.Dispose();
        _updates?.Dispose();
        // Stop activation first; release ownership only after profile resources are closed.
        try { _instance?.StopListening(); }
        finally
        {
            try { _trayLifecycle?.Dispose(); }
            finally
            {
                try { _statusRefreshLoop?.Dispose(); }
                finally
                {
                    try { _profileStore?.Dispose(); }
                    finally { _instance?.Dispose(); }
                }
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            // The updater waits for this process to exit, then installs and relaunches (ADR 0032).
            if (_restartForUpdate) _updates?.ApplyAfterExit(restart: true);
            DisposeResources();
        }
        finally { base.OnExit(e); }
    }
}
