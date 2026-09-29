using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop.Infrastructure.Time;

namespace SchoolTimetableWidget.Tests.Time;

public class KrissClockTests
{
    [Theory]
    [InlineData(-7)] [InlineData(0)] [InlineData(9)]
    public void ImmediateFallbackRetainsPcOffsetThenSuccessUsesKstIndependentOfPcZone(int offset)
    {
        var time = new ManualTimeProvider();
        var fallback = new FakeApplicationClock(new(NtpPacketTests.Reference.ToOffset(TimeSpan.FromHours(offset)), ApplicationTimeSource.PcLocalFallback, 0));
        var clock = new SynchronizedApplicationClock(fallback, time);
        Assert.Equal(fallback.CurrentSnapshot.LocalTime, clock.GetSnapshot().LocalTime);
        Assert.Equal(TimeSpan.FromHours(offset), clock.GetSnapshot().LocalTime.Offset);
        Assert.Equal(ClockSyncStatus.LocalFallback, clock.Diagnostics.Status);
        clock.BeginSynchronization();
        Assert.Equal(ClockSyncStatus.Synchronizing, clock.Diagnostics.Status);
        Assert.Equal(ApplicationTimeSource.PcLocalFallback, clock.GetSnapshot().Source);
        clock.Apply(new(TimeSpan.FromHours(3), TimeSpan.FromMilliseconds(10), NtpPacketTests.Reference.AddHours(3), 0));
        Assert.Equal(ClockSyncStatus.KrissSynchronized, clock.Diagnostics.Status);
        Assert.Equal(TimeSpan.FromHours(9), clock.GetSnapshot().LocalTime.Offset);
        Assert.Equal(new TimeOnly(12, 0), clock.GetSnapshot().TimeOfDay);
        Assert.Equal(1, clock.GetSnapshot().Revision);
    }

    [Fact]
    public void GoodAnchorAdvancesMonotonicallyDespiteWallClockEditsAndResyncFailure()
    {
        var time = new ManualTimeProvider();
        var clock = Create(time);
        clock.Apply(NtpSampleSelectionTests.Sample(0, 10));
        time.Advance(TimeSpan.FromMinutes(2));
        time.Utc = time.Utc.AddDays(-4);
        clock.BeginSynchronization(); clock.Fail("Timeout");
        var snapshot = clock.GetSnapshot();
        Assert.Equal(NtpPacketTests.Reference.AddMinutes(2), snapshot.LocalTime.ToUniversalTime());
        Assert.Equal(1, snapshot.Revision);
        Assert.Equal(ClockSyncStatus.KrissSynchronized, clock.Diagnostics.Status);
        Assert.Equal("Timeout", clock.Diagnostics.LastFailure);
        Assert.Equal(NtpPacketTests.Reference, clock.Diagnostics.LastSuccessfulSync);
        Assert.Equal(TimeSpan.FromMinutes(2), clock.LastSuccessfulSyncAge);
    }

    [Fact]
    public void InitialFailureLaterSuccessAndNewProcessHaveDistinctState()
    {
        var time = new ManualTimeProvider(); var clock = Create(time);
        clock.BeginSynchronization(); clock.Fail("DNS");
        Assert.Equal(ClockSyncStatus.LocalFallback, clock.Diagnostics.Status);
        Assert.Null(clock.Diagnostics.LastSuccessfulSync);
        clock.Apply(NtpSampleSelectionTests.Sample(0, 10));
        clock.Apply(NtpSampleSelectionTests.Sample(5000, 10));
        Assert.Equal(2, clock.GetSnapshot().Revision);
        Assert.Equal(NtpPacketTests.Reference.AddSeconds(5), clock.GetSnapshot().LocalTime.ToUniversalTime());
        Assert.Null(clock.Diagnostics.LastFailure);
        Assert.Equal(0, Create(time).GetSnapshot().Revision);
    }

    [Fact]
    public async Task ConcurrentUpdatesNeverTearInstantSourceAndRevision()
    {
        var time = new ManualTimeProvider(); var clock = Create(time);
        var writer = Task.Run(() => { for (var i = 1; i <= 1000; i++) clock.Apply(NtpSampleSelectionTests.Sample(i * 1000, 10)); }, TestContext.Current.CancellationToken);
        for (var i = 0; i < 2000; i++)
        {
            var snapshot = clock.GetSnapshot();
            Assert.Equal(NtpPacketTests.Reference.AddSeconds(snapshot.Revision), snapshot.LocalTime.ToUniversalTime());
            Assert.Equal(snapshot.Revision == 0 ? ApplicationTimeSource.PcLocalFallback : ApplicationTimeSource.SynchronizedStandardTime, snapshot.Source);
        }
        await writer;
    }

    internal static SynchronizedApplicationClock Create(ManualTimeProvider time) => new(
        new FakeApplicationClock(new(NtpPacketTests.Reference, ApplicationTimeSource.PcLocalFallback, 0)), time);
}
