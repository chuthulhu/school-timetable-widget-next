using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Desktop.Features.DateOverrides;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Tests.Time;
using SchoolTimetableWidget.Tests.Timetable;
namespace SchoolTimetableWidget.Tests.DateOverrides;

// Unshown STA objects/events/bindings; no keyboard, IME, focus, pixels or native-X claim.
public class DateOverrideViewTests
{
    [Fact]
    public void DateSelectionLocksTargetAndBothIndependentTogglesBindCompleteDrafts() => HighlightTestDispatcher.Run(() =>
    {
        var store = new RuntimeDateOverrides(); var refreshes = 0;
        var editor = new DateOverrideEditor(store, DayFixtures.Week, DayFixtures.Schedule, _ => refreshes++);
        var window = new DateOverrideEditorWindow(editor, DayFixtures.Monday);
        try
        {
            Layout((FrameworkElement)window.Content);
            var picker = (DatePicker)window.FindName("DateInput");
            Assert.Equal(DayFixtures.Monday.ToDateTime(TimeOnly.MinValue), picker.SelectedDate);
            Assert.False(((Button)window.FindName("ApplyButton")).IsEnabled);
            Click(window, "BeginButton"); Layout((FrameworkElement)window.Content);
            Assert.False(picker.IsEnabled); Assert.NotNull(window.Session);
            Assert.Equal(7, ((ItemsControl)window.FindName("TimetableRows")).Items.Count);
            Assert.Equal(7, ((ItemsControl)window.FindName("ScheduleRows")).Items.Count);
            var timetable = (CheckBox)window.FindName("TimetableToggle"); var schedule = (CheckBox)window.FindName("ScheduleToggle");
            Assert.False(timetable.IsChecked); Assert.False(schedule.IsChecked);
            timetable.IsChecked = true; schedule.IsChecked = true; Drain(window);
            Assert.True(window.Session.UseTimetable); Assert.True(window.Session.UseSchedule);
            var fields = Descendants<TextBox>((ItemsControl)window.FindName("TimetableRows")).ToArray();
            Assert.Equal(14, fields.Length); Assert.All(fields, f => Assert.True(f.AcceptsReturn));
            fields[0].Text = "한글\n수업"; fields[1].Text = " 반 "; Drain(window);
            var times = Descendants<TextBox>((ItemsControl)window.FindName("ScheduleRows")).ToArray();
            Assert.Equal(14, times.Length); times[8].Text = "invalid"; Drain(window);
            Click(window, "ApplyButton"); Drain(window);
            Assert.Null(store.Get(DayFixtures.Monday)); Assert.NotEmpty(((TextBlock)window.FindName("ErrorMessage")).Text);
            times[8].Text = "13:00"; times[9].Text = "13:50"; Drain(window); Click(window, "ApplyButton");
            Assert.True(window.Session.IsApplied); Assert.Equal(1, refreshes);
            Assert.Equal("한글\n수업", store.Get(DayFixtures.Monday)!.Timetable![1].SubjectText);
            Assert.Equal(" 반 ", store.Get(DayFixtures.Monday)!.Timetable![1].ClassText);
        }
        finally { window.Close(); }
    });
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CancelButtonOrProgrammaticCloseDiscardsBothDrafts(bool cancel) => HighlightTestDispatcher.Run(() =>
    {
        var store = new RuntimeDateOverrides();
        var window = new DateOverrideEditorWindow(new(store, DayFixtures.Week, DayFixtures.Schedule, _ => throw new Exception()), DayFixtures.Monday);
        Click(window, "BeginButton"); var session = window.Session!;
        session.UseSchedule = session.UseTimetable = true; session.TimetableRows[0].SubjectText = "draft";
        Assert.True(((Button)window.FindName("CancelButton")).IsCancel);
        Assert.False(((Button)window.FindName("ApplyButton")).IsDefault);
        if (cancel) Click(window, "CancelButton"); else window.Close();
        Assert.True(session.IsClosed); Assert.False(session.IsApplied); Assert.Null(store.Get(DayFixtures.Monday));
    });
    [Fact]
    public void WeekendRejectedAndExplicitDateChangeDiscardsUnappliedDraft() => HighlightTestDispatcher.Run(() =>
    {
        var store = new RuntimeDateOverrides();
        var window = new DateOverrideEditorWindow(new(store, DayFixtures.Week, DayFixtures.Schedule, _ => { }), new(2026, 9, 12));
        try
        {
            Click(window, "BeginButton"); Assert.Null(window.Session);
            Assert.Contains("주말", ((TextBlock)window.FindName("DateError")).Text);
            var picker = (DatePicker)window.FindName("DateInput"); picker.SelectedDate = new DateTime(2026, 9, 17);
            Click(window, "BeginButton"); var first = window.Session!;
            first.UseTimetable = true; first.TimetableRows[0].SubjectText = "discard";
            Click(window, "ChangeDateButton"); Assert.True(first.IsClosed); Assert.True(picker.IsEnabled);
            picker.SelectedDate = new DateTime(2026, 9, 18); Click(window, "BeginButton");
            Assert.Equal(new DateOnly(2026, 9, 18), window.Session!.Date);
            Assert.Equal("Friday-1", window.Session.TimetableRows[0].SubjectText);
            Assert.Null(store.Get(new(2026, 9, 17)));
        }
        finally { window.Close(); }
    });
    [Fact]
    public void RemoveAllRequiresExplicitApplyAndThenReturnsToBase() => HighlightTestDispatcher.Run(() =>
    {
        var store = new RuntimeDateOverrides(); var original = new DateSpecificOverride(DayFixtures.Monday, DayFixtures.Day(), DayFixtures.ShortSchedule());
        store.TryReplace(original.Date, null, original);
        var window = new DateOverrideEditorWindow(new(store, DayFixtures.Week, DayFixtures.Schedule, _ => { }), original.Date);
        try
        {
            Click(window, "BeginButton"); Click(window, "RemoveButton");
            Assert.False(window.Session!.UseTimetable); Assert.False(window.Session.UseSchedule);
            Assert.Same(original, store.Get(original.Date));
            Click(window, "ApplyButton"); Assert.Null(store.Get(original.Date)); Assert.True(window.Session.IsApplied);
        }
        finally { window.Close(); }
    });
    [Fact]
    public void DateAndLunchMenusBindSuppliedRuntimeAndUpdateHeaderWithoutNativeInput() => HighlightTestDispatcher.Run(() =>
    {
        var store = new RuntimeDateOverrides(); var vm = new WeeklyTimetableViewModel(DayFixtures.Week());
        var clock = new FakeApplicationClock(DayFixtures.Time()); var header = new CurrentStatusHeaderViewModel();
        CurrentStatusRefreshLoop? loop = null; var option = new LunchPresentationOption(() => loop!.RefreshNow());
        using (loop = new(clock, date => EffectiveDayResolver.Resolve(date, vm.CommittedTimetable, DayFixtures.Schedule(), store.Get(date)), header, vm, () => option.Enabled))
        {
            loop.RefreshNow();
            var view = new WeeklyTimetableView { DataContext = vm, LunchOption = option,
                DateEditor = new(store, () => vm.CommittedTimetable, DayFixtures.Schedule, _ => loop.RefreshNow()), GetCurrentDate = () => loop.CurrentDate };
            view.ContextMenu.PlacementTarget = view; Layout(view.ContextMenu);
            var edit = Assert.Single(view.ContextMenu.Items.OfType<MenuItem>(), m => m.Command == DateOverrideCommands.Edit);
            var lunch = Assert.Single(view.ContextMenu.Items.OfType<MenuItem>(), m => m.Command == DateOverrideCommands.Lunch);
            Assert.Same(view, edit.CommandTarget);
            Assert.True(DateOverrideCommands.Edit.CanExecute(null, view)); Assert.True(lunch.IsCheckable); Assert.False(lunch.IsChecked);
            DateOverrideCommands.Lunch.Execute(null, view); Drain(view);
            Assert.True(option.Enabled); Assert.True(lunch.IsChecked); Assert.StartsWith("점심시간", header.StatusText);
            Assert.Equal(2, clock.ReadCount); Assert.DoesNotContain(vm.Cells, c => c.IsCurrent);
            DateOverrideCommands.Lunch.Execute(null, view); Drain(view);
            Assert.False(lunch.IsChecked); Assert.StartsWith("쉬는시간", header.StatusText);
        }
    });
    [Fact]
    public void EffectiveProjectionUsesStableControlsAndLunchHighlightDoesNotAlterGeometry() => HighlightTestDispatcher.Run(() =>
    {
        var store = new RuntimeDateOverrides(); var week = DayFixtures.Week(); var vm = new WeeklyTimetableViewModel(week);
        var entry = new DateSpecificOverride(DayFixtures.Monday, DayFixtures.Day(), null); store.TryReplace(entry.Date, null, entry);
        var clock = new FakeApplicationClock(DayFixtures.Time()); var header = new CurrentStatusHeaderViewModel();
        using var loop = new CurrentStatusRefreshLoop(clock, date => EffectiveDayResolver.Resolve(date, week, DayFixtures.Schedule(), store.Get(date)), header, vm, () => true);
        vm.Editor.DateEditor = new(store, () => week, DayFixtures.Schedule, _ => loop.RefreshNow());
        loop.RefreshNow(); var view = new WeeklyTimetableView { DataContext = vm }; Layout(view);
        var controls = Descendants<TimetableCellControl>(view).ToArray(); Assert.Equal(35, controls.Length);
        var rectangles = controls.Select(c => new Rect(c.TranslatePoint(new Point(), view), c.RenderSize)).ToArray();
        Assert.StartsWith("점심시간", header.StatusText); Assert.DoesNotContain(vm.Cells, c => c.IsCurrent);
        clock.CurrentSnapshot = DayFixtures.Time(7, 14, 0); loop.RefreshNow(); Layout(view);
        Assert.Same(vm.Cells[20], Assert.Single(vm.Cells, c => c.IsCurrent));
        Assert.Equal(controls, Descendants<TimetableCellControl>(view));
        Assert.Equal(rectangles, controls.Select(c => new Rect(c.TranslatePoint(new Point(), view), c.RenderSize)));
        var session = vm.Editor.BeginEdit(vm.Cells[20]); var dialog = new CellEditorWindow(session);
        try
        {
            Layout((FrameworkElement)dialog.Content);
            Assert.Contains(Descendants<TextBlock>((FrameworkElement)dialog.Content), t => t.Text.Contains("편집 대상: 2026년 09월 07일"));
        }
        finally { dialog.Close(); }
    });
    private static void Click(Window window, string name) => ((Button)window.FindName(name)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
    private static void Drain(DispatcherObject obj) => obj.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void Layout(FrameworkElement root)
    { Drain(root); root.Measure(new Size(760, 800)); root.Arrange(new Rect(0, 0, 760, 800)); root.UpdateLayout(); Drain(root); }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T item) yield return item;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
}
