using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.DateOverrides;
namespace SchoolTimetableWidget.Tests.DateOverrides;
public class EffectiveDayTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void IndependentOverridesProjectOnlySelectedWeekdayAndPreserveBase(bool timetable, bool schedule)
    {
        var week = DayFixtures.Week(); var baseSchedule = DayFixtures.Schedule();
        var day = DayFixtures.Day(); var shortSchedule = DayFixtures.ShortSchedule();
        var date = new DateOnly(2026, 9, 17);
        var entry = timetable || schedule ? new DateSpecificOverride(date, timetable ? day : null, schedule ? shortSchedule : null) : null;
        var result = EffectiveDayResolver.Resolve(date, week, baseSchedule, entry);
        Assert.Equal(date, result.Date);
        Assert.Same(schedule ? shortSchedule : baseSchedule, result.Schedule);
        for (var i = 0; i < 35; i++)
        {
            var cell = week.Cells[i];
            Assert.Equal(timetable && cell.Day == SchoolDay.Thursday ? day[cell.PeriodNumber] : cell.Value, result.Timetable.Cells[i].Value);
            Assert.Equal($"{cell.Day}-{cell.PeriodNumber}", cell.Value.SubjectText);
        }
        if (!timetable) Assert.Same(week, result.Timetable);
    }
    [Theory]
    [InlineData(0)] [InlineData(6)] [InlineData(8)] [InlineData(35)]
    public void DayRejectsIncompleteOrWeeklyValues(int count) => Assert.Throws<ArgumentException>(() =>
        new DayTimetable(Enumerable.Repeat(new TimetableCellValue("", ""), count)));
    [Fact]
    public void SnapshotCopiesContainerAndPreservesExactIndependentValues()
    {
        var strings = new[] { "", " ", "\t ", "한글\r\n둘째", "<b>물리</b>", "반복", "반복" };
        var values = strings.Select(s => new TimetableCellValue(s, " 반 ")).ToArray();
        var day = new DayTimetable(values); values[0] = new("외부 변경", "");
        Assert.Equal(strings, day.Values.Select(v => v.SubjectText));
        Assert.All(day.Values, v => Assert.Equal(" 반 ", v.ClassText));
        Assert.Throws<NotSupportedException>(() => ((IList<TimetableCellValue>)day.Values)[0] = new("", ""));
        var next = day.WithCell(6, new("새 수업", "새 반"));
        Assert.Equal("반복", day[6].SubjectText); Assert.Equal("반복", next[7].SubjectText);
        Assert.Same(day, day.WithCell(6, day[6]));
        Assert.Throws<ArgumentOutOfRangeException>(() => day.WithCell(0, new("", "")));
        Assert.Throws<ArgumentNullException>(() => day.WithCell(1, null!));
        Assert.Throws<ArgumentException>(() => new DayTimetable(new TimetableCellValue[7]));
    }
    [Theory]
    [InlineData(12)] [InlineData(13)]
    public void WeekendCannotAcquireOverride(int day) => Assert.Throws<ArgumentException>(() =>
        new DateSpecificOverride(new(2026, 9, day), DayFixtures.Day(), null));
    [Fact]
    public void EmptyEntryAndMismatchedDatesRejected()
    {
        Assert.Throws<ArgumentException>(() => new DateSpecificOverride(DayFixtures.Monday, null, null));
        var entry = new DateSpecificOverride(DayFixtures.Monday, DayFixtures.Day(), null);
        Assert.Throws<ArgumentException>(() => EffectiveDayResolver.Resolve(DayFixtures.Monday.AddDays(1), DayFixtures.Week(), DayFixtures.Schedule(), entry));
        Assert.Throws<ArgumentException>(() => new RuntimeDateOverrides().TryReplace(DayFixtures.Monday.AddDays(1), null, entry));
    }
    [Theory]
    [InlineData(true, false)] [InlineData(false, true)] [InlineData(false, false)]
    public void RemovingComponentsFallsBackIndependentlyAndDoesNotAffectOtherDates(bool keepTimetable, bool keepSchedule)
    {
        var store = new RuntimeDateOverrides(); var date = DayFixtures.Monday;
        var initial = new DateSpecificOverride(date, DayFixtures.Day(), DayFixtures.ShortSchedule());
        var other = new DateSpecificOverride(date.AddDays(1), DayFixtures.Day(), DayFixtures.ShortSchedule());
        Assert.True(store.TryReplace(date, null, initial)); Assert.True(store.TryReplace(other.Date, null, other));
        var editor = new DateOverrideEditor(store, DayFixtures.Week, DayFixtures.Schedule, _ => { });
        var draft = editor.CreateSession(date); draft.UseTimetable = keepTimetable; draft.UseSchedule = keepSchedule;
        Assert.True(draft.TryApply());
        var week = DayFixtures.Week(); var schedule = DayFixtures.Schedule();
        var resolved = EffectiveDayResolver.Resolve(date, week, schedule, store.Get(date));
        Assert.Equal(keepTimetable, store.Get(date)?.Timetable is not null);
        Assert.Equal(keepSchedule, store.Get(date)?.Schedule is not null);
        if (!keepTimetable) Assert.Same(week, resolved.Timetable);
        if (!keepSchedule) Assert.Same(schedule, resolved.Schedule);
        if (!keepTimetable && !keepSchedule) Assert.Null(store.Get(date));
        Assert.Same(other, store.Get(other.Date));
        Assert.Null(store.Get(date.AddDays(2)));
    }
}
