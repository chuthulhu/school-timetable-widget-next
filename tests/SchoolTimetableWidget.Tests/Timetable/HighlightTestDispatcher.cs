using System.Runtime.ExceptionServices;
using System.Windows.Threading;

namespace SchoolTimetableWidget.Tests.Timetable;

internal static class HighlightTestDispatcher
{
    private static readonly object ResourceGate = new();
    public static void Run(Action test)
    {
        // WPF package resources are process-shared even across separate STA dispatchers.
        lock (ResourceGate) RunIsolated(test);
    }
    private static void RunIsolated(Action test)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { test(); }
            catch (Exception exception) { failure = exception; }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
