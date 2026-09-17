using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Features.TrayLifecycle;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using SchoolTimetableWidget.Tests.Persistence;
using SchoolTimetableWidget.Tests.Time;
using SchoolTimetableWidget.Tests.Timetable;
using SchoolTimetableWidget.Tests.WindowPlacement;
using Forms = System.Windows.Forms;

namespace SchoolTimetableWidget.Tests.TrayLifecycle;

// Object/event evidence only: no visible windows, shell icon publication, activation or native input.
public class TrayIntegrationTests
{
    [Fact]
    public void ActualTrayObjectsHaveIconMenuCallbacksAndDisposeWithoutPublishing() => HighlightTestDispatcher.Run(() =>
    {
        var tray = new WindowsTrayIcon(publish: false);
        var icon = tray.Icon; var menu = tray.Menu; var toggles = 0; var exits = 0;
        tray.ToggleRequested += () => toggles++; tray.ExitRequested += () => exits++;
        Assert.NotNull(icon.Icon); Assert.NotEqual(IntPtr.Zero, icon.Icon.Handle);
        Assert.Equal("School Timetable Widget", icon.Text); Assert.False(icon.Visible);
        Assert.Equal(4, menu.Items.Count); Assert.IsType<Forms.ToolStripSeparator>(menu.Items[2]);
        Assert.Equal("종료", menu.Items[3].Text);
        tray.SetWindowVisible(true); Assert.Equal("위젯 숨기기", menu.Items[0].Text);
        menu.Items[0].PerformClick(); tray.SetWindowVisible(false);
        Assert.Equal("위젯 보이기", menu.Items[0].Text);
        var dispatch = typeof(Forms.NotifyIcon).GetMethod("OnMouseDoubleClick", BindingFlags.NonPublic | BindingFlags.Instance)!;
        dispatch.Invoke(icon, [new Forms.MouseEventArgs(Forms.MouseButtons.Left, 2, 0, 0, 0)]);
        dispatch.Invoke(icon, [new Forms.MouseEventArgs(Forms.MouseButtons.Right, 2, 0, 0, 0)]);
        menu.Items[3].PerformClick();
        Assert.Equal(2, toggles); Assert.Equal(1, exits);
        var iconDisposed = false; icon.Disposed += (_, _) => iconDisposed = true;
        tray.Dispose(); tray.Dispose();
        Assert.True(iconDisposed); Assert.True(menu.IsDisposed); Assert.False(icon.Visible);
    });

    [Fact]
    public void WpfCloseEventIsCancelledUntilExplicitExit() => HighlightTestDispatcher.Run(() =>
    {
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), new(WeeklyTimetable.Empty()));
        Assert.False(window.ShowInTaskbar);
        var controller = new WindowPlacementController(window, new MemoryWindowStore(), new FakePlacementDesktop());
        var adapter = new WpfWidgetWindow(window, controller, () => { });
        var closed = false; window.Closed += (_, _) => closed = true;
        using var lifecycle = new WidgetTrayLifecycle(adapter, new FakeTrayIcon(), null, () => window.Close());
        window.Close(); Assert.False(closed); Assert.False(window.IsVisible);
        lifecycle.Exit(); Assert.True(closed);
    });

    [Fact]
    public void DeepestOwnedDialogDiscoveryUsesOwnerTree() => HighlightTestDispatcher.Run(() =>
    {
        var main = new Window(); new WindowInteropHelper(main).EnsureHandle();
        var settings = new Window { Owner = main }; new WindowInteropHelper(settings).EnsureHandle();
        var editor = new Window { Owner = settings };
        try
        {
            Assert.Same(editor, WpfWidgetWindow.FindDialog(main, _ => true));
            Assert.Same(settings, WpfWidgetWindow.FindDialog(main, w => w != editor));
            Assert.Null(WpfWidgetWindow.FindDialog(main, _ => false));
        }
        finally { editor.Close(); settings.Close(); main.Close(); }
    });

    [Fact]
    public void RefitAfterMonitorChangeAndNormalClosePreservesPreferredGeometry() => HighlightTestDispatcher.Run(() =>
    {
        var store = new MemoryWindowStore(new(900, 850, 500, 200, "A"));
        var preferred = store.Saved;
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), new(WeeklyTimetable.Empty()));
        var desktop = new FakePlacementDesktop();
        var controller = new WindowPlacementController(window, store, desktop);
        var fake = new FakeWidgetWindow { OnShow = controller.PrepareForShow };
        using var lifecycle = new WidgetTrayLifecycle(fake, new FakeTrayIcon(), null, () => { });
        try
        {
            lifecycle.Hide();
            desktop.Monitors = [new("B", -800, 0, 800, 600, 1, true)];
            lifecycle.Show();
            Assert.Equal(800, window.Width); Assert.Equal(600, window.Height);
            fake.Close(); lifecycle.Show();
            Assert.Equal(preferred, controller.Session.Preferred); Assert.Equal(0, store.Writes);
        }
        finally { window.Close(); }
        Assert.Equal(preferred, store.Saved); Assert.Equal(0, store.Writes);
    });

    [Fact]
    public void HiddenRuntimeRefreshAndShowKeepViewedWeekAndProfile() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store);
        var runtime = new ProfileRuntime(session, () => { }, _ => { });
        var original = session.Current;
        var clock = new FakeApplicationClock(new(new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.FromHours(9)),
            ApplicationTimeSource.PcLocalFallback, 0));
        var header = new CurrentStatusHeaderViewModel();
        using var loop = new CurrentStatusRefreshLoop(clock, runtime.Resolve, header, runtime.Timetable, () => runtime.Lunch.Enabled);
        loop.Start(); runtime.Timetable.NextWeekCommand.Execute(null);
        var week = runtime.Timetable.ViewedWeekStart;
        var window = new FakeWidgetWindow { OnShow = loop.RefreshNow };
        using var lifecycle = new WidgetTrayLifecycle(window, new FakeTrayIcon(), null, () => { });
        lifecycle.Hide();
        clock.CurrentSnapshot = new(clock.CurrentSnapshot.LocalTime.AddDays(1), ApplicationTimeSource.PcLocalFallback, 1);
        loop.RefreshNow();
        Assert.Equal(new DateOnly(2026, 9, 17), loop.CurrentDate);
        lifecycle.Show();
        Assert.Equal(week, runtime.Timetable.ViewedWeekStart); Assert.Same(original, session.Current);
        Assert.False(File.Exists(temp.File));
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void DegradedAndRecoveryStateSurviveCloseShowAndSecondaryGate(bool recoveryRequired) => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile();
        var corrupt = System.Text.Encoding.UTF8.GetBytes("invalid profile");
        File.WriteAllBytes(temp.File, corrupt);
        var recovery = new ProfileRecoveryFiles(temp.Directory);
        if (recoveryRequired) recovery.Secure(ProfileSnapshot.Defaults(), ProfileLoadState.Invalid, corrupt);
        using var store = new JsonProfileStore(temp.Directory); var session = new ProfileSession(store);
        var runtime = new ProfileRuntime(session, () => { }, _ => { });
        var before = Directory.GetFiles(temp.Directory).Where(p => Path.GetFileName(p) != "profile.lock").ToDictionary(p => p, File.ReadAllBytes);
        var original = session.Current;
        var window = new FakeWidgetWindow();
        using var lifecycle = new WidgetTrayLifecycle(window, new FakeTrayIcon(), new JsonTrayNoticeStore(temp.Directory), () => { });
        window.Close(); lifecycle.Show();
        var secondary = new ActivationOnly(lifecycle.Show);
        Assert.False(SingleInstanceStartup.Run(secondary, () => throw new Exception("Secondary opened profile")));
        Assert.Equal(1, secondary.Signals);
        Assert.Equal(recoveryRequired, session.IsRecoveryRequired);
        Assert.False(session.LoadResult.CanWrite); Assert.Same(original, session.Current);
        Assert.Same(original.Timetable, runtime.Timetable.CommittedTimetable);
        foreach (var pair in before) Assert.Equal(pair.Value, File.ReadAllBytes(pair.Key));
    });

    [Fact]
    public void ModalExitKeepsRealSettingsDraftUntilUserCancels() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store); var runtime = new ProfileRuntime(session, () => { }, _ => { });
        var draft = runtime.Display.Open(); draft.ShowSeconds = !draft.ShowSeconds;
        var preview = runtime.Display.Current;
        var window = new FakeWidgetWindow { DialogOpen = true };
        var exits = 0;
        using var lifecycle = new WidgetTrayLifecycle(window, new FakeTrayIcon(), null, () => exits++);
        lifecycle.Hide(); lifecycle.Exit(); lifecycle.Show();
        Assert.False(draft.IsClosed); Assert.True(runtime.Display.HasActiveSession);
        Assert.Same(preview, runtime.Display.Current); Assert.Equal(0, exits);
        Assert.False(File.Exists(temp.File));
        draft.Cancel(); window.DialogOpen = false; lifecycle.Exit();
        Assert.Equal(1, exits); Assert.False(runtime.Display.HasActiveSession);
        Assert.Equal(session.Current.Display, runtime.Display.Current);
    });
    private sealed class ActivationOnly(Action show) : IInstanceOwnership
    {
        public bool IsPrimary => false;
        public int Signals;
        public void SignalActivation() { Signals++; show(); }
    }
}
