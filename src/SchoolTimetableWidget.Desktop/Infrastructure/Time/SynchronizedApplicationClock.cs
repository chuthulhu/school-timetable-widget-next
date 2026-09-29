using SchoolTimetableWidget.Core.Time;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Time;

internal enum ClockSyncStatus { LocalFallback, Synchronizing, KrissSynchronized }
internal sealed record ClockSyncDiagnostics(ClockSyncStatus Status, DateTimeOffset? LastSuccessfulSync, string? LastFailure);

/// <summary>Process-local reference, with an atomic immutable source/anchor/revision.</summary>
internal sealed class SynchronizedApplicationClock(IApplicationClock fallback, TimeProvider time) : IApplicationClock
{
    private sealed record Reference(NtpSample? Anchor, long Revision, ClockSyncDiagnostics Diagnostics);
    private readonly object _gate = new();
    private Reference _reference = new(null, 0, new(ClockSyncStatus.LocalFallback, null, null));
    public ClockSyncDiagnostics Diagnostics => Volatile.Read(ref _reference).Diagnostics;
    public TimeSpan? LastSuccessfulSyncAge => Volatile.Read(ref _reference).Anchor is { } anchor
        ? time.GetElapsedTime(anchor.ReceivedTimestamp) : null;

    public ApplicationTimeSnapshot GetSnapshot()
    {
        var reference = Volatile.Read(ref _reference);
        if (reference.Anchor is not { } anchor) return fallback.GetSnapshot();
        var utc = anchor.UtcAtReceive + time.GetElapsedTime(anchor.ReceivedTimestamp);
        return new(utc.ToOffset(TimeSpan.FromHours(9)), ApplicationTimeSource.SynchronizedStandardTime, reference.Revision);
    }

    public void BeginSynchronization()
    {
        lock (_gate) Volatile.Write(ref _reference, _reference with
        { Diagnostics = _reference.Diagnostics with { Status = ClockSyncStatus.Synchronizing } });
    }

    public void Apply(NtpSample sample)
    {
        lock (_gate) Volatile.Write(ref _reference, new(sample, _reference.Revision + 1,
            new(ClockSyncStatus.KrissSynchronized, sample.UtcAtReceive, null)));
    }

    public void Fail(string reason)
    {
        lock (_gate) Volatile.Write(ref _reference, _reference with
        { Diagnostics = _reference.Diagnostics with
            { Status = _reference.Anchor is null ? ClockSyncStatus.LocalFallback : ClockSyncStatus.KrissSynchronized, LastFailure = reason } });
    }
}
