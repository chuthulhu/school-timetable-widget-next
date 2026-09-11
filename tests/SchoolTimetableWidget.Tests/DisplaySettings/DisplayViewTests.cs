using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.DisplaySettings;

// Unshown WPF objects/events and Measure/Arrange. No native input, clipboard or foreground windows.
public class DisplayViewTests
{
    [Fact]
    public void DialogSelectorsAndPerElementInputsPreviewAndApplyWithoutClosing() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        var headerModel = new CurrentStatusHeaderViewModel(); headerModel.Apply(DisplayModelTests.Text(13));
        owner.Changed += (_, _) => headerModel.SetDisplay(owner.Current);
        var header = new CurrentStatusHeaderView { DataContext = headerModel };
        var dialog = new DisplaySettingsWindow(session);
        try
        {
            Layout((FrameworkElement)dialog.Content, 690, 560);
            var preset = (ComboBox)dialog.FindName("PresetSelector");
            Assert.Equal(4, preset.Items.Count);
            preset.SelectedValue = DisplayPreset.Digital; Drain(dialog);
            Assert.Equal(DisplayPreset.Digital, owner.Current.Preset);
            Layout((FrameworkElement)dialog.Content, 690, 560);
            var editors = (ItemsControl)dialog.FindName("ElementEditors");
            var sizes = Descendants<TextBox>(editors).Where(t => t.Name == "SizeInput").ToArray();
            var fonts = Descendants<ComboBox>(editors).Where(t => t.Name == "FontSelector").ToArray();
            Assert.Equal(4, sizes.Length); Assert.Equal(4, fonts.Length);
            sizes[0].Text = "56"; Drain(dialog); Assert.Equal(56, owner.Current.Time.Size);
            Assert.Equal(14, owner.Current.Date.Size);
            fonts[0].Text = "Missing Test Font"; Drain(dialog);
            Assert.Equal("Missing Test Font", owner.Current.Time.Font.Family);
            Assert.NotNull(((TextBlock)header.FindName("CurrentTimeTextBlock")).FontFamily);
            Click(dialog, "ApplyButton"); Assert.False(session.IsClosed);
            Assert.Equal(56, owner.Committed.Time.Size);
            sizes[0].Text = "60"; Drain(dialog); Assert.Equal(60, owner.Current.Time.Size);
            Click(dialog, "CancelButton");
            Assert.True(session.IsClosed); Assert.Equal(56, owner.Current.Time.Size);
            Assert.False(dialog.IsVisible);
        }
        finally { dialog.Close(); }
    });

    [Fact]
    public void WindowXRestoresLastApplyAndResetRemainsPreview() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        var dialog = new DisplaySettingsWindow(session);
        session.Preset = DisplayPreset.Digital; session.Elements[0].SizeText = "56";
        Click(dialog, "ApplyButton"); Click(dialog, "ResetButton");
        Assert.Equal(48, owner.Current.Time.Size); Assert.Equal(56, owner.Committed.Time.Size);
        dialog.Close(); Assert.Equal(56, owner.Current.Time.Size); Assert.True(session.IsClosed);
    });

    [Fact]
    public void OkSavesAndClosesButFailureStaysOpen() => HighlightTestDispatcher.Run(() =>
    {
        var fail = true;
        var owner = new RuntimeDisplaySettings(DisplayPresets.Create(DisplayPreset.Standard), _ => fail ? "저장 실패" : null);
        var dialog = new DisplaySettingsWindow(owner.Open());
        try
        {
            dialog.Session.Preset = DisplayPreset.Digital;
            Click(dialog, "AcceptButton"); Assert.False(dialog.Session.IsClosed); Assert.Equal("저장 실패", dialog.Session.ErrorText);
            fail = false; Click(dialog, "AcceptButton"); Assert.True(dialog.Session.IsClosed);
            Assert.Equal(DisplayPreset.Digital, owner.Committed.Preset);
        }
        finally { dialog.Close(); }
    });

    [Theory]
    [InlineData(DisplayPreset.Standard)] [InlineData(DisplayPreset.Digital)]
    [InlineData(DisplayPreset.Compact)] [InlineData(DisplayPreset.Minimal)]
    public void LayoutAndThirtyFiveCellsStayStableAcrossTicksAndResize(DisplayPreset preset) => HighlightTestDispatcher.Run(() =>
    {
        var model = new CurrentStatusHeaderViewModel(); model.SetDisplay(DisplayPresets.Create(preset));
        model.Apply(DisplayModelTests.Text(9));
        var tableModel = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var window = new MainWindow(model, tableModel);
        try
        {
            var root = (Grid)window.Content; var header = (CurrentStatusHeaderView)window.FindName("StatusHeader");
            var time = (TextBlock)header.FindName("CurrentTimeTextBlock");
            var date = (TextBlock)header.FindName("CurrentDateTextBlock");
            var controls = tableModel.Cells.ToArray();
            foreach (var width in new[] { 800d, 620d, 500d, 800d })
            {
                Layout(root, width, 600);
                var baseline = HeaderGeometry(header); var cells = CellGeometry(root);
                foreach (var hour in new[] { 0, 9, 12, 13, 23 })
                {
                    model.Apply(DisplayModelTests.Text(hour)); Layout(root, width, 600);
                    Assert.Equal(baseline, HeaderGeometry(header)); Assert.Equal(cells, CellGeometry(root));
                    Assert.Same(time, header.FindName("CurrentTimeTextBlock")); Assert.Same(date, header.FindName("CurrentDateTextBlock"));
                    Assert.Equal(controls, tableModel.Cells);
                }
            }
            var layout = (Grid)header.FindName("HeaderLayout");
            Assert.Equal(preset == DisplayPreset.Digital ? 3 : 1, layout.RowDefinitions.Count);
            var standard = new CurrentStatusHeaderView();
            if (preset == DisplayPreset.Compact) Assert.True(header.Height < standard.Height);
            if (preset == DisplayPreset.Digital) Assert.True(header.Height > standard.Height);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void VisibilityAndTypographyAreIndependentAndExtremeSizesRemainFinite() => HighlightTestDispatcher.Run(() =>
    {
        var model = new CurrentStatusHeaderViewModel(); model.Apply(DisplayModelTests.Text(13));
        var header = new CurrentStatusHeaderView { DataContext = model };
        var display = DisplayPresets.Create(DisplayPreset.Digital);
        model.SetDisplay(display with { Time = display.Time with { Size = 96 }, Date = display.Date with { Size = 48, Weight = DisplayFontWeight.Bold },
            Weekday = display.Weekday with { Size = 8, Style = DisplayFontStyle.Italic }, Status = display.Status with { Size = 48 },
            ShowDate = false, ShowWeekday = true, ShowStatus = false, Use24Hour = false });
        Layout(header, 500, 400);
        Assert.Equal(Visibility.Collapsed, ((TextBlock)header.FindName("CurrentDateTextBlock")).Visibility);
        Assert.Equal(Visibility.Visible, ((TextBlock)header.FindName("WeekdayTextBlock")).Visibility);
        Assert.Equal(Visibility.Collapsed, ((TextBlock)header.FindName("StatusTextBlock")).Visibility);
        Assert.Equal(Visibility.Visible, ((TextBlock)header.FindName("AmPmTextBlock")).Visibility);
        Assert.Equal(96, ((TextBlock)header.FindName("CurrentTimeTextBlock")).FontSize);
        Assert.Equal(FontWeights.Bold, ((TextBlock)header.FindName("CurrentDateTextBlock")).FontWeight);
        Assert.Equal(FontStyles.Italic, ((TextBlock)header.FindName("WeekdayTextBlock")).FontStyle);
        Assert.True(double.IsFinite(header.Height));
        model.SetDisplay(display with { ShowDate = true, ShowWeekday = false, ShowStatus = true });
        Drain(header);
        Assert.Equal(Visibility.Visible, ((TextBlock)header.FindName("CurrentDateTextBlock")).Visibility);
        Assert.Equal(Visibility.Collapsed, ((TextBlock)header.FindName("WeekdayTextBlock")).Visibility);
        Assert.Equal(Visibility.Visible, ((TextBlock)header.FindName("StatusTextBlock")).Visibility);
    });

    [Fact]
    public void DisplayCommandRoutesFromHeaderAndTimetableAndArrowsHaveInteractionStyle() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner();
        var window = new MainWindow(new(), new(WeeklyTimetable.Empty()), display: owner);
        try
        {
            var table = (WeeklyTimetableView)window.FindName("Timetable");
            Assert.True(DisplaySettingsCommands.Open.CanExecute(null, table));
            Assert.True(DisplaySettingsCommands.Open.CanExecute(null, (CurrentStatusHeaderView)window.FindName("StatusHeader")));
            Assert.Contains(table.ContextMenu.Items.OfType<MenuItem>(), m => m.Command == DisplaySettingsCommands.Open);
            var arrow = (Button)table.FindName("PreviousWeekButton");
            Assert.Equal("‹", arrow.Content); Assert.NotNull(arrow.Template); Assert.NotNull(arrow.Style);
            var next = (Button)table.FindName("NextWeekButton"); Assert.Equal("›", next.Content);
        }
        finally { window.Close(); }
    });

    private static void Click(Window window, string name) { ((Button)window.FindName(name)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Drain(window); }
    private static void Drain(DispatcherObject target) => target.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void Layout(FrameworkElement view, double width, double height)
    { Drain(view); view.Measure(new Size(width, height)); view.Arrange(new Rect(0, 0, width, height)); view.UpdateLayout(); Drain(view); }
    private static Rect[] HeaderGeometry(CurrentStatusHeaderView view) =>
        new[] { "CurrentTimeTextBlock", "CurrentDateTextBlock", "StatusTextBlock" }.Select(name =>
        { var block = (TextBlock)view.FindName(name); return new Rect(block.TranslatePoint(new Point(), view), block.RenderSize); }).ToArray();
    private static Rect[] CellGeometry(FrameworkElement root)
    {
        var cells = Descendants<Border>(root).Where(b => b.Child is TextBlock && b.DataContext is TimetableCellViewModel).ToArray();
        Assert.Equal(35, cells.Length);
        return cells.Select(c => new Rect(c.TranslatePoint(new Point(), root), c.RenderSize)).ToArray();
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T item) yield return item;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
}
