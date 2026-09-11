using System.Text;
using System.Windows;
using System.Windows.Controls;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Features.TimetableImport;
using SchoolTimetableWidget.Desktop;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Features.TimetableImport;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using SchoolTimetableWidget.Tests.Timetable;
using SchoolTimetableWidget.Tests.Time;

namespace SchoolTimetableWidget.Tests.Persistence;

public class ProfileTransactionTests
{
    [Theory]
    [InlineData("cell", false)] [InlineData("cell", true)]
    [InlineData("bulk", false)] [InlineData("bulk", true)]
    [InlineData("period", false)] [InlineData("period", true)]
    [InlineData("date", false)] [InlineData("date", true)]
    [InlineData("dateCell", false)] [InlineData("dateCell", true)]
    [InlineData("remove", false)] [InlineData("remove", true)]
    [InlineData("lunch", false)] [InlineData("lunch", true)]
    public void AllUserPathsSaveBeforeRuntimePublishAndRetainDraftOnFailure(string action, bool fail)
    {
        using var temp = new TempProfile();
        var shouldFail = false; var renames = 0;
        using var store = new JsonProfileStore(temp.Directory, stage =>
        {
            if (shouldFail && stage == ProfileWriteStage.PartialWrite) throw new IOException("injected");
            if (stage == ProfileWriteStage.BeforeRename) renames++;
        });
        var session = new ProfileSession(store, ProfileStorageTests.Sample());
        Assert.Null(session.SaveLunch(true));
        var runtime = new ProfileRuntime(session, () => { }, _ => { });
        runtime.Timetable.UpdateCurrent(ProfileStorageTests.Monday, null);
        var before = File.ReadAllBytes(temp.File);
        var beforeSnapshot = session.Current;
        var beforeTimetable = runtime.Timetable.CommittedTimetable;
        var beforeSchedule = runtime.Schedule.Current;
        var beforeOverride = runtime.Overrides.Get(ProfileStorageTests.Monday);
        var beforeLunch = runtime.Lunch.Enabled;
        var writesBefore = renames;
        shouldFail = fail;
        // Notifications must never precede the complete durable profile.
        runtime.Timetable.ContentChanged += (_, _) => Assert.Equal(File.ReadAllBytes(temp.File), ProfileJson.Serialize(Capture(runtime)));
        var (accepted, retry) = Apply(runtime, action);
        Assert.Equal(!fail, accepted);
        Assert.Equal(writesBefore + (fail ? 0 : 1), renames);
        if (fail)
        {
            Assert.Same(beforeSnapshot, session.Current);
            Assert.Same(beforeTimetable, runtime.Timetable.CommittedTimetable);
            Assert.Same(beforeSchedule, runtime.Schedule.Current);
            Assert.Same(beforeOverride, runtime.Overrides.Get(ProfileStorageTests.Monday));
            Assert.Equal(beforeLunch, runtime.Lunch.Enabled);
            Assert.Equal(before, File.ReadAllBytes(temp.File));
            shouldFail = false;
            Assert.True(retry());
            Assert.Equal(writesBefore + 1, renames);
        }
        Assert.Equal(ProfileJson.Serialize(session.Current), File.ReadAllBytes(temp.File));
        Assert.Equal(ProfileJson.Serialize(session.Current), ProfileJson.Serialize(Capture(runtime)));
    }

    private static (bool Accepted, Func<bool> Retry) Apply(ProfileRuntime runtime, string action)
    {
        switch (action)
        {
            case "cell":
            case "dateCell":
                var cell = runtime.Timetable.Editor.BeginEdit(runtime.Timetable.Cells[action == "cell" ? 1 : 0]);
                cell.SubjectText = " 화학\n "; cell.ClassText = "3-2";
                var cellResult = cell.TryApply();
                if (!cellResult) { Assert.False(cell.IsClosed); Assert.Equal(" 화학\n ", cell.SubjectText); Assert.Contains("저장", cell.ErrorText); }
                return (cellResult, cell.TryApply);
            case "bulk":
                var import = new TimetableImportActions(new NoClipboard()).CreateSession(runtime.Timetable, TimetableImportMode.Canonical);
                import.LoadText(CanonicalTimetableImporter.CreateTemplate());
                var bulkResult = import.TryApply();
                if (!bulkResult) { Assert.False(import.IsClosed); Assert.NotNull(import.Preview); Assert.Contains("저장", import.ErrorText); }
                return (bulkResult, import.TryApply);
            case "period":
                var period = runtime.ScheduleEditor.CreateSession(); period.Rows[4].StartText = "13:00";
                var periodResult = period.TryApply();
                if (!periodResult) { Assert.False(period.IsClosed); Assert.Equal("13:00", period.Rows[4].StartText); Assert.Contains("저장", period.ErrorText); }
                return (periodResult, period.TryApply);
            case "date":
            case "remove":
                var date = runtime.DateEditor.CreateSession(ProfileStorageTests.Monday);
                date.UseTimetable = date.UseSchedule = action == "date";
                date.TimetableRows[0].SubjectText = "날짜 변경";
                date.ScheduleDraft.Rows[4].StartText = "13:00";
                var dateResult = date.TryApply();
                if (!dateResult) { Assert.False(date.IsClosed); Assert.Contains("저장", date.ErrorText); }
                return (dateResult, date.TryApply);
            case "lunch":
                bool Toggle() { runtime.Lunch.Enabled = false; return !runtime.Lunch.Enabled; }
                return (Toggle(), Toggle);
            default: throw new ArgumentException(action);
        }
    }

    [Theory]
    [InlineData("{", ProfileLoadState.Invalid)]
    [InlineData("{\"schemaVersion\":500}", ProfileLoadState.Unsupported)]
    [InlineData("{\"schemaVersion\":1,\"profile\":{}}", ProfileLoadState.Invalid)]
    public void DegradedModeBlocksEveryCommitIncludingNoOpAndExit(string json, ProfileLoadState expected)
    {
        using var temp = new TempProfile(); File.WriteAllText(temp.File, json);
        var bytes = File.ReadAllBytes(temp.File);
        using (var store = new JsonProfileStore(temp.Directory))
        {
            var session = new ProfileSession(store); Assert.Equal(expected, session.LoadResult.State);
            foreach (var action in new[] { "cell", "bulk", "period", "date", "remove" })
            {
                var runtime = new ProfileRuntime(session, () => throw new Exception("Must not refresh"), _ => throw new Exception("Must not refresh"));
                var old = session.Current;
                Assert.False(Apply(runtime, action).Accepted);
                Assert.Same(old, session.Current);
                Assert.Equal(ProfileJson.Serialize(ProfileSnapshot.Defaults()), ProfileJson.Serialize(Capture(runtime)));
            }
            var lunchRuntime = new ProfileRuntime(session, () => throw new Exception("Must not refresh"), _ => { });
            lunchRuntime.Lunch.Enabled = true;
            Assert.False(lunchRuntime.Lunch.Enabled); Assert.Contains("저장", lunchRuntime.Lunch.ErrorText);
            Assert.Equal(bytes, File.ReadAllBytes(temp.File));
        }
        Assert.Equal(bytes, File.ReadAllBytes(temp.File));
    }

    [Fact]
    public void RestartUsesSameCompositionAndRecomputesEffectiveHeaderFromClock() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile();
        byte[] saved; string headerText;
        using (var store = new JsonProfileStore(temp.Directory))
        {
            var session = new ProfileSession(store); var runtime = new ProfileRuntime(session, () => { }, _ => { });
            var cell = runtime.Timetable.Editor.BeginEdit(runtime.Timetable.Cells[0]); cell.SubjectText = "물리"; cell.ClassText = "3-1"; Assert.True(cell.TryApply());
            var period = runtime.ScheduleEditor.CreateSession(); period.Rows[0].StartText = "08:55"; Assert.True(period.TryApply());
            var date = runtime.DateEditor.CreateSession(ProfileStorageTests.Monday); date.UseTimetable = date.UseSchedule = true;
            date.TimetableRows[0].SubjectText = "예외 수업"; date.ScheduleDraft.Rows[4].StartText = "13:30"; Assert.True(date.TryApply());
            runtime.Lunch.Enabled = true;
            headerText = Refresh(runtime);
            Assert.StartsWith("점심시간", headerText);
            saved = File.ReadAllBytes(temp.File);
        }
        using var nextStore = new JsonProfileStore(temp.Directory);
        var next = new ProfileRuntime(new(nextStore), () => { }, _ => { });
        Assert.Equal(saved, ProfileJson.Serialize(Capture(next)));
        Assert.Equal(headerText, Refresh(next));
        Assert.Equal("예외 수업", next.Timetable.Cells[0].Value.SubjectText);
        Assert.Equal("물리", next.Timetable.CommittedTimetable.Cells[0].Value.SubjectText);
        Assert.Equal(saved, File.ReadAllBytes(temp.File));
    });

    private static string Refresh(ProfileRuntime runtime)
    {
        var header = new CurrentStatusHeaderViewModel();
        var clock = new FakeApplicationClock(new(new DateTimeOffset(2026, 9, 7, 13, 10, 0, TimeSpan.FromHours(9)), SchoolTimetableWidget.Core.Time.ApplicationTimeSource.PcLocalFallback, 0));
        using var loop = new CurrentStatusRefreshLoop(clock, runtime.Resolve, header, runtime.Timetable, () => runtime.Lunch.Enabled);
        loop.RefreshNow(); loop.RefreshNow();
        return header.StatusText;
    }

    [Fact]
    public void DraftCancelPreviewAndRefreshNeverWrite() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var runtime = new ProfileRuntime(new(store), () => { }, _ => { });
        var cell = runtime.Timetable.Editor.BeginEdit(runtime.Timetable.Cells[0]); cell.SubjectText = "draft"; cell.Cancel();
        var period = runtime.ScheduleEditor.CreateSession(); period.Rows[0].StartText = "08:00"; period.Cancel();
        var date = runtime.DateEditor.CreateSession(ProfileStorageTests.Monday); date.UseTimetable = true; date.Cancel();
        var import = new TimetableImportActions(new NoClipboard()).CreateSession(runtime.Timetable, TimetableImportMode.Canonical);
        import.LoadText(CanonicalTimetableImporter.CreateTemplate()); import.Cancel();
        Refresh(runtime);
        Assert.False(File.Exists(temp.File));
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ActualUnshownWindowShowsDegradedNoticeBeforeDisplay(bool invalid) => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); if (invalid) File.WriteAllText(temp.File, "{");
        using var store = new JsonProfileStore(temp.Directory); var session = new ProfileSession(store);
        var runtime = new ProfileRuntime(session, () => { }, _ => { });
        var window = new MainWindow(new(), runtime.Timetable, runtime.ScheduleEditor, session.LoadResult.Notice);
        try
        {
            var notice = (TextBlock)window.FindName("PersistenceNotice");
            Assert.Equal(invalid ? Visibility.Visible : Visibility.Collapsed, notice.Visibility);
            if (invalid)
            {
                Assert.Contains("불러오지 못했습니다", notice.Text); Assert.Contains("임시 실행", notice.Text);
                Assert.Contains("저장할 수 없습니다", notice.Text); Assert.Contains("수정하지 않았습니다", notice.Text);
                Assert.Contains(temp.File, notice.Text);
            }
        }
        finally { window.Close(); }
    });

    private static ProfileSnapshot Capture(ProfileRuntime runtime) => new(runtime.Timetable.CommittedTimetable,
        runtime.Schedule.Current, Enumerable.Range(0, 3).Select(i => runtime.Overrides.Get(ProfileStorageTests.Monday.AddDays(i))).OfType<DateSpecificOverride>(), runtime.Lunch.Enabled);

    private sealed class NoClipboard : ISpreadsheetClipboard
    {
        public string ReadText() => throw new Exception("Tests must not use clipboard");
        public void WriteText(string text) => throw new Exception("Tests must not use clipboard");
    }
}
