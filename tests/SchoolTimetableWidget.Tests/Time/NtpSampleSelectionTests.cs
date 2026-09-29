using SchoolTimetableWidget.Desktop.Infrastructure.Time;

namespace SchoolTimetableWidget.Tests.Time;

public class NtpSampleSelectionTests
{
    [Theory]
    [InlineData(0, 10, 20, 10)]
    [InlineData(3600000, 3600010, 3600020, 3600010)]
    [InlineData(-3600000, -3599990, -3599980, -3599990)]
    [InlineData(1000000, 10, 20, 10)]
    public void UniqueCoherentGroupUsesLowestDelayMember(double a, double b, double c, double expected)
    {
        var selected = NtpSample.Select([Sample(a, 30), Sample(b, 10), Sample(c, 20)]);
        Assert.NotNull(selected);
        Assert.Equal(expected, selected.Offset.TotalMilliseconds);
    }

    [Fact] public void SingletonFails() => Assert.Null(NtpSample.Select([Sample(0, 10)]));
    [Fact] public void EmptyFails() => Assert.Null(NtpSample.Select([]));
    [Fact] public void IncoherentPairFails() => Assert.Null(NtpSample.Select([Sample(0, 10), Sample(251, 10)]));
    [Fact] public void AmbiguousChainFails() => Assert.Null(NtpSample.Select([Sample(0, 10), Sample(200, 10), Sample(400, 10)]));
    [Fact] public void HighDelayExcluded() => Assert.Null(NtpSample.Select([Sample(0, 1001), Sample(0, 10)]));
    [Fact] public void NegativeDelayExcluded() => Assert.Null(NtpSample.Select([Sample(0, -1), Sample(0, 10)]));
    [Fact] public void BoundaryCoherenceAccepted() => Assert.NotNull(NtpSample.Select([Sample(0, 1000), Sample(250, 1000)]));

    internal static NtpSample Sample(double offsetMs, double delayMs) => new(TimeSpan.FromMilliseconds(offsetMs),
        TimeSpan.FromMilliseconds(delayMs), NtpPacketTests.Reference.AddMilliseconds(offsetMs), 0);
}
