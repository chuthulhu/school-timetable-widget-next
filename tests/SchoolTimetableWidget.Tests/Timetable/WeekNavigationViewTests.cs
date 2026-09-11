using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Tests.DateOverrides;

namespace SchoolTimetableWidget.Tests.Timetable;

// Unshown WPF objects, bindings, command dispatch and layout only; no native input/render claims.
public class WeekNavigationViewTests
{
    [Theory]
    [InlineData(500)] [InlineData(620)] [InlineData(800)]
    public void DateHeadersArrowsAndTodayStylePreserveAlignedEqualColumns(double width) => HighlightTestDispatcher.Run(() =>
    {
        var vm = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        vm.UpdateCurrent(DayFixtures.Monday.AddDays(2), (SchoolDay.Wednesday, 1));
        var view = new WeeklyTimetableView { DataContext = vm }; Layout(view, width);
        var days = (ItemsControl)view.FindName("WeekdayHeaders");
        Assert.Equal(new[] { "9/7", "월", "9/8", "화", "9/9", "수", "9/10", "목", "9/11", "금" },
            Descendants<TextBlock>(days).Select(t => t.Text));
        var previous = (Button)view.FindName("PreviousWeekButton"); var next = (Button)view.FindName("NextWeekButton");
        Assert.Equal("이전 주", AutomationProperties.GetName(previous)); Assert.Equal("다음 주", AutomationProperties.GetName(next));
        Assert.Same(vm.PreviousWeekCommand, previous.Command); Assert.Same(vm.NextWeekCommand, next.Command);
        Assert.True(previous.TranslatePoint(new(), view).X + previous.ActualWidth <= days.TranslatePoint(new(), view).X);
        Assert.True(next.TranslatePoint(new(), view).X >= days.TranslatePoint(new(), view).X + days.ActualWidth);
        var style = Assert.IsType<Style>(view.Resources["DateHeaderBorder"]);
        var trigger = Assert.IsType<DataTrigger>(Assert.Single(style.Triggers));
        Assert.Equal(nameof(TimetableDateColumn.IsToday), Assert.IsType<Binding>(trigger.Binding).Path.Path);
        Assert.Equal(Border.BackgroundProperty, Assert.IsType<Setter>(Assert.Single(trigger.Setters)).Property);
        var headers = Descendants<Border>(days).Where(b => b.DataContext is TimetableDateColumn).ToArray(); Assert.Equal(5, headers.Length);
        Assert.NotEqual(headers[0].Background, headers[2].Background);
        var cells = Descendants<TimetableCellControl>((ItemsControl)view.FindName("BodyCells")).ToArray();
        var bounds = Bounds(cells, view); var desired = view.DesiredSize;
        foreach (var cell in cells) Assert.Equal(cells[0].ActualWidth, cell.ActualWidth, 5);
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(headers[i].TranslatePoint(new(), view).X, cells[i].TranslatePoint(new(), view).X, 5);
            Assert.Equal(headers[i].ActualWidth, cells[i].ActualWidth, 5);
        }
        vm.UpdateCurrent(DayFixtures.Monday.AddDays(3), (SchoolDay.Thursday, 1)); Drain(view);
        Assert.True(view.IsMeasureValid); Layout(view, width);
        Assert.Equal(desired, view.DesiredSize); Assert.Equal(bounds, Bounds(cells, view));
        Assert.NotEqual(headers[0].Background, headers[3].Background);
        ObjectClick(next); Layout(view, width);
        Assert.Equal("9/14", Descendants<TextBlock>(days).First().Text);
        Assert.DoesNotContain(vm.Cells, c => c.IsCurrent); Assert.DoesNotContain(vm.Columns, c => c.IsToday);
        Assert.Equal(desired, view.DesiredSize); Assert.Equal(bounds, Bounds(cells, view));
        ObjectClick(previous); Layout(view, width);
        Assert.True(vm.Cells[3].IsCurrent); Assert.Equal(bounds, Bounds(cells, view));
        Assert.Equal(cells, Descendants<TimetableCellControl>((ItemsControl)view.FindName("BodyCells")));
    });

    [Fact]
    public void BoundArrowUpdatesBothDatesAndExactOverrideWithoutRecreatingCells() => HighlightTestDispatcher.Run(() =>
    {
        var vm = new WeeklyTimetableViewModel(DayFixtures.Week());
        var entry = new DateSpecificOverride(new(2026, 9, 17), DayFixtures.Day(), null);
        vm.ConfigureDateOverrides(d => d == entry.Date ? entry : null); vm.UpdateCurrent(DayFixtures.Monday, null);
        var view = new WeeklyTimetableView { DataContext = vm }; Layout(view, 800);
        var body = (ItemsControl)view.FindName("BodyCells"); var controls = Descendants<TimetableCellControl>(body).ToArray();
        ObjectClick((Button)view.FindName("NextWeekButton")); Layout(view, 800);
        Assert.Equal("반복\n1", Descendants<TextBlock>(body).ElementAt(3).Text);
        Assert.Equal(entry.Date, vm.Columns[3].Date); Assert.Equal(controls, Descendants<TimetableCellControl>(body));
        ObjectClick((Button)view.FindName("PreviousWeekButton")); Layout(view, 800);
        Assert.Equal("Thursday-1\n반", Descendants<TextBlock>(body).ElementAt(3).Text);
    });

    private static void ObjectClick(Button button) => typeof(Button).GetMethod("OnClick",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(button, null);
    private static Rect[] Bounds(IEnumerable<FrameworkElement> cells, Visual root) =>
        cells.Select(c => new Rect(c.TranslatePoint(new(), (UIElement)root), c.RenderSize)).ToArray();
    private static void Drain(DispatcherObject view) => view.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void Layout(FrameworkElement view, double width)
    {
        Drain(view); view.Measure(new Size(width, double.PositiveInfinity));
        view.Arrange(new Rect(0, 0, width, Math.Max(544, view.DesiredSize.Height))); view.UpdateLayout(); Drain(view);
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
