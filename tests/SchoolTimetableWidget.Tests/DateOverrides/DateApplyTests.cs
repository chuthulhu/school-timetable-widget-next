using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Desktop.Features.DateOverrides;
namespace SchoolTimetableWidget.Tests.DateOverrides;
public class DateApplyTests
{
    [Theory]
    [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void IndependentDraftsStartFromBaseAndCommitOnce(bool timetable, bool schedule)
    {
        var week = DayFixtures.Week(); var baseSchedule = DayFixtures.Schedule();
        var store = new RuntimeDateOverrides(); var refreshes = 0;
        var editor = new DateOverrideEditor(store, () => week, () => baseSchedule, _ => refreshes++);
        var session = editor.CreateSession(DayFixtures.Monday);
        Assert.Equal(Enumerable.Range(1, 7).Select(p => week[SchoolTimetableWidget.Core.Features.Timetable.SchoolDay.Monday, p].Value.SubjectText), session.TimetableRows.Select(r => r.SubjectText));
        Assert.Equal("14:00", session.ScheduleDraft.Rows[4].StartText);
        session.UseTimetable = timetable; session.UseSchedule = schedule;
        session.TimetableRows[0].SubjectText = " "; session.TimetableRows[0].ClassText = "한글\r\n반";
        session.ScheduleDraft.Rows[4].StartText = "13:00";
        Assert.Null(store.Get(DayFixtures.Monday));
        Assert.True(session.TryApply()); Assert.Equal(1, refreshes); Assert.False(session.TryApply());
        var entry = store.Get(DayFixtures.Monday)!;
        Assert.Equal(timetable, entry.Timetable is not null); Assert.Equal(schedule, entry.Schedule is not null);
        if (timetable) Assert.Equal(" ", entry.Timetable![1].SubjectText);
        Assert.Equal("14:00", baseSchedule.Periods[4].Start.ToString("HH:mm"));
        Assert.Equal("Monday-1", week.Cells[0].Value.SubjectText);
    }
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void InvalidEitherComponentRejectsBothAndRetainsPreviousSuccessfulState(bool invalidSchedule)
    {
        var store = new RuntimeDateOverrides(); var calls = 0;
        var editor = new DateOverrideEditor(store, DayFixtures.Week, DayFixtures.Schedule, _ => calls++);
        var first = editor.CreateSession(DayFixtures.Monday); first.UseTimetable = first.UseSchedule = true;
        Assert.True(first.TryApply()); var baseline = store.Get(DayFixtures.Monday);
        var second = editor.CreateSession(DayFixtures.Monday);
        second.TimetableRows[0].SubjectText = invalidSchedule ? "new" : null!;
        second.ScheduleDraft.Rows[4].StartText = invalidSchedule ? "25:99" : "13:00";
        Assert.False(second.TryApply()); Assert.False(second.IsClosed); Assert.NotEmpty(second.ErrorText);
        Assert.Same(baseline, store.Get(DayFixtures.Monday)); Assert.Equal(1, calls);
        second.TimetableRows[0].SubjectText = "corrected"; second.ScheduleDraft.Rows[4].StartText = "13:00";
        Assert.True(second.TryApply()); Assert.Equal(2, calls);
        Assert.Equal("corrected", store.Get(DayFixtures.Monday)!.Timetable![1].SubjectText);
    }
    [Fact]
    public void CancelAndStaleApplyPreserveOriginalAndOtherComponents()
    {
        var store = new RuntimeDateOverrides();
        var editor = new DateOverrideEditor(store, DayFixtures.Week, DayFixtures.Schedule, _ => { });
        var canceled = editor.CreateSession(DayFixtures.Monday); canceled.UseTimetable = true; canceled.Cancel();
        Assert.False(canceled.TryApply()); Assert.Null(store.Get(DayFixtures.Monday));
        var a = editor.CreateSession(DayFixtures.Monday); var b = editor.CreateSession(DayFixtures.Monday);
        a.UseSchedule = true; b.UseTimetable = true;
        Assert.True(a.TryApply()); var saved = store.Get(DayFixtures.Monday);
        Assert.False(b.TryApply()); Assert.Same(saved, store.Get(DayFixtures.Monday));
        b.Cancel(); Assert.Same(saved, store.Get(DayFixtures.Monday));
        Assert.Null(new RuntimeDateOverrides().Get(DayFixtures.Monday));
    }
}
