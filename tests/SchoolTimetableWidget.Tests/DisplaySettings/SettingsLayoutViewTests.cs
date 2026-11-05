using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.DisplaySettings;

// ADR 0034: left section list, per-row colors and inline errors. Unshown WPF objects and Measure/Arrange only.
public class SettingsLayoutViewTests
{
    [Fact]
    public void SectionListShowsExactlyOneSectionAndStartsAtStyle() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        var dialog = new DisplaySettingsWindow(session);
        try
        {
            var list = (ListBox)dialog.FindName("SectionList");
            Assert.Equal(new[] { "스타일", "시계", "시간표 글꼴", "색상·투명도" },
                list.Items.Cast<ListBoxItem>().Select(item => (string)item.Content));
            Assert.Equal(DisplaySettingsSection.Style, dialog.Section);
            AssertOnlyVisible(dialog, "StylePage");
            foreach (var (section, page) in new[] { (DisplaySettingsSection.Clock, "ClockPage"),
                         (DisplaySettingsSection.TableFonts, "TableFontPage"), (DisplaySettingsSection.Colors, "ColorPage"),
                         (DisplaySettingsSection.Style, "StylePage") })
            {
                dialog.ShowSection(section);
                Assert.Equal(section, dialog.Section);
                Assert.Equal((int)section, list.SelectedIndex);
                AssertOnlyVisible(dialog, page);
            }
            list.SelectedIndex = 3; // the list itself drives the section
            AssertOnlyVisible(dialog, "ColorPage");
        }
        finally { dialog.Close(); }
    });

    [Fact]
    public void SwitchingSectionsKeepsTheDraftAndNeverSaves() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        var dialog = new DisplaySettingsWindow(session);
        try
        {
            session.Colors[6].Text = "#123456";
            session.CellOpacity = 0.5;
            var before = owner.Current;
            dialog.ShowSection(DisplaySettingsSection.Clock);
            dialog.ShowSection(DisplaySettingsSection.Colors);
            Assert.Equal("#123456", session.Colors[6].Text);
            Assert.Equal(0.5, session.CellOpacity);
            Assert.Equal(before, owner.Current);
            Assert.False(session.IsClosed);
        }
        finally { dialog.Close(); }
    });

    [Fact]
    public void ColorRowsPairSwatchHexAndOpacityAndReportErrorsInline() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        var dialog = new DisplaySettingsWindow(session);
        try
        {
            dialog.ShowSection(DisplaySettingsSection.Colors);
            Layout((FrameworkElement)dialog.Content);
            var page = (FrameworkElement)dialog.FindName("ColorPage");
            var hexBoxes = Descendants<TextBox>(page).ToArray();
            Assert.Equal(8, hexBoxes.Length);
            Assert.Equal(session.Colors.Select(c => c.Text), hexBoxes.Select(t => t.Text));
            var sliders = new[] { "HeaderOpacitySlider", "CellOpacitySlider", "CurrentOpacitySlider", "BorderOpacitySlider" }
                .Select(name => (Slider)dialog.FindName(name)).ToArray();
            Assert.All(sliders, slider => Assert.True(page.IsAncestorOf(slider)));
            sliders[1].Value = 0.4; Drain(dialog);
            Assert.Equal(0.4, session.CellOpacity, 3);
            Assert.Contains(Descendants<TextBlock>(page), block => block.Text == "40%");

            Assert.True(session.Colors[6].IsValid); Assert.Empty(session.Colors[6].ErrorText);
            hexBoxes[6].Text = "#00000G"; Drain(dialog); Layout((FrameworkElement)dialog.Content);
            Assert.False(session.Colors[6].IsValid);
            Assert.Contains(Descendants<TextBlock>(page), block => block.Text == session.Colors[6].ErrorText
                && block.Visibility == Visibility.Visible);
            Assert.Equal("#000000", session.Colors[6].Swatch); // the sample keeps the last valid color
            Assert.False(session.TryApply());
        }
        finally { dialog.Close(); }
    });

    [Fact]
    public void ThemeButtonsShowColorStrips() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        var dialog = new DisplaySettingsWindow(session);
        try
        {
            Layout((FrameworkElement)dialog.Content);
            var themes = (ItemsControl)dialog.FindName("ThemeButtons");
            var buttons = Descendants<Button>(themes).ToArray();
            Assert.Equal(DisplayThemes.All.Count, buttons.Length);
            var strip = Descendants<Rectangle>(buttons[0]).Select(r => ((SolidColorBrush)r.Fill).Color).ToArray();
            var colors = DisplayThemes.All[0].Colors;
            Assert.Equal(new[] { colors.HeaderBackground, colors.CellBackground, colors.CurrentBackground, colors.CellText }
                .Select(c => Color.FromRgb(c.R, c.G, c.B)), strip);
        }
        finally { dialog.Close(); }
    });

    [Fact]
    public void PresetManagementButtonsSitBehindTheExpanderAndKeepTheirBindings() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        var dialog = new DisplaySettingsWindow(session);
        try
        {
            Layout((FrameworkElement)dialog.Content);
            var expander = (Expander)dialog.FindName("PresetManagement");
            Assert.False(expander.IsExpanded);
            foreach (var name in new[] { "RenamePresetButton", "UpdatePresetButton", "DeletePresetButton", "ExportPresetButton", "ImportPresetButton" })
                Assert.Same(expander, LogicalTreeHelper.GetParent(LogicalTreeHelper.GetParent((Button)dialog.FindName(name))));
            Assert.False(((Button)dialog.FindName("RenamePresetButton")).IsEnabled); // built-in preset selected
            Assert.True(((Button)dialog.FindName("ImportPresetButton")).IsEnabled);
            Assert.NotSame(expander, LogicalTreeHelper.GetParent(LogicalTreeHelper.GetParent((Button)dialog.FindName("SaveAsPresetButton"))));
        }
        finally { dialog.Close(); }
    });

    private static void AssertOnlyVisible(Window dialog, string page)
    {
        foreach (var name in new[] { "StylePage", "ClockPage", "TableFontPage", "ColorPage" })
            Assert.Equal(name == page ? Visibility.Visible : Visibility.Collapsed, ((UIElement)dialog.FindName(name)).Visibility);
    }
    private static void Drain(DispatcherObject target) => target.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void Layout(FrameworkElement root)
    { Drain(root); root.Measure(new Size(760, 1400)); root.Arrange(new Rect(0, 0, 760, 1400)); root.UpdateLayout(); Drain(root); }
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
