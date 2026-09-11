using System.Globalization;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.DateOverrides;
using SchoolTimetableWidget.Tests.Persistence;
using SchoolTimetableWidget.Tests.Time;

namespace SchoolTimetableWidget.Tests.Timetable;

public class WeekNavigationTests
{
    [Theory]
    [InlineData("2026-09-07", "2026-09-07")]
    [InlineData("2026-09-08", "2026-09-07")]
    [InlineData("2026-09-09", "2026-09-07")]
    [InlineData("2026-09-10", "2026-09-07")]
    [InlineData("2026-09-11", "2026-09-07")]
    [InlineData("2026-09-12", "2026-09-07")]
    [InlineData("2026-09-13", "2026-09-07")]
    [InlineData("2027-01-01", "2026-12-28")]
    [InlineData("2024-02-29", "2024-02-26")]
    [InlineData("2026-10-01", "2026-09-28")]
    [InlineData("0001-01-01", "0001-01-01")]
    [InlineData("9999-12-31", "9999-12-27")]
    public void StartupUsesContainingMondayAndFiveGregorianDates(string input, string expected)
    {
        var today = DateOnly.ParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var monday = DateOnly.ParseExact(expected, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var vm = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        vm.UpdateCurrent(today, null);
        Assert.Equal(monday, vm.ViewedWeekStart);
        Assert.Equal(Enumerable.Range(0, 5).Select(monday.AddDays), vm.DisplayedDates);
        Assert.Equal(new[] { "월", "화", "수", "목", "금" }, vm.Columns.Select(c => c.WeekdayText));
        Assert.All(vm.Columns, c => Assert.Equal(7, c.Cells.Count));
        Assert.Equal(today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0 : 1,
            vm.Columns.Count(c => c.IsToday));
    }

    [Theory]
    [InlineData("2026-09-28", "2026-10-05")]
    [InlineData("2026-12-28", "2027-01-04")]
    [InlineData("2024-02-26", "2024-03-04")]
    public void NavigationIsExactlySevenDaysAndRepeatedRoundTripsAreDeterministic(string input, string next)
    {
        var start = DateOnly.ParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var vm = new WeeklyTimetableViewModel(WeeklyTimetable.Empty()); vm.UpdateCurrent(start, null);
        var cells = vm.Cells.ToArray();
        for (var i = 0; i < 5; i++)
        {
            vm.NextWeekCommand.Execute(null);
            Assert.Equal(DateOnly.ParseExact(next, "yyyy-MM-dd", CultureInfo.InvariantCulture), vm.ViewedWeekStart);
            vm.PreviousWeekCommand.Execute(null); Assert.Equal(start, vm.ViewedWeekStart);
            vm.PreviousWeekCommand.Execute(null); Assert.Equal(start.AddDays(-7), vm.ViewedWeekStart);
            vm.NextWeekCommand.Execute(null); Assert.Equal(start, vm.ViewedWeekStart);
        }
        Assert.Equal(cells, vm.Cells);
    }

    [Theory]
    [InlineData("ar-SA")]
    [InlineData("ko-KR")]
    [InlineData("en-US")]
    public void CompactDatesAreInvariantAndUnpadded(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var vm = new WeeklyTimetableViewModel(WeeklyTimetable.Empty()); vm.UpdateCurrent(DayFixtures.Monday, null);
            Assert.Equal(new[] { "9/7", "9/8", "9/9", "9/10", "9/11" }, vm.Columns.Select(c => c.DateText));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void NavigationAtDateOnlyBoundsIsDisabledWithoutThrowing()
    {
        var vm = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        Assert.False(vm.NextWeekCommand.CanExecute(null));
        vm.UpdateCurrent(DateOnly.MinValue, null);
        Assert.False(vm.PreviousWeekCommand.CanExecute(null)); vm.PreviousWeekCommand.Execute(null);
        Assert.Equal(DateOnly.MinValue, vm.ViewedWeekStart);
        var end = new WeeklyTimetableViewModel(WeeklyTimetable.Empty()); end.UpdateCurrent(DateOnly.MaxValue, null);
        Assert.False(end.NextWeekCommand.CanExecute(null)); end.NextWeekCommand.Execute(null);
        Assert.Equal(new DateOnly(9999, 12, 27), end.ViewedWeekStart);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void ExactDateOverrideAffectsOnlyItsSevenCells(int day)
    {
        var week = DayFixtures.Week(); var vm = new WeeklyTimetableViewModel(week);
        var date = DayFixtures.Monday.AddDays(day);
        var entry = new DateSpecificOverride(date, DayFixtures.Day(), null);
        vm.ConfigureDateOverrides(d => d == date ? entry : null); vm.UpdateCurrent(DayFixtures.Monday, null);
        foreach (var cell in week.Cells)
            Assert.Equal((int)cell.Day == day ? entry.Timetable![cell.PeriodNumber] : cell.Value,
                vm.Cells[(cell.PeriodNumber - 1) * 5 + (int)cell.Day].Value);
        Assert.Single(vm.Columns, c => c.IsDateOverride);
        vm.NextWeekCommand.Execute(null);
        Assert.Equal(week.Cells.Select(c => c.Value), vm.Cells.Select(c => c.Value));
        vm.PreviousWeekCommand.Execute(null); Assert.Same(entry, vm.Columns[day].DateOverride);
    }

    [Fact]
    public void AllColumnsResolveIndependentlyAndScheduleOnlyKeepsBaseProvenance()
    {
        var vm = new WeeklyTimetableViewModel(DayFixtures.Week());
        var entries = Enumerable.Range(0, 5).Select(i => new DateSpecificOverride(DayFixtures.Monday.AddDays(i),
            i == 2 ? null : new DayTimetable(Enumerable.Range(1, 7).Select(p => new TimetableCellValue($"{i}:{p}", ""))),
            DayFixtures.ShortSchedule())).ToDictionary(e => e.Date);
        var reads = new List<DateOnly>();
        vm.ConfigureDateOverrides(d => { reads.Add(d); return entries.GetValueOrDefault(d); });
        vm.UpdateCurrent(DayFixtures.Monday, null);
        Assert.Equal(vm.DisplayedDates, reads);
        Assert.Equal(4, vm.Columns.Count(c => c.IsDateOverride));
        Assert.Equal("Wednesday-1", vm.Cells[2].Value.SubjectText);
        Assert.Equal("0:1", vm.Cells[0].Value.SubjectText); Assert.Equal("4:7", vm.Cells[34].Value.SubjectText);
        var headers = vm.Columns; var cells = vm.Cells;
        for (var i = 0; i < 20; i++) vm.UpdateCurrent(DayFixtures.Monday, (SchoolDay.Monday, 1));
        Assert.Equal(5, reads.Count); Assert.Same(headers, vm.Columns); Assert.Same(cells, vm.Cells);
    }

    [Theory]
    [InlineData(-1)] [InlineData(1)]
    public void BrowsingChangesOnlyVisibleHighlightAndDatesWithNoClockRead(int direction) => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var runtime = new ProfileRuntime(new(store, new(DayFixtures.Week(), DayFixtures.Schedule(), [], true)), () => { }, _ => { });
        var clock = new FakeApplicationClock(DayFixtures.Time(9, 9, 10)); var header = new CurrentStatusHeaderViewModel();
        using var loop = new CurrentStatusRefreshLoop(clock, runtime.Resolve, header, runtime.Timetable, () => runtime.Lunch.Enabled);
        loop.RefreshNow(); var vm = runtime.Timetable;
        var text = (header.CurrentDateText, header.CurrentTimeText, header.StatusText);
        Assert.Same(vm.Cells[2], Assert.Single(vm.Cells, c => c.IsCurrent));
        Assert.Equal(new DateOnly(2026, 9, 9), Assert.Single(vm.Columns, c => c.IsToday).Date);
        (direction < 0 ? vm.PreviousWeekCommand : vm.NextWeekCommand).Execute(null);
        Assert.DoesNotContain(vm.Cells, c => c.IsCurrent); Assert.DoesNotContain(vm.Columns, c => c.IsToday);
        Assert.Equal(1, clock.ReadCount); Assert.Equal(text, (header.CurrentDateText, header.CurrentTimeText, header.StatusText));
        loop.RefreshNow(); Assert.DoesNotContain(vm.Cells, c => c.IsCurrent);
        (direction < 0 ? vm.NextWeekCommand : vm.PreviousWeekCommand).Execute(null);
        Assert.Same(vm.Cells[2], Assert.Single(vm.Cells, c => c.IsCurrent));
        Assert.Equal(2, clock.ReadCount); Assert.False(File.Exists(temp.File));
    });

    [Fact]
    public void MidnightAndMondayBoundaryNeverMoveViewedWeek() => HighlightTestDispatcher.Run(() =>
    {
        var vm = new WeeklyTimetableViewModel(DayFixtures.Week()); var header = new CurrentStatusHeaderViewModel();
        var clock = new FakeApplicationClock(DayFixtures.Time(9, 23, 59, 59));
        using var loop = new CurrentStatusRefreshLoop(clock, DayFixtures.Schedule().Periods, header, vm);
        loop.RefreshNow(); var columns = vm.Columns;
        clock.CurrentSnapshot = DayFixtures.Time(10, 0, 0); loop.RefreshNow();
        Assert.Same(columns, vm.Columns); Assert.True(vm.Columns[3].IsToday);
        clock.CurrentSnapshot = DayFixtures.Time(14, 9, 10); loop.RefreshNow();
        Assert.Equal(DayFixtures.Monday, vm.ViewedWeekStart); Assert.Equal("2026년 09월 14일", header.CurrentDateText);
        Assert.StartsWith("1교시", header.StatusText); Assert.DoesNotContain(vm.Cells, c => c.IsCurrent);
        Assert.DoesNotContain(vm.Columns, c => c.IsToday); vm.NextWeekCommand.Execute(null);
        Assert.True(vm.Columns[0].IsToday); Assert.True(vm.Cells[0].IsCurrent);
    });

    [Theory]
    [InlineData(false, -1)] [InlineData(false, 1)] [InlineData(true, -1)] [InlineData(true, 1)]
    public void EqualTextEditingCapturesSourceAndDateAcrossNavigation(bool useOverride, int direction)
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var date = DayFixtures.Monday.AddDays(direction * 7); var week = DayFixtures.Week();
        var entry = new DateSpecificOverride(date, useOverride ? DayTimetable.FromBase(week, SchoolDay.Monday) : null, DayFixtures.Schedule());
        var runtime = new ProfileRuntime(new(store, new(week, DayFixtures.Schedule(), [entry], false)), () => { }, _ => { });
        var vm = runtime.Timetable; vm.UpdateCurrent(DayFixtures.Monday, null);
        (direction < 0 ? vm.PreviousWeekCommand : vm.NextWeekCommand).Execute(null);
        var editor = vm.Editor.BeginEdit(vm.Cells[0]); var label = editor.TargetLabel;
        Assert.Contains(useOverride ? date.ToString("yyyy년 MM월 dd일", CultureInfo.InvariantCulture) : "기본 시간표", label);
        vm.NextWeekCommand.Execute(null); Assert.Equal(label, editor.TargetLabel);
        editor.SubjectText = "captured"; Assert.True(editor.TryApply());
        Assert.Equal(useOverride ? "Monday-1" : "captured", vm.CommittedTimetable[SchoolDay.Monday, 1].Value.SubjectText);
        Assert.Equal(useOverride ? "captured" : null, runtime.Overrides.Get(date)!.Timetable?[1].SubjectText);
        vm.PreviousWeekCommand.Execute(null); Assert.Equal("captured", vm.Cells[0].Value.SubjectText);
    }

    [Fact]
    public void FutureDateApplyAndRemovalRefreshVisibleColumnsImmediatelyAndBaseEditsReachOtherWeeks()
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var runtime = new ProfileRuntime(new(store, new(DayFixtures.Week(), DayFixtures.Schedule(), [], false)), () => { }, _ => { });
        var vm = runtime.Timetable; vm.UpdateCurrent(DayFixtures.Monday, null); vm.NextWeekCommand.Execute(null);
        var date = new DateOnly(2026, 9, 17); var draft = runtime.DateEditor.CreateSession(date);
        draft.UseTimetable = true; draft.TimetableRows[0].SubjectText = "future override"; Assert.True(draft.TryApply());
        Assert.Equal("future override", vm.Cells[3].Value.SubjectText);
        var edit = vm.Editor.BeginEdit(vm.Cells[3]); edit.SubjectText = "date edit"; Assert.True(edit.TryApply());
        Assert.Equal("date edit", vm.Cells[3].Value.SubjectText);
        var remove = runtime.DateEditor.CreateSession(date); remove.UseTimetable = false; Assert.True(remove.TryApply());
        Assert.False(vm.Columns[3].IsDateOverride); Assert.Equal("Thursday-1", vm.Cells[3].Value.SubjectText);
        var baseEdit = vm.Editor.BeginEdit(vm.Cells[3]); baseEdit.SubjectText = "base edit"; Assert.True(baseEdit.TryApply());
        vm.PreviousWeekCommand.Execute(null); Assert.Equal("base edit", vm.Cells[3].Value.SubjectText);
        vm.NextWeekCommand.Execute(null); Assert.Equal("base edit", vm.Cells[3].Value.SubjectText);
    }

    [Fact]
    public void RestartRestoresFutureOverrideButNotViewedWeekAndBrowsingDoesNotWrite()
    {
        using var temp = new TempProfile(); byte[] saved; DateTime modified;
        using (var store = new JsonProfileStore(temp.Directory))
        {
            var runtime = new ProfileRuntime(new(store), () => { }, _ => { });
            runtime.Timetable.UpdateCurrent(DayFixtures.Monday, null);
            var draft = runtime.DateEditor.CreateSession(new(2026, 9, 17)); draft.UseTimetable = true;
            draft.TimetableRows[0].SubjectText = "restored future"; Assert.True(draft.TryApply());
            saved = File.ReadAllBytes(temp.File); modified = File.GetLastWriteTimeUtc(temp.File);
            runtime.Timetable.NextWeekCommand.Execute(null);
            Assert.Equal("restored future", runtime.Timetable.Cells[3].Value.SubjectText);
        }
        using (var store = new JsonProfileStore(temp.Directory))
        {
            var runtime = new ProfileRuntime(new(store), () => { }, _ => { });
            runtime.Timetable.UpdateCurrent(DayFixtures.Monday, null);
            Assert.Equal(DayFixtures.Monday, runtime.Timetable.ViewedWeekStart);
            Assert.Equal("", runtime.Timetable.Cells[3].Value.SubjectText);
            runtime.Timetable.NextWeekCommand.Execute(null);
            Assert.Equal("restored future", runtime.Timetable.Cells[3].Value.SubjectText);
            Assert.Equal(new DateOnly(2026, 9, 17), runtime.Timetable.Columns[3].DateOverride!.Date);
        }
        Assert.Equal(saved, File.ReadAllBytes(temp.File)); Assert.Equal(modified, File.GetLastWriteTimeUtc(temp.File));
        using var json = System.Text.Json.JsonDocument.Parse(saved);
        Assert.Equal(4, json.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.DoesNotContain("ViewedWeek", System.Text.Encoding.UTF8.GetString(saved), StringComparison.OrdinalIgnoreCase);
    }
}
