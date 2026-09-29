using System.Net;
using System.Net.Sockets;
using SchoolTimetableWidget.Desktop.Infrastructure.Time;

namespace SchoolTimetableWidget.Tests.Time;

public class NtpClientTests
{
    [Fact]
    public async Task ThreeCoherentSamplesAndFreshDnsEachCycle()
    {
        var time = new ManualTimeProvider(); var network = new FakeNetwork(time);
        var client = Client(time, network);
        Assert.True((await CompleteCycle(client, time, network)).Success);
        Assert.Equal(3, network.Requests);
        Assert.True((await CompleteCycle(client, time, network)).Success);
        Assert.Equal(2, network.Resolutions);
        Assert.Equal("ntp.kriss.re.kr", network.Host);
    }

    [Theory]
    [InlineData("dns")] [InlineData("socket")] [InlineData("malformed")] [InlineData("endpoint")] [InlineData("port")]
    public async Task ExpectedFailuresNeverBecomeSyncSuccess(string failure)
    {
        var time = new ManualTimeProvider(); var network = new FakeNetwork(time) { Failure = failure };
        var result = await CompleteCycle(Client(time, network), time, network);
        Assert.False(result.Success);
        Assert.Null(result.Sample);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task FirstAddressFailureFallsBackToSecond()
    {
        var time = new ManualTimeProvider(); var network = new FakeNetwork(time) { Failure = "first-address" };
        Assert.True((await CompleteCycle(Client(time, network), time, network)).Success);
        Assert.Contains(IPAddress.IPv6Loopback, network.Attempted);
        Assert.InRange(network.Requests, 3, 6);
    }

    [Fact]
    public async Task MalformedSingleSampleDoesNotDiscardTwoValidSamples()
    {
        var time = new ManualTimeProvider(); var network = new FakeNetwork(time) { Failure = "one-malformed", Addresses = [IPAddress.Loopback] };
        Assert.True((await CompleteCycle(Client(time, network), time, network)).Success);
    }

    [Theory]
    [InlineData("singleton")] [InlineData("inconsistent")] [InlineData("high-delay")]
    public async Task NoQualityQuorumLeavesCycleUnsuccessful(string failure)
    {
        var time = new ManualTimeProvider();
        var network = new FakeNetwork(time) { Failure = failure, Addresses = [IPAddress.Loopback] };
        Assert.False((await CompleteCycle(Client(time, network), time, network)).Success);
    }

    [Fact]
    public async Task BlockedUdpRequestsTimeoutAndAllAddressesAreBounded()
    {
        var time = new ManualTimeProvider(); var network = new FakeNetwork(time) { Block = true };
        var task = Client(time, network).SynchronizeAsync(TestContext.Current.CancellationToken);
        await network.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var nextRequest = network.Attempts.Reader.ReadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, await nextRequest);
        time.Advance(TimeSpan.FromSeconds(2));
        Assert.Equal(2, await network.Attempts.Reader.ReadAsync(TestContext.Current.CancellationToken).AsTask().WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        // The whole-cycle deadline additionally bounds all remaining attempts and sample gaps.
        time.Advance(TimeSpan.FromSeconds(18));
        var result = await task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.False(result.Success);
        Assert.InRange(network.Requests, 1, 6);
        network.Release.TrySetResult();
    }

    [Fact]
    public async Task WallClockChangeDuringCycleDoesNotAlterCorrection()
    {
        var time = new ManualTimeProvider(); var network = new FakeNetwork(time) { ChangeWall = true };
        var result = await CompleteCycle(Client(time, network), time, network);
        Assert.True(result.Success);
        Assert.InRange(result.Sample!.Offset.TotalMilliseconds, 999.999, 1000.001);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CancellationRejectsLateResponseEvenWhenDependencyIgnoresCancellation(bool dns)
    {
        var time = new ManualTimeProvider(); var network = new FakeNetwork(time) { Block = true, BlockDns = dns };
        using var cancel = new CancellationTokenSource();
        var task = Client(time, network).SynchronizeAsync(cancel.Token);
        await network.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        network.Release.TrySetResult();
    }

    [Fact]
    public async Task DnsTimeoutIsBoundedByInjectedTime()
    {
        var time = new ManualTimeProvider(); var network = new FakeNetwork(time) { Block = true, BlockDns = true };
        var task = Client(time, network).SynchronizeAsync(TestContext.Current.CancellationToken);
        await network.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(2));
        var result = await task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.False(result.Success);
        network.Release.TrySetResult();
    }

    internal static NtpClient Client(ManualTimeProvider time, FakeNetwork network) => new(new ProviderFallback(time), time, network);
    private sealed class ProviderFallback(ManualTimeProvider time) : SchoolTimetableWidget.Core.Time.IApplicationClock
    {
        public SchoolTimetableWidget.Core.Time.ApplicationTimeSnapshot GetSnapshot() =>
            new(time.GetUtcNow(), SchoolTimetableWidget.Core.Time.ApplicationTimeSource.PcLocalFallback, 0);
    }
    internal static async Task<NtpSyncResult> CompleteCycle(NtpClient client, ManualTimeProvider time, FakeNetwork network)
    {
        var task = client.SynchronizeAsync(default);
        // Await each actual delay installation instead of racing fake-time advancement.
        while (!task.IsCompleted)
        {
            var scheduled = time.Scheduled.Reader.ReadAsync().AsTask();
            var completed = await Task.WhenAny(task, scheduled).WaitAsync(TimeSpan.FromSeconds(10));
            if (completed == task) break;
            if (await scheduled == TimeSpan.FromSeconds(1)) time.Advance(TimeSpan.FromSeconds(1));
        }
        return await task;
    }

    internal sealed class FakeNetwork(ManualTimeProvider time) : INtpNetwork
    {
        public string? Failure; public bool ChangeWall; public bool Block; public bool BlockDns;
        public IPAddress[] Addresses = [IPAddress.Loopback, IPAddress.IPv6Loopback];
        public int Requests; public int Resolutions; public string? Host;
        public List<IPAddress> Attempted = [];
        public readonly System.Threading.Channels.Channel<int> Attempts = System.Threading.Channels.Channel.CreateUnbounded<int>();
        public TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken)
        {
            Host = host; Resolutions++;
            if (Block && BlockDns) { Entered.TrySetResult(); await Release.Task; }
            if (Failure == "dns") throw new SocketException();
            return Addresses;
        }
        public async Task<NtpDatagram> ExchangeAsync(IPAddress address, byte[] request, CancellationToken cancellationToken)
        {
            Requests++; Attempted.Add(address);
            Attempts.Writer.TryWrite(Requests);
            if (Block && !BlockDns) { Entered.TrySetResult(); await Release.Task; }
            if (Failure == "socket" || (Failure == "first-address" && address.Equals(IPAddress.Loopback))) throw new SocketException();
            var t1 = NtpPacket.ReadTimestamp(request.AsSpan(40), NtpPacketTests.Reference);
            var serverDelta = Failure == "inconsistent" ? Requests * 1000 : 1050;
            var response = NtpPacketTests.Response(request, t1.AddMilliseconds(serverDelta), t1.AddMilliseconds(serverDelta));
            time.Advance(TimeSpan.FromMilliseconds(Failure == "high-delay" ? 1500 : 100));
            if (ChangeWall) time.Utc = time.Utc.AddDays(3);
            if (Failure == "malformed" || (Failure == "one-malformed" && Requests == 1) || (Failure == "singleton" && Requests > 1)) response = [];
            var result = new NtpDatagram(response, new IPEndPoint(Failure == "endpoint" ? IPAddress.Any : address, Failure == "port" ? 124 : 123), time.GetTimestamp());
            return result;
        }
    }
}
