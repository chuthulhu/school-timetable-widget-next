using SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;
using SchoolTimetableWidget.Desktop.Features.DateOverrides;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Features.TimetableImport;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.TimetableImport;
using SchoolTimetableWidget.Tests.Time;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.TimetableImport;

// Unshown STA objects, bindings and routed commands; no native keys, focus, clipboard or pixel evidence.
public class ImportViewTests
{
    [Fact]
    public void CandidateSelectionBindsThirtyFiveSeparatePairsAndApplyRequiresConfirmation() => HighlightTestDispatcher.Run(() =>
    {
        var target = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var session = new TimetableImportActions(new ImportTransactionTests.MemoryClipboard()).CreateSession(target, TimetableImportMode.School);
        session.LoadText(ImportFixtures.SchoolText());
        var window = new TimetableImportWindow(session, () => { });
        try
        {
            Layout((FrameworkElement)window.Content);
            var selector = (ComboBox)window.FindName("CandidateSelector");
            Assert.Equal(2, selector.Items.Count); Assert.Equal(-1, selector.SelectedIndex);
            var apply = (Button)window.FindName("ApplyButton");
            Assert.False(apply.IsEnabled);
            selector.SelectedIndex = 1;
            Layout((FrameworkElement)window.Content);
            var items = (ItemsControl)window.FindName("PreviewCells");
            Assert.Equal(35, items.Items.Count);
            var fields = Descendants<TextBlock>(items).ToArray();
            Assert.Equal(70, fields.Length);
            Assert.Equal(session.SelectedCandidate!.Timetable.Cells.SelectMany(c => new[] { c.Value.SubjectText, c.Value.ClassText }), fields.Select(t => t.Text));
            Assert.False(apply.IsEnabled);
            ((CheckBox)window.FindName("MappingConfirmation")).IsChecked = true;
            Drain(window);
            Assert.True(apply.IsEnabled);
            Assert.Same(session.ApplyCommand, apply.Command);
            apply.Command.Execute(null);
            Assert.True(session.IsClosed); Assert.True(session.IsApplied);
            Assert.Equal(session.SelectedCandidate.Timetable.Cells.Select(c => c.Value), target.Cells.Select(c => c.Value));
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelCommandAndProgrammaticCloseLeaveOriginalUnchanged(bool cancelCommand) => HighlightTestDispatcher.Run(() =>
    {
        var target = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var original = target.CommittedTimetable;
        var session = new TimetableImportActions(new ImportTransactionTests.MemoryClipboard()).CreateSession(target, TimetableImportMode.Canonical);
        session.LoadText(CanonicalTimetableImporter.CreateTemplate());
        var window = new TimetableImportWindow(session, () => { });
        Drain(window);
        var cancel = (Button)window.FindName("CancelButton");
        Assert.True(cancel.IsCancel);
        Assert.False(((Button)window.FindName("ApplyButton")).IsDefault);
        Assert.Same(session.CancelCommand, cancel.Command);
        if (cancelCommand) cancel.Command.Execute(null); else window.Close();
        Assert.True(session.IsClosed); Assert.False(session.IsApplied);
        Assert.Same(original, target.CommittedTimetable);
    });

    [Fact]
    public void ReReadButtonClearsPreviewAndShowsBoundValidationError() => HighlightTestDispatcher.Run(() =>
    {
        var session = new TimetableImportSession(TimetableImportMode.Canonical, _ => true);
        session.LoadText(CanonicalTimetableImporter.CreateTemplate());
        var window = new TimetableImportWindow(session, () => session.LoadText("invalid"));
        try
        {
            Layout((FrameworkElement)window.Content);
            ((Button)window.FindName("ReadClipboardButton")).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Layout((FrameworkElement)window.Content);
            Assert.Empty(((ItemsControl)window.FindName("PreviewCells")).Items);
            Assert.NotEmpty(((TextBlock)window.FindName("ErrorMessage")).Text);
            Assert.False(((Button)window.FindName("ApplyButton")).IsEnabled);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void ContextCommandsAndCopyRouteToViewWhileEditorPasteRemainsLocal() => HighlightTestDispatcher.Run(() =>
    {
        var clipboard = new ImportTransactionTests.MemoryClipboard();
        var target = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var view = new WeeklyTimetableView { DataContext = target, ImportActions = new(clipboard) };
        Layout(view);
        Assert.Equal(6, view.CommandBindings.Count);
        var binding = Assert.IsType<KeyBinding>(Assert.Single(view.InputBindings.Cast<InputBinding>()));
        Assert.Equal(Key.V, binding.Key); Assert.Equal(ModifierKeys.Control, binding.Modifiers);
        Assert.Same(TimetableImportCommands.School, binding.Command);
        var menu = view.ContextMenu;
        menu.PlacementTarget = view;
        Layout(menu);
        var entries = menu.Items.OfType<MenuItem>().ToArray();
        Assert.All(entries, entry => Assert.Same(view, entry.CommandTarget));
        Assert.Equal(9, entries.Length);
        Assert.Equal(new ICommand[] { SchoolTimetableWidget.Desktop.Features.DisplaySettings.DisplaySettingsCommands.Open, TimetableImportCommands.School, TimetableImportCommands.Canonical, TimetableImportCommands.CopyTemplate, PeriodScheduleCommands.Edit, DateOverrideCommands.Edit, DateOverrideCommands.Lunch, BackupCommands.Backup, BackupCommands.Restore }, entries.Select(m => m.Command));
        Assert.True(TimetableImportCommands.School.CanExecute(null, view));
        TimetableImportCommands.CopyTemplate.Execute(null, view); // unshown view: fake adapter, no dialog
        Assert.Equal(1, clipboard.Writes);
        var edit = target.Editor.BeginEdit(target.Cells[0]);
        var editor = new CellEditorWindow(edit);
        try
        {
            Drain(editor);
            var input = (TextBox)editor.FindName("SubjectInput");
            Assert.Empty(editor.InputBindings);
            Assert.Empty(input.InputBindings);
            Assert.Empty(editor.CommandBindings);
            Assert.False(TimetableImportCommands.School.CanExecute(null, input));
            Assert.False(TimetableImportCommands.School.CanExecute(null, view));
            Assert.Equal(1, clipboard.Writes);
        }
        finally { editor.Close(); }
    });

    [Fact]
    public void SharedClockRefreshContinuesDuringPreviewAndApplyRetainsIdentityAndGeometry() => HighlightTestDispatcher.Run(() =>
    {
        var target = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var header = new CurrentStatusHeaderViewModel();
        var clock = new FakeApplicationClock(Snapshot(9, 49, 59));
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, header, target);
        loop.RefreshNow();
        var session = new TimetableImportActions(new ImportTransactionTests.MemoryClipboard()).CreateSession(target, TimetableImportMode.School);
        session.LoadText(ImportFixtures.SchoolText(1)); session.SelectedCandidate = session.Candidates[0]; session.MappingConfirmed = true;
        var window = new MainWindow(header, target);
        var root = (FrameworkElement)window.Content;
        try
        {
            Layout(root);
            var controls = Descendants<TimetableCellControl>(root).ToArray();
            clock.CurrentSnapshot = Snapshot(9, 50, 0); loop.RefreshNow();
            Assert.DoesNotContain(target.Cells, c => c.IsCurrent);
            clock.CurrentSnapshot = Snapshot(10, 0, 0); loop.RefreshNow();
            Assert.True(target.Cells[5].IsCurrent); Assert.Equal("10:00:00", header.CurrentTimeText);
            Assert.True(session.TryApply()); Layout(root);
            Assert.True(target.Cells[5].IsCurrent);
            Assert.Equal(controls, Descendants<TimetableCellControl>(root));
            var geometry = controls.Select(c => (c.RenderSize, c.TranslatePoint(new Point(), root))).ToArray();
            target.SetCurrentCell((SchoolDay.Friday, 7)); Layout(root);
            Assert.Equal(geometry, controls.Select(c => (c.RenderSize, c.TranslatePoint(new Point(), root))));
            target.SetCurrentCell(null); Layout(root);
            Assert.Equal(geometry, controls.Select(c => (c.RenderSize, c.TranslatePoint(new Point(), root))));
            Assert.Equal(3, clock.ReadCount);
        }
        finally { window.Close(); }
    });

    private static ApplicationTimeSnapshot Snapshot(int h, int m, int s) => new(new DateTimeOffset(2026, 9, 7, h, m, s, TimeSpan.FromHours(9)), ApplicationTimeSource.PcLocalFallback, 0);
    private static void Drain(DispatcherObject target) => target.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void Layout(FrameworkElement root)
    {
        Drain(root); root.Measure(new Size(900, double.PositiveInfinity));
        root.Arrange(new Rect(0, 0, 900, Math.Max(620, root.DesiredSize.Height))); root.UpdateLayout(); Drain(root);
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var item in Descendants<T>(child)) yield return item;
        }
    }
}
