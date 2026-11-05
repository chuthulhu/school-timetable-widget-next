using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.DateOverrides;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Features.Semesters;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.TimetableImport;
using SchoolTimetableWidget.Desktop.Features.TrayLifecycle;
using SchoolTimetableWidget.Desktop.Features.WidgetMenu;
using SchoolTimetableWidget.Desktop.Features.WindowPlacement;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using SchoolTimetableWidget.Tests.Persistence;
using SchoolTimetableWidget.Tests.Timetable;
using SchoolTimetableWidget.Tests.TimetableImport;
using SchoolTimetableWidget.Tests.WindowPlacement;

namespace SchoolTimetableWidget.Tests.WidgetMenu;

/// <summary>ADR 0033: one right-click menu, its grouping, recovery guard, hide and position lock.</summary>
public class WidgetContextMenuTests
{
    [Fact]
    public void ClockAreaAndTimetableShareOneGroupedMenu() => HighlightTestDispatcher.Run(() =>
    {
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), new(WeeklyTimetable.Empty()));
        try
        {
            var table = (WeeklyTimetableView)window.FindName("Timetable");
            Assert.NotNull(window.ContextMenu);
            Assert.Same(table.ContextMenu, window.ContextMenu);
            Assert.Null(((FrameworkElement)window.FindName("StatusHeader")).ContextMenu); // falls through to the window's menu
            Assert.Equal(new[] { "시간표", "일과", "-", "표시 설정...", "창", "데이터", "-", "위치 고정", "위젯 숨기기" }, TopLevel(window.ContextMenu));
            Assert.Equal(new[] { "학교 시간표 가져오기...", "표준 양식 가져오기...", "-", "표준 양식 복사" }, Children(window.ContextMenu, "시간표"));
            Assert.Equal(new[] { "일과 시간 편집...", "날짜별 예외 설정...", "-", "점심시간 표시 (4·5교시 사이)" }, Children(window.ContextMenu, "일과"));
            Assert.Equal(new[] { "창 설정...", "창 위치·크기 초기화" }, Children(window.ContextMenu, "창"));
            Assert.Equal(new[] { "백업...", "복원..." }, Children(window.ContextMenu, "데이터"));
            Assert.All(WidgetContextMenu.AllItems(window.ContextMenu).Where(item => item.Command is not null),
                item => Assert.Same(table, item.CommandTarget));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void SemesterSubmenuLeadsTheSharedMenuOnce() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var runtime = new ProfileRuntime(new ProfileSession(store), () => { }, _ => { });
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), runtime.Timetable, runtime.ScheduleEditor,
            runtime: runtime);
        try
        {
            var menu = window.ContextMenu;
            Assert.IsType<SemesterMenu>(menu.Items[0]);
            Assert.IsType<Separator>(menu.Items[1]);
            Assert.Single(menu.Items.OfType<SemesterMenu>());
            Assert.Equal("시간표", ((MenuItem)menu.Items[2]).Header);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void DisabledTimetableOffersNoTimetableActionsFromTheSharedMenu() => HighlightTestDispatcher.Run(() =>
    {
        var clipboard = new ImportTransactionTests.MemoryClipboard();
        var view = new WeeklyTimetableView
        {
            DataContext = new WeeklyTimetableViewModel(WeeklyTimetable.Empty()), ImportActions = new(clipboard),
            LunchOption = new LunchPresentationOption(() => { })
        };
        Assert.True(TimetableImportCommands.School.CanExecute(null, view));
        Assert.True(TimetableImportCommands.CopyTemplate.CanExecute(null, view));
        Assert.True(DateOverrideCommands.Lunch.CanExecute(null, view));
        view.IsEnabled = false; // MainWindow disables the table while recovery is required.
        Assert.False(TimetableImportCommands.School.CanExecute(null, view));
        Assert.False(TimetableImportCommands.Canonical.CanExecute(null, view));
        Assert.False(TimetableImportCommands.CopyTemplate.CanExecute(null, view));
        Assert.False(DateOverrideCommands.Lunch.CanExecute(null, view));
        Assert.Equal(0, clipboard.Writes);
    });

    [Fact]
    public void HideIsBoundOnTheWindowAndReachableFromTheMenuTarget() => HighlightTestDispatcher.Run(() =>
    {
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), new(WeeklyTimetable.Empty()));
        try
        {
            var table = (WeeklyTimetableView)window.FindName("Timetable");
            Assert.Contains(window.CommandBindings.Cast<CommandBinding>(), binding => binding.Command == WidgetCommands.Hide);
            Assert.True(WidgetCommands.Hide.CanExecute(null, table));
            var item = Assert.Single(WidgetContextMenu.AllItems(window.ContextMenu), i => i.Command == WidgetCommands.Hide);
            Assert.Equal("위젯 숨기기", item.Header);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void PositionLockTogglesTheSavedMovementLockAndItsMenuLabel() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); var store = new JsonWindowStateStore(temp.Directory);
        Assert.True(store.Save(new(846, 724, 120, 151, "A")));
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), new(WeeklyTimetable.Empty()));
        var errors = new List<string>();
        var controller = new WindowPlacementController(window, store, new FakePlacementDesktop(), errors.Add);
        try
        {
            var table = (WeeklyTimetableView)window.FindName("Timetable");
            var item = Assert.Single(WidgetContextMenu.AllItems(window.ContextMenu), i => i.Command == WindowPlacementCommands.ToggleMoveLock);
            Assert.Equal("위치 고정", item.Header);
            Assert.True(WindowPlacementCommands.ToggleMoveLock.CanExecute(null, table));

            WindowPlacementCommands.ToggleMoveLock.Execute(null, table); Drain(window);
            Assert.True(controller.Session.Preferred.MoveLocked);
            Assert.True(store.Load()!.MoveLocked);
            Assert.Equal(846, store.Load()!.Width);
            Assert.Equal("위치 고정 풀기", item.Header);

            WindowPlacementCommands.ToggleMoveLock.Execute(null, table); Drain(window);
            Assert.False(controller.Session.Preferred.MoveLocked);
            Assert.False(store.Load()!.MoveLocked);
            Assert.Equal("위치 고정", item.Header);
            Assert.Empty(errors);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void FailedPositionLockSaveKeepsTheCommittedLockAndReportsIt() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); var store = new JsonWindowStateStore(temp.Directory);
        Assert.True(store.Save(PreferredWindowBounds.Default));
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), new(WeeklyTimetable.Empty()));
        var errors = new List<string>();
        var controller = new WindowPlacementController(window, store, new FakePlacementDesktop(), errors.Add);
        try
        {
            var item = Assert.Single(WidgetContextMenu.AllItems(window.ContextMenu), i => i.Command == WindowPlacementCommands.ToggleMoveLock);
            store.BeforeRename = () => throw new IOException("injected failure");
            Assert.False(controller.ToggleMoveLock());
            Assert.False(controller.Session.Preferred.MoveLocked);
            Assert.False(store.Load()!.MoveLocked);
            Assert.Equal("위치 고정", item.Header);
            Assert.Equal(new[] { WindowPlacementCommands.MoveLockSaveError }, errors);
        }
        finally { store.BeforeRename = null; window.Close(); }
    });

    [Fact]
    public void WindowSettingsLockChangeAlsoUpdatesTheMenuLabel() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); var store = new JsonWindowStateStore(temp.Directory);
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), new(WeeklyTimetable.Empty()));
        var controller = new WindowPlacementController(window, store, new FakePlacementDesktop());
        try
        {
            var item = Assert.Single(WidgetContextMenu.AllItems(window.ContextMenu), i => i.Command == WindowPlacementCommands.ToggleMoveLock);
            var preferred = controller.Session.Preferred;
            Assert.True(controller.TrySetPreferences(preferred.Width, preferred.Height, true));
            Assert.Equal("위치 고정 풀기", item.Header);
        }
        finally { window.Close(); }
    });

    private static string[] TopLevel(ItemsControl menu) => menu.Items.Cast<object>().Select(Label).ToArray();
    private static string[] Children(ItemsControl menu, string header) =>
        TopLevel(menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, header)));
    private static string Label(object item) => item is MenuItem menuItem ? (string)menuItem.Header : "-";
    private static void Drain(DispatcherObject target) => target.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
}
