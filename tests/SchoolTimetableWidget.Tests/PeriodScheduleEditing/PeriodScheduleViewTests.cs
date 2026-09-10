using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Tests.Time;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.PeriodScheduleEditing;

// Unshown WPF objects, compiled bindings and routed events; no native input/visual claim.
public class PeriodScheduleViewTests
{
    [Fact]
    public void SevenReadOnlyNumbersAndFourteenBoundInputsRetainInvalidDraftAndAllowCorrection() => HighlightTestDispatcher.Run(() =>
    {
        var runtime = new RuntimePeriodSchedule(PeriodScheduleContractTests.Default());
        var initial = runtime.Current;
        var session = new PeriodScheduleEditor(runtime, () => { }).CreateSession();
        var window = new PeriodScheduleEditorWindow(session);
        try
        {
            Layout((FrameworkElement)window.Content);
            var rows = (ItemsControl)window.FindName("PeriodRows");
            Assert.Equal(7, rows.Items.Count);
            var numbers = Descendants<TextBlock>(rows).ToArray();
            Assert.Equal(Enumerable.Range(1, 7).Select(n => n.ToString()), numbers.Select(t => t.Text));
            var inputs = Descendants<TextBox>(rows).ToArray();
            Assert.Equal(14, inputs.Length);
            Assert.All(inputs, input =>
            {
                Assert.False(input.IsReadOnly);
                Assert.False(input.AcceptsReturn);
                Assert.False(input.AcceptsTab);
                Assert.Equal(UpdateSourceTrigger.PropertyChanged, BindingOperations.GetBinding(input, TextBox.TextProperty)!.UpdateSourceTrigger);
                Assert.NotEmpty(AutomationProperties.GetName(input));
            });
            Assert.Equal("09:00", inputs[0].Text);
            inputs[9].Text = "abc";
            Drain(window);
            Click(window, "ApplyButton");
            Drain(window);
            Assert.Same(initial, runtime.Current);
            Assert.False(session.IsClosed);
            Assert.Contains("5교시 종료", ((TextBlock)window.FindName("ErrorMessage")).Text);
            inputs[8].Text = "13:00";
            inputs[9].Text = "13:50";
            Drain(window);
            Click(window, "ApplyButton");
            Assert.True(session.IsApplied);
            Assert.True(session.IsClosed);
            Assert.Equal(new TimeOnly(13, 0), runtime.Current.Periods[4].Start);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CancelButtonOrProgrammaticCloseDiscardsDraft(bool cancel) => HighlightTestDispatcher.Run(() =>
    {
        var runtime = new RuntimePeriodSchedule(PeriodScheduleContractTests.Default());
        var initial = runtime.Current;
        var session = new PeriodScheduleEditor(runtime, () => throw new Exception("Cancel must not refresh")).CreateSession();
        var window = new PeriodScheduleEditorWindow(session);
        Layout((FrameworkElement)window.Content);
        var inputs = Descendants<TextBox>((ItemsControl)window.FindName("PeriodRows")).ToArray();
        inputs[0].Text = "abc";
        Drain(window);
        Assert.True(((Button)window.FindName("CancelButton")).IsCancel);
        Assert.False(((Button)window.FindName("ApplyButton")).IsDefault);
        if (cancel) Click(window, "CancelButton"); else window.Close();
        Assert.True(session.IsClosed);
        Assert.False(session.IsApplied);
        Assert.Same(initial, runtime.Current);
    });

    [Fact]
    public void ContextEntryUsesSuppliedEditorAndApplyKeepsCurrentViewAndCellGeometry() => HighlightTestDispatcher.Run(() =>
    {
        var runtime = new RuntimePeriodSchedule(PeriodScheduleContractTests.Default());
        var clock = new FakeApplicationClock(new ApplicationTimeSnapshot(
            new DateTimeOffset(2026, 9, 7, 13, 10, 0, TimeSpan.FromHours(9)), ApplicationTimeSource.PcLocalFallback, 0));
        var header = new CurrentStatusHeaderViewModel();
        var timetable = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        using var loop = new CurrentStatusRefreshLoop(clock, () => runtime.Current.Periods, header, timetable);
        var editor = new PeriodScheduleEditor(runtime, loop.RefreshNow);
        var main = new MainWindow(header, timetable, editor);
        try
        {
            var root = (Grid)main.Content;
            var view = (WeeklyTimetableView)root.Children[1];
            Assert.Same(editor, view.ScheduleEditor);
            var menu = Assert.Single(view.ContextMenu.Items.OfType<MenuItem>(), item => Equals(item.Header, "일과 시간 편집..."));
            Assert.Same(PeriodScheduleCommands.Edit, menu.Command);
            view.ContextMenu.PlacementTarget = view;
            Drain(view);
            Assert.Same(view, menu.CommandTarget);
            Assert.True(PeriodScheduleCommands.Edit.CanExecute(null, view));
            loop.RefreshNow();
            Layout(root);
            var bodies = Descendants<Border>(view).Where(b => b.Child is TextBlock && b.DataContext is TimetableCellViewModel).ToArray();
            Assert.Equal(35, bodies.Length);
            var rectangles = bodies.Select(b => new Rect(b.TranslatePoint(new Point(), root), b.RenderSize)).ToArray();
            var week = timetable.CommittedTimetable;
            var session = editor.CreateSession();
            var dialog = new PeriodScheduleEditorWindow(session);
            try
            {
                Layout((FrameworkElement)dialog.Content);
                var inputs = Descendants<TextBox>((ItemsControl)dialog.FindName("PeriodRows")).ToArray();
                inputs[8].Text = "13:00";
                inputs[9].Text = "13:50";
                Drain(dialog);
                Click(dialog, "ApplyButton");
                Layout(root);
                Assert.True(session.IsApplied);
                Assert.Same(root, main.Content);
                Assert.Same(view, root.Children[1]);
                Assert.Same(week, timetable.CommittedTimetable);
                Assert.Equal(rectangles, bodies.Select(b => new Rect(b.TranslatePoint(new Point(), root), b.RenderSize)));
                Assert.Equal("5교시 · 종료까지 40분", header.StatusText);
                Assert.Same(timetable.Cells[20], Assert.Single(timetable.Cells, c => c.IsCurrent));
                Assert.False(main.IsVisible);
            }
            finally { dialog.Close(); }
        }
        finally { main.Close(); }
    });

    private static void Click(Window window, string name) =>
        ((Button)window.FindName(name)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
    private static void Drain(DispatcherObject target) => target.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void Layout(FrameworkElement root)
    {
        Drain(root);
        root.Measure(new Size(800, 600));
        root.Arrange(new Rect(0, 0, 800, 600));
        root.UpdateLayout();
        Drain(root);
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
}
