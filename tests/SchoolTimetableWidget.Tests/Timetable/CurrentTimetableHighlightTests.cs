using SchoolTimetableWidget.Core.Features.CurrentStatus;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop.Features.Timetable;

namespace SchoolTimetableWidget.Tests.Timetable;

public class CurrentTimetableHighlightTests
{
    [Theory]
    [InlineData(SchoolDay.Monday, 1, "국어")]
    [InlineData(SchoolDay.Friday, 7, "마지막 수업")]
    [InlineData(SchoolDay.Tuesday, 2, "")]
    [InlineData(SchoolDay.Wednesday, 2, "   ")]
    [InlineData(SchoolDay.Thursday, 4, "  <b>과목</b>\n한글  ")]
    public void IdentitySelectsExactlyOneSlotRegardlessOfRepeatedContentOrInputOrder(
        SchoolDay day, int period, string content)
    {
        var week = new WeeklyTimetable(WeeklyTimetable.Empty().Cells.Reverse().Select(
            cell => new TimetableCell(cell.Day, cell.PeriodNumber, new TimetableCellValue(content, ""))));
        var model = new WeeklyTimetableViewModel(week);
        var original = model.Cells.ToArray();
        model.SetCurrentCell((day, period));
        Assert.Same(model.Cells[(period - 1) * 5 + (int)day],
            Assert.Single(model.Cells, cell => cell.IsCurrent));
        Assert.Equal(original, model.Cells);
        Assert.All(model.Cells, cell => Assert.Equal(content, cell.DisplayText));
    }

    [Fact]
    public void SameSlotAndRepeatedClearDoNotNotifyAndMoveOnlyChangesOldAndNew()
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var changes = new List<(int Index, bool Current)>();
        for (var i = 0; i < model.Cells.Count; i++)
        {
            var index = i;
            model.Cells[i].PropertyChanged += (_, args) =>
            {
                Assert.Equal("IsCurrent", args.PropertyName);
                Assert.InRange(model.Cells.Count(cell => cell.IsCurrent), 0, 1);
                changes.Add((index, model.Cells[index].IsCurrent));
            };
        }
        model.SetCurrentCell(null);
        Assert.Empty(changes);
        model.SetCurrentCell((SchoolDay.Monday, 1));
        model.SetCurrentCell((SchoolDay.Monday, 1));
        model.SetCurrentCell((SchoolDay.Friday, 7));
        model.SetCurrentCell((SchoolDay.Friday, 7));
        model.SetCurrentCell(null);
        model.SetCurrentCell(null);
        Assert.Equal(new[] { (0, true), (0, false), (34, true), (34, false) }, changes);
        Assert.DoesNotContain(model.Cells, cell => cell.IsCurrent);
        Assert.Null(typeof(TimetableCellViewModel).GetProperty("IsCurrent")!.SetMethod);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(5, 1)]
    [InlineData(0, 0)]
    [InlineData(0, 8)]
    public void InvalidSlotFailsWithoutClearingExistingHighlight(int day, int period)
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        model.SetCurrentCell((SchoolDay.Monday, 1));
        var notifications = 0;
        foreach (var cell in model.Cells) cell.PropertyChanged += (_, _) => notifications++;
        Assert.Throws<ArgumentOutOfRangeException>("slot", () =>
            model.SetCurrentCell(((SchoolDay)day, period)));
        Assert.True(model.Cells[0].IsCurrent);
        Assert.Equal(1, model.Cells.Count(cell => cell.IsCurrent));
        Assert.Equal(0, notifications);
    }

    [Fact]
    public void ObserverSelectingAnotherSlotWhileOldCellClearsCannotLeaveTwoCurrentCells()
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        model.SetCurrentCell((SchoolDay.Monday, 1));
        model.Cells[0].PropertyChanged += (_, _) => model.SetCurrentCell((SchoolDay.Friday, 7));
        model.SetCurrentCell((SchoolDay.Tuesday, 1));
        Assert.Same(model.Cells[34], Assert.Single(model.Cells, cell => cell.IsCurrent));
    }

    [Theory]
    [InlineData(7, SchoolDay.Monday)]
    [InlineData(8, SchoolDay.Tuesday)]
    [InlineData(9, SchoolDay.Wednesday)]
    [InlineData(10, SchoolDay.Thursday)]
    [InlineData(11, SchoolDay.Friday)]
    public void LogicalWeekdayMappingUsesSnapshotLocalDate(int date, SchoolDay expected)
    {
        var snapshot = Snapshot(date, 9, 10);
        var status = CurrentStatusResolver.Resolve(snapshot, DefaultPeriodSchedule.Periods);
        Assert.Equal((expected, 1), CurrentTimetableSlot.From(snapshot, status));
    }

    [Theory]
    [InlineData(7, 8, 59, CurrentStatusKind.BeforeFirstPeriod)]
    [InlineData(7, 9, 50, CurrentStatusKind.Break)]
    [InlineData(7, 16, 50, CurrentStatusKind.AfterLastPeriod)]
    [InlineData(12, 9, 10, CurrentStatusKind.Weekend)]
    [InlineData(13, 9, 10, CurrentStatusKind.Weekend)]
    public void EveryNonPeriodStateClearsThePreviousSlot(int date, int hour, int minute, CurrentStatusKind kind)
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        model.SetCurrentCell((SchoolDay.Friday, 7));
        var snapshot = Snapshot(date, hour, minute);
        var status = CurrentStatusResolver.Resolve(snapshot, DefaultPeriodSchedule.Periods);
        Assert.Equal(kind, status.Kind);
        model.SetCurrentCell(CurrentTimetableSlot.From(snapshot, status));
        Assert.DoesNotContain(model.Cells, cell => cell.IsCurrent);
    }

    [Fact]
    public void WeekendSnapshotNeverProducesASlotEvenWhenGivenInPeriodFacts()
    {
        var snapshot = Snapshot(12, 9, 10);
        Assert.Null(CurrentTimetableSlot.From(snapshot,
            CurrentStatusResult.InPeriod(DefaultPeriodSchedule.Periods[0])));
    }

    [Fact]
    public void ProjectionRejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CurrentTimetableSlot.From(null!, CurrentStatusResult.Weekend()));
        Assert.Throws<ArgumentNullException>(() =>
            CurrentTimetableSlot.From(Snapshot(7, 9, 10), null!));
    }

    private static ApplicationTimeSnapshot Snapshot(int date, int hour, int minute) =>
        new(new DateTimeOffset(2026, 9, date, hour, minute, 0, TimeSpan.FromHours(9)),
            ApplicationTimeSource.PcLocalFallback, 0);
}
