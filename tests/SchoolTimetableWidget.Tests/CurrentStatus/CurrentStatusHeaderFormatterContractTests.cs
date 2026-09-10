using System.Globalization;
using System.Runtime.ExceptionServices;
using SchoolTimetableWidget.Core.Features.CurrentStatus;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;

namespace SchoolTimetableWidget.Tests.CurrentStatus;

public class CurrentStatusHeaderFormatterContractTests
{
    [Theory]
    [InlineData(7, 8, 42, 11, "08:42:11", "1교시까지 17분")]
    [InlineData(7, 9, 23, 18, "09:23:18", "1교시 · 종료까지 26분")]
    [InlineData(7, 9, 49, 59, "09:49:59", "1교시 · 종료까지 1분 미만")]
    [InlineData(7, 9, 50, 0, "09:50:00", "쉬는시간 · 2교시까지 10분")]
    [InlineData(7, 9, 54, 7, "09:54:07", "쉬는시간 · 2교시까지 5분")]
    [InlineData(7, 12, 50, 0, "12:50:00", "쉬는시간 · 5교시까지 1시간 10분")]
    [InlineData(7, 16, 58, 26, "16:58:26", "오늘 수업 종료")]
    [InlineData(12, 11, 24, 3, "11:24:03", "오늘은 수업이 없습니다")]
    [InlineData(13, 9, 23, 18, "09:23:18", "오늘은 수업이 없습니다")]
    public void ApprovedExamplesProvideSeparateTimeAndStatusTexts(
        int day, int hour, int minute, int second, string timeText, string statusText)
    {
        var result = ResolveAndFormat(Snapshot(new TimeOnly(hour, minute, second), day));

        Assert.Equal(timeText, result.CurrentTimeText);
        Assert.Equal(statusText, result.StatusText);
    }

    [Theory]
    [InlineData(0, 0, 0, "00:00:00")]
    [InlineData(9, 5, 7, "09:05:07")]
    [InlineData(13, 5, 7, "13:05:07")]
    [InlineData(23, 59, 59, "23:59:59")]
    public void CurrentTimeUsesPadded24HourLocalTimeWithoutFractionOrOffset(
        int hour, int minute, int second, string expected)
    {
        var time = new TimeOnly(hour, minute, second).Add(TimeSpan.FromTicks(9999999));
        var result = ResolveAndFormat(Snapshot(time));

        Assert.Equal(expected, result.CurrentTimeText);
    }

    [Theory]
    [InlineData(1L, "1분 미만")]
    [InlineData(TimeSpan.TicksPerMinute - 1, "1분 미만")]
    [InlineData(TimeSpan.TicksPerMinute, "1분")]
    [InlineData(17 * TimeSpan.TicksPerMinute + 59 * TimeSpan.TicksPerSecond, "17분")]
    [InlineData(60 * TimeSpan.TicksPerMinute, "1시간")]
    [InlineData(60 * TimeSpan.TicksPerMinute + 59 * TimeSpan.TicksPerSecond, "1시간")]
    [InlineData(61 * TimeSpan.TicksPerMinute, "1시간 1분")]
    [InlineData(70 * TimeSpan.TicksPerMinute, "1시간 10분")]
    [InlineData(120 * TimeSpan.TicksPerMinute, "2시간")]
    public void CountdownUsesCoreMeaningAndOmitsZeroUnits(long durationTicks, string expected)
    {
        var snapshot = Snapshot(new TimeOnly(7, 0));
        var period = new PeriodDefinition(3, snapshot.TimeOfDay,
            snapshot.TimeOfDay.Add(TimeSpan.FromTicks(durationTicks)));
        var status = CurrentStatusResult.InPeriod(period);
        var countdown = CurrentStatusCountdownCalculator.Calculate(snapshot, status);

        var result = CurrentStatusHeaderFormatter.Format(snapshot, status, countdown);

        Assert.Equal($"3교시 · 종료까지 {expected}", result.StatusText);
    }

    [Theory]
    [InlineData(8, 30, "4교시까지 30분")]
    [InlineData(9, 20, "4교시 · 종료까지 30분")]
    [InlineData(10, 0, "쉬는시간 · 2교시까지 1시간")]
    public void PeriodLabelsUseCurrentOrNextFactsRatherThanDefaultsOrNumberOrder(
        int hour, int minute, string expected)
    {
        PeriodDefinition[] schedule =
        [
            new(2, new TimeOnly(11, 0), new TimeOnly(11, 50)),
            new(4, new TimeOnly(9, 0), new TimeOnly(9, 50)),
        ];
        var snapshot = Snapshot(new TimeOnly(hour, minute));
        var status = CurrentStatusResolver.Resolve(snapshot, schedule);
        var countdown = CurrentStatusCountdownCalculator.Calculate(snapshot, status);

        Assert.Equal(expected, CurrentStatusHeaderFormatter.Format(snapshot, status, countdown).StatusText);
    }

    [Theory]
    [InlineData(8, 30)]
    [InlineData(9, 20)]
    [InlineData(9, 55)]
    public void AllCountdownStatesRejectMissingCountdown(int hour, int minute)
    {
        var snapshot = Snapshot(new TimeOnly(hour, minute));
        var status = CurrentStatusResolver.Resolve(snapshot, DefaultPeriodSchedule.Periods);

        Assert.Throws<ArgumentException>("countdown", () =>
            CurrentStatusHeaderFormatter.Format(snapshot, status, null));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(12)]
    public void AfterLastAndWeekendRejectUnexpectedCountdown(int day)
    {
        var earlier = Snapshot(new TimeOnly(9, 20));
        var earlierStatus = CurrentStatusResolver.Resolve(earlier, DefaultPeriodSchedule.Periods);
        var countdown = CurrentStatusCountdownCalculator.Calculate(earlier, earlierStatus);
        Assert.NotNull(countdown);
        var snapshot = Snapshot(new TimeOnly(17, 0), day);
        var status = CurrentStatusResolver.Resolve(snapshot, DefaultPeriodSchedule.Periods);

        Assert.Throws<ArgumentException>("countdown", () =>
            CurrentStatusHeaderFormatter.Format(snapshot, status, countdown));
    }

    [Fact]
    public void NullSnapshotAndStatusAreRejected()
    {
        Assert.Throws<ArgumentNullException>("snapshot", () =>
            CurrentStatusHeaderFormatter.Format(null!, CurrentStatusResult.Weekend(), null));
        Assert.Throws<ArgumentNullException>("status", () =>
            CurrentStatusHeaderFormatter.Format(Snapshot(new TimeOnly(9, 0)), null!, null));
    }

    [Theory]
    [InlineData(9)]
    [InlineData(0)]
    [InlineData(-7)]
    public void SameLocalTimeHasSameTextsRegardlessOfSourceRevisionOrOffset(int offsetHours)
    {
        foreach (var day in new[] { 7, 12 })
        {
            foreach (var time in new[] { new TimeOnly(8, 42, 11), new TimeOnly(9, 23, 18),
                new TimeOnly(12, 50), new TimeOnly(16, 58, 26) })
            {
                var fallback = Snapshot(time, day, offsetHours: offsetHours);
                var standard = Snapshot(time, day, ApplicationTimeSource.SynchronizedStandardTime, revision: 42);
                var later = Snapshot(time, day, ApplicationTimeSource.SynchronizedStandardTime, revision: 99);

                var expected = ResolveAndFormat(fallback);
                foreach (var snapshot in new[] { standard, later })
                {
                    var actual = ResolveAndFormat(snapshot);
                    Assert.Equal(expected.CurrentDateText, actual.CurrentDateText);
                    Assert.Equal(expected.CurrentTimeText, actual.CurrentTimeText);
                    Assert.Equal(expected.StatusText, actual.StatusText);
                }
            }
        }
    }

    [Theory]
    [InlineData("ko-KR")]
    [InlineData("en-US")]
    [InlineData("ar-SA")]
    public void CultureCannotChangeTimeSeparatorDigitsOrKoreanText(string cultureName)
    {
        Exception? failure = null;
        // Only a dedicated thread's cultures change; no DefaultThreadCurrentCulture mutation.
        var thread = new Thread(() =>
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var culture = new CultureInfo(cultureName);
                culture.DateTimeFormat.TimeSeparator = "~";
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                var morning = ResolveAndFormat(Snapshot(new TimeOnly(9, 5, 7)));
                Assert.Equal("2026년 09월 07일", morning.CurrentDateText);
                Assert.Equal("09:05:07", morning.CurrentTimeText);
                Assert.Equal("1교시 · 종료까지 44분", morning.StatusText);
                var afternoon = ResolveAndFormat(Snapshot(new TimeOnly(12, 50)));
                Assert.Equal("12:50:00", afternoon.CurrentTimeText);
                Assert.Equal("쉬는시간 · 5교시까지 1시간 10분", afternoon.StatusText);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        });
        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static CurrentStatusHeaderText ResolveAndFormat(ApplicationTimeSnapshot snapshot)
    {
        var status = CurrentStatusResolver.Resolve(snapshot, DefaultPeriodSchedule.Periods);
        var countdown = CurrentStatusCountdownCalculator.Calculate(snapshot, status);
        return CurrentStatusHeaderFormatter.Format(snapshot, status, countdown);
    }

    private static ApplicationTimeSnapshot Snapshot(
        TimeOnly time,
        int septemberDay = 7,
        ApplicationTimeSource source = ApplicationTimeSource.PcLocalFallback,
        int offsetHours = 9,
        long revision = 0) =>
        new(new DateTimeOffset(2026, 9, septemberDay, 0, 0, 0, TimeSpan.FromHours(offsetHours))
            .AddTicks(time.Ticks), source, revision);
}
