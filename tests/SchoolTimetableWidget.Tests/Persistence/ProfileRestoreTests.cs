using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.Persistence;

public class ProfileRestoreTests
{
    [Fact]
    public void ConsecutiveRestoresPreserveOnePreviousRevisionAndViewedWeek()
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store, ProfileBackupFileTests.Sample()); Assert.Null(session.SaveLunch(true));
        var runtime = new ProfileRuntime(session, () => { }, _ => { });
        runtime.Timetable.UpdateCurrent(new(2026, 9, 15), null); runtime.Timetable.NextWeekCommand.Execute(null);
        var viewed = runtime.Timetable.ViewedWeekStart; var a = ProfileJson.Serialize(session.Current);
        var b = ProfileSnapshot.Defaults(); Assert.Null(runtime.Restore(b));
        Assert.Equal(a, File.ReadAllBytes(new ProfileRecoveryFiles(temp.Directory).SnapshotPath));
        Assert.Equal(viewed, runtime.Timetable.ViewedWeekStart); Assert.Equal(ProfileJson.Serialize(b), File.ReadAllBytes(temp.File));
        Assert.Null(runtime.Restore(ProfileBackupFileTests.Sample()));
        Assert.Equal(ProfileJson.Serialize(b), File.ReadAllBytes(new ProfileRecoveryFiles(temp.Directory).SnapshotPath));
        Assert.False(new ProfileRecoveryFiles(temp.Directory).IsPending);
    }

    [Fact]
    public void FirstSuccessfulSaveBecomesLoadedCommittedOrigin()
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store); Assert.Equal(ProfileLoadState.Missing, session.LoadResult.State);
        Assert.Null(session.SaveLunch(true));
        Assert.Equal(ProfileLoadState.Loaded, session.LoadResult.State);
        Assert.True(session.LoadResult.CanWrite);
    }

    [Fact]
    public void ActualPublishFailureRollsBackDiskAndAllRuntimeOwners()
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store); Assert.Null(session.SaveLunch(true));
        var failOnce = true;
        var runtime = new ProfileRuntime(session, () =>
        {
            if (failOnce) { failOnce = false; throw new InvalidOperationException("publish"); }
        }, _ => { });
        var old = session.Current; var bytes = File.ReadAllBytes(temp.File);
        Assert.NotNull(runtime.Restore(ProfileBackupFileTests.Sample()));
        Assert.Same(old, session.Current); Assert.Equal(bytes, File.ReadAllBytes(temp.File));
        Assert.Same(old.Timetable, runtime.Timetable.CommittedTimetable);
        Assert.Same(old.Schedule, runtime.Schedule.Current);
        Assert.Equal(old.ShowLunch, runtime.Lunch.Enabled);
        Assert.Equal(old.Display, runtime.Display.Committed);
        Assert.False(session.IsRecoveryRequired);
    }

    [Fact]
    public void DegradedRecoveryCopyFailureNeverReplacesCorruptOriginal()
    {
        using var temp = new TempProfile(); var corrupt = "{ original damaged bytes"u8.ToArray();
        File.WriteAllBytes(temp.File, corrupt);
        var files = new ProfileRecoveryFiles(temp.Directory); Directory.CreateDirectory(files.OriginalPath);
        using var store = new JsonProfileStore(temp.Directory); var session = new ProfileSession(store);
        var runtime = new ProfileRuntime(session, () => { }, _ => { });
        Assert.NotNull(runtime.Restore(ProfileBackupFileTests.Sample()));
        Assert.Equal(corrupt, File.ReadAllBytes(temp.File));
        Assert.Equal(ProfileLoadState.Invalid, session.LoadResult.State);
        Assert.False(session.LoadResult.CanWrite); Assert.False(files.IsPending);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void CandidateAtomicWriteFailurePreservesDiskAndRuntime(int stage)
    {
        using var temp = new TempProfile(); var fail = false;
        using var store = new JsonProfileStore(temp.Directory, point => { if (fail && (int)point == stage) throw new IOException("injected"); });
        var session = new ProfileSession(store); Assert.Null(session.SaveLunch(true));
        var runtime = new ProfileRuntime(session, () => { }, _ => { }); var old = session.Current;
        var before = File.ReadAllBytes(temp.File); fail = true;
        Assert.NotNull(runtime.Restore(ProfileBackupFileTests.Sample()));
        Assert.Same(old, session.Current); Assert.Equal(before, File.ReadAllBytes(temp.File));
        Assert.False(session.IsRecoveryRequired); Assert.True(session.LoadResult.CanWrite);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void TransactionStageFailureRollsBackCompletely(int stage)
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store); Assert.Null(session.SaveLunch(true));
        var runtime = new ProfileRuntime(session, () => { }, _ => { }); var old = session.Current;
        var before = File.ReadAllBytes(temp.File);
        store.RestoreCheckpoint = point => { if ((int)point == stage) throw new IOException("injected"); };
        Assert.NotNull(runtime.Restore(ProfileBackupFileTests.Sample()));
        Assert.Same(old, session.Current); Assert.Equal(before, File.ReadAllBytes(temp.File));
        Assert.Equal(old.ShowLunch, runtime.Lunch.Enabled); Assert.False(session.IsRecoveryRequired);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ActualRuntimeFailureAndRollbackWriteFailureRequireExplicitRecovery(bool degraded)
    {
        using var temp = new TempProfile(); byte[] original = "{ damaged original\r\n"u8.ToArray();
        if (degraded) File.WriteAllBytes(temp.File, original);
        using var store = new JsonProfileStore(temp.Directory); var session = new ProfileSession(store);
        if (!degraded) { Assert.Null(session.SaveLunch(true)); original = File.ReadAllBytes(temp.File); }
        var previous = session.Current; var shouldFail = true;
        var runtime = new ProfileRuntime(session, () => { if (shouldFail) { shouldFail = false; throw new InvalidOperationException("publish"); } }, _ => { });
        store.RestoreCheckpoint = stage => { if (stage == RestoreStage.RollbackWrite) throw new IOException("rollback"); };
        Assert.NotNull(runtime.Restore(ProfileBackupFileTests.Sample()));
        Assert.True(session.IsRecoveryRequired); Assert.False(session.CanRestore); Assert.True(session.CanRecover);
        Assert.NotNull(session.SaveLunch(false)); Assert.Same(previous, session.Current);
        Assert.Same(previous.Timetable, runtime.Timetable.CommittedTimetable);
        var files = new ProfileRecoveryFiles(temp.Directory); Assert.True(files.IsPending);
        var source = files.ReadSource(); Assert.Equal(original, source.Bytes);
        Assert.NotEqual(original, File.ReadAllBytes(temp.File));
        store.RestoreCheckpoint = null;
        Assert.Null(runtime.Recover()); Assert.False(session.IsRecoveryRequired);
        Assert.Equal(original, File.ReadAllBytes(temp.File)); Assert.Equal(!degraded, session.LoadResult.CanWrite);
        Assert.True(session.CanRestore); Assert.False(files.IsPending);
        if (degraded) { Assert.True(File.Exists(files.OriginalPath)); Assert.NotNull(session.SaveLunch(true)); }
        Assert.Null(runtime.Restore(ProfileBackupFileTests.Sample())); Assert.True(session.LoadResult.CanWrite);
        Assert.Equal(ProfileJson.Serialize(session.Current), File.ReadAllBytes(temp.File));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RestartNeverAcceptsValidAmbiguousCandidateAndRetryFailureKeepsEvidence(bool degraded)
    {
        using var temp = new TempProfile();
        if (degraded) File.WriteAllText(temp.File, "{\"schemaVersion\":999}");
        byte[] source;
        using (var store = new JsonProfileStore(temp.Directory))
        {
            var session = new ProfileSession(store); if (!degraded) Assert.Null(session.SaveLunch(true));
            source = File.ReadAllBytes(temp.File);
            store.RestoreCheckpoint = stage => { if (stage is RestoreStage.PublishCandidate or RestoreStage.RollbackWrite) throw new IOException("injected"); };
            Assert.NotNull(session.Restore(ProfileBackupFileTests.Sample(), _ => { }));
        }
        var files = new ProfileRecoveryFiles(temp.Directory); var marker = File.ReadAllBytes(files.MarkerPath);
        using (var restarted = new JsonProfileStore(temp.Directory))
        {
            var session = new ProfileSession(restarted); Assert.True(session.IsRecoveryRequired); Assert.False(session.LoadResult.CanWrite);
            Assert.NotEqual(ProfileJson.Serialize(session.Current), File.ReadAllBytes(temp.File));
            var before = File.ReadAllBytes(temp.File);
            restarted.RestoreCheckpoint = stage => { if (stage == RestoreStage.RecoveryWrite) throw new IOException("injected"); };
            Assert.NotNull(session.Recover(_ => { })); Assert.Equal(before, File.ReadAllBytes(temp.File));
            Assert.Equal(marker, File.ReadAllBytes(files.MarkerPath)); Assert.Equal(source, files.ReadSource().Bytes);
            restarted.RestoreCheckpoint = null; Assert.Null(session.Recover(_ => { }));
            Assert.Equal(!degraded, session.LoadResult.CanWrite); Assert.Equal(source, File.ReadAllBytes(temp.File));
        }
        using var last = new JsonProfileStore(temp.Directory); var loaded = last.Load();
        Assert.Equal(!degraded, loaded.CanWrite); Assert.NotEqual(ProfileLoadState.RecoveryRequired, loaded.State);
    }

    [Theory]
    [InlineData("missing")] [InlineData("invalid")] [InlineData("marker")]
    public void InvalidRecoveryEvidenceKeepsStartupAndRecoveryBlocked(string kind)
    {
        using var temp = new TempProfile(); var files = new ProfileRecoveryFiles(temp.Directory);
        files.Secure(ProfileSnapshot.Defaults()); File.WriteAllBytes(temp.File, ProfileJson.Serialize(ProfileBackupFileTests.Sample()));
        if (kind == "missing") File.Delete(files.SnapshotPath);
        if (kind == "invalid") File.WriteAllText(files.SnapshotPath, "{");
        if (kind == "marker") File.WriteAllText(files.MarkerPath, "{");
        var before = File.ReadAllBytes(temp.File);
        using var store = new JsonProfileStore(temp.Directory); var session = new ProfileSession(store);
        Assert.True(session.IsRecoveryRequired); Assert.NotNull(session.Recover(_ => { }));
        Assert.True(files.IsPending); Assert.Equal(before, File.ReadAllBytes(temp.File)); Assert.NotNull(session.SaveLunch(true));
    }

    [Fact]
    public void InaccessibleMarkerShapeStillTakesPriorityOverValidProfile()
    {
        using var temp = new TempProfile(); File.WriteAllBytes(temp.File, ProfileJson.Serialize(ProfileBackupFileTests.Sample()));
        var files = new ProfileRecoveryFiles(temp.Directory); Directory.CreateDirectory(files.MarkerPath);
        using var store = new JsonProfileStore(temp.Directory); var session = new ProfileSession(store);
        Assert.True(session.IsRecoveryRequired); Assert.False(session.LoadResult.CanWrite);
        Assert.True(session.CanRecover); Assert.NotNull(session.Recover(_ => { }));
        Assert.Equal(ProfileJson.Serialize(ProfileBackupFileTests.Sample()), File.ReadAllBytes(temp.File));
    }

    [Fact]
    public void ActiveDisplayAndCellDraftPreventRestore() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var runtime = new ProfileRuntime(new ProfileSession(store), () => { }, _ => { });
        var display = runtime.Display.Open(); Assert.NotNull(runtime.Restore(ProfileBackupFileTests.Sample())); display.Cancel();
        var cell = runtime.Timetable.Editor.BeginEdit(runtime.Timetable.Cells[0]); Assert.NotNull(runtime.Restore(ProfileBackupFileTests.Sample())); cell.Cancel();
        Assert.False(File.Exists(temp.File)); Assert.Null(runtime.Restore(ProfileBackupFileTests.Sample()));
    });
}
