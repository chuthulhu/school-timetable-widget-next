using System.Runtime.ExceptionServices;
using System.Windows.Threading;
using SchoolTimetableWidget.Desktop.Features.TrayLifecycle;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.TrayLifecycle;

public class SingleInstanceTests
{
    [Fact]
    public void SecondarySignalsWithoutInitializingProfileWindowOrTray()
    {

        // Each contender needs another OS thread: mutex acquisition is reentrant on its owner thread.
        var scope = Scope();
        using var owner = new WindowsSingleInstance(scope);
        var initialized = false;
        RunThread(() =>
        {
            using var secondary = new WindowsSingleInstance(scope);
            Assert.False(secondary.IsPrimary);
            Assert.False(SingleInstanceStartup.Run(secondary, () => initialized = true));
        });
        Assert.False(initialized); Assert.True(owner.IsPrimary);
    }

    [Fact]
    public void PrimaryRunsInitializationOnceAndDisposalReleasesOwnership()
    {
        var scope = Scope(); var starts = 0;
        using (var primary = new WindowsSingleInstance(scope))
        {
            Assert.True(primary.IsPrimary);
            Assert.True(SingleInstanceStartup.Run(primary, () => starts++));
        }
        RunThread(() =>
        {
            using var next = new WindowsSingleInstance(scope);
            Assert.True(next.IsPrimary);
        });
        Assert.Equal(1, starts);
    }

    [Fact]
    public void SimultaneousContendersHaveExactlyOnePrimary()
    {
        var scope = Scope(); using var ready = new Barrier(5);
        using var elected = new CountdownEvent(4); using var release = new ManualResetEventSlim();
        var winners = 0; var failures = new System.Collections.Concurrent.ConcurrentQueue<Exception>();
        var threads = Enumerable.Range(0, 4).Select(_ => new Thread(() =>
        {
            try
            {
                Assert.True(ready.SignalAndWait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
                using var instance = new WindowsSingleInstance(scope);
                if (instance.IsPrimary) Interlocked.Increment(ref winners);
                elected.Signal();
                Assert.True(release.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            }
            catch (Exception error) { failures.Enqueue(error); }
        })).ToArray();
        foreach (var thread in threads) thread.Start();
        try
        {
            Assert.True(ready.SignalAndWait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.True(elected.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.Equal(1, winners);
        }
        finally
        {
            release.Set();
            foreach (var thread in threads) Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
        }
        Assert.Empty(failures);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SignalBeforeListenerShowsExistingWindowOnDispatcher(bool initiallyHidden) => HighlightTestDispatcher.Run(() =>
    {
        var scope = Scope(); using var primary = new WindowsSingleInstance(scope);
        var window = new FakeWidgetWindow(); var tray = new FakeTrayIcon();
        if (initiallyHidden) window.SetVisible(false);
        using var lifecycle = new WidgetTrayLifecycle(window, tray, null, () => { });
        RunThread(() =>
        {
            using var secondary = new WindowsSingleInstance(scope);
            secondary.SignalActivation();
        });
        var frame = new DispatcherFrame(); var onDispatcher = false; var dispatcher = Dispatcher.CurrentDispatcher;
        primary.Listen(Dispatcher.CurrentDispatcher, () =>
        {
            onDispatcher = dispatcher.CheckAccess();
            lifecycle.Show(); frame.Continue = false;
        });
        var timedOut = false;
        var timer = new DispatcherTimer(DispatcherPriority.Send) { Interval = TimeSpan.FromSeconds(10) };
        timer.Tick += (_, _) => { timedOut = true; frame.Continue = false; };
        timer.Start();
        try { Dispatcher.PushFrame(frame); }
        finally { timer.Stop(); primary.StopListening(); }
        Assert.False(timedOut); Assert.True(onDispatcher); Assert.True(window.IsVisible);
        Assert.Equal(1, window.Shows);
    });

    [Fact]
    public void StopListenerPreventsLaterActivationAndDisposalIsIdempotent() => HighlightTestDispatcher.Run(() =>
    {
        var scope = Scope(); var primary = new WindowsSingleInstance(scope);
        var calls = 0;
        primary.Listen(Dispatcher.CurrentDispatcher, () => calls++);
        primary.StopListening();
        RunThread(() => { using var secondary = new WindowsSingleInstance(scope); secondary.SignalActivation(); });
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        primary.Dispose(); primary.Dispose();
        Assert.Equal(0, calls);
        Assert.Throws<ObjectDisposedException>(primary.SignalActivation);
    });

    [Fact]
    public void ScopeIsPerUserSessionAndDevelopmentPathsAreIsolated()
    {
        Assert.True(WindowsSingleInstance.ScopeOptions.CurrentUserOnly);
        Assert.True(WindowsSingleInstance.ScopeOptions.CurrentSessionOnly);
        Assert.Equal("production", WindowsSingleInstance.ScopeFor(null, false));
        Assert.NotEqual(WindowsSingleInstance.ScopeFor(null, false), WindowsSingleInstance.ScopeFor(null, true));
        Assert.Equal(WindowsSingleInstance.ScopeFor(@"C:\Temp\A", false),
            WindowsSingleInstance.ScopeFor(@"c:\temp\a\", false));
        Assert.NotEqual(WindowsSingleInstance.ScopeFor(@"C:\Temp\A", false),
            WindowsSingleInstance.ScopeFor(@"C:\Temp\B", false));
    }

#if DEBUG
    [Fact]
    public void ActualSecondaryExecutableExitsWithoutOpeningAnyProfileFiles() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new SchoolTimetableWidget.Tests.Persistence.TempProfile();
        using var primary = new WindowsSingleInstance(WindowsSingleInstance.ScopeFor(temp.Directory, false));
        var start = new System.Diagnostics.ProcessStartInfo(
            System.IO.Path.Combine(AppContext.BaseDirectory, "SchoolTimetableWidget.Desktop.exe"))
            { UseShellExecute = false, WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden };
        start.ArgumentList.Add("--dev-profile-directory=" + temp.Directory);
        using var process = System.Diagnostics.Process.Start(start)!;
        try
        {
            Assert.True(process.WaitForExit(10000));
            Assert.Equal(0, process.ExitCode);
            Assert.Empty(System.IO.Directory.GetFiles(temp.Directory));
            var frame = new DispatcherFrame(); var activated = false;
            primary.Listen(Dispatcher.CurrentDispatcher, () => { activated = true; frame.Continue = false; });
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            timer.Tick += (_, _) => frame.Continue = false;
            timer.Start();
            try { Dispatcher.PushFrame(frame); }
            finally { timer.Stop(); primary.StopListening(); }
            Assert.True(activated);
        }
        finally
        {
            // Only the process owned by this diagnostic is eligible for forced failure cleanup.
            if (!process.HasExited) { process.Kill(); process.WaitForExit(10000); }
        }
    });
#endif
    private static string Scope() => "test-" + Guid.NewGuid().ToString("N");
    private static void RunThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { action(); } catch (Exception e) { failure = e; } });
        thread.Start(); Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
