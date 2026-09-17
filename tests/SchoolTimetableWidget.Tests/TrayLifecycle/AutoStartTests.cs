using System.ComponentModel;
using System.Reflection;
using System.Security;
using SchoolTimetableWidget.Desktop.Features.Autostart;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Features.TrayLifecycle;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using SchoolTimetableWidget.Tests.Persistence;
using SchoolTimetableWidget.Tests.Timetable;
using Forms = System.Windows.Forms;

namespace SchoolTimetableWidget.Tests.TrayLifecycle;

// Fake registration only. Forms objects are never published or opened on the desktop.
public class AutoStartTests
{
    private const string Command = "\"C:\\Test User\\학교 시간표\\SchoolTimetableWidget.Desktop.exe\"";

    [Fact]
    public void DefaultDoesNotReadOrWriteUntilRequestedAndEnableDisableAreIdempotent()
    {
        var store = new MemoryRegistration(); var registration = new AutoStartRegistration(store, Command);
        Assert.Equal(0, store.Reads); Assert.Equal(0, store.Writes);
        Assert.Equal(AutoStartStatus.Disabled, registration.Refresh()); Assert.Equal(0, store.Writes);
        Assert.True(registration.SetEnabled(true)); Assert.Equal(Command, store.Value);
        Assert.Equal(AutoStartStatus.Enabled, registration.Status);
        Assert.True(registration.SetEnabled(true)); Assert.Equal(Command, store.Value);
        Assert.True(registration.SetEnabled(false)); Assert.Null(store.Value);
        Assert.Equal(AutoStartStatus.Disabled, registration.Status);
        Assert.True(registration.SetEnabled(false)); Assert.Null(store.Value);
    }

    [Theory]
    [InlineData("C:\\Apps\\SchoolTimetableWidget.Desktop.exe")]
    [InlineData("C:\\Test User\\학교 시간표\\SchoolTimetableWidget.Desktop.exe")]
    [InlineData("C:\\Apps & Tools\\SchoolTimetableWidget.Desktop.exe")]
    public void CommandIsOnlyQuotedExecutable(string executable) =>
        Assert.Equal("\"" + executable + "\"", AutoStartCommand.FromExecutable(executable));

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("SchoolTimetableWidget.Desktop.exe")]
    [InlineData("C:\\app\\SchoolTimetableWidget.Desktop.dll")]
    [InlineData("C:\\dotnet\\dotnet.exe")] [InlineData("C:\\app\\testhost.exe")]
    [InlineData("C:\\bad\" --hidden \\SchoolTimetableWidget.Desktop.exe")]
    [InlineData("C:\\bad\n\\SchoolTimetableWidget.Desktop.exe")]
    public void UnsupportedHostAndCommandTextAreNeverRegistered(string? executable) =>
        Assert.Null(AutoStartCommand.FromExecutable(executable));

    [Fact]
    public void TooLongRunCommandIsRejected() => Assert.Null(AutoStartCommand.FromExecutable(
        "C:\\" + new string('a', 250) + "\\SchoolTimetableWidget.Desktop.exe"));

    [Theory]
    [InlineData("\"C:\\Old\\SchoolTimetableWidget.Desktop.exe\"")]
    [InlineData("\"C:\\Test User\\학교 시간표\\SchoolTimetableWidget.Desktop.exe\" --hidden")]
    [InlineData(17)]
    public void StaleAndNonStringRegistrationRemainUntouchedUntilExplicitRepair(object value)
    {
        var store = new MemoryRegistration { Value = value };
        var registration = new AutoStartRegistration(store, Command);
        Assert.Equal(AutoStartStatus.StaleOrDifferent, registration.Refresh());
        Assert.Equal(value, store.Value); Assert.Equal(0, store.Writes);
        Assert.True(registration.SetEnabled(true)); Assert.Equal(Command, store.Value);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void ExpectedReadFailureIsUnavailableAndNeverMutates(int kind)
    {
        var store = new MemoryRegistration { ReadFailure = Failure(kind) };
        var registration = new AutoStartRegistration(store, Command);
        Assert.Equal(AutoStartStatus.Unavailable, registration.Refresh());
        Assert.False(registration.SetEnabled(true)); Assert.False(registration.SetEnabled(false));
        Assert.Equal(0, store.Writes); Assert.DoesNotContain("SECRET", registration.Error!);
    }

    [Theory]
    [InlineData(null, true, 0)]
    [InlineData(Command, false, 1)]
    [InlineData("old path", true, 2)]
    public void FailedMutationKeepsActualState(object? previous, bool enabled, int expected)
    {
        var store = new MemoryRegistration { Value = previous, WriteFailure = new UnauthorizedAccessException("SECRET") };
        var registration = new AutoStartRegistration(store, Command);
        Assert.False(registration.SetEnabled(enabled)); Assert.Equal((AutoStartStatus)expected, registration.Status);
        Assert.Equal(previous, store.Value); Assert.DoesNotContain("SECRET", registration.Error!);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void IgnoredWriteOrDeleteCannotReportSuccess(bool enabled)
    {
        var store = new MemoryRegistration { Value = enabled ? null : Command, IgnoreWrites = true };
        var registration = new AutoStartRegistration(store, Command);
        Assert.False(registration.SetEnabled(enabled)); Assert.NotNull(registration.Error);
        Assert.Equal(enabled ? AutoStartStatus.Disabled : AutoStartStatus.Enabled, registration.Status);
    }

    [Fact]
    public void ReadBackFailureIsUnknownRatherThanFalseSuccess()
    {
        var store = new MemoryRegistration { AfterWrite = s => s.ReadFailure = new IOException("SECRET") };
        var registration = new AutoStartRegistration(store, Command);
        Assert.False(registration.SetEnabled(true)); Assert.Equal(Command, store.Value);
        Assert.Equal(AutoStartStatus.Unavailable, registration.Status); Assert.NotNull(registration.Error);
    }

    [Fact]
    public void ProgrammingDefectsAreNotSwallowed()
    {
        var store = new MemoryRegistration { ReadFailure = new InvalidOperationException("defect") };
        Assert.Throws<InvalidOperationException>(() => new AutoStartRegistration(store, Command).Refresh());
    }

    [Fact]
    public void MissingExecutableDoesNotWrite()
    {
        var store = new MemoryRegistration(); var registration = new AutoStartRegistration(store, null);
        Assert.False(registration.SetEnabled(true)); Assert.Equal(0, store.Writes);
        Assert.Equal(AutoStartStatus.Disabled, registration.Status);
    }

    [Fact]
    public void TrayOpeningRefreshesExternalChangesAndClickUsesVerifiedState() => HighlightTestDispatcher.Run(() =>
    {
        var store = new MemoryRegistration(); var registration = new AutoStartRegistration(store, Command);
        using var tray = new WindowsTrayIcon(false, registration, _ => throw new Exception("Unexpected error"));
        var item = Assert.IsType<Forms.ToolStripMenuItem>(tray.Menu.Items[1]);
        Assert.Equal("Windows 시작 시 실행", item.Text); Assert.False(item.CheckOnClick);
        Open(tray); Assert.False(item.Checked); Assert.True(item.Enabled);
        item.PerformClick(); Assert.True(item.Checked); Assert.Equal(Command, store.Value);
        tray.SetWindowVisible(false); Open(tray); Assert.True(item.Checked);
        item.PerformClick(); Assert.False(item.Checked); Assert.Null(store.Value);
        store.Value = "old"; Open(tray); Assert.False(item.Checked); Assert.NotEmpty(item.ToolTipText!);
        item.PerformClick(); Assert.True(item.Checked); Assert.Equal(Command, store.Value);
        store.Value = null; Open(tray); Assert.False(item.Checked);
        store.ReadFailure = new IOException(); Open(tray);
        Assert.False(item.Enabled); Assert.Equal(Forms.CheckState.Indeterminate, item.CheckState);
        store.ReadFailure = null; Open(tray); Assert.True(item.Enabled); Assert.False(item.Checked);
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void TrayFailureDoesNotOptimisticallyFlipCheckmark(bool initiallyEnabled) => HighlightTestDispatcher.Run(() =>
    {
        var store = new MemoryRegistration { Value = initiallyEnabled ? Command : null, WriteFailure = new IOException("SECRET") };
        var errors = new List<string>();
        using var tray = new WindowsTrayIcon(false, new(store, Command), errors.Add);
        Open(tray); tray.Menu.Items[1].PerformClick();
        Assert.Equal(initiallyEnabled, ((Forms.ToolStripMenuItem)tray.Menu.Items[1]).Checked);
        Assert.DoesNotContain("SECRET", Assert.Single(errors));
    });

    [Fact]
    public void VisibilityExitAndSecondaryActivationNeverTouchRegistration() => HighlightTestDispatcher.Run(() =>
    {
        var store = new MemoryRegistration { Value = Command };
        using var tray = new WindowsTrayIcon(false, new(store, Command));
        var window = new FakeWidgetWindow(); var exits = 0;
        using var lifecycle = new WidgetTrayLifecycle(window, tray, null, () => exits++);
        lifecycle.Hide(); lifecycle.Show(); window.Close();
        Assert.False(SingleInstanceStartup.Run(new Secondary(lifecycle.Show), () => throw new Exception("Full startup")));
        lifecycle.Exit(); Assert.Equal(1, exits);
        Assert.Equal(0, store.Reads); Assert.Equal(0, store.Writes); Assert.Equal(Command, store.Value);
    });

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void NormalDegradedAndRecoveryFilesAndPortableDataAreIndependent(int mode) => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile();
        var sample = ProfileBackupFileTests.Sample();
        File.WriteAllBytes(temp.File, mode == 0 ? ProfileJson.Serialize(sample) : "corrupt"u8.ToArray());
        if (mode == 2) new ProfileRecoveryFiles(temp.Directory).Secure(sample, ProfileLoadState.Invalid, "corrupt"u8.ToArray());
        File.WriteAllText(Path.Combine(temp.Directory, "window-state.json"), "window sentinel");
        File.WriteAllText(Path.Combine(temp.Directory, "tray-state.json"), "notice sentinel");
        using var profileStore = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(profileStore); var runtime = new ProfileRuntime(session, () => { }, _ => { });
        var before = Directory.GetFiles(temp.Directory).Where(p => !p.EndsWith("profile.lock")).ToDictionary(p => p, File.ReadAllBytes);
        var original = session.Current; var backup = ProfileBackupFile.Export(original);
        var store = new MemoryRegistration(); var registration = new AutoStartRegistration(store, Command);
        using var tray = new WindowsTrayIcon(false, registration);
        Open(tray); tray.Menu.Items[1].PerformClick(); Assert.Equal(Command, store.Value);
        tray.Menu.Items[1].PerformClick(); Assert.Null(store.Value);
        Assert.Same(original, session.Current); Assert.Equal(backup, ProfileBackupFile.Export(session.Current));
        Assert.Equal(mode == 2, session.IsRecoveryRequired);
        Assert.Equal(mode == 0, session.LoadResult.CanWrite);
        foreach (var pair in before) Assert.Equal(pair.Value, File.ReadAllBytes(pair.Key));
        if (mode == 0)
        {
            registration.SetEnabled(true); var reads = store.Reads; var writes = store.Writes;
            Assert.Null(runtime.Restore(ProfileBackupFile.Import(backup)));
            Assert.Equal(reads, store.Reads); Assert.Equal(writes, store.Writes); Assert.Equal(Command, store.Value);
        }
    });

    private static void Open(WindowsTrayIcon tray) => typeof(Forms.ToolStripDropDown)
        .GetMethod("OnOpening", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(tray.Menu, [new CancelEventArgs()]);
    private static Exception Failure(int kind) => kind switch
    {
        0 => new SecurityException("SECRET"), 1 => new UnauthorizedAccessException("SECRET"),
        2 => new IOException("SECRET"), _ => new Win32Exception("SECRET")
    };
    private sealed class Secondary(Action activate) : IInstanceOwnership
    {
        public bool IsPrimary => false;
        public void SignalActivation() => activate();
    }
    private sealed class MemoryRegistration : IAutoStartRegistrationStore
    {
        public object? Value;
        public int Reads, Writes;
        public Exception? ReadFailure, WriteFailure;
        public bool IgnoreWrites;
        public Action<MemoryRegistration>? AfterWrite;
        public object? Read() { Reads++; if (ReadFailure is not null) throw ReadFailure; return Value; }
        public void Write(string command) => Mutate(command);
        public void Remove() => Mutate(null);
        private void Mutate(object? value)
        {
            Writes++; if (WriteFailure is not null) throw WriteFailure;
            if (!IgnoreWrites) Value = value;
            AfterWrite?.Invoke(this);
        }
    }
}
