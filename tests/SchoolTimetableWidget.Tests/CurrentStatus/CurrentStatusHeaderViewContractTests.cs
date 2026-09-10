using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.CurrentStatus;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Desktop;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Tests.Time;

namespace SchoolTimetableWidget.Tests.CurrentStatus;

// WPF object/binding evidence only: no Window.Show, native input, or font pixel assertions.
public class CurrentStatusHeaderViewContractTests
{
    [Fact]
    public void LoadedXamlBindsSeparateTextsFromTheSuppliedModel() => OnDispatcher(() =>
    {
        var model = new CurrentStatusHeaderViewModel();
        model.Apply(TextAt(9, 49, 59));
        var view = new CurrentStatusHeaderView { DataContext = model };
        var time = TextBlockOf(view, "CurrentTimeTextBlock");
        var status = TextBlockOf(view, "StatusTextBlock");

        DrainBindings(view);
        Assert.NotSame(time, status);
        Assert.Equal("09:49:59", time.Text);
        Assert.Equal("1교시 · 종료까지 1분 미만", status.Text);
        Assert.Equal(nameof(model.CurrentTimeText), BindingOperations.GetBinding(time, TextBlock.TextProperty)!.Path.Path);
        Assert.Equal(nameof(model.StatusText), BindingOperations.GetBinding(status, TextBlock.TextProperty)!.Path.Path);
    });

    [Fact]
    public void RefreshPipelineUpdatesBothBindingTargetsAcrossAStateBoundary() => OnDispatcher(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(9, 49, 58));
        var model = new CurrentStatusHeaderViewModel();
        var view = new CurrentStatusHeaderView { DataContext = model };
        using var loop = new CurrentStatusHeaderRefreshLoop(clock, DefaultPeriodSchedule.Periods, model);
        var time = TextBlockOf(view, "CurrentTimeTextBlock");
        var status = TextBlockOf(view, "StatusTextBlock");

        loop.RefreshNow();
        DrainBindings(view);
        Assert.Equal("09:49:58", time.Text);
        Assert.Equal("1교시 · 종료까지 1분 미만", status.Text);
        clock.CurrentSnapshot = Snapshot(9, 49, 59);
        loop.RefreshNow();
        DrainBindings(view);
        Assert.Equal("09:49:59", time.Text);
        Assert.Equal("1교시 · 종료까지 1분 미만", status.Text);
        clock.CurrentSnapshot = Snapshot(9, 50, 0);
        loop.RefreshNow();
        DrainBindings(view);
        Assert.Equal("09:50:00", time.Text);
        Assert.Equal("쉬는시간 · 2교시까지 10분", status.Text);
        Assert.Equal(3, clock.ReadCount);
    });

    [Fact]
    public void HeaderUsesFixedHeightAndTimeColumnWithSingleLineText() => OnDispatcher(() =>
    {
        var view = new CurrentStatusHeaderView();
        var border = Assert.IsType<Border>(view.Content);
        var grid = Assert.IsType<Grid>(border.Child);
        var time = TextBlockOf(view, "CurrentTimeTextBlock");
        var status = TextBlockOf(view, "StatusTextBlock");

        // Assert layout strategy, not unapproved candidate DIP values or rendered font pixels.
        Assert.True(double.IsFinite(view.Height) && view.Height > 0);
        Assert.Equal(2, grid.ColumnDefinitions.Count);
        Assert.True(grid.ColumnDefinitions[0].Width.IsAbsolute);
        Assert.True(grid.ColumnDefinitions[0].Width.Value > 0);
        Assert.True(grid.ColumnDefinitions[1].Width.IsStar);
        Assert.Equal(0, Grid.GetColumn(time));
        Assert.Equal(1, Grid.GetColumn(status));
        Assert.Equal(TextWrapping.NoWrap, time.TextWrapping);
        Assert.Equal(TextWrapping.NoWrap, status.TextWrapping);
        Assert.Equal(TextTrimming.CharacterEllipsis, status.TextTrimming);
        Assert.Equal(VerticalAlignment.Center, time.VerticalAlignment);
        Assert.Equal(VerticalAlignment.Center, status.VerticalAlignment);
        Assert.Equal(FontNumeralAlignment.Tabular, Typography.GetNumeralAlignment(time));
    });

    [Fact]
    public void MainWindowPlacesInjectedHeaderAboveTheTimetable() => OnDispatcher(() =>
    {
        var model = new CurrentStatusHeaderViewModel();
        var timetableModel = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var window = new MainWindow(model, timetableModel);
        try
        {
            var grid = Assert.IsType<Grid>(window.Content);
            Assert.Equal(2, grid.Children.Count);
            var header = Assert.IsType<CurrentStatusHeaderView>(grid.Children[0]);
            var timetable = Assert.IsType<WeeklyTimetableView>(grid.Children[1]);
            Assert.Same(timetableModel, timetable.DataContext);
            Assert.Equal(1, Grid.GetRow(timetable));
            Assert.Same(model, header.DataContext);
            Assert.Equal(0, Grid.GetRow(header));
            Assert.Equal(2, grid.RowDefinitions.Count);
            Assert.True(grid.RowDefinitions[0].Height.IsAuto);
            Assert.True(grid.RowDefinitions[1].Height.IsStar);
            Assert.Equal(SizeToContent.Manual, window.SizeToContent);
            Assert.False(window.IsVisible);
        }
        finally
        {
            window.Close();
        }
    });

    // Drain queued WPF binding work; do not force UpdateTarget or wait for a timer tick.
    private static void DrainBindings(CurrentStatusHeaderView view) =>
        view.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    private static TextBlock TextBlockOf(CurrentStatusHeaderView view, string name) =>
        Assert.IsType<TextBlock>(view.FindName(name));

    private static ApplicationTimeSnapshot Snapshot(int hour, int minute, int second) =>
        new(new DateTimeOffset(2026, 9, 7, hour, minute, second, TimeSpan.FromHours(9)),
            ApplicationTimeSource.PcLocalFallback, revision: 0);

    private static CurrentStatusHeaderText TextAt(int hour, int minute, int second)
    {
        var snapshot = Snapshot(hour, minute, second);
        var status = CurrentStatusResolver.Resolve(snapshot, DefaultPeriodSchedule.Periods);
        return CurrentStatusHeaderFormatter.Format(snapshot, status,
            CurrentStatusCountdownCalculator.Calculate(snapshot, status));
    }

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
