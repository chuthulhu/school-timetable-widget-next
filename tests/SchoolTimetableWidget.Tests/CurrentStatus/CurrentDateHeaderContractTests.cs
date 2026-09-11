using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.CurrentStatus;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.CurrentStatus;

// Pure formatting and unshown WPF object/Measure/Arrange evidence, not native rendering.
public class CurrentDateHeaderContractTests
{
    [Theory]
    [InlineData(2026, 9, 10, "2026년 09월 10일")]
    [InlineData(2027, 1, 1, "2027년 01월 01일")]
    [InlineData(2028, 2, 29, "2028년 02월 29일")]
    public void CanonicalDateUsesSnapshotLocalGregorianDate(int year, int month, int day, string expected)
    {
        var snapshot = new ApplicationTimeSnapshot(new DateTimeOffset(year, month, day, 0, 0, 0,
            TimeSpan.FromHours(9)), ApplicationTimeSource.PcLocalFallback, 0);
        Assert.Equal(expected, Format(snapshot).CurrentDateText);
    }

    [Fact]
    public void MidnightWeekendAndSourceChangesUseOneReadForDateTimeStatusAndHighlight() =>
        HighlightTestDispatcher.Run(() =>
        {
            var snapshots = new[]
            {
                Snapshot(11, 23, 59, 59), // Friday, local date differs from UTC after midnight.
                Snapshot(12, 0, 0, 0),
                Snapshot(14, 9, 20, 0, ApplicationTimeSource.SynchronizedStandardTime, 42),
                Snapshot(13, 9, 20, 0), // Backward correction: same time text, different date/status.
            };
            var clock = new SequenceClock(snapshots);
            var model = new CurrentStatusHeaderViewModel();
            var timetable = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
            var view = new CurrentStatusHeaderView { DataContext = model };
            using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, model, timetable);
            for (var i = 0; i < snapshots.Length; i++)
            {
                var expected = Format(snapshots[i]);
                var changes = new List<string?>();
                System.ComponentModel.PropertyChangedEventHandler observer = (_, e) =>
                {
                    // All header values must already belong to this result at the first notification.
                    Assert.Equal(expected.CurrentDateText, model.CurrentDateText);
                    Assert.Equal(expected.CurrentTimeText, model.CurrentTimeText);
                    Assert.Equal(expected.StatusText, model.StatusText);
                    changes.Add(e.PropertyName);
                };
                model.PropertyChanged += observer;
                loop.RefreshNow();
                model.PropertyChanged -= observer;
                Drain(view);
                Assert.Equal(i + 1, clock.ReadCount);
                Assert.Contains(nameof(model.CurrentDateText), changes);
                Assert.Equal(expected.CurrentDateText, Text(view, "CurrentDateTextBlock").Text);
                Assert.Equal(expected.CurrentTimeText, Text(view, "CurrentTimeTextBlock").Text);
                Assert.Equal(expected.StatusText, Text(view, "StatusTextBlock").Text);
                if (i == 2) Assert.Same(timetable.Cells[0], Assert.Single(timetable.Cells, cell => cell.IsCurrent));
                else Assert.DoesNotContain(timetable.Cells, cell => cell.IsCurrent);
            }
        });

    [Fact]
    public void StandardCandidateKeepsStatusOriginAndAllCellRectanglesStableAcrossTicksAndResizeRoundTrips() =>
        HighlightTestDispatcher.Run(() =>
        {
            var model = new CurrentStatusHeaderViewModel();
            var window = new MainWindow(model, new WeeklyTimetableViewModel(WeeklyTimetable.Empty()));
            try
            {
                var root = (Grid)window.Content;
                var header = (CurrentStatusHeaderView)root.Children[0];
                var timetable = (WeeklyTimetableView)window.FindName("Timetable");
                Rect[]? wideBaseline = null;
                foreach (var width in new[] { 800d, 620d, 500d, 800d })
                {
                    model.Apply(Format(Snapshot(10, 9, 49, 58)));
                    Layout(root, width);
                    var headerSize = header.RenderSize;
                    var statusOrigin = Text(header, "StatusTextBlock").TranslatePoint(new Point(), root);
                    var baseline = CellRectangles(timetable, root);
                    if (width == 800)
                    {
                        if (wideBaseline is null) wideBaseline = baseline;
                        else Assert.Equal(wideBaseline, baseline);
                    }
                    foreach (var snapshot in new[] { Snapshot(10, 9, 49, 59), Snapshot(10, 9, 50, 0),
                        Snapshot(11, 23, 59, 59), Snapshot(12, 0, 0, 0) })
                    {
                        model.Apply(Format(snapshot));
                        Drain(root);
                        Assert.True(timetable.IsMeasureValid);
                        Layout(root, width);
                        Assert.Equal(headerSize, header.RenderSize);
                        Assert.Equal(statusOrigin, Text(header, "StatusTextBlock").TranslatePoint(new Point(), root));
                        Assert.Equal(baseline, CellRectangles(timetable, root));
                        Assert.False(window.IsVisible);
                    }
                    // Candidate slot accommodates the canonical date under object text measurement.
                    var date = Text(header, "CurrentDateTextBlock");
                    var probe = new TextBlock { Text = date.Text, FontFamily = date.FontFamily, FontSize = date.FontSize };
                    probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    Assert.True(probe.DesiredSize.Width <= date.ActualWidth);
                }
            }
            finally { window.Close(); }
        });

    private static Rect[] CellRectangles(WeeklyTimetableView view, FrameworkElement root)
    {
        var cells = Descendants<Border>(view).Where(border =>
            border.Child is TextBlock && border.DataContext is TimetableCellViewModel).ToArray();
        Assert.Equal(35, cells.Length);
        return cells.Select(cell => new Rect(cell.TranslatePoint(new Point(), root), cell.RenderSize)).ToArray();
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

    private static void Layout(FrameworkElement root, double width)
    {
        Drain(root);
        root.Measure(new Size(width, 600));
        root.Arrange(new Rect(0, 0, width, 600));
        root.UpdateLayout();
        Drain(root);
    }

    private static void Drain(DispatcherObject target) =>
        target.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static TextBlock Text(CurrentStatusHeaderView view, string name) => (TextBlock)view.FindName(name);
    private static CurrentStatusHeaderText Format(ApplicationTimeSnapshot snapshot)
    {
        var status = CurrentStatusResolver.Resolve(snapshot, DefaultPeriodSchedule.Periods);
        return CurrentStatusHeaderFormatter.Format(snapshot, status, CurrentStatusCountdownCalculator.Calculate(snapshot, status));
    }
    private static ApplicationTimeSnapshot Snapshot(int day, int hour, int minute, int second,
        ApplicationTimeSource source = ApplicationTimeSource.PcLocalFallback, long revision = 0) =>
        new(new DateTimeOffset(2026, 9, day, hour, minute, second, TimeSpan.FromHours(9)), source, revision);
    private sealed class SequenceClock(ApplicationTimeSnapshot[] snapshots) : IApplicationClock
    {
        public int ReadCount { get; private set; }
        public ApplicationTimeSnapshot GetSnapshot() => snapshots[ReadCount++];
    }
}
