using System.Text;
using SchoolTimetableWidget.Core.Features.DataInterchange;
using SchoolTimetableWidget.Core.Features.Semesters;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.DataInterchange;

namespace SchoolTimetableWidget.Tests.Persistence;

public class TimetableDataImportTests
{
    [Theory]
    [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void OneDurableCommitPublishesAllSelectedComponentsBeforeNotification(bool timetable, bool schedule)
    {
        using var temp = new TempProfile();
        var renames = 0;
        using var store = new JsonProfileStore(temp.Directory, stage => { if (stage == ProfileWriteStage.BeforeRename) renames++; });
        var first = ProfileStorageTests.Sample();
        var other = new SemesterSet(Guid.NewGuid(), "다른 학기", TimetableSharingTests.Week("다른 교사", "2-1"), first.Schedule, []);
        var initial = first.WithSemesters(first.SemesterSets.Append(other), first.ActiveSemesterId);
        var session = new ProfileSession(store, initial);
        Assert.Null(session.SaveLunch(true));
        var reviewed = session.Current;
        var refreshes = 0;
        ProfileRuntime? runtime = null;
        runtime = new ProfileRuntime(session, () =>
        {
            refreshes++;
            Assert.Equal(File.ReadAllBytes(temp.File), ProfileJson.Serialize(session.Current));
            Assert.Same(session.Current.Timetable, runtime!.Timetable.CommittedTimetable);
            Assert.Same(session.Current.Schedule, runtime.Schedule.Current);
        }, _ => { });
        var package = new TimetableDataPackage(TimetableSharingTests.Week(), TimetableSharingTests.Schedule());
        var writesBefore = renames;
        var notifications = 0;
        runtime.Timetable.ContentChanged += (_, _) =>
        {
            notifications++;
            Assert.Equal(File.ReadAllBytes(temp.File), ProfileJson.Serialize(session.Current));
            Assert.Same(session.Current.Timetable, runtime.Timetable.CommittedTimetable);
            Assert.Same(session.Current.Schedule, runtime.Schedule.Current);
        };
        Assert.Null(runtime.ImportData(package, reviewed, timetable, schedule));
        Assert.Equal(writesBefore + 1, renames);
        Assert.Equal(1, refreshes);
        Assert.Equal(timetable, notifications > 0);
        Assert.Same(timetable ? package.Timetable : reviewed.Timetable, session.Current.Timetable);
        Assert.Same(schedule ? package.Schedule : reviewed.Schedule, session.Current.Schedule);
        Assert.Same(other, session.Current.SemesterSets.Single(s => s.SemesterId == other.SemesterId));
        Assert.Equal(reviewed.Overrides, session.Current.Overrides);
        Assert.Same(reviewed.Display, session.Current.Display);
        Assert.Same(reviewed.DisplayPresets, session.Current.DisplayPresets);
        Assert.Equal(reviewed.ShowLunch, session.Current.ShowLunch);
        store.Dispose();
        using var restarted = new JsonProfileStore(temp.Directory);
        var restart = restarted.Load();
        Assert.Equal(ProfileLoadState.Loaded, restart.State);
        Assert.Equal(ProfileJson.Serialize(session.Current), ProfileJson.Serialize(restart.Snapshot));
    }

    [Fact]
    public void FailedCommitRetainsDiskAndRuntimeAndCanRetrySameReviewedCandidate()
    {
        using var temp = new TempProfile();
        var fail = false;
        using var store = new JsonProfileStore(temp.Directory, stage =>
        { if (fail && stage == ProfileWriteStage.PartialWrite) throw new IOException("injected data write failure"); });
        var session = new ProfileSession(store, ProfileStorageTests.Sample());
        Assert.Null(session.SaveLunch(true));
        var baseline = session.Current;
        var before = File.ReadAllBytes(temp.File);
        var refreshes = 0;
        var notifications = 0;
        var runtime = new ProfileRuntime(session, () => refreshes++, _ => { });
        runtime.Timetable.ContentChanged += (_, _) => notifications++;
        var package = new TimetableDataPackage(TimetableSharingTests.Week(), TimetableSharingTests.Schedule());
        fail = true;
        Assert.Contains("저장", runtime.ImportData(package, baseline, true, true));
        Assert.Same(baseline, session.Current);
        Assert.Same(baseline.Timetable, runtime.Timetable.CommittedTimetable);
        Assert.Same(baseline.Schedule, runtime.Schedule.Current);
        Assert.Equal(before, File.ReadAllBytes(temp.File));
        Assert.Equal(0, refreshes);
        Assert.Equal(0, notifications);
        fail = false;
        Assert.Null(runtime.ImportData(package, baseline, true, true));
        Assert.Equal(1, refreshes);
    }

    [Theory]
    [InlineData("cell")] [InlineData("schedule")] [InlineData("lunch")] [InlineData("semester")]
    public void StalePreviewNeverRetargetsOrOverwritesNewerCommit(string change)
    {
        using var temp = new TempProfile();
        using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store, ProfileStorageTests.Sample());
        Assert.Null(session.SaveLunch(true));
        var reviewed = session.Current;
        switch (change)
        {
            case "cell": Assert.Null(session.SaveTimetable(TimetableSharingTests.Week("더 최근 내용"))); break;
            case "schedule": Assert.Null(session.SaveSchedule(TimetableSharingTests.Schedule())); break;
            case "lunch": Assert.Null(session.SaveLunch(false)); break;
            case "semester":
                var other = new SemesterSet(Guid.NewGuid(), "다른 학기", TimetableSharingTests.Week(), reviewed.Schedule, []);
                Assert.Null(session.SaveSemesters(reviewed.WithSemesters(reviewed.SemesterSets.Append(other), other.SemesterId)));
                break;
        }
        var current = session.Current;
        var before = File.ReadAllBytes(temp.File);
        Assert.NotNull(session.ImportData(new(TimetableSharingTests.Week(), TimetableSharingTests.Schedule()), reviewed, true, true));
        Assert.Same(current, session.Current);
        Assert.Equal(before, File.ReadAllBytes(temp.File));
    }

    [Fact]
    public void InvalidSelectionAndDegradedProfileCannotCommit()
    {
        using var temp = new TempProfile();
        File.WriteAllText(temp.File, "{");
        using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store);
        var before = File.ReadAllBytes(temp.File);
        Assert.NotNull(session.ImportData(new(TimetableSharingTests.Week(), null), session.Current, true, false));
        Assert.NotNull(session.ImportData(new(TimetableSharingTests.Week(), null), session.Current, false, true));
        Assert.Equal(before, File.ReadAllBytes(temp.File));
    }

    [Fact]
    public void OpenCellDraftBlocksRuntimeReplacement()
    {
        using var temp = new TempProfile();
        using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store, ProfileStorageTests.Sample());
        var baseline = session.Current;
        var runtime = new ProfileRuntime(session, () => throw new Exception("must not refresh"), _ => { });
        var draft = runtime.Timetable.Editor.BeginEdit(runtime.Timetable.Cells[0]);
        Assert.NotNull(runtime.ImportData(new(TimetableSharingTests.Week(), null), baseline, true, false));
        Assert.Same(baseline, session.Current);
        draft.Cancel();
    }

    [Fact]
    public void OlderScheduleDraftCannotOverwriteSuccessfulCombinedImport()
    {
        using var temp = new TempProfile();
        using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store, ProfileStorageTests.Sample());
        var runtime = new ProfileRuntime(session, () => { }, _ => { });
        var draft = runtime.ScheduleEditor.CreateSession();
        draft.Rows[4].StartText = "13:00";
        Assert.Null(runtime.ImportData(new(TimetableSharingTests.Week(), TimetableSharingTests.Schedule()), session.Current, true, true));
        var afterImport = session.Current;
        Assert.False(draft.TryApply());
        Assert.Same(afterImport, session.Current);
        draft.Cancel();
    }

    [Fact]
    public void FileOperationsPreserveLegacySourceAndSharedRoundTrip()
    {
        using var temp = new TempProfile();
        var source = Path.Combine(temp.Directory, "timetable_data.json");
        var periods = Path.Combine(temp.Directory, "time_settings.json");
        File.WriteAllText(source, "{\"월\":{\"1\":\" 물리\\n3-2 \"}}", new UTF8Encoding(true));
        File.WriteAllText(periods, "{\"1\":{\"start\":\"9:00\",\"end\":\"9:50\"}}");
        var sourceBytes = File.ReadAllBytes(source);
        var periodBytes = File.ReadAllBytes(periods);
        var candidate = TimetableDataFiles.ReadLegacy(source, periods);
        Assert.Equal(new(" 물리\n3-2 ", ""), candidate.Timetable[SchoolDay.Monday, 1].Value);
        Assert.Equal(sourceBytes, File.ReadAllBytes(source));
        Assert.Equal(periodBytes, File.ReadAllBytes(periods));
        var shared = Path.Combine(temp.Directory, "example.stwshare");
        TimetableDataFiles.ExportShared(shared, new(candidate.Timetable, candidate.Schedule));
        var restored = TimetableDataFiles.ReadShared(shared);
        Assert.Equal(candidate.Timetable.Cells.Select(c => c.Value), restored.Timetable!.Cells.Select(c => c.Value));
        Assert.Empty(Directory.GetFiles(temp.Directory, ".stw-*.tmp"));
    }

    [Fact]
    public void BadLegacyTimeFileAndOversizedFilesCannotReturnPartialCandidates()
    {
        using var temp = new TempProfile();
        var source = Path.Combine(temp.Directory, "legacy.json");
        var periods = Path.Combine(temp.Directory, "times.json");
        File.WriteAllText(source, "{\"월\":{\"1\":\"국어\"}}");
        File.WriteAllText(periods, "{");
        Assert.Throws<FormatException>(() => TimetableDataFiles.ReadLegacy(source, periods));
        Assert.Throws<FileNotFoundException>(() => TimetableDataFiles.ReadLegacy(source, Path.Combine(temp.Directory, "missing.json")));
        using (var output = new FileStream(source, FileMode.Create)) output.SetLength(TimetableShareFile.MaximumBytes + 1L);
        Assert.Throws<FormatException>(() => TimetableDataFiles.ReadLegacy(source));
        Assert.Throws<FormatException>(() => TimetableDataFiles.ReadShared(source));
    }

    [Fact]
    public void FailedExportDoesNotReplaceExistingFile()
    {
        using var temp = new TempProfile();
        var path = Path.Combine(temp.Directory, "existing.stwshare");
        var original = TimetableShareFile.Export(new(TimetableSharingTests.Week("원본"), null));
        File.WriteAllBytes(path, original);
        Assert.Throws<FormatException>(() => TimetableDataFiles.ExportShared(path,
            new(TimetableSharingTests.Week(new string('x', 130_000)), null)));
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Empty(Directory.GetFiles(temp.Directory, ".stw-*.tmp"));
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    public void InvalidSelectionDoesNotWriteAHealthyProfile(bool timetable, bool schedule)
    {
        using var temp = new TempProfile();
        using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store, ProfileStorageTests.Sample());
        Assert.Null(session.SaveLunch(true));
        var baseline = session.Current;
        var before = File.ReadAllBytes(temp.File);
        Assert.NotNull(session.ImportData(new(TimetableSharingTests.Week(), null), baseline, timetable, schedule));
        Assert.Same(baseline, session.Current);
        Assert.Equal(before, File.ReadAllBytes(temp.File));
    }

    [Fact]
    public void LockedDestinationExportFailureRetainsOriginalAndRemovesTemporaryFile()
    {
        using var temp = new TempProfile();
        var path = Path.Combine(temp.Directory, "locked.stwshare");
        var original = TimetableShareFile.Export(new(TimetableSharingTests.Week("原본"), null));
        File.WriteAllBytes(path, original);
        using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        Assert.Throws<IOException>(() => TimetableDataFiles.ExportShared(path, new(TimetableSharingTests.Week(), null)));
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Empty(Directory.GetFiles(temp.Directory, ".stw-*.tmp"));
    }

    [Fact]
    public void MalformedUtf8LegacySourceIsRejectedWithoutRewriting()
    {
        using var temp = new TempProfile();
        var path = Path.Combine(temp.Directory, "invalid-utf8.json");
        byte[] bytes = [0x7B, 0x22, 0xFF, 0x22, 0x7D];
        File.WriteAllBytes(path, bytes);
        Assert.Throws<FormatException>(() => TimetableDataFiles.ReadLegacy(path));
        Assert.Equal(bytes, File.ReadAllBytes(path));
    }
}
