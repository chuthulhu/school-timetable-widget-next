using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.Timetable;

namespace SchoolTimetableWidget.Tests.Timetable;

public class WeeklyTimetablePresentationTests
{
    [Fact]
    public void HeadersUseKoreanWeekdaysAndPlainPeriodNumbersInOrder()
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        Assert.Equal(new[] { "월", "화", "수", "목", "금" }, model.WeekdayHeaders);
        Assert.Equal(new[] { "1", "2", "3", "4", "5", "6", "7" }, model.PeriodHeaders);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)model.WeekdayHeaders)[0] = "changed");
    }

    [Fact]
    public void BodyOrderIsPeriodThenDayRegardlessOfInputOrder()
    {
        var week = new WeeklyTimetable(WeeklyTimetable.Empty().Cells.Reverse().Select(cell =>
            new TimetableCell(cell.Day, cell.PeriodNumber, $"{cell.PeriodNumber}:{cell.Day}")));
        var model = new WeeklyTimetableViewModel(week);
        Assert.Equal(35, model.Cells.Count);
        Assert.Equal(week.Cells.Select(cell => cell.Content), model.Cells.Select(cell => cell.Content));
        Assert.Equal("1:Monday", model.Cells[0].Content);
        Assert.Equal("2:Monday", model.Cells[5].Content);
        Assert.Equal("7:Friday", model.Cells[34].Content);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<TimetableCellViewModel>)model.Cells)[0] = new("changed"));
    }

    [Fact]
    public void RepeatedTextIsPreservedInThirtyFiveIndependentPresentationObjects()
    {
        const string text = "  <b>과목</b>\n한글 Ω 🎵  ";
        var week = new WeeklyTimetable(WeeklyTimetable.Empty().Cells.Select(cell =>
            new TimetableCell(cell.Day, cell.PeriodNumber, text)));
        var model = new WeeklyTimetableViewModel(week);
        Assert.All(model.Cells, cell => Assert.Equal(text, cell.Content));
        Assert.Equal(35, model.Cells.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.NotSame(model.Cells[0], model.Cells[1]);
        Assert.NotSame(model.Cells[0], model.Cells[5]);
    }

    [Fact]
    public void NullPresentationInputsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new WeeklyTimetableViewModel(null!));
        Assert.Throws<ArgumentNullException>(() => new TimetableCellViewModel(null!));
    }
}
