using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using SchoolTimetableWidget.Tests.Persistence;
using SchoolTimetableWidget.Tests.Time;

namespace SchoolTimetableWidget.Tests.Timetable;

// Compiled WPF layout with a fake monitor boundary. No Window.Show or native input.
public class WindowContentMinimumTests
{
    [Fact]
    public void FittingMultilineContentGrowsThenReturnsToPreferredHeightWithoutScrolling() =>
        HighlightTestDispatcher.Run(() =>
        {
            var (window, model, workArea) = Create(1200);
            try
            {
                Layout(window, 600);
                WindowContentMinimum.Refresh(window);
                Drain(window);
                var preferred = WindowContentMinimum.GetPreferredHeight(window);
                Assert.Equal(600, preferred, 5);

                Apply(model, "첫 줄\n둘째 줄\n셋째 줄", "1-1");
                WindowContentMinimum.Refresh(window);
                Drain(window);
                Assert.True(window.Height > preferred);
                Assert.True(window.Height < workArea.AvailableHeight);
                Layout(window, window.Height);
                Assert.Equal(0, BodyScroll(window).ScrollableHeight, 5);
                AssertTextFits(window);

                Apply(model, "짧게", "");
                WindowContentMinimum.Refresh(window);
                Drain(window);
                Assert.Equal(preferred, window.Height, 5);
                Assert.Equal(preferred, WindowContentMinimum.GetPreferredHeight(window), 5);
            }
            finally { window.Close(); }
        });

    [Fact]
    public void ScreenCapUsesBodyScrollKeepsWeekHeaderAndPreventsManualClipping() =>
        HighlightTestDispatcher.Run(() =>
        {
            var (window, model, _) = Create(640);
            try
            {
                Layout(window, 600);
                Apply(model, string.Join("\n", Enumerable.Repeat("많은 내용", 12)), "7-7");
                WindowContentMinimum.Refresh(window);
                Drain(window);
                Assert.Equal(640, window.Height, 5);
                Assert.Equal(640, window.MinHeight, 5);
                Assert.Equal(640, window.MaxHeight, 5);
                Layout(window, window.Height);

                var timetable = Timetable(window);
                var scroll = BodyScroll(window);
                Assert.True(scroll.ScrollableHeight > 0);
                Assert.True(((FrameworkElement)timetable.FindName("WeekdayHeaders")).TranslatePoint(new(), timetable).Y <
                    scroll.TranslatePoint(new(), timetable).Y);
                AssertTextFits(window);
                scroll.ScrollToEnd();
                Drain(scroll);
                var periodHeaders = Assert.IsType<ItemsControl>(timetable.FindName("PeriodHeaders"));
                var last = Descendants<TextBlock>(periodHeaders).Last();
                var bottom = last.TranslatePoint(new Point(0, last.ActualHeight), scroll).Y;
                Assert.InRange(bottom, 0, scroll.ViewportHeight + 0.01);

                Assert.Equal(window.MaxHeight, window.MinHeight, 5);
                Layout(window, window.MinHeight);
                AssertTextFits(window);
            }
            finally { window.Close(); }
        });

    [Fact]
    public void WorkAreaChangeRecapsHeightAndAppliesMinimalTopCorrection() =>
        HighlightTestDispatcher.Run(() =>
        {
            var (window, model, workArea) = Create(1000);
            try
            {
                window.Top = 700;
                Layout(window, 600);
                Apply(model, string.Join("\n", Enumerable.Repeat("여러 줄", 8)), "");
                workArea.TopAdjustment = -125;
                WindowContentMinimum.Refresh(window);
                Drain(window);
                Assert.True(window.Height <= 1000);
                Assert.Equal(575, window.Top, 5);

                workArea.AvailableHeight = 620;
                workArea.TopAdjustment = -75;
                WindowContentMinimum.Refresh(window);
                Drain(window);
                Assert.Equal(620, window.Height, 5);
                Assert.Equal(620, window.MaxHeight, 5);
                Assert.Equal(500, window.Top, 5);
                Assert.True(workArea.AvailableCalls >= 2);

                workArea.AvailableHeight = 1200;
                workArea.TopAdjustment = 0;
                WindowContentMinimum.Refresh(window);
                Drain(window);
                Assert.Equal(1200, window.MaxHeight, 5);
                Assert.True(window.Height > 620);
            }
            finally { window.Close(); }
        });

    [Fact]
    public void PerMonitorDpiMathConvertsPhysicalWorkAreaWithoutAssumingOnePixelPerDip()
    {
        Assert.Equal(960, MonitorWindowWorkAreaProvider.PixelsToDips(1440, 1.5), 5);
        Assert.Equal(1350, MonitorWindowWorkAreaProvider.DipsToPixelsCeiling(900, 1.5));
        Assert.Equal(768, MonitorWindowWorkAreaProvider.PixelsToDips(1440, 1.875), 5);
    }

    [Fact]
    public void OrdinaryClockRefreshDoesNotInvokeWindowSizing() => HighlightTestDispatcher.Run(() =>
    {
        var clock = new FakeApplicationClock(new(new DateTimeOffset(2026, 9, 15, 9, 0, 0, TimeSpan.FromHours(9)),
            ApplicationTimeSource.PcLocalFallback, 0));
        var header = new CurrentStatusHeaderViewModel();
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var workArea = new FakeWorkArea(1000);
        var window = new MainWindow(header, model);
        try
        {
            WindowContentMinimum.SetWorkAreaProvider(window, workArea);
            Layout(window, 600);
            WindowContentMinimum.Refresh(window);
            Drain(window);
            var height = window.Height;
            var calls = workArea.AvailableCalls;
            using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, header, model);
            loop.RefreshNow();
            clock.CurrentSnapshot = new(new DateTimeOffset(2026, 9, 15, 9, 0, 1, TimeSpan.FromHours(9)),
                ApplicationTimeSource.PcLocalFallback, 0);
            loop.RefreshNow();
            Drain(window);
            Assert.Equal(height, window.Height);
            Assert.Equal(calls, workArea.AvailableCalls);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void RestorePublishesMultilineAndDisplayConfigurationBeforeOneCoalescedRemeasure() =>
        HighlightTestDispatcher.Run(() =>
        {
            using var temp = new TempProfile();
            using var store = new JsonProfileStore(temp.Directory);
            var session = new ProfileSession(store);
            var runtime = new ProfileRuntime(session, () => { }, _ => { });
            var header = new CurrentStatusHeaderViewModel();
            header.SetDisplay(runtime.Display.Current);
            runtime.Display.Changed += (_, _) => header.SetDisplay(runtime.Display.Current);
            var window = new MainWindow(header, runtime.Timetable);
            var workArea = new FakeWorkArea(640);
            WindowContentMinimum.SetWorkAreaProvider(window, workArea);
            runtime.Timetable.ContentChanged += (_, _) => WindowContentMinimum.Refresh(window);
            try
            {
                Layout(window, 600);
                WindowContentMinimum.Refresh(window);
                Drain(window);
                var calls = workArea.AvailableCalls;
                var sample = ProfileBackupFileTests.Sample();
                var timetable = new WeeklyTimetable(sample.Timetable.Cells.Select(cell =>
                    new TimetableCell(cell.Day, cell.PeriodNumber,
                        cell.Day == SchoolDay.Monday && cell.PeriodNumber == 1
                            ? new TimetableCellValue(string.Join("\n", Enumerable.Repeat("복원 여러 줄", 12)), "1-1")
                            : cell.Value)));
                var candidate = new ProfileSnapshot(timetable, sample.Schedule, sample.Overrides,
                    sample.ShowLunch, sample.Display, sample.DisplayPresets);

                Assert.Null(runtime.Restore(candidate));
                Drain(window);
                Assert.Equal(calls + 1, workArea.AvailableCalls);
                Assert.Equal(candidate.Display, runtime.Display.Current);
                Assert.Equal(640, window.Height, 5);
                Layout(window, window.Height);
                Assert.True(BodyScroll(window).ScrollableHeight > 0);
                AssertTextFits(window);
            }
            finally { window.Close(); }
        });

    private static (MainWindow Window, WeeklyTimetableViewModel Model, FakeWorkArea WorkArea) Create(double height)
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        model.UpdateCurrent(new DateOnly(2026, 9, 15), null);
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), model);
        var workArea = new FakeWorkArea(height);
        WindowContentMinimum.SetWorkAreaProvider(window, workArea);
        return (window, model, workArea);
    }

    private static void Apply(WeeklyTimetableViewModel model, string subject, string classText)
    {
        var edit = model.Editor.BeginEdit(model.Cells[0]);
        edit.SubjectText = subject;
        edit.ClassText = classText;
        Assert.True(edit.TryApply());
    }

    private static void Layout(MainWindow window, double height)
    {
        var root = Assert.IsType<Grid>(window.Content);
        Drain(root);
        root.Measure(new Size(800, height));
        root.Arrange(new Rect(0, 0, 800, height));
        root.UpdateLayout();
        Drain(root);
    }

    private static WeeklyTimetableView Timetable(MainWindow window) =>
        Assert.IsType<WeeklyTimetableView>(window.FindName("Timetable"));

    private static ScrollViewer BodyScroll(MainWindow window) =>
        Assert.IsType<ScrollViewer>(Timetable(window).FindName("TimetableBodyScroll"));

    private static void AssertTextFits(MainWindow window)
    {
        var first = Descendants<TimetableCellControl>((DependencyObject)window.Content).First();
        var text = Assert.Single(Descendants<TextBlock>(first));
        var origin = text.TranslatePoint(new Point(), first);
        Assert.True(origin.Y >= 0);
        Assert.True(origin.Y + text.ActualHeight <= first.ActualHeight + 0.01);
        Assert.True(text.DesiredSize.Height <= text.ActualHeight + 0.01);
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

    private static void Drain(DispatcherObject target) =>
        target.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    private sealed class FakeWorkArea(double availableHeight) : IWindowWorkAreaProvider
    {
        public double AvailableHeight { get; set; } = availableHeight;
        public double TopAdjustment { get; set; }
        public int AvailableCalls { get; private set; }

        public double GetAvailableHeight(Window window)
        {
            AvailableCalls++;
            return AvailableHeight;
        }

        public double GetTopAdjustment(Window window, double targetHeight) => TopAdjustment;
    }
}
