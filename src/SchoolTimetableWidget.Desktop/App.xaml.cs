using System.Windows;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop.Development;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Infrastructure.Time;

namespace SchoolTimetableWidget.Desktop;

/// <summary>Owns the application clock and header refresh lifetime on the UI dispatcher.</summary>
public partial class App : Application
{
    internal IApplicationClock ApplicationClock { get; } = new PcFallbackApplicationClock();
    private CurrentStatusHeaderRefreshLoop? _headerRefreshLoop;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var headerViewModel = new CurrentStatusHeaderViewModel();
        _headerRefreshLoop = new CurrentStatusHeaderRefreshLoop(
            ApplicationClock, DefaultPeriodSchedule.Periods, headerViewModel);
        try
        {
            var preview = e.Args.Contains("--timetable-preview", StringComparer.Ordinal);
            var timetable = preview ? TimetablePreviewData.Create() : WeeklyTimetable.Empty();
            MainWindow = new MainWindow(headerViewModel, new WeeklyTimetableViewModel(timetable));
            if (preview) MainWindow.Title += " — 개발용 시간표 미리보기";
            // Populate both texts before the first visible frame, then enable live refresh.
            _headerRefreshLoop.Start();
            MainWindow.Show();
        }
        catch
        {
            _headerRefreshLoop.Dispose();
            throw;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _headerRefreshLoop?.Dispose();
        }
        finally
        {
            base.OnExit(e);
        }
    }
}
