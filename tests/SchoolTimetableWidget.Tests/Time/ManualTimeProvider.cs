namespace SchoolTimetableWidget.Tests.Time;

/// <summary>Deterministic wall/monotonic time and one-shot timers; never changes OS time.</summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly object _gate = new();
    private readonly List<ManualTimer> _timers = [];
    private long _ticks;
    public global::System.Threading.Channels.Channel<TimeSpan> Scheduled { get; } = global::System.Threading.Channels.Channel.CreateUnbounded<TimeSpan>();
    public DateTimeOffset Utc { get; set; } = NtpPacketTests.Reference;
    public override DateTimeOffset GetUtcNow() => Utc;
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() { lock (_gate) return _ticks; }
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        lock (_gate) _timers.Add(timer);
        timer.Change(dueTime, period);
        return timer;
    }
    public void Advance(TimeSpan elapsed)
    {
        List<ManualTimer> due;
        lock (_gate)
        {
            _ticks += elapsed.Ticks; Utc += elapsed;
            due = _timers.Where(t => t.Due <= _ticks).ToList();
            foreach (var timer in due) timer.Due = long.MaxValue;
        }
        foreach (var timer in due) timer.Fire();
    }
    public TimeSpan? NextDelay
    {
        get { lock (_gate) return _timers.Where(t => t.Due != long.MaxValue).Select(t => (TimeSpan?)TimeSpan.FromTicks(t.Due - _ticks)).Min(); }
    }
    private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        public long Due = long.MaxValue;
        private bool _disposed;
        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (owner._gate)
            {
                if (_disposed) return false;
                if (period != Timeout.InfiniteTimeSpan) throw new NotSupportedException("Tests use one-shot timers.");
                Due = dueTime == Timeout.InfiniteTimeSpan ? long.MaxValue : owner._ticks + dueTime.Ticks;
                owner.Scheduled.Writer.TryWrite(dueTime);
                return true;
            }
        }
        public void Fire() { if (!_disposed) callback(state); }
        public void Dispose() { lock (owner._gate) { _disposed = true; Due = long.MaxValue; } }
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
