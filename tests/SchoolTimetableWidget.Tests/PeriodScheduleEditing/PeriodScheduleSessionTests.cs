using SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;

namespace SchoolTimetableWidget.Tests.PeriodScheduleEditing;

public class PeriodScheduleSessionTests
{
    [Fact]
    public void OpenCopiesPaddedValuesAndInvalidDraftNeverChangesRuntime()
    {
        var initial = PeriodScheduleContractTests.Default();
        var runtime = new RuntimePeriodSchedule(initial);
        var refreshes = 0;
        var session = new PeriodScheduleEditor(runtime, () => refreshes++).CreateSession();
        Assert.Equal(Enumerable.Range(1, 7), session.Rows.Select(r => r.PeriodNumber));
        Assert.Equal("09:00", session.Rows[0].StartText);
        Assert.Equal("09:50", session.Rows[0].EndText);
        session.Rows[0].StartText = "";
        session.Rows[6].EndText = "abc";
        Assert.Same(initial, runtime.Current);
        Assert.Equal(0, refreshes);
        session.Cancel();
        Assert.False(session.TryApply());
        Assert.Same(initial, runtime.Current);
    }

    [Theory]
    [InlineData(2, true, "", "3교시 시작")]
    [InlineData(2, false, "25:99", "3교시 종료")]
    [InlineData(6, false, "abc", "7교시 종료")]
    [InlineData(0, true, "9:00", "1교시 시작")]
    [InlineData(0, true, "09:0", "1교시 시작")]
    [InlineData(0, true, " 09:00", "1교시 시작")]
    [InlineData(0, true, "09:00:00", "1교시 시작")]
    [InlineData(0, true, "24:00", "1교시 시작")]
    [InlineData(0, true, "09:60", "1교시 시작")]
    [InlineData(0, false, "09:00", "1교시 종료")]
    [InlineData(0, false, "08:59", "1교시 종료")]
    [InlineData(4, true, "12:40", "4교시와 5교시")]
    [InlineData(4, true, "08:00", "4교시와 5교시")]
    public void OneBadFieldRejectsEntireApplyAndKeepsDraft(int row, bool start, string value, string error)
    {
        var initial = PeriodScheduleContractTests.Default();
        var runtime = new RuntimePeriodSchedule(initial);
        var refreshes = 0;
        var session = new PeriodScheduleEditor(runtime, () => refreshes++).CreateSession();
        session.Rows[3].StartText = "11:55"; // Valid earlier row must not partially commit.
        if (start) session.Rows[row].StartText = value; else session.Rows[row].EndText = value;
        Assert.False(session.TryApply());
        Assert.False(session.IsClosed);
        Assert.False(session.IsApplied);
        Assert.Contains(error, session.ErrorText);
        Assert.Equal(value, start ? session.Rows[row].StartText : session.Rows[row].EndText);
        Assert.Same(initial, runtime.Current);
        Assert.Equal(0, refreshes);
    }

    [Fact]
    public void RepeatedInvalidApplyThenCorrectionCommitsOnceAndReopenUsesLatestValues()
    {
        var initial = PeriodScheduleContractTests.Default();
        var runtime = new RuntimePeriodSchedule(initial);
        var refreshes = 0;
        var editor = new PeriodScheduleEditor(runtime, () => refreshes++);
        var session = editor.CreateSession();
        session.Rows[4].StartText = "abc";
        Assert.False(session.TryApply());
        Assert.False(session.TryApply());
        session.Rows[4].StartText = "13:00";
        session.Rows[4].EndText = "13:50";
        Assert.True(session.TryApply());
        Assert.True(session.IsApplied);
        Assert.True(session.IsClosed);
        Assert.Empty(session.ErrorText);
        Assert.Equal(1, refreshes);
        Assert.False(session.TryApply());
        session.Cancel();
        Assert.Equal(new TimeOnly(13, 0), runtime.Current.Periods[4].Start);
        var reopened = editor.CreateSession();
        Assert.Equal("13:00", reopened.Rows[4].StartText);
        reopened.Rows[4].StartText = "14:00";
        reopened.Cancel();
        Assert.Equal(new TimeOnly(13, 0), runtime.Current.Periods[4].Start);
    }

    [Fact]
    public void StaleSessionCannotOverwriteASeparateSuccessfulApply()
    {
        var runtime = new RuntimePeriodSchedule(PeriodScheduleContractTests.Default());
        var refreshes = 0;
        var editor = new PeriodScheduleEditor(runtime, () => refreshes++);
        var first = editor.CreateSession();
        var stale = editor.CreateSession();
        first.Rows[4].StartText = "13:00";
        Assert.True(first.TryApply());
        var accepted = runtime.Current;
        Assert.False(stale.TryApply());
        Assert.False(stale.IsClosed);
        Assert.NotEmpty(stale.ErrorText);
        Assert.Same(accepted, runtime.Current);
        Assert.Equal(1, refreshes);
    }
}
