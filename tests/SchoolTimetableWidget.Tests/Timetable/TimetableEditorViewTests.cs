using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using SchoolTimetableWidget.Tests.Time;

namespace SchoolTimetableWidget.Tests.Timetable;

// Unshown WPF objects and routed events. No native keyboard/IME, clipboard or OS focus evidence.
public class TimetableEditorViewTests
{
    [Theory]
    [MemberData(nameof(TimetableEditingTests.Pairs), MemberType = typeof(TimetableEditingTests))]
    public void BoundInputsApplyBothExactStringsAndReopenWithoutNormalization(string subject, string classText) =>
        HighlightTestDispatcher.Run(() =>
        {
            var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
            var session = model.Editor.BeginEdit(model.Cells[0]);
            var window = new CellEditorWindow(session);
            try
            {
                Drain(window);
                var subjectInput = Input(window, "SubjectInput");
                var classInput = Input(window, "ClassInput");
                subjectInput.Text = subject;
                classInput.Text = classText;
                Drain(window);
                Assert.Equal(subject, session.SubjectText);
                Assert.Equal(classText, session.ClassText);
                Assert.Equal(new TimetableCellValue("", ""), model.Cells[0].Value);
                Button(window, "ApplyButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                Assert.True(session.IsApplied);
                Assert.True(session.IsClosed);
                Assert.Equal(new TimetableCellValue(subject, classText), model.Cells[0].Value);
                var reopened = model.Editor.BeginEdit(model.Cells[0]);
                var second = new CellEditorWindow(reopened);
                try
                {
                    Drain(second);
                    Assert.Equal(subject, Input(second, "SubjectInput").Text);
                    Assert.Equal(classText, Input(second, "ClassInput").Text);
                    Button(second, "ApplyButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    Assert.Equal(new TimetableCellValue(subject, classText), model.Cells[0].Value);
                }
                finally { second.Close(); }
            }
            finally { window.Close(); }
        });

    [Theory]
    [InlineData("one\ntwo\n")]
    [InlineData("one\rtwo\r")]
    [InlineData("one\r\ntwo\n")]
    [InlineData("  한글 🎵 e\u0301\n\t ")]
    public void PartialTextBoxEditPreservesExistingLineEndingsInEachField(string original) => HighlightTestDispatcher.Run(() =>
    {
        TimetableCellValue? committed = null;
        var session = new CellEditSession("대상", new(original, original), value => { committed = value; return true; });
        var window = new CellEditorWindow(session);
        try
        {
            Drain(window);
            foreach (var name in new[] { "SubjectInput", "ClassInput" })
            {
                var input = Input(window, name);
                input.Select(0, 0);
                input.SelectedText = "X";
            }
            Drain(window);
            Button(window, "ApplyButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(new TimetableCellValue("X" + original, "X" + original), committed);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelButtonAndWindowCloseDiscardBothDraftFields(bool useButton) => HighlightTestDispatcher.Run(() =>
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var snapshot = model.CommittedTimetable;
        var session = model.Editor.BeginEdit(model.Cells[34]);
        var window = new CellEditorWindow(session);
        Drain(window);
        Input(window, "SubjectInput").Text = "버릴 교과";
        Input(window, "ClassInput").Text = "버릴 반";
        Drain(window);
        if (useButton) Button(window, "CancelButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        else window.Close(); // programmatic close, not title-bar evidence
        Assert.True(session.IsClosed);
        Assert.False(session.IsApplied);
        Assert.Null(model.Editor.ActiveSession);
        Assert.Same(snapshot, model.CommittedTimetable);
    });

    [Fact]
    public void RejectedApplyLeavesSessionDraftAndBoundErrorAvailable() => HighlightTestDispatcher.Run(() =>
    {
        var session = new CellEditSession("대상", new("원래", "반"), _ => false);
        var window = new CellEditorWindow(session);
        try
        {
            Drain(window);
            Input(window, "SubjectInput").Text = "변경";
            Input(window, "ClassInput").Text = "새 반";
            Button(window, "ApplyButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Drain(window);
            Assert.False(session.IsClosed);
            Assert.Contains(Descendants<TextBlock>((DependencyObject)window.Content), text => text.Text == session.ErrorText && text.Text.Length > 0);
            Assert.Equal("변경", Input(window, "SubjectInput").Text);
            Assert.Equal("새 반", Input(window, "ClassInput").Text);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void EditorUsesPlainMultilineInputsTabNavigationAndNoDefaultApplyOrShortcut() => HighlightTestDispatcher.Run(() =>
    {
        var window = new CellEditorWindow(new("대상", new("", ""), _ => true));
        try
        {
            Drain(window);
            Assert.Empty(window.InputBindings);
            Assert.False(Button(window, "ApplyButton").IsDefault);
            Assert.True(Button(window, "CancelButton").IsCancel);
            foreach (var name in new[] { "SubjectInput", "ClassInput" })
            {
                var input = Input(window, name);
                Assert.True(input.AcceptsReturn);
                Assert.False(input.AcceptsTab);
                Assert.Equal(0, input.MaxLength);
                Assert.Equal(TextWrapping.Wrap, input.TextWrapping);
                Assert.Equal(ScrollBarVisibility.Auto, input.VerticalScrollBarVisibility);
                Assert.Equal(UpdateSourceTrigger.PropertyChanged, BindingOperations.GetBinding(input, TextBox.TextProperty)!.UpdateSourceTrigger);
                Assert.Empty(input.InputBindings);
            }
            Layout((FrameworkElement)window.Content, 320);
            var inputs = Descendants<TextBox>((DependencyObject)window.Content).ToArray();
            Assert.Equal(2, inputs.Length);
            Assert.All(inputs, input => Assert.True(input.ActualHeight > 0));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void EveryCellHasIndependentFocusableControlAndF2TargetsThatControlOnly() => HighlightTestDispatcher.Run(() =>
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var view = new WeeklyTimetableView { DataContext = model };
        Layout(view, 800);
        var controls = Descendants<TimetableCellControl>(view).ToArray();
        Assert.Equal(35, controls.Length);
        for (var index = 0; index < controls.Length; index++)
        {
            var control = controls[index];
            var focusTrigger = Assert.IsType<Trigger>(Assert.Single(control.Template.Triggers));
            Assert.Equal(UIElement.IsKeyboardFocusedProperty, focusTrigger.Property);
            var focusSetter = Assert.IsType<Setter>(Assert.Single(focusTrigger.Setters));
            Assert.Equal(UIElement.OpacityProperty, focusSetter.Property);
            Assert.True(control.Focusable);
            Assert.True(control.IsTabStop);
            Assert.Same(model.Cells[index], control.DataContext);
            Assert.Equal(model.Cells[index].SlotLabel, AutomationProperties.GetName(control));
            var requests = 0;
            control.EditRequested += (_, args) => { requests++; Assert.Same(control, args.Source); };
            var key = new KeyEventArgs(Keyboard.PrimaryDevice, new ObjectPresentationSource(), 0, Key.F2)
                { RoutedEvent = Keyboard.KeyDownEvent };
            control.RaiseEvent(key);
            Assert.True(key.Handled);
            Assert.Equal(1, requests);
            control.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, new ObjectPresentationSource(), 0, Key.Enter)
                { RoutedEvent = Keyboard.KeyDownEvent });
            Assert.Equal(1, requests);
        }
        Assert.DoesNotContain(model.Cells, cell => cell.IsCurrent);
    });

    [Fact]
    public void HeaderAndHighlightRefreshDuringDraftAndApplyDoNotRetargetTheEdit() => HighlightTestDispatcher.Run(() =>
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var header = new CurrentStatusHeaderViewModel();
        var clock = new FakeApplicationClock(Snapshot(7, 9, 49, 59));
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, header, model);
        loop.RefreshNow();
        var session = model.Editor.BeginEdit(model.Cells[0]);
        var window = new CellEditorWindow(session);
        try
        {
            Drain(window);
            Input(window, "SubjectInput").Text = "교과";
            Input(window, "ClassInput").Text = "반";
            clock.CurrentSnapshot = Snapshot(7, 9, 50, 0);
            loop.RefreshNow();
            Assert.DoesNotContain(model.Cells, cell => cell.IsCurrent);
            clock.CurrentSnapshot = Snapshot(11, 16, 10, 0);
            loop.RefreshNow();
            Assert.True(model.Cells[34].IsCurrent);
            Assert.Equal("16:10:00", header.CurrentTimeText);
            Assert.Equal("교과", Input(window, "SubjectInput").Text);
            Assert.Equal(new TimetableCellValue("", ""), model.Cells[0].Value);
            Button(window, "ApplyButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(new TimetableCellValue("교과", "반"), model.Cells[0].Value);
            Assert.Equal(new TimetableCellValue("", ""), model.Cells[34].Value);
            Assert.True(model.Cells[34].IsCurrent);
            clock.CurrentSnapshot = Snapshot(12, 9, 10, 0);
            loop.RefreshNow();
            Assert.DoesNotContain(model.Cells, cell => cell.IsCurrent);
            Assert.Equal(4, clock.ReadCount);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void CommittedMultilinePairUpdatesBodyAndMeasuredMinimumWithoutHighlightGeometryChanges() => HighlightTestDispatcher.Run(() =>
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var window = new MainWindow(new CurrentStatusHeaderViewModel(), model);
        try
        {
            var root = (FrameworkElement)window.Content;
            Layout(root, 620);
            WindowContentMinimum.Refresh(window);
            Drain(window);
            var before = window.MinHeight;
            var controls = Descendants<TimetableCellControl>(root).ToArray();
            var session = model.Editor.BeginEdit(model.Cells[0]);
            session.SubjectText = "국어\n두 줄";
            session.ClassText = "1-1\n추가 반\n마지막 줄";
            Assert.True(session.TryApply());
            Drain(window);
            WindowContentMinimum.Refresh(window);
            Drain(window);
            Layout(root, 620);
            Assert.True(window.MinHeight > before);
            Assert.Equal(session.SubjectText + "\n" + session.ClassText, Assert.Single(Descendants<TextBlock>(controls[0])).Text);
            var rectangles = controls.Select(control => (control.RenderSize, control.TranslatePoint(new Point(), root))).ToArray();
            model.SetCurrentCell((SchoolDay.Monday, 1));
            Drain(window);
            Layout(root, 620);
            Assert.Equal(rectangles, controls.Select(control => (control.RenderSize, control.TranslatePoint(new Point(), root))));
            Assert.Equal(controls, Descendants<TimetableCellControl>(root));
            var clear = model.Editor.BeginEdit(model.Cells[0]);
            clear.SubjectText = clear.ClassText = "";
            Assert.True(clear.TryApply());
            Drain(window);
            WindowContentMinimum.Refresh(window);
            Drain(window);
            Assert.Equal(before, window.MinHeight);
        }
        finally { window.Close(); }
    });

    private sealed class ObjectPresentationSource : PresentationSource
    {
        public override Visual RootVisual { get; set; } = null!;
        public override bool IsDisposed => false;
        protected override CompositionTarget GetCompositionTargetCore() => null!;
    }

    private static ApplicationTimeSnapshot Snapshot(int day, int hour, int minute, int second) =>
        new(new DateTimeOffset(2026, 9, day, hour, minute, second, TimeSpan.FromHours(9)), ApplicationTimeSource.PcLocalFallback, 0);
    private static TextBox Input(Window window, string name) => Assert.IsType<TextBox>(window.FindName(name));
    private static Button Button(Window window, string name) => Assert.IsType<Button>(window.FindName(name));
    private static void Drain(DispatcherObject target) => target.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void Layout(FrameworkElement view, double width)
    {
        Drain(view);
        view.Measure(new Size(width, double.PositiveInfinity));
        view.Arrange(new Rect(0, 0, width, Math.Max(440, view.DesiredSize.Height)));
        view.UpdateLayout();
        Drain(view);
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
}
