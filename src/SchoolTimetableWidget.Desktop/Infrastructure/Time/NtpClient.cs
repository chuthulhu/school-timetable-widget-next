using System.Net.Sockets;
using SchoolTimetableWidget.Core.Time;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Time;

internal sealed record NtpSyncResult(NtpSample? Sample, string? Failure)
{
    public bool Success => Sample is not null;
}
internal interface INtpSynchronizer
{
    Task<NtpSyncResult> SynchronizeAsync(CancellationToken cancellationToken);
}

/// <summary>A bounded three-sample cycle. All await continuations are independent of WPF.</summary>
internal sealed class NtpClient(IApplicationClock clock, TimeProvider time, INtpNetwork network) : INtpSynchronizer
{
    internal const string Host = "ntp.kriss.re.kr";
    internal static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(2);
    internal static readonly TimeSpan CycleTimeout = TimeSpan.FromSeconds(20);

    public async Task<NtpSyncResult> SynchronizeAsync(CancellationToken cancellationToken)
    {
        using var deadline = new CancellationTokenSource(CycleTimeout, time);
        using var cycle = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        var token = cycle.Token;
        try
        {
            var addresses = (await Bounded(ct => network.ResolveAsync(Host, ct), token).ConfigureAwait(false))
                .Where(a => a.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6).Distinct().ToArray();
            if (addresses.Length == 0) return new(null, "DNS returned no usable addresses");
            var originTimestamp = time.GetTimestamp();
            var originUtc = clock.GetSnapshot().LocalTime.ToUniversalTime();
            var samples = new List<NtpSample>();
            string? failure = null;
            for (var sampleIndex = 0; sampleIndex < 3; sampleIndex++)
            {
                if (sampleIndex > 0) await Task.Delay(TimeSpan.FromSeconds(1), time, token).ConfigureAwait(false);
                for (var attempt = 0; attempt < Math.Min(2, addresses.Length); attempt++)
                {
                    token.ThrowIfCancellationRequested();
                    var address = addresses[(sampleIndex + attempt) % addresses.Length];
                    var t1 = originUtc + time.GetElapsedTime(originTimestamp);
                    var request = NtpPacket.CreateRequest(t1);
                    try
                    {
                        var response = await Bounded(ct => network.ExchangeAsync(address, request, ct), token).ConfigureAwait(false);
                        token.ThrowIfCancellationRequested();
                        if (response.RemoteEndPoint.Port != 123 || !response.RemoteEndPoint.Address.Equals(address))
                            throw new FormatException("Unexpected NTP endpoint.");
                        var t4 = originUtc + time.GetElapsedTime(originTimestamp, response.ReceivedTimestamp);
                        samples.Add(NtpPacket.Parse(response.Packet, request, t1, t4, response.ReceivedTimestamp));
                        break;
                    }
                    catch (Exception error) when (error is SocketException or TimeoutException or FormatException)
                    { failure = error.GetType().Name; }
                }
            }
            token.ThrowIfCancellationRequested();
            var selected = NtpSample.Select(samples);
            return new(selected, selected is null ? failure ?? "No coherent sample quorum" : null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return new(null, "Cycle timeout"); }
        catch (Exception error) when (error is SocketException or TimeoutException)
        { return new(null, error.GetType().Name); }
    }

    private async Task<T> Bounded<T>(Func<CancellationToken, Task<T>> operation, CancellationToken token)
    {
        using var timeout = new CancellationTokenSource(OperationTimeout, time);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, timeout.Token);
        try
        {
            // WaitAsync also bounds a dependency that completes late or disregards its token.
            var result = await operation(linked.Token).WaitAsync(linked.Token).ConfigureAwait(false);
            linked.Token.ThrowIfCancellationRequested();
            return result;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && timeout.IsCancellationRequested)
        { throw new TimeoutException("NTP operation timeout."); }
    }
}
