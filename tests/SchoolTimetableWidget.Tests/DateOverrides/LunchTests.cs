using SchoolTimetableWidget.Core.Features.CurrentStatus;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.DateOverrides;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Tests.Time;
using SchoolTimetableWidget.Tests.Timetable;
namespace SchoolTimetableWidget.Tests.DateOverrides;
public class LunchTests
{
    [Theory]
    [InlineData(false, 12, 50, 0, "쉬는시간")]
    [InlineData(true, 12, 49, 59, "4교시")]
    [InlineData(true, 12, 50, 0, "점심시간")]
    [InlineData(true, 13, 59, 59, "점심시간")]
    [InlineData(true, 14, 0, 0, "5교시")]
    [InlineData(true, 9, 50, 0, "쉬는시간")]
    [InlineData(true, 10, 50, 0, "쉬는시간")]
    [InlineData(true, 11, 50, 0, "쉬는시간")]
    [InlineData(true, 14, 50, 0, "쉬는시간")]
    [InlineData(true, 15, 50, 0, "쉬는시간")]
    public void OptionChangesOnlyFourToFiveBreak(bool enabled, int h, int m, int s, string expected)
    {
        var time = DayFixtures.Time(7, h, m, s); var schedule = DayFixtures.Schedule();
        var status = CurrentStatusResolver.Resolve(time, schedule.Periods);
        var countdown = CurrentStatusCountdownCalculator.Calculate(time, status);
        var text = CurrentStatusHeaderFormatter.Format(time, status, countdown, schedule.Periods, enabled);
        Assert.StartsWith(expected, text.StatusText);
        if (expected == "점심시간")
        {
            Assert.Equal(CurrentStatusKind.Break, status.Kind);
            Assert.Null(CurrentTimetableSlot.From(time, status));
            Assert.Equal(CurrentStatusHeaderFormatter.Format(time, status, countdown).StatusText.Replace("쉬는시간", "점심시간"), text.StatusText);
        }
    }
    [Fact]
    public void ShiftedEffectiveEndpointsAndTouchingUseIdentityNotHardCodedClockTimes()
    {
        var p = DayFixtures.Schedule().Periods.ToArray();
        p[2] = new(3, new(10, 50), new(11, 10)); p[3] = new(4, new(11, 10), new(11, 40));
        p[4] = new(5, new(13, 0), new(13, 50));
        var schedule = new PeriodSchedule(p); var time = DayFixtures.Time(7, 11, 40);
        var effective = EffectiveDayResolver.Resolve(time.Date, DayFixtures.Week(), DayFixtures.Schedule(), new(time.Date, null, schedule));
        var status = CurrentStatusResolver.Resolve(time, effective.Schedule.Periods);
        Assert.Equal("점심시간 · 5교시까지 1시간 20분", CurrentStatusHeaderFormatter.Format(time, status,
            CurrentStatusCountdownCalculator.Calculate(time, status), effective.Schedule.Periods, true).StatusText);
        p[4] = new(5, new(11, 40), new(12, 30));
        status = CurrentStatusResolver.Resolve(time, new PeriodSchedule(p).Periods);
        Assert.Equal(CurrentStatusKind.InPeriod, status.Kind);
        Assert.StartsWith("5교시", CurrentStatusHeaderFormatter.Format(time, status,
            CurrentStatusCountdownCalculator.Calculate(time, status), p, true).StatusText);
    }
    [Fact]
    public void OptionRefreshesImmediatelyAndFreshRunDefaultsOff() => HighlightTestDispatcher.Run(() =>
    {
        var clock = new FakeApplicationClock(DayFixtures.Time()); var header = new CurrentStatusHeaderViewModel();
        var vm = new WeeklyTimetableViewModel(DayFixtures.Week()); CurrentStatusRefreshLoop? loop = null;
        var option = new LunchPresentationOption(() => loop!.RefreshNow());
        using (loop = new(clock, date => EffectiveDayResolver.Resolve(date, vm.CommittedTimetable, DayFixtures.Schedule(), null), header, vm, () => option.Enabled))
        {
            Assert.False(option.Enabled); loop.RefreshNow(); Assert.StartsWith("쉬는시간", header.StatusText);
            option.Enabled = true; Assert.StartsWith("점심시간", header.StatusText); Assert.Equal(2, clock.ReadCount);
            Assert.DoesNotContain(vm.Cells, c => c.IsCurrent);
            option.Enabled = true; Assert.Equal(2, clock.ReadCount);
            option.Enabled = false; Assert.StartsWith("쉬는시간", header.StatusText); Assert.Equal(3, clock.ReadCount);
        }
        Assert.False(new LunchPresentationOption(() => { }).Enabled);
    });
}
