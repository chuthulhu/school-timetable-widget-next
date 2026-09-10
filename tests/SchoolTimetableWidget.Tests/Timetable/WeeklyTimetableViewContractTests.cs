using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Tests.Time;

namespace SchoolTimetableWidget.Tests.Timetable;

// Compiled XAML, binding and layout objects only. No Window.Show or native input.
public class WeeklyTimetableViewContractTests
{
    [Fact]
    public void XamlContainsFiveDayHeadersSevenPeriodHeadersOneCornerAndThirtyFiveBodySlots() => OnDispatcher(() =>
    {
        var view = new WeeklyTimetableView { DataContext = RepresentativeModel() };
        Layout(view, 800);
        var days = Items(view, "WeekdayHeaders");
        var periods = Items(view, "PeriodHeaders");
        var body = Items(view, "BodyCells");
        Assert.Equal(5, days.Items.Count);
        Assert.Equal(7, periods.Items.Count);
        Assert.Equal(35, body.Items.Count);
        Assert.Equal(new[] { "월", "화", "수", "목", "금" }, Descendants<TextBlock>(days).Select(t => t.Text));
        Assert.Equal(new[] { "1", "2", "3", "4", "5", "6", "7" }, Descendants<TextBlock>(periods).Select(t => t.Text));
        var corner = Assert.IsType<Border>(view.FindName("CornerHeader"));
        Assert.Equal("", Assert.IsType<TextBlock>(corner.Child).Text);
        Assert.Equal(48, Descendants<TextBlock>(view).Count());
        var panel = Assert.Single(Descendants<UniformGrid>(body));
        Assert.Equal(7, panel.Rows);
        Assert.Equal(5, panel.Columns);
        Assert.Equal(35, panel.Children.Count);
        var grid = Assert.IsType<Grid>(view.Content);
        Assert.Equal(2, grid.ColumnDefinitions.Count);
        Assert.Equal(1, Grid.GetColumn(body));
        Assert.Equal(1, Grid.GetRow(body));
        Assert.Equal(0, Grid.GetColumn(periods));
        Assert.Equal(1, Grid.GetRow(periods));
        Assert.Equal(1, Grid.GetColumn(days));
        Assert.Equal(0, Grid.GetRow(days));
        Assert.All(Descendants<TextBlock>(view), text =>
        {
            Assert.Equal(TextAlignment.Center, text.TextAlignment);
            Assert.Equal(VerticalAlignment.Center, text.VerticalAlignment);
        });
    });

    [Fact]
    public void BindingPreservesTextAsLiteralRunsIncludingMarkupNewlinesAndWhitespace() => OnDispatcher(() =>
    {
        var model = RepresentativeModel();
        var view = new WeeklyTimetableView { DataContext = model };
        Layout(view, 800);
        var texts = Descendants<TextBlock>(Items(view, "BodyCells")).ToArray();
        Assert.Equal(35, texts.Length);
        for (var i = 0; i < texts.Length; i++)
        {
            Assert.Equal(model.Cells[i].Content, texts[i].Text);
            if (model.Cells[i].Content.Length == 0)
                Assert.Empty(texts[i].Inlines);
            else
                Assert.Equal(model.Cells[i].Content, Assert.IsType<Run>(Assert.Single(texts[i].Inlines)).Text);
            Assert.Equal(nameof(TimetableCellViewModel.Content),
                BindingOperations.GetBinding(texts[i], TextBlock.TextProperty)!.Path.Path);
            Assert.Equal(TextWrapping.Wrap, texts[i].TextWrapping);
            Assert.Equal(TextTrimming.None, texts[i].TextTrimming);
        }
        Assert.Contains(texts, text => text.Text == "<b>과목</b> &amp; {Binding Secret}");
        Assert.Contains(texts, text => text.Text == "물리학\n실험 A반!");
        Assert.Contains(texts, text => text.Text == "");
        Assert.Contains(texts, text => text.Text == "   ");
        Assert.Contains(texts, text => text.Text == "  앞뒤 공백  ");
        Assert.NotSame(texts[0], texts[5]);
        Assert.Equal(texts[0].Text, texts[5].Text);
    });

    [Theory]
    [InlineData(800)]
    [InlineData(620)]
    [InlineData(500)]
    public void MeasuredContentFitsEqualRowsAndColumnsWithAlignedHeaders(double width) => OnDispatcher(() =>
    {
        var view = new WeeklyTimetableView { DataContext = RepresentativeModel() };
        Layout(view, width);
        var body = Assert.Single(Descendants<UniformGrid>(Items(view, "BodyCells")));
        var days = Assert.Single(Descendants<UniformGrid>(Items(view, "WeekdayHeaders")));
        var periods = Assert.Single(Descendants<UniformGrid>(Items(view, "PeriodHeaders")));
        var first = (FrameworkElement)body.Children[0];
        for (var i = 0; i < 35; i++)
        {
            var slot = (FrameworkElement)body.Children[i];
            Assert.Equal(first.ActualWidth, slot.ActualWidth, 5);
            Assert.Equal(first.ActualHeight, slot.ActualHeight, 5);
            var origin = slot.TranslatePoint(new Point(), view);
            var dayOrigin = ((FrameworkElement)days.Children[i % 5]).TranslatePoint(new Point(), view);
            var periodOrigin = ((FrameworkElement)periods.Children[i / 5]).TranslatePoint(new Point(), view);
            Assert.Equal(dayOrigin.X, origin.X, 5);
            Assert.Equal(periodOrigin.Y, origin.Y, 5);
            var text = Assert.Single(Descendants<TextBlock>(slot));
            var textOrigin = text.TranslatePoint(new Point(), slot);
            Assert.True(textOrigin.X >= 0 && textOrigin.Y >= 0);
            Assert.True(textOrigin.X + text.ActualWidth <= slot.ActualWidth + 0.01);
            Assert.True(textOrigin.Y + text.ActualHeight <= slot.ActualHeight + 0.01);
            Assert.True(text.DesiredSize.Height <= text.ActualHeight + 0.01);
        }
    });

    [Fact]
    public void MoreLinesIncreaseContentMinimumInsteadOfBeingTrimmed() => OnDispatcher(() =>
    {
        var view = new WeeklyTimetableView { DataContext = new WeeklyTimetableViewModel(WeeklyTimetable.Empty()) };
        Layout(view, 620);
        var emptyHeight = view.DesiredSize.Height;
        var week = new WeeklyTimetable(WeeklyTimetable.Empty().Cells.Select(cell =>
            new TimetableCell(cell.Day, cell.PeriodNumber,
                cell.Day == SchoolDay.Monday && cell.PeriodNumber == 1
                    ? string.Join("\n", Enumerable.Repeat("여러 줄 내용", 12)) : "")));
        view.DataContext = new WeeklyTimetableViewModel(week);
        Layout(view, 620);
        Assert.True(view.DesiredSize.Height > emptyHeight);
        Assert.All(Descendants<TextBlock>(Items(view, "BodyCells")),
            text => Assert.True(text.DesiredSize.Height <= text.ActualHeight + 0.01));
    });

    [Fact]
    public void SyntheticLoadedEventAppliesMeasuredContentMinimumWithoutShowingWindow() => OnDispatcher(() =>
    {
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), RepresentativeModel());
        try
        {
            var root = Assert.IsType<Grid>(window.Content);
            Layout(root, 620);
            var requiredHeight = root.DesiredSize.Height;
            // No native chrome exists in this test. Native border/DPI sizing remains a smoke check.
            window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Assert.Equal(root.MinWidth, window.MinWidth);
            Assert.Equal(requiredHeight, window.MinHeight, 5);
            Assert.False(window.IsVisible);
            Layout(root, 500);
            window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Assert.True(window.MinHeight >= requiredHeight);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void HeaderRefreshChangesTextsWithoutRemeasuringOrRepositioningTimetable() => OnDispatcher(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(9, 49, 59));
        var model = new CurrentStatusHeaderViewModel();
        using var loop = new CurrentStatusHeaderRefreshLoop(clock, DefaultPeriodSchedule.Periods, model);
        var header = new CurrentStatusHeaderView { DataContext = model };
        var timetable = new WeeklyTimetableView { DataContext = RepresentativeModel() };
        var viewContent = Assert.IsAssignableFrom<UIElement>(timetable.Content);
        timetable.Content = null;
        var measurementProbe = new CountingDecorator { Child = viewContent };
        timetable.Content = measurementProbe;
        var host = new Grid();
        host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        host.RowDefinitions.Add(new RowDefinition());
        host.Children.Add(header);
        Grid.SetRow(timetable, 1);
        host.Children.Add(timetable);
        loop.RefreshNow();
        Layout(host, 800);
        var measured = measurementProbe.MeasureCount;
        var before = Descendants<TextBlock>(Items(timetable, "BodyCells"))
            .Select(text => text.TranslatePoint(new Point(), host)).ToArray();
        clock.CurrentSnapshot = Snapshot(9, 50, 0);
        loop.RefreshNow();
        Drain(host);
        host.Measure(new Size(800, double.PositiveInfinity));
        host.Arrange(new Rect(0, 0, 800, Math.Max(544, host.DesiredSize.Height)));
        host.UpdateLayout();
        Assert.Equal("09:50:00", Assert.IsType<TextBlock>(header.FindName("CurrentTimeTextBlock")).Text);
        Assert.Equal("쉬는시간 · 2교시까지 10분", Assert.IsType<TextBlock>(header.FindName("StatusTextBlock")).Text);
        Assert.Equal(2, clock.ReadCount);
        Assert.Equal(measured, measurementProbe.MeasureCount);
        Assert.Equal(before, Descendants<TextBlock>(Items(timetable, "BodyCells"))
            .Select(text => text.TranslatePoint(new Point(), host)).ToArray());
    });

    private sealed class CountingDecorator : Decorator
    {
        public int MeasureCount { get; private set; }
        protected override Size MeasureOverride(Size constraint)
        {
            MeasureCount++;
            return base.MeasureOverride(constraint);
        }
    }

    private static WeeklyTimetableViewModel RepresentativeModel()
    {
        string[] samples = ["국어", "물리학\n실험 A반!", "과학 탐구 프로젝트 발표 및 토론 수업!",
            "<b>과목</b> &amp; {Binding Secret}", "", "국어", "  앞뒤 공백  ", "   ", "한글 Ω 🎵"];
        return new WeeklyTimetableViewModel(new WeeklyTimetable(
            WeeklyTimetable.Empty().Cells.Select((cell, i) =>
                new TimetableCell(cell.Day, cell.PeriodNumber, i < samples.Length ? samples[i] : ""))));
    }

    private static ApplicationTimeSnapshot Snapshot(int hour, int minute, int second) =>
        new(new DateTimeOffset(2026, 9, 7, hour, minute, second, TimeSpan.FromHours(9)),
            ApplicationTimeSource.PcLocalFallback, 0);

    private static ItemsControl Items(WeeklyTimetableView view, string name) =>
        Assert.IsType<ItemsControl>(view.FindName(name));

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }

    private static void Layout(FrameworkElement view, double width)
    {
        Drain(view);
        view.Measure(new Size(width, double.PositiveInfinity));
        view.Arrange(new Rect(0, 0, width, Math.Max(544, view.DesiredSize.Height)));
        view.UpdateLayout();
        Drain(view);
    }

    private static void Drain(DispatcherObject view) =>
        view.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    private static void OnDispatcher(Action test)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { test(); }
            catch (Exception exception) { failure = exception; }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
