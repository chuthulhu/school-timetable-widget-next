namespace SchoolTimetableWidget.Desktop.Infrastructure.Time;

internal interface IResumeSignal : IDisposable
{
    event EventHandler? Resumed;
}

/// <summary>App lifetime, not window visibility. No dispatcher/network work runs in Start.</summary>
internal sealed class ClockSynchronizationCoordinator : IDisposable
{
    internal static readonly TimeSpan SuccessInterval = TimeSpan.FromHours(1);
    internal static readonly TimeSpan FailureInterval = TimeSpan.FromMinutes(15);
    internal static readonly TimeSpan MinimumTriggerInterval = TimeSpan.FromMinutes(1);
    private readonly object _gate = new();
    private readonly SynchronizedApplicationClock _clock;
    private readonly INtpSynchronizer _synchronizer;
    private readonly TimeProvider _time;
    private readonly IResumeSignal _resume;
    private readonly ITimer _timer;
    private readonly CancellationTokenSource _shutdown = new();
    private Task? _active;
    private long? _lastStarted;
    private bool _started;
    private bool _disposed;

    public ClockSynchronizationCoordinator(SynchronizedApplicationClock clock, INtpSynchronizer synchronizer,
        TimeProvider time, IResumeSignal resume)
    {
        _clock = clock; _synchronizer = synchronizer; _time = time; _resume = resume;
        _timer = time.CreateTimer(_ => RequestSynchronization(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _resume.Resumed += OnResumed;
    }

    public Task Start()
    {
        lock (_gate)
        {
            if (_started || _disposed) return _active ?? Task.CompletedTask;
            _started = true;
            return RequestSynchronization();
        }
    }

    public Task RequestSynchronization()
    {
        lock (_gate)
        {
            if (_disposed || !_started) return Task.CompletedTask;
            if (_active is not null) return _active;
            if (_lastStarted is { } last && _time.GetElapsedTime(last) < MinimumTriggerInterval) return Task.CompletedTask;
            _lastStarted = _time.GetTimestamp();
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _clock.BeginSynchronization();
            // No synchronous DNS, sockets or user dependency callbacks on the startup/UI thread.
            _active = Task.Run(RunCycleAsync);
            return _active;
        }
    }

    private async Task RunCycleAsync()
    {
        NtpSyncResult result;
        try
        {
            result = await _synchronizer.SynchronizeAsync(_shutdown.Token).WaitAsync(_shutdown.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested) { return; }
        catch (Exception error)
        {
            // This background boundary must never turn a failed operation into success or a popup.
            // Preserve a category, not network payloads or personal data.
            result = new(null, error.GetType().Name);
        }
        lock (_gate)
        {
            if (_disposed) return;
            if (result.Sample is { } sample) _clock.Apply(sample);
            else _clock.Fail(result.Failure ?? "Synchronization failed");
            _active = null;
            _timer.Change(result.Success ? SuccessInterval : FailureInterval, Timeout.InfiniteTimeSpan);
        }
    }

    private void OnResumed(object? sender, EventArgs e) => RequestSynchronization();

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _timer.Dispose();
            _resume.Resumed -= OnResumed;
            _resume.Dispose();
            if (_active is not null) _clock.Fail("Canceled");
        }
        // Cancel without waiting for the worker, DNS or UDP timeout on the UI/session-ending thread.
        _shutdown.Cancel();
        // CTS is disposed only after its worker can no longer obtain/register its token.
        var active = _active;
        if (active is null) _shutdown.Dispose();
        else _ = active.ContinueWith(_ => _shutdown.Dispose(), CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}
