using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.Timetable;

namespace SchoolTimetableWidget.Tests.Timetable;

// Measure/Arrange and trigger evidence, not native pixels, resize gestures or DPI evidence.
public class HighlightViewContractTests
{
    [Theory]
    [InlineData(800)]
    [InlineData(620)]
    [InlineData(500)]
    public void HighlightMoveAndClearPreserveDesiredSizeWrappingAndEverySlotRectangle(double width) =>
        HighlightTestDispatcher.Run(() =>
        {
            var model = Model();
            var view = new WeeklyTimetableView { DataContext = model };
            Layout(view, width);
            var cells = BodyBorders(view);
            var initialDesired = view.DesiredSize;
            var initialSize = view.RenderSize;
            var initialGeometry = Geometry(view, cells);
            var normalBrushes = cells.Select(cell => cell.Background).ToArray();
            // Non-empty, repeated, empty, whitespace-only and multiline slots.
            foreach (var index in new[] { 0, 5, 4, 7, 1, 34 })
            {
                model.SetCurrentCell(((SchoolDay)(index % 5), index / 5 + 1));
                Drain(view);
                Assert.True(view.IsMeasureValid);
                Assert.NotNull(cells[index].Background);
                Assert.True(cells[index].Background.Opacity > 0);
                Assert.NotEqual(normalBrushes[index], cells[index].Background);
                Assert.Single(cells.Select((cell, i) => !Equals(cell.Background, normalBrushes[i])), changed => changed);
                // Re-measure independently as well as checking no measure invalidation.
                view.InvalidateMeasure();
                Layout(view, width);
                Assert.Equal(initialDesired, view.DesiredSize);
                Assert.Equal(initialSize, view.RenderSize);
                Assert.Equal(initialGeometry, Geometry(view, cells));
                Assert.Equal(cells, BodyBorders(view));
            }
            model.SetCurrentCell(null);
            Drain(view);
            Assert.True(view.IsMeasureValid);
            Layout(view, width);
            Assert.Equal(initialGeometry, Geometry(view, cells));
            Assert.Equal(normalBrushes, cells.Select(cell => cell.Background));
        });

    [Fact]
    public void TriggerUsesOnlyBackgroundAndNeverChangesLayoutPropertiesOrHeaders() => HighlightTestDispatcher.Run(() =>
    {
        var model = Model();
        var view = new WeeklyTimetableView { DataContext = model };
        Layout(view, 800);
        var bodyStyle = Assert.IsType<Style>(view.Resources["BodySlotBorder"]);
        var trigger = Assert.IsType<DataTrigger>(Assert.Single(bodyStyle.Triggers));
        var binding = Assert.IsType<System.Windows.Data.Binding>(trigger.Binding);
        Assert.Equal(nameof(TimetableCellViewModel.IsCurrent), binding.Path.Path);
        var setter = Assert.IsType<Setter>(Assert.Single(trigger.Setters));
        Assert.Equal(Border.BackgroundProperty, setter.Property);
        var days = Assert.IsType<ItemsControl>(view.FindName("WeekdayHeaders"));
        var periods = Assert.IsType<ItemsControl>(view.FindName("PeriodHeaders"));
        var headers = Descendants<Border>(days).Concat(Descendants<Border>(periods)).ToArray();
        var headerBrushes = headers.Select(header => header.Background).ToArray();
        model.SetCurrentCell((SchoolDay.Friday, 1));
        Drain(view);
        Assert.Equal(headerBrushes, headers.Select(header => header.Background));
        Assert.Equal("", Assert.Single(Descendants<TextBlock>(BodyBorders(view)[4])).Text);
    });

    private static WeeklyTimetableViewModel Model() => new(new WeeklyTimetable(
        WeeklyTimetable.Empty().Cells.Select((cell, index) =>
            new TimetableCell(cell.Day, cell.PeriodNumber, new TimetableCellValue(index switch
            {
                0 or 5 => "반복",
                1 => "물리학\n실험 A반!",
                2 => "긴 내용을 자동으로 줄바꿈하는 시간표 과목",
                3 => "<b>과목</b>",
                7 => "   ",
                _ => ""
            }, "")))));

    private static Border[] BodyBorders(WeeklyTimetableView view) =>
        Descendants<Border>(Assert.IsType<ItemsControl>(view.FindName("BodyCells")))
            .Where(border => border.Child is TextBlock && border.DataContext is TimetableCellViewModel).ToArray();

    private static CellGeometry[] Geometry(WeeklyTimetableView view, Border[] cells)
    {
        Assert.Equal(35, cells.Length);
        return cells.Select(cell =>
        {
            var text = Assert.Single(Descendants<TextBlock>(cell));
            return new CellGeometry(cell.DesiredSize, cell.RenderSize,
                cell.TranslatePoint(new Point(), view), cell.BorderThickness, cell.Padding,
                text.DesiredSize, text.RenderSize, text.Text, text.FontSize, text.FontWeight, text.TextWrapping);
        }).ToArray();
    }

    private sealed record CellGeometry(Size Desired, Size Render, Point Origin, Thickness Border, Thickness Padding,
        Size TextDesired, Size TextRender, string Text, double FontSize, FontWeight FontWeight, TextWrapping Wrapping);

    private static void Layout(FrameworkElement view, double width)
    {
        Drain(view);
        view.Measure(new Size(width, double.PositiveInfinity));
        view.Arrange(new Rect(0, 0, width, Math.Max(544, view.DesiredSize.Height)));
        view.UpdateLayout();
        Drain(view);
    }

    private static void Drain(DispatcherObject target) =>
        target.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

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
