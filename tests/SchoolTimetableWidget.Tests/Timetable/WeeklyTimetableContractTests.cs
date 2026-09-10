using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Tests.Timetable;

public class WeeklyTimetableContractTests
{
    [Fact]
    public void EmptyWeekContainsAllThirtyFiveIndependentSlots()
    {
        var week = WeeklyTimetable.Empty();
        Assert.Equal(35, week.Cells.Count);
        Assert.Equal(5, week.Cells.Select(cell => cell.Day).Distinct().Count());
        Assert.Equal(7, week.Cells.Select(cell => cell.PeriodNumber).Distinct().Count());
        Assert.Equal(35, week.Cells.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.All(week.Cells, cell => Assert.Equal(string.Empty, cell.Content));
        foreach (var day in Enum.GetValues<SchoolDay>())
        foreach (var period in Enumerable.Range(1, 7))
        {
            var cell = week[day, period];
            Assert.Equal(day, cell.Day);
            Assert.Equal(period, cell.PeriodNumber);
            Assert.Same(cell, week.Cells[(period - 1) * 5 + (int)day]);
        }
    }

    [Fact]
    public void ConstructionCopiesAndOrdersInputWithoutChangingTheCaller()
    {
        var input = WeeklyTimetable.Empty().Cells.Reverse().ToArray();
        var original = input.ToArray();
        var week = new WeeklyTimetable(input);
        Assert.Equal(original, input);
        input[0] = new TimetableCell(SchoolDay.Friday, 7, "changed");
        Assert.Equal(string.Empty, week[SchoolDay.Friday, 7].Content);
        Assert.Equal(SchoolDay.Monday, week.Cells[0].Day);
        Assert.Equal(1, week.Cells[0].PeriodNumber);
        Assert.Equal(SchoolDay.Friday, week.Cells[34].Day);
        Assert.Equal(7, week.Cells[34].PeriodNumber);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<TimetableCell>)week.Cells)[0] = input[0]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("  앞뒤 공백  ")]
    [InlineData("물리학\n실험 A반!")]
    [InlineData("\n첫 줄\r\n둘째 줄\n")]
    [InlineData("한글 Ω 🎵 e\u0301")]
    [InlineData("<b>과목</b> &amp; {Binding Secret}")]
    public void TextIsPreservedExactlyAndRepeatedValuesRemainSeparate(string content)
    {
        var week = new WeeklyTimetable(WeeklyTimetable.Empty().Cells.Select(
            cell => new TimetableCell(cell.Day, cell.PeriodNumber, content)));
        Assert.All(week.Cells, cell => Assert.Equal(content, cell.Content));
        Assert.NotSame(week[SchoolDay.Monday, 1], week[SchoolDay.Tuesday, 1]);
        Assert.NotSame(week[SchoolDay.Monday, 1], week[SchoolDay.Monday, 2]);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(5, 1)]
    [InlineData(int.MaxValue, 1)]
    [InlineData(0, 0)]
    [InlineData(0, 8)]
    [InlineData(0, int.MinValue)]
    public void InvalidSlotsCannotBeConstructedOrAccessed(int day, int period)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimetableCell((SchoolDay)day, period, ""));
        var week = WeeklyTimetable.Empty();
        Assert.Throws<ArgumentOutOfRangeException>(() => week[(SchoolDay)day, period]);
    }

    [Fact]
    public void NullContentAndNullInputsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new TimetableCell(SchoolDay.Monday, 1, null!));
        Assert.Throws<ArgumentNullException>(() => new WeeklyTimetable(null!));
        var cells = WeeklyTimetable.Empty().Cells.ToArray();
        cells[0] = null!;
        Assert.Throws<ArgumentNullException>(() => new WeeklyTimetable(cells));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(34)]
    public void IncompleteWeeksAreRejectedRatherThanImplicitlyFilled(int count) =>
        Assert.Throws<ArgumentException>(() => new WeeklyTimetable(WeeklyTimetable.Empty().Cells.Take(count)));

    [Fact]
    public void DuplicateSlotsCannotHideAMissingSlotOrAddAnExtraCell()
    {
        var cells = WeeklyTimetable.Empty().Cells.ToArray();
        cells[34] = cells[0];
        Assert.Throws<ArgumentException>(() => new WeeklyTimetable(cells));
        Assert.Throws<ArgumentException>(() => new WeeklyTimetable(
            WeeklyTimetable.Empty().Cells.Append(cells[0])));
    }

    [Fact]
    public void InputIsEnumeratedOnlyOnce()
    {
        var enumerations = 0;
        IEnumerable<TimetableCell> Input()
        {
            Assert.Equal(1, ++enumerations);
            foreach (var cell in WeeklyTimetable.Empty().Cells) yield return cell;
        }
        var week = new WeeklyTimetable(Input());
        Assert.Equal(35, week.Cells.Count);
        Assert.Equal(1, enumerations);
    }
}
