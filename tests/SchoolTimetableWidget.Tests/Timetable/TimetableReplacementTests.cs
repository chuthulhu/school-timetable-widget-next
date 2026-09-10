using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Tests.Timetable;

public class TimetableReplacementTests
{
    public static TheoryData<string> ExactText => new()
    {
        "", "   ", "\t \t", "  앞뒤 공백  ", "물리학\n실험 A반!",
        "\r\n첫 줄\r둘째\n\n", "한글 Ω 🎵 e\u0301 \u200d",
        "<b>과목</b> &amp; {Binding Secret}", new string('가', 2000)
    };

    [Theory]
    [MemberData(nameof(ExactText))]
    public void EverySlotCanChangeWithoutTouchingNeighboursOrOriginal(string text)
    {
        var original = new WeeklyTimetable(WeeklyTimetable.Empty().Cells.Select(cell =>
            new TimetableCell(cell.Day, cell.PeriodNumber, new TimetableCellValue("반복", ""))));
        foreach (var target in original.Cells)
        {
            var result = original.WithCellValue(target.Day, target.PeriodNumber, new TimetableCellValue(text, ""));
            Assert.Equal(35, result.Cells.Count);
            Assert.Equal(text, result[target.Day, target.PeriodNumber].Value.SubjectText);
            Assert.All(original.Cells, cell => Assert.Equal("반복", cell.Value.SubjectText));
            foreach (var other in original.Cells.Where(cell => !ReferenceEquals(cell, target)))
                Assert.Same(other, result[other.Day, other.PeriodNumber]);
        }
    }

    [Fact]
    public void InvalidReplacementCannotChangeExistingSnapshot()
    {
        var week = WeeklyTimetable.Empty();
        Assert.Throws<ArgumentNullException>(() => week.WithCellValue(SchoolDay.Monday, 1, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => week.WithCellValue((SchoolDay)5, 1, new TimetableCellValue("x", "")));
        Assert.Throws<ArgumentOutOfRangeException>(() => week.WithCellValue(SchoolDay.Monday, 0, new TimetableCellValue("x", "")));
        Assert.Throws<ArgumentOutOfRangeException>(() => week.WithCellValue(SchoolDay.Friday, 8, new TimetableCellValue("x", "")));
        Assert.All(week.Cells, cell => Assert.Equal("", cell.Value.SubjectText));
        Assert.Same(week, week.WithCellValue(SchoolDay.Monday, 1, new TimetableCellValue("", "")));
    }
}
