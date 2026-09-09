using System.Windows;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Time;
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
            MainWindow = new MainWindow(headerViewModel);
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
