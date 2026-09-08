using SchoolTimetableWidget.Core.Time;

namespace SchoolTimetableWidget.Tests.Time;

public class ApplicationClockContractTests
{
    [Theory]
    [InlineData(9)]
    [InlineData(0)]
    [InlineData(-7)]
    public void FakeClockPreservesExactInstantAndFallbackLocalOffset(int offsetHours)
    {
        var expected = new DateTimeOffset(2026, 9, 8, 0, 0, 0, TimeSpan.FromHours(offsetHours)).AddTicks(1234567);
        IApplicationClock clock = new FakeApplicationClock(
            new ApplicationTimeSnapshot(expected, ApplicationTimeSource.PcLocalFallback, 0));

        var snapshot = clock.GetSnapshot();

        Assert.True(expected.EqualsExact(snapshot.LocalTime));
        Assert.Equal(new DateOnly(2026, 9, 8), snapshot.Date);
        Assert.Equal(new TimeOnly(1234567), snapshot.TimeOfDay);
        Assert.Equal(ApplicationTimeSource.PcLocalFallback, snapshot.Source);
        Assert.Equal(0, snapshot.Revision);
    }

    [Fact]
    public void SynchronizedSnapshotUsesKstDateEvenWhenUtcDateDiffers()
    {
        var localTime = new DateTimeOffset(2026, 9, 8, 0, 1, 2, TimeSpan.FromHours(9));
        var snapshot = new ApplicationTimeSnapshot(localTime, ApplicationTimeSource.SynchronizedStandardTime, 1);

        Assert.Equal(new DateTimeOffset(2026, 9, 7, 15, 1, 2, TimeSpan.Zero), snapshot.LocalTime.ToUniversalTime());
        Assert.Equal(new DateOnly(2026, 9, 8), snapshot.Date);
        Assert.Equal(DayOfWeek.Tuesday, snapshot.Date.DayOfWeek);
        Assert.Equal(new TimeOnly(0, 1, 2), snapshot.TimeOfDay);
        Assert.Equal(ApplicationTimeSource.SynchronizedStandardTime, snapshot.Source);
        Assert.Equal(1, snapshot.Revision);
    }

    [Theory]
    [InlineData(ApplicationTimeSource.PcLocalFallback, 0, ApplicationTimeSource.SynchronizedStandardTime, 1)]
    [InlineData(ApplicationTimeSource.SynchronizedStandardTime, 1, ApplicationTimeSource.SynchronizedStandardTime, 2)]
    [InlineData(ApplicationTimeSource.SynchronizedStandardTime, 2, ApplicationTimeSource.PcLocalFallback, 3)]
    public void ReferenceIdentityCanChangeWithoutChangingTheInstant(
        ApplicationTimeSource firstSource, long firstRevision,
        ApplicationTimeSource secondSource, long secondRevision)
    {
        var localTime = new DateTimeOffset(2026, 9, 8, 9, 0, 0, TimeSpan.FromHours(9));
        var clock = new FakeApplicationClock(new ApplicationTimeSnapshot(localTime, firstSource, firstRevision));
        var first = clock.GetSnapshot();
        clock.CurrentSnapshot = new ApplicationTimeSnapshot(localTime, secondSource, secondRevision);

        var second = clock.GetSnapshot();

        Assert.True(first.LocalTime.EqualsExact(second.LocalTime));
        Assert.Equal((firstSource, firstRevision), (first.Source, first.Revision));
        Assert.Equal((secondSource, secondRevision), (second.Source, second.Revision));
        Assert.NotEqual((first.Source, first.Revision), (second.Source, second.Revision));
    }

    [Fact]
    public void AdvancingTimeDoesNotRequireANewReferenceRevision()
    {
        var localTime = new DateTimeOffset(2026, 9, 8, 9, 0, 0, TimeSpan.FromHours(9));
        var clock = new FakeApplicationClock(new ApplicationTimeSnapshot(localTime, ApplicationTimeSource.PcLocalFallback, 0));
        var first = clock.GetSnapshot();
        clock.CurrentSnapshot = new ApplicationTimeSnapshot(localTime.AddSeconds(1), first.Source, first.Revision);

        var second = clock.GetSnapshot();

        Assert.Equal(first.LocalTime.AddSeconds(1), second.LocalTime);
        Assert.Equal((first.Source, first.Revision), (second.Source, second.Revision));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RelatedCalculationsKeepOneSnapshotAcrossMidnightOrSourceTransition(bool switchSource)
    {
        var beforeMidnight = new DateTimeOffset(2026, 9, 11, 23, 59, 59, TimeSpan.FromHours(9));
        var clock = new FakeApplicationClock(new ApplicationTimeSnapshot(beforeMidnight, ApplicationTimeSource.PcLocalFallback, 0));
        var captured = clock.GetSnapshot();

        // Simulate a transition after capture, before related calculations finish.
        clock.CurrentSnapshot = new ApplicationTimeSnapshot(
            beforeMidnight.AddSeconds(1),
            switchSource ? ApplicationTimeSource.SynchronizedStandardTime : ApplicationTimeSource.PcLocalFallback,
            switchSource ? 1 : 0);

        var facts = ReadDateAndTime(captured);
        var identity = ReadReference(captured);

        Assert.Equal((new DateOnly(2026, 9, 11), new TimeOnly(23, 59, 59)), facts);
        Assert.Equal((ApplicationTimeSource.PcLocalFallback, 0L), identity);
        Assert.Equal(1, clock.ReadCount);

        var nextCalculation = clock.GetSnapshot();
        Assert.Equal((new DateOnly(2026, 9, 12), new TimeOnly(0, 0)), ReadDateAndTime(nextCalculation));
        Assert.Equal((clock.CurrentSnapshot.Source, clock.CurrentSnapshot.Revision), ReadReference(nextCalculation));
    }

    [Fact]
    public void SnapshotRejectsUnknownSource()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ApplicationTimeSnapshot(DateTimeOffset.UnixEpoch, (ApplicationTimeSource)42, 0));
    }

    [Fact]
    public void SnapshotRejectsNegativeRevision()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ApplicationTimeSnapshot(DateTimeOffset.UnixEpoch, ApplicationTimeSource.PcLocalFallback, -1));
    }

    [Fact]
    public void SynchronizedSnapshotRejectsNonKstRepresentation()
    {
        Assert.Throws<ArgumentException>(() =>
            new ApplicationTimeSnapshot(DateTimeOffset.UnixEpoch, ApplicationTimeSource.SynchronizedStandardTime, 1));
    }

    // Test-only consumers demonstrate passing facts without implementing a school feature.
    private static (DateOnly, TimeOnly) ReadDateAndTime(ApplicationTimeSnapshot snapshot) =>
        (snapshot.Date, snapshot.TimeOfDay);

    private static (ApplicationTimeSource, long) ReadReference(ApplicationTimeSnapshot snapshot) =>
        (snapshot.Source, snapshot.Revision);
}
