using SchoolTimetableWidget.Desktop.Infrastructure.Time;

namespace SchoolTimetableWidget.Tests.Time;

public class ClockSynchronizationTests
{
    [Theory]
    [InlineData(true, 60)] [InlineData(false, 15)]
    public async Task ImmediateBackgroundCycleReschedulesFromCompletion(bool success, int minutes)
    {
        var time = new ManualTimeProvider(); var sync = new ControlledSync(); var resume = new ResumeSignal();
        var clock = KrissClockTests.Create(time);
        using var coordinator = new ClockSynchronizationCoordinator(clock, sync, time, resume);
        var first = coordinator.Start();
        await sync.Entered.Reader.ReadAsync(TestContext.Current.CancellationToken);
        Assert.False(first.IsCompleted);
        Assert.Equal(ClockSyncStatus.Synchronizing, clock.Diagnostics.Status);
        sync.Complete(success);
        await first.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(TimeSpan.FromMinutes(minutes), time.NextDelay);
        Assert.Equal(success ? ClockSyncStatus.KrissSynchronized : ClockSyncStatus.LocalFallback, clock.Diagnostics.Status);
        time.Advance(TimeSpan.FromMinutes(minutes));
        await sync.Entered.Reader.ReadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, sync.Calls);
        sync.Complete(success);
        await coordinator.RequestSynchronization();
    }

    [Fact]
    public async Task OverlappingRequestsShareCycleAndRapidResumeIsDebounced()
    {
        var time = new ManualTimeProvider(); var sync = new ControlledSync(); var resume = new ResumeSignal();
        using var coordinator = new ClockSynchronizationCoordinator(KrissClockTests.Create(time), sync, time, resume);
        var first = coordinator.Start();
        await sync.Entered.Reader.ReadAsync(TestContext.Current.CancellationToken);
        Assert.Same(first, coordinator.RequestSynchronization());
        for (var i = 0; i < 20; i++) resume.Fire();
        Assert.Equal(1, sync.Calls);
        sync.Complete(true); await first;
        for (var i = 0; i < 20; i++) resume.Fire();
        Assert.Equal(1, sync.Calls);
        Assert.Equal(TimeSpan.FromHours(1), time.NextDelay);
        time.Advance(TimeSpan.FromMinutes(1)); resume.Fire();
        await sync.Entered.Reader.ReadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, sync.Calls);
        sync.Complete(true); await coordinator.RequestSynchronization();
    }

    [Fact]
    public async Task DisposeCancelsWithoutWaitingForNonCooperativeNetworkAndDetachesResume()
    {
        var time = new ManualTimeProvider(); var sync = new ControlledSync(); var resume = new ResumeSignal();
        var clock = KrissClockTests.Create(time);
        var coordinator = new ClockSynchronizationCoordinator(clock, sync, time, resume);
        var task = coordinator.Start();
        await sync.Entered.Reader.ReadAsync(TestContext.Current.CancellationToken);
        coordinator.Dispose(); coordinator.Dispose();
        Assert.True(sync.Token.IsCancellationRequested);
        Assert.Equal(0, resume.Subscribers);
        Assert.True(resume.Disposed);
        await task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        sync.Complete(true);
        time.Advance(TimeSpan.FromDays(1)); resume.Fire();
        Assert.Equal(1, sync.Calls);
        Assert.Equal(0, clock.GetSnapshot().Revision);
        Assert.Null(time.NextDelay);
    }

    [Fact]
    public async Task LaterFailedCycleKeepsLastGoodAndLaterSuccessReplacesIt()
    {
        var time = new ManualTimeProvider(); var sync = new ControlledSync();
        var clock = KrissClockTests.Create(time);
        using var coordinator = new ClockSynchronizationCoordinator(clock, sync, time, new ResumeSignal());
        var first = coordinator.Start();
        await sync.Entered.Reader.ReadAsync(TestContext.Current.CancellationToken); sync.Complete(true); await first;
        time.Advance(TimeSpan.FromMinutes(1));
        var second = coordinator.RequestSynchronization();
        await sync.Entered.Reader.ReadAsync(TestContext.Current.CancellationToken); sync.Complete(false); await second;
        Assert.Equal(1, clock.GetSnapshot().Revision);
        Assert.Equal("Test network failure", clock.Diagnostics.LastFailure);
        time.Advance(TimeSpan.FromMinutes(1));
        var third = coordinator.RequestSynchronization();
        await sync.Entered.Reader.ReadAsync(TestContext.Current.CancellationToken); sync.Complete(true); await third;
        Assert.Equal(2, clock.GetSnapshot().Revision);
        Assert.Null(clock.Diagnostics.LastFailure);
    }

    internal sealed class ControlledSync : INtpSynchronizer
    {
        public int Calls; public CancellationToken Token;
        public readonly System.Threading.Channels.Channel<int> Entered = System.Threading.Channels.Channel.CreateUnbounded<int>();
        private TaskCompletionSource<NtpSyncResult> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<NtpSyncResult> SynchronizeAsync(CancellationToken token)
        { Token = token; Entered.Writer.TryWrite(++Calls); return _completion.Task; }
        public void Complete(bool success)
        {
            var completion = _completion; _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            completion.TrySetResult(success ? new(NtpSampleSelectionTests.Sample(0, 10), null) : new(null, "Test network failure"));
        }
    }
    internal sealed class ResumeSignal : IResumeSignal
    {
        private EventHandler? _resumed;
        public int Subscribers;
        public bool Disposed;
        public event EventHandler? Resumed { add { _resumed += value; Subscribers++; } remove { _resumed -= value; Subscribers--; } }
        public void Fire() => _resumed?.Invoke(this, EventArgs.Empty);
        public void Dispose() { Disposed = true; }
    }
}
