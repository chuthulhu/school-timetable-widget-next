using SchoolTimetableWidget.Desktop.Features.TrayLifecycle;
using SchoolTimetableWidget.Desktop.Infrastructure.Time;
using SchoolTimetableWidget.Tests.TrayLifecycle;

namespace SchoolTimetableWidget.Tests.Time;

public class KrissTrayIntegrationTests
{
    [Fact]
    public async Task HiddenTrayKeepsSyncAliveShowReadsCurrentReferenceAndExitCancels()
    {
        var time = new ManualTimeProvider(); var sync = new ClockSynchronizationTests.ControlledSync();
        var clock = KrissClockTests.Create(time); var resume = new ClockSynchronizationTests.ResumeSignal();
        using var coordinator = new ClockSynchronizationCoordinator(clock, sync, time, resume);
        var window = new FakeWidgetWindow(); var tray = new FakeTrayIcon(); long shownRevision = 0;
        window.OnShow = () => shownRevision = clock.GetSnapshot().Revision;
        using var lifecycle = new WidgetTrayLifecycle(window, tray, new MemoryNotice { Requested = true }, coordinator.Dispose);
        var first = coordinator.Start();
        await sync.Entered.Reader.ReadAsync(TestContext.Current.CancellationToken);
        Assert.True(window.Close()); Assert.False(window.IsVisible);
        Assert.False(sync.Token.IsCancellationRequested);
        sync.Complete(true); await first;
        time.Advance(TimeSpan.FromHours(1));
        await sync.Entered.Reader.ReadAsync(TestContext.Current.CancellationToken);
        var second = coordinator.RequestSynchronization();
        Assert.False(window.IsVisible); sync.Complete(true); await second;
        lifecycle.Show(); Assert.True(window.IsVisible); Assert.Equal(2, shownRevision);
        time.Advance(TimeSpan.FromMinutes(1)); var pending = coordinator.RequestSynchronization();
        await sync.Entered.Reader.ReadAsync(TestContext.Current.CancellationToken);
        tray.Exit();
        Assert.True(lifecycle.AllowClose); Assert.True(sync.Token.IsCancellationRequested);
        await pending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.True(resume.Disposed); Assert.Equal(0, resume.Subscribers);
        sync.Complete(true); Assert.Equal(2, clock.GetSnapshot().Revision);
    }

    [Fact]
    public void SecondarySignalsAndCannotEnterClockNetworkStartup()
    {
        var time = new ManualTimeProvider(); var sync = new ClockSynchronizationTests.ControlledSync();
        using var coordinator = new ClockSynchronizationCoordinator(KrissClockTests.Create(time), sync, time, new ClockSynchronizationTests.ResumeSignal());
        var secondary = new Secondary();
        Assert.False(SingleInstanceStartup.Run(secondary, () => _ = coordinator.Start()));
        Assert.Equal(1, secondary.Signals); Assert.Equal(0, sync.Calls); Assert.Null(time.NextDelay);
    }
    private sealed class Secondary : IInstanceOwnership
    {
        public bool IsPrimary => false;
        public int Signals;
        public void SignalActivation() => Signals++;
    }
}
