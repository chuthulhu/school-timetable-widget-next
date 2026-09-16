using System.Windows;
using System.Windows.Controls;
using SchoolTimetableWidget.Desktop;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.Time;
using SchoolTimetableWidget.Tests.Timetable;
using SchoolTimetableWidget.Core.Time;

namespace SchoolTimetableWidget.Tests.Persistence;
public class ProfileBackupViewTests
{
    private static FakeApplicationClock Clock() => new(new(new DateTimeOffset(2026, 9, 15, 12, 34, 0, TimeSpan.FromHours(9)), ApplicationTimeSource.PcLocalFallback, 0));
    private sealed class Dialogs : IBackupDialogs
    {
        public string? Path; public bool Confirm; public string? Summary; public string? Suggested; public List<string> Messages = [];
        public string? SavePath(string name) { Suggested = name; return Path; }
        public string? OpenPath() => Path;
        public bool ConfirmRestore(string text) { Summary = text; return Confirm; }
        public bool ConfirmRecovery() => Confirm;
        public void Message(string text) => Messages.Add(text);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void PreviewCancelOrConfirmationUsesReadOnlySourceAndRefreshesWholeRuntime(bool confirm) => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store); Assert.Null(session.SaveLunch(false));
        var refreshes = 0; var runtime = new ProfileRuntime(session, () => refreshes++, _ => { });
        var source = Path.Combine(temp.Directory, "input.stwbackup"); var backup = ProfileBackupFile.Export(ProfileBackupFileTests.Sample()); File.WriteAllBytes(source, backup);
        var before = File.ReadAllBytes(temp.File); var dialogs = new Dialogs { Path = source, Confirm = confirm };
        var actions = new ProfileBackupActions(runtime, Clock(), dialogs); actions.Restore();
        Assert.Contains("현재 데이터를 이 백업으로 교체", dialogs.Summary); Assert.Contains("다운로드 필요", dialogs.Summary);
        Assert.Contains("35칸", dialogs.Summary); Assert.Contains("7교시", dialogs.Summary);
        Assert.Equal(backup, File.ReadAllBytes(source)); Assert.False(Directory.Exists(Path.Combine(temp.Directory, "fonts")));
        if (!confirm) { Assert.Equal(before, File.ReadAllBytes(temp.File)); Assert.Equal(0, refreshes); }
        else { Assert.Equal(1, refreshes); Assert.Equal(ProfileJson.Serialize(ProfileBackupFileTests.Sample()), File.ReadAllBytes(temp.File));
            Assert.Equal(session.Current.Display, runtime.Display.Current); Assert.Equal(session.Current.DisplayPresets.Items, runtime.Display.CommittedPresets.Items); }
    });
    [Fact]
    public void FileDialogsAreDedicatedAndBackupUsesApplicationClock() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var runtime = new ProfileRuntime(new ProfileSession(store), () => { }, _ => { }); var dialogs = new Dialogs();
        new ProfileBackupActions(runtime, Clock(), dialogs).Backup();
        Assert.Equal("SchoolTimetableWidget-Backup-20260915-1234.stwbackup", dialogs.Suggested);
        Assert.Contains(dialogs.Messages, m => m.Contains("시간표와 설정"));
        Assert.False(File.Exists(temp.File));
        var save = WindowsBackupDialogs.CreateSave("backup.stwbackup"); Assert.True(save.OverwritePrompt); Assert.Equal("stwbackup", save.DefaultExt);
        var open = WindowsBackupDialogs.CreateOpen(); Assert.True(open.CheckFileExists); Assert.False(open.Multiselect);
    });
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void DegradedEntryAndRecoveryButtonReflectSessionState(bool recovery) => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); File.WriteAllText(temp.File, "{");
        if (recovery) new ProfileRecoveryFiles(temp.Directory).Secure(ProfileSnapshot.Defaults());
        using var store = new JsonProfileStore(temp.Directory); var session = new ProfileSession(store);
        var runtime = new ProfileRuntime(session, () => { }, _ => { }); var dialogs = new Dialogs();
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), runtime.Timetable, runtime.ScheduleEditor,
            session.LoadResult.Notice, runtime.Display, runtime: runtime, clock: Clock(), backupDialogs: dialogs);
        Assert.Equal(!recovery, BackupCommands.Restore.CanExecute(null, window));
        Assert.Equal(recovery, BackupCommands.Recover.CanExecute(null, window));
        Assert.False(BackupCommands.Backup.CanExecute(null, window));
        Assert.Equal(recovery ? Visibility.Visible : Visibility.Collapsed, ((Button)window.FindName("RecoveryButton")).Visibility);
        Assert.Contains(window.ContextMenu.Items.OfType<MenuItem>(), i => Equals(i.Header, "데이터 백업..."));
        Assert.Contains(window.ContextMenu.Items.OfType<MenuItem>(), i => Equals(i.Header, "데이터 복원..."));
        window.Close();
    });
    [Theory]
    [InlineData("malformed")]
    [InlineData("unsupported")]
    [InlineData("semantic")]
    public void InvalidInputReportsFailureWithoutPreviewOrMutation(string kind) => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store); Assert.Null(session.SaveLunch(true));
        var runtime = new ProfileRuntime(session, () => { }, _ => { });
        var input = Path.Combine(temp.Directory, "bad.stwbackup");
        var inputBytes = kind switch
        {
            "malformed" => "{"u8.ToArray(),
            "unsupported" => MutateBackup(root => root["backupFileVersion"] = 500),
            "semantic" => MutateBackup(root => root["profile"]!["periodSchedule"]![0]!["end"] = "10:30:00.0000000"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        File.WriteAllBytes(input, inputBytes);
        var profileBefore = File.ReadAllBytes(temp.File); var committedBefore = session.Current;
        var dialogs = new Dialogs { Path = input, Confirm = true };
        new ProfileBackupActions(runtime, Clock(), dialogs).Restore();
        Assert.Null(dialogs.Summary); Assert.Single(dialogs.Messages);
        Assert.StartsWith("이 백업 파일을 불러올 수 없습니다.", dialogs.Messages[0]);
        Assert.DoesNotContain("System.", dialogs.Messages[0]);
        Assert.Same(committedBefore, session.Current); Assert.Equal(profileBefore, File.ReadAllBytes(temp.File));
        Assert.Equal(inputBytes, File.ReadAllBytes(input));
        var recovery = new ProfileRecoveryFiles(temp.Directory);
        Assert.False(recovery.IsPending); Assert.False(File.Exists(recovery.SnapshotPath));
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidInputPreservesDegradedOrRecoveryRequiredState(bool recoveryRequired) => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile();
        var corrupt = "{ corrupt original"u8.ToArray(); File.WriteAllBytes(temp.File, corrupt);
        var recovery = new ProfileRecoveryFiles(temp.Directory);
        if (recoveryRequired)
        {
            recovery.Secure(ProfileSnapshot.Defaults(), ProfileLoadState.Invalid, corrupt);
            File.WriteAllBytes(temp.File, ProfileJson.Serialize(ProfileBackupFileTests.Sample()));
        }
        using var store = new JsonProfileStore(temp.Directory); var session = new ProfileSession(store);
        var runtime = new ProfileRuntime(session, () => { }, _ => { });
        var input = Path.Combine(temp.Directory, "bad.stwbackup"); var inputBytes = "{"u8.ToArray(); File.WriteAllBytes(input, inputBytes);
        var profileBefore = File.ReadAllBytes(temp.File);
        var markerBefore = recoveryRequired ? File.ReadAllBytes(recovery.MarkerPath) : null;
        var sourceBefore = recoveryRequired ? File.ReadAllBytes(recovery.OriginalPath) : null;
        var dialogs = new Dialogs { Path = input, Confirm = true };
        var actions = new ProfileBackupActions(runtime, Clock(), dialogs);

        if (recoveryRequired)
        {
            Assert.False(actions.CanRestore);
            actions.Restore();
            Assert.Empty(dialogs.Messages);
            Assert.True(session.IsRecoveryRequired);
            Assert.Equal(markerBefore, File.ReadAllBytes(recovery.MarkerPath));
            Assert.Equal(sourceBefore, File.ReadAllBytes(recovery.OriginalPath));
        }
        else
        {
            Assert.True(actions.CanRestore);
            actions.Restore();
            Assert.Single(dialogs.Messages);
            Assert.Equal(ProfileLoadState.Invalid, session.LoadResult.State);
            Assert.False(session.LoadResult.CanWrite);
            Assert.False(recovery.IsPending);
        }
        Assert.Equal(profileBefore, File.ReadAllBytes(temp.File)); Assert.Equal(inputBytes, File.ReadAllBytes(input));
    });

    [Fact]
    public void UserCanSelectValidBackupAfterInvalidInput()
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store); Assert.Null(session.SaveLunch(true));
        var runtime = new ProfileRuntime(session, () => { }, _ => { });
        var input = Path.Combine(temp.Directory, "input.stwbackup"); var dialogs = new Dialogs { Path = input, Confirm = true };
        var actions = new ProfileBackupActions(runtime, Clock(), dialogs);
        File.WriteAllText(input, "{"); actions.Restore();
        Assert.Single(dialogs.Messages); Assert.True(actions.CanRestore);
        var valid = ProfileBackupFile.Export(ProfileBackupFileTests.Sample()); File.WriteAllBytes(input, valid);
        actions.Restore();
        Assert.Equal(ProfileJson.Serialize(ProfileBackupFileTests.Sample()), File.ReadAllBytes(temp.File));
        Assert.Equal(valid, File.ReadAllBytes(input)); Assert.True(session.LoadResult.CanWrite);
    }

    private static byte[] MutateBackup(Action<System.Text.Json.Nodes.JsonNode> mutate)
    {
        var root = System.Text.Json.Nodes.JsonNode.Parse(ProfileBackupFile.Export(ProfileBackupFileTests.Sample()))!;
        mutate(root);
        return System.Text.Encoding.UTF8.GetBytes(root.ToJsonString());
    }
    [Fact]
    public void ModalGuardBlocksAllFileEntryActions()
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var runtime = new ProfileRuntime(new ProfileSession(store), () => { }, _ => { }); var dialogs = new Dialogs();
        var actions = new ProfileBackupActions(runtime, Clock(), dialogs, () => true);
        Assert.False(actions.CanRestore); Assert.False(actions.CanBackup); actions.Backup(); actions.Restore(); Assert.Empty(dialogs.Messages);
    }

    [Fact]
    public void UnexpectedProgrammingErrorIsNotHiddenByInputErrorBoundary()
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var runtime = new ProfileRuntime(new ProfileSession(store), () => { }, _ => { });
        var dialogs = new ThrowingDialogs();
        Assert.Throws<InvalidOperationException>(() => new ProfileBackupActions(runtime, Clock(), dialogs).Restore());
        Assert.Empty(dialogs.Messages);
        Assert.False(File.Exists(temp.File));
    }
    [Fact]
    public void PreviewUsesExplicitRestoreAndCancelWithoutDefaultAccept() => HighlightTestDispatcher.Run(() =>
    {
        var window = new BackupRestorePreviewWindow("현재 데이터를 이 백업으로 교체합니다.");
        var layout = Assert.IsType<DockPanel>(window.Content); var buttons = Assert.IsType<StackPanel>(layout.Children[0]);
        Assert.True(Assert.IsType<Button>(buttons.Children[0]).IsCancel);
        Assert.False(Assert.IsType<Button>(buttons.Children[1]).IsDefault); window.Close();
    });

    private sealed class ThrowingDialogs : IBackupDialogs
    {
        public List<string> Messages { get; } = [];
        public string? SavePath(string suggestedName) => throw new InvalidOperationException("programming defect");
        public string? OpenPath() => throw new InvalidOperationException("programming defect");
        public bool ConfirmRestore(string summary) => throw new InvalidOperationException("programming defect");
        public bool ConfirmRecovery() => throw new InvalidOperationException("programming defect");
        public void Message(string text) => Messages.Add(text);
    }
}
