using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop.Features.DateOverrides;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.TimetableImport;
using SchoolTimetableWidget.Core.Features.TimetableImport;
using SchoolTimetableWidget.Tests.Time;
using SchoolTimetableWidget.Tests.Timetable;
namespace SchoolTimetableWidget.Tests.DateOverrides;

public class EffectiveRefreshTests
{
    [Fact]
    public void ApplyAndRemovalImmediatelyShareEffectiveScheduleHeaderGridAndHighlight() => HighlightTestDispatcher.Run(() =>
    {
        var week = DayFixtures.Week(); var vm = new WeeklyTimetableViewModel(week);
        var store = new RuntimeDateOverrides(); vm.ConfigureDateOverrides(store.Get); var clock = new FakeApplicationClock(DayFixtures.Time());
        var header = new CurrentStatusHeaderViewModel(); var resolutions = 0;
        using var loop = new CurrentStatusRefreshLoop(clock, date =>
        { resolutions++; return EffectiveDayResolver.Resolve(date, vm.CommittedTimetable, DayFixtures.Schedule(), store.Get(date)); }, header, vm, () => false);
        loop.RefreshNow(); Assert.StartsWith("쉬는시간", header.StatusText);
        var editor = new DateOverrideEditor(store, () => vm.CommittedTimetable, DayFixtures.Schedule, date =>
        { vm.RefreshDisplayedWeek(); if (loop.CurrentDate == date) loop.RefreshNow(); });
        var draft = editor.CreateSession(DayFixtures.Monday);
        draft.UseTimetable = draft.UseSchedule = true; draft.TimetableRows[4].SubjectText = "예외";
        draft.ScheduleDraft.Rows[4].StartText = "13:00"; draft.ScheduleDraft.Rows[4].EndText = "13:50";
        Assert.True(draft.TryApply());
        Assert.Equal(2, clock.ReadCount); Assert.Equal(2, resolutions);
        Assert.Equal("5교시 · 종료까지 40분", header.StatusText);
        Assert.Equal("예외", vm.Cells[20].Value.SubjectText);
        Assert.Same(vm.Cells[20], Assert.Single(vm.Cells, c => c.IsCurrent));
        Assert.Same(week, vm.CommittedTimetable);
        Assert.Same(store.Get(DayFixtures.Monday), loop.CurrentConfiguration!.DateOverride);
        var future = editor.CreateSession(DayFixtures.Monday.AddDays(1)); future.UseSchedule = true;
        Assert.True(future.TryApply()); Assert.Equal(2, clock.ReadCount);
        var remove = editor.CreateSession(DayFixtures.Monday); remove.UseTimetable = remove.UseSchedule = false;
        Assert.True(remove.TryApply()); Assert.Equal(3, resolutions); Assert.Equal(3, clock.ReadCount);
        Assert.StartsWith("쉬는시간", header.StatusText); Assert.DoesNotContain(vm.Cells, c => c.IsCurrent);
        Assert.Equal(week.Cells.Select(c => c.Value), vm.Cells.Select(c => c.Value));
    });
    [Fact]
    public void MidnightReadsOneSnapshotAndChangesDateScheduleAndProjectionTogether() => HighlightTestDispatcher.Run(() =>
    {
        var clock = new SequenceClock(DayFixtures.Time(16, 23, 59, 59), DayFixtures.Time(17, 0, 0));
        var week = DayFixtures.Week(); var vm = new WeeklyTimetableViewModel(week); var header = new CurrentStatusHeaderViewModel();
        var entry = new DateSpecificOverride(new(2026, 9, 17), DayFixtures.Day(), DayFixtures.ShortSchedule());
        vm.ConfigureDateOverrides(d => d == entry.Date ? entry : null);
        var dates = new List<DateOnly>();
        using var loop = new CurrentStatusRefreshLoop(clock, date =>
        { dates.Add(date); return EffectiveDayResolver.Resolve(date, week, DayFixtures.Schedule(), date == entry.Date ? entry : null); }, header, vm, () => true);
        loop.RefreshNow();
        Assert.Equal("2026년 09월 16일", header.CurrentDateText); Assert.Equal("23:59:59", header.CurrentTimeText);
        Assert.Equal("오늘 수업 종료", header.StatusText); Assert.Same(entry, vm.Columns.Single(c => c.Date == entry.Date).DateOverride);
        loop.RefreshNow();
        Assert.Equal(2, clock.Reads); Assert.Equal(new[] { new DateOnly(2026, 9, 16), entry.Date }, dates);
        Assert.Equal("2026년 09월 17일", header.CurrentDateText); Assert.Equal("00:00:00", header.CurrentTimeText);
        Assert.Equal("1교시까지 9시간", header.StatusText);
        Assert.Same(entry.Schedule, loop.CurrentConfiguration!.Schedule);
        Assert.Same(entry, vm.Columns.Single(c => c.Date == entry.Date).DateOverride); Assert.Equal("반복", vm.Cells[3].Value.SubjectText);
        Assert.DoesNotContain(vm.Cells, c => c.IsCurrent);
    });
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OpenEditorKeepsProvenanceAndDateAcrossMidnightEvenForEqualValues(bool timetableOverride) => HighlightTestDispatcher.Run(() =>
    {
        var week = DayFixtures.Week(); var store = new RuntimeDateOverrides();
        var entry = new DateSpecificOverride(DayFixtures.Monday,
            timetableOverride ? DayTimetable.FromBase(week, SchoolDay.Monday) : null, DayFixtures.ShortSchedule());
        store.TryReplace(entry.Date, null, entry);
        var clock = new FakeApplicationClock(DayFixtures.Time()); var header = new CurrentStatusHeaderViewModel();
        var vm = new WeeklyTimetableViewModel(week); vm.ConfigureDateOverrides(store.Get);
        using var loop = new CurrentStatusRefreshLoop(clock, date => EffectiveDayResolver.Resolve(date, vm.CommittedTimetable,
            DayFixtures.Schedule(), store.Get(date)), header, vm, () => false);
        vm.Editor.DateEditor = new(store, () => vm.CommittedTimetable, DayFixtures.Schedule, date =>
        { vm.RefreshDisplayedWeek(); if (loop.CurrentDate == date) loop.RefreshNow(); });
        loop.RefreshNow(); var cell = vm.Editor.BeginEdit(vm.Cells[0]);
        Assert.Contains(timetableOverride ? "2026년 09월 07일" : "기본 시간표", cell.TargetLabel);
        clock.CurrentSnapshot = DayFixtures.Time(8); loop.RefreshNow();
        cell.SubjectText = "fixed target"; Assert.True(cell.TryApply());
        if (timetableOverride)
        {
            Assert.Same(week, vm.CommittedTimetable);
            Assert.Equal("fixed target", store.Get(entry.Date)!.Timetable![1].SubjectText);
            Assert.Equal("fixed target", vm.Cells[0].Value.SubjectText);
        }
        else
        {
            Assert.Equal("fixed target", vm.CommittedTimetable[SchoolDay.Monday, 1].Value.SubjectText);
            Assert.Null(store.Get(entry.Date)!.Timetable);
        }
        Assert.Equal("2026년 09월 08일", header.CurrentDateText);
    });
    [Fact]
    public void DeletedOrReplacedTimetableRejectsOpenEditorButScheduleOnlyChangeDoesNotRetarget() => HighlightTestDispatcher.Run(() =>
    {
        var store = new RuntimeDateOverrides(); var entry = new DateSpecificOverride(DayFixtures.Monday, DayFixtures.Day(), null);
        store.TryReplace(entry.Date, null, entry);
        var editor = new DateOverrideEditor(store, DayFixtures.Week, DayFixtures.Schedule, _ => { });
        var session = editor.CreateCellSession(entry, 1, "월요일 1교시");
        var withSchedule = new DateSpecificOverride(entry.Date, entry.Timetable, DayFixtures.ShortSchedule());
        store.TryReplace(entry.Date, entry, withSchedule);
        session.SubjectText = "changed"; Assert.True(session.TryApply());
        Assert.Same(withSchedule.Schedule, store.Get(entry.Date)!.Schedule);
        var current = store.Get(entry.Date)!;
        var stale = editor.CreateCellSession(current, 1, "월요일 1교시");
        store.TryReplace(entry.Date, current, null);
        Assert.False(stale.TryApply()); Assert.Null(store.Get(entry.Date));
        var replacement = new DateSpecificOverride(entry.Date, DayFixtures.Day(), null);
        store.TryReplace(entry.Date, null, replacement);
        Assert.False(stale.TryApply()); Assert.Same(replacement, store.Get(entry.Date)); stale.Cancel();
    });
    [Fact]
    public void BaseOnlyImportAndOtherWeekdayEditingPreserveOverrideAndRevertToLatestBase() => HighlightTestDispatcher.Run(() =>
    {
        var store = new RuntimeDateOverrides(); var vm = new WeeklyTimetableViewModel(DayFixtures.Week());
        var entry = new DateSpecificOverride(DayFixtures.Monday, DayFixtures.Day(), null); store.TryReplace(entry.Date, null, entry);
        vm.ConfigureDateOverrides(store.Get); vm.UpdateCurrent(entry.Date, null);
        void Refresh() => vm.RefreshDisplayedWeek();
        vm.Editor.DateEditor = new(store, () => vm.CommittedTimetable, DayFixtures.Schedule, _ => Refresh()); Refresh();
        var other = vm.Editor.BeginEdit(vm.Cells[1]); Assert.Contains("기본 시간표", other.TargetLabel);
        other.SubjectText = "Tuesday change"; Assert.True(other.TryApply());
        Assert.Equal("Tuesday change", vm.CommittedTimetable[SchoolDay.Tuesday, 1].Value.SubjectText);
        var session = new TimetableImportActions(new NoClipboard()).CreateSession(vm, TimetableImportMode.Canonical);
        session.LoadText(CanonicalTimetableImporter.CreateTemplate()); Assert.True(session.TryApply());
        Assert.All(vm.CommittedTimetable.Cells, c => Assert.Equal("", c.Value.SubjectText));
        Assert.Equal("반복", vm.Cells[0].Value.SubjectText); Assert.Same(entry, store.Get(entry.Date));
        var remove = vm.Editor.DateEditor.CreateSession(entry.Date); remove.UseTimetable = false; Assert.True(remove.TryApply());
        Assert.All(vm.Cells, c => Assert.Equal("", c.Value.SubjectText));
    });
    [Theory]
    [InlineData(12)] [InlineData(13)]
    public void WeekendRemainsAuthoritativeWithLunchEnabled(int date) => HighlightTestDispatcher.Run(() =>
    {
        var vm = new WeeklyTimetableViewModel(DayFixtures.Week()); var header = new CurrentStatusHeaderViewModel();
        using var loop = new CurrentStatusRefreshLoop(new FakeApplicationClock(DayFixtures.Time(date)),
            d => EffectiveDayResolver.Resolve(d, vm.CommittedTimetable, DayFixtures.Schedule(), null), header, vm, () => true);
        loop.RefreshNow(); Assert.Equal("오늘은 수업이 없습니다", header.StatusText); Assert.DoesNotContain(vm.Cells, c => c.IsCurrent);
    });
    [Fact]
    public void ChangedDateAutomaticallyUsesBaseScheduleAndKeepsOldEffectiveSnapshotImmutable() => HighlightTestDispatcher.Run(() =>
    {
        var week = DayFixtures.Week(); var store = new RuntimeDateOverrides();
        var entry = new DateSpecificOverride(DayFixtures.Monday, DayFixtures.Day(), DayFixtures.ShortSchedule());
        store.TryReplace(entry.Date, null, entry);
        var vm = new WeeklyTimetableViewModel(week); vm.ConfigureDateOverrides(store.Get); var header = new CurrentStatusHeaderViewModel();
        var clock = new FakeApplicationClock(DayFixtures.Time()); var baseSchedule = DayFixtures.Schedule();
        using var loop = new CurrentStatusRefreshLoop(clock,
            date => EffectiveDayResolver.Resolve(date, week, baseSchedule, store.Get(date)), header, vm, () => true);
        loop.RefreshNow(); var captured = loop.CurrentConfiguration!;
        Assert.StartsWith("5교시", header.StatusText);
        clock.CurrentSnapshot = DayFixtures.Time(8); loop.RefreshNow();
        Assert.StartsWith("점심시간", header.StatusText); Assert.DoesNotContain(vm.Cells, c => c.IsCurrent);
        Assert.Same(baseSchedule, loop.CurrentConfiguration!.Schedule); Assert.Same(entry, vm.Columns.Single(c => c.Date == entry.Date).DateOverride);
        Assert.Equal("반복", vm.Cells[0].Value.SubjectText);
        store.TryReplace(entry.Date, entry, null);
        Assert.Same(entry, captured.DateOverride); Assert.Same(entry.Schedule, captured.Schedule);
        Assert.Equal("반복", captured.Timetable[SchoolDay.Monday, 1].Value.SubjectText);
        Assert.Equal(2, clock.ReadCount);
    });
    [Fact]
    public void ExactEmptyWhitespaceAndRepeatedFieldsRemainIndependentInProjectedCellsAndEditing() => HighlightTestDispatcher.Run(() =>
    {
        var store = new RuntimeDateOverrides(); var week = DayFixtures.Week(); var vm = new WeeklyTimetableViewModel(week);
        var values = new[] { "", " ", "\t", "반복", "반복", "한글\r\n둘째", "<b>교과</b>" }
            .Select(s => new TimetableCellValue(s, " 반 ")).ToArray();
        var entry = new DateSpecificOverride(DayFixtures.Monday, new DayTimetable(values), null);
        store.TryReplace(entry.Date, null, entry);
        vm.ConfigureDateOverrides(store.Get); vm.UpdateCurrent(entry.Date, null);
        void Refresh() => vm.RefreshDisplayedWeek();
        vm.Editor.DateEditor = new(store, () => week, DayFixtures.Schedule, _ => Refresh()); Refresh();
        for (var period = 1; period <= 7; period++)
        {
            var cell = vm.Cells[(period - 1) * 5]; Assert.Equal(values[period - 1], cell.Value);
            var edit = vm.Editor.BeginEdit(cell); Assert.Equal(values[period - 1], edit.OriginalValue);
            edit.Cancel();
        }
        var change = vm.Editor.BeginEdit(vm.Cells[15]); change.ClassText = "변경 반"; Assert.True(change.TryApply());
        Assert.Equal("변경 반", vm.Cells[15].Value.ClassText); Assert.Equal(" 반 ", vm.Cells[20].Value.ClassText);
        Assert.Same(week, vm.CommittedTimetable);
    });
    private sealed class SequenceClock(params ApplicationTimeSnapshot[] values) : IApplicationClock
    { public int Reads { get; private set; } public ApplicationTimeSnapshot GetSnapshot() => values[Reads++]; }
    private sealed class NoClipboard : SchoolTimetableWidget.Desktop.Infrastructure.Windows.ISpreadsheetClipboard
    { public string ReadText() => throw new Exception(); public void WriteText(string text) => throw new Exception(); }
}
