using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Features.WindowPlacement;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using SchoolTimetableWidget.Tests.Persistence;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.WindowPlacement;

// Unshown compiled WPF object/event tests. They do not establish actual native drag or DPI behavior.
public class WindowPlacementWpfTests
{
    [Fact]
    public void MeasuredMultilineGrowsRepositionsThenShrinksWithoutWriting() => HighlightTestDispatcher.Run(() =>
    {
        var model = new SchoolTimetableWidget.Desktop.Features.Timetable.WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), model);
        var store = new MemoryWindowStore(new(800, 700, 100, 300, "A"));
        var desktop = new FakePlacementDesktop();
        var controller = new WindowPlacementController(window, store, desktop);
        try
        {
            Layout(window);
            var edit = model.Editor.BeginEdit(model.Cells[0]);
            edit.SubjectText = string.Join("\n", Enumerable.Repeat("여러 줄", 12));
            Assert.True(edit.TryApply());
            WindowContentMinimum.Refresh(window); Drain(window);
            Assert.True(window.Height > 700);
            Assert.True(desktop.Last.TopPixels < 300);
            Assert.Equal(700, controller.Session.Preferred.Height);
            Assert.Equal(300, controller.Session.Preferred.Top);
            Assert.Equal(0, store.Writes);
            Layout(window);
            edit = model.Editor.BeginEdit(model.Cells[0]); edit.SubjectText = "짧게"; Assert.True(edit.TryApply());
            WindowContentMinimum.Refresh(window); Drain(window);
            Assert.Equal(700, window.Height, 4); Assert.Equal(300, desktop.Last.TopPixels, 4);
        }
        finally { window.Close(); }
        Assert.Equal(0, store.Writes);
    });

    [Fact]
    public void NativeMessageSeamDefersFitAndWritesOnceAtCompletedGesture() => HighlightTestDispatcher.Run(() =>
    {
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), new(WeeklyTimetable.Empty()));
        var store = new MemoryWindowStore(new(800, 700, 100, 300, "A"));
        var desktop = new FakePlacementDesktop { Observed = new(800, 1000, 100, 100, "A") };
        var controller = new WindowPlacementController(window, store, desktop);
        try
        {
            Layout(window);
            controller.ProcessMessage(0x0231); controller.ProcessMessage(0x0216);
            var calls = desktop.Applies;
            for (var i = 0; i < 10; i++) { controller.Apply(400, 1000); controller.ProcessMessage(0x0216); }
            Assert.Equal(calls, desktop.Applies); Assert.Equal(0, store.Writes);
            desktop.Observed = desktop.Observed with { Top = 120 };
            controller.ProcessMessage(0x0232); Drain(window);
            Assert.Equal(120, controller.Session.Preferred.Top); Assert.Equal(700, controller.Session.Preferred.Height);
            Assert.Equal(1, store.Writes);
            window.Width = 1000; window.Height = 900; window.Top = 20;
            controller.Apply(400, 1000); Drain(window);
            Assert.Equal(1, store.Writes); Assert.Equal(800, controller.Session.Preferred.Width);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData("LEFT", 1.0, -1920)]
    [InlineData("LEFT", 1.25, -1920)]
    [InlineData("LEFT", 1.5, -1920)]
    [InlineData("MISSING", 1.5, 0)]
    public void SavedMonitorOrPrimaryFallbackUsesCurrentDpi(string hint, double scale, double expectedOrigin) => HighlightTestDispatcher.Run(() =>
    {
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), new(WeeklyTimetable.Empty()));
        var saved = new PreferredWindowBounds(780, 700, 100, 50, hint);
        var store = new MemoryWindowStore(saved);
        var desktop = new FakePlacementDesktop
        {
            Monitors = [new("LEFT", -1920, -100, 1920, 1440, scale), new("A", 0, 0, 2560, 1440, scale, true)]
        };
        var controller = new WindowPlacementController(window, store, desktop);
        try
        {
            controller.Apply(400, 500);
            Assert.Equal(expectedOrigin + 100 * scale, desktop.Last.LeftPixels);
            Assert.Equal(780, window.Width); Assert.Equal(700, window.Height);
            Assert.Equal(saved, controller.Session.Preferred); Assert.Equal(0, store.Writes);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void WorkAreaChangeAndSourceTargetDpiTransitionKeepPreferredAndFitBothAxes() => HighlightTestDispatcher.Run(() =>
    {
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), new(WeeklyTimetable.Empty()));
        var saved = new PreferredWindowBounds(1400, 900, 400, 300, "A");
        var store = new MemoryWindowStore(saved); var desktop = new FakePlacementDesktop();
        var controller = new WindowPlacementController(window, store, desktop);
        try
        {
            foreach (var scale in new[] { 1d, 1.25, 1.5, 1d })
            {
                desktop.Monitors = [new("A", -1200, 40, 1200, 960, scale, true)];
                controller.Apply(400, 1000);
                Assert.True(window.Width * scale <= 1200); Assert.True(window.Height * scale <= 960);
                Assert.Equal(-1200, desktop.Last.LeftPixels); Assert.Equal(40, desktop.Last.TopPixels);
                Assert.Equal(saved, controller.Session.Preferred);
            }
            desktop.Monitors = [new("A", 0, 0, 2560, 1600, 1, true)];
            controller.Apply(400, 500);
            Assert.Equal(1400, window.Width); Assert.Equal(900, window.Height);
            Assert.Equal(400, desktop.Last.LeftPixels); Assert.Equal(300, desktop.Last.TopPixels);
            Assert.Equal(0, store.Writes);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void RestoreAndResetDoNotCrossProfileWindowBoundary() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); using var profileStore = new JsonProfileStore(temp.Directory);
        var profile = new ProfileSession(profileStore); var runtime = new ProfileRuntime(profile, () => { }, _ => { });
        var store = new JsonWindowStateStore(temp.Directory);
        var saved = new PreferredWindowBounds(800, 700, 100, 300, "A"); Assert.True(store.Save(saved));
        var before = File.ReadAllBytes(store.FilePath);
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), runtime.Timetable);
        var desktop = new FakePlacementDesktop(); var controller = new WindowPlacementController(window, store, desktop);
        runtime.Timetable.ContentChanged += (_, _) => WindowContentMinimum.Refresh(window);
        try
        {
            Layout(window);
            Assert.Null(runtime.Restore(ProfileBackupFileTests.Sample())); Drain(window);
            Assert.Equal(before, File.ReadAllBytes(store.FilePath)); Assert.Equal(saved, controller.Session.Preferred);
            var profileBytes = File.ReadAllBytes(temp.File);
            var backup = Path.Combine(temp.Directory, "test.stwbackup"); profile.ExportBackup(backup);
            Assert.DoesNotContain("windowStateVersion", File.ReadAllText(backup));
            Assert.Throws<IOException>(() => profile.ExportBackup(store.FilePath));
            WindowPlacementCommands.Reset.Execute(null, window); Drain(window);
            Assert.Equal(PreferredWindowBounds.Default with { MonitorHint = "A" }, store.Load());
            Assert.Equal(profileBytes, File.ReadAllBytes(temp.File));
            Assert.Equal(40, desktop.Last.LeftPixels); Assert.Equal(40, desktop.Last.TopPixels);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void DegradedOrRecoveryRequiredDoesNotBlockLocalGestureSave(bool recovery)
    {
        using var temp = new TempProfile();
        File.WriteAllText(temp.File, "손상 원본");
        if (recovery) File.WriteAllText(Path.Combine(temp.Directory, "recovery-required.json"), "{}");
        using var profileStore = new JsonProfileStore(temp.Directory);
        var profile = new ProfileSession(profileStore);
        Assert.False(profile.LoadResult.CanWrite);
        Assert.Equal(recovery, profile.IsRecoveryRequired);
        var bytes = File.ReadAllBytes(temp.File);
        var store = new JsonWindowStateStore(temp.Directory); var placement = new WindowPlacementSession(store);
        placement.Begin(placement.Preferred); placement.Moving(); placement.End(placement.Preferred with { Top = 100 });
        Assert.Equal(100, store.Load()!.Top); Assert.Equal(bytes, File.ReadAllBytes(temp.File));
        Assert.False(profile.LoadResult.CanWrite); Assert.Equal(recovery, profile.IsRecoveryRequired);
    }

    [Fact]
    public void UnshownHwndInitializesAndAppliesUsingActualMonitorApi() => HighlightTestDispatcher.Run(() =>
    {
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), new(WeeklyTimetable.Empty()));
        var store = new MemoryWindowStore(new(800, 600, 100000, 100000, "MISSING"));
        var controller = new WindowPlacementController(window, store);
        try
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle();
            Assert.NotEqual(nint.Zero, handle);
            Assert.False(window.IsVisible);
            var native = new NativeWindowPlacement(window);
            var area = native.PrepareMonitor(controller.Session.Preferred.MonitorHint);
            controller.Apply(400, 500);
            var actual = native.Observe(); Assert.NotNull(actual);
            Assert.InRange(actual.Left, -1, area.Width - actual.Width + 1);
            Assert.InRange(actual.Top, -1, area.Height - actual.Height + 1);
            Assert.Equal(600, controller.Session.Preferred.Height);
            Assert.Equal(0, store.Writes);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void OrdinaryClockTicksDoNotFitOrPersistAttachedPlacement() => HighlightTestDispatcher.Run(() =>
    {
        var clock = new SchoolTimetableWidget.Tests.Time.FakeApplicationClock(new(
            new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.FromHours(9)),
            SchoolTimetableWidget.Core.Time.ApplicationTimeSource.PcLocalFallback, 0));
        var header = new CurrentStatusHeaderViewModel();
        var model = new SchoolTimetableWidget.Desktop.Features.Timetable.WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var window = new MainWindow(header, model); var store = new MemoryWindowStore();
        var desktop = new FakePlacementDesktop(); var controller = new WindowPlacementController(window, store, desktop);
        try
        {
            Layout(window); var calls = desktop.Applies;
            using var loop = new CurrentStatusRefreshLoop(clock,
                SchoolTimetableWidget.Core.Features.Periods.DefaultPeriodSchedule.Periods, header, model);
            for (var i = 0; i < 100; i++)
            {
                clock.CurrentSnapshot = new(clock.CurrentSnapshot.LocalTime.AddSeconds(1),
                    SchoolTimetableWidget.Core.Time.ApplicationTimeSource.PcLocalFallback, 0);
                loop.RefreshNow(); Drain(window);
            }
            Assert.Equal(calls, desktop.Applies); Assert.Equal(0, store.Writes);
            Assert.Equal(PreferredWindowBounds.Default, controller.Session.Preferred);
        }
        finally { window.Close(); }
    });
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ActualContextMenuClickResetsDurablePreference(bool timetableMenu) => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile();
        var store = new JsonWindowStateStore(temp.Directory);
        Assert.True(store.Save(new(846, 724, 1020, 151, "A")));
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), new(WeeklyTimetable.Empty()));
        var desktop = new FakePlacementDesktop();
        var controller = new WindowPlacementController(window, store, desktop);
        try
        {
            Layout(window);
            var target = timetableMenu ? (FrameworkElement)window.FindName("Timetable") : window;
            var menu = target.ContextMenu;
            menu.PlacementTarget = target;
            menu.Measure(new Size(400, 600)); menu.Arrange(new Rect(0, 0, 400, 600)); menu.UpdateLayout();
            Drain(window);
            var item = Assert.Single(menu.Items.OfType<MenuItem>(), x => x.Command == WindowPlacementCommands.Reset);
            Assert.Same(target, item.CommandTarget);
            Assert.True(WindowPlacementCommands.Reset.CanExecute(null, target));
            typeof(MenuItem).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(item, null);
            Drain(window);
            Assert.Equal(PreferredWindowBounds.Default with { MonitorHint = "A" }, store.Load());
            Assert.Equal(800, window.Width); Assert.Equal(600, window.Height);
            Assert.Equal(40, desktop.Last.LeftPixels); Assert.Equal(40, desktop.Last.TopPixels);
        }
        finally { window.Close(); }
    });
    private static void Layout(MainWindow window)
    {
        var root = (Grid)window.Content; Drain(window);
        root.Measure(new Size(window.Width, window.Height));
        root.Arrange(new Rect(0, 0, window.Width, window.Height)); root.UpdateLayout();
        WindowContentMinimum.Refresh(window); Drain(window);
    }
    private static void Drain(Window window) => window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
}

internal sealed class FakePlacementDesktop : IWindowPlacementDesktop
{
    public WindowWorkArea[] Monitors { get; set; } = [new("A", 0, 0, 1600, 1100, 1, true)];
    public PreferredWindowBounds? Observed { get; set; } = new(800, 700, 100, 300, "A");
    public AppliedWindowBounds Last { get; private set; }
    public int Applies { get; private set; }
    public WindowWorkArea PrepareMonitor(string? hint) => WindowBoundsCalculator.SelectMonitor(Monitors, hint)!.Value;
    public PreferredWindowBounds? Observe() => Observed;
    public void ApplyPosition(AppliedWindowBounds bounds) { Last = bounds; Applies++; }
}
