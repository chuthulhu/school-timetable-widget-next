using System.Text;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.Persistence;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.DisplaySettings;

// ADR 0027 display colors. WPF checks use unshown objects only; no native input or rendering.
public class DisplayColorTests
{
    private static readonly DisplayColors Custom = new(new(0x10, 0x20, 0x30), new(0x40, 0x50, 0x60), new(0x01, 0x02, 0x03),
        new(0xAA, 0xBB, 0xCC), new(0xFE, 0xDC, 0xBA), new(0xFF, 0xFF, 0xFF), new(0xF0, 0xE0, 0xD0), new(0x12, 0x34, 0x56));

    [Theory]
    [InlineData("#000000", 0, 0, 0)] [InlineData("#FFFFFF", 255, 255, 255)]
    [InlineData("#1a2B3c", 0x1A, 0x2B, 0x3C)]
    public void RgbParsesExactHexAndFormatsUppercase(string text, byte r, byte g, byte b)
    {
        var value = RgbColor.Parse(text);
        Assert.Equal(new RgbColor(r, g, b), value);
        Assert.Equal(text.ToUpperInvariant(), value.ToString());
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("000000")] [InlineData("#12345")] [InlineData("#1234567")]
    [InlineData("#GGGGGG")] [InlineData("#80FFFFFF")] [InlineData("red")] [InlineData(" #FFFFFF")] [InlineData("#+12345")]
    public void RgbRejectsAnythingButSixHexDigits(string? text)
    {
        Assert.False(RgbColor.TryParse(text, out _));
        Assert.Throws<ArgumentException>(() => RgbColor.Parse(text));
    }

    [Fact]
    public void DefaultColorsReproduceThePriorFixedLook()
    {
        Assert.Equal("#E5EFF6", DisplayColors.Default.TodayHeaderBackground.ToString());
        Assert.Equal("#FFFFFF", DisplayColors.Default.CellBackground.ToString());
        Assert.Equal("#FFF3CD", DisplayColors.Default.CurrentBackground.ToString());
        Assert.Equal("#000000", DisplayColors.Default.ClockText.ToString());
        Assert.All(Enum.GetValues<DisplayPreset>(), p => Assert.Equal(DisplayColors.Default, DisplayPresets.Create(p).Colors));
    }

    [Fact]
    public void ProfileRoundTripsColorsForDisplayAndUserPresetsWithoutChangingSemesters()
    {
        var original = ProfileStorageTests.Sample();
        var preset = new UserDisplayPreset(Guid.NewGuid(), "색상", original.Display with { Colors = Custom });
        var candidate = new ProfileSnapshot(original.SemesterSets, original.ActiveSemesterId, original.ShowLunch,
            original.Display with { Colors = Custom }, new(original.DisplayPresets.Items.Append(preset)));
        var bytes = ProfileJson.Serialize(candidate);
        var restored = ProfileJson.Deserialize(bytes);
        Assert.Equal(Custom, restored.Display.Colors);
        Assert.Equal(Custom, restored.DisplayPresets.Get(preset.Id).Display.Colors);
        var colors = JsonNode.Parse(bytes)!["profile"]!["display"]!["settings"]!["colors"]!;
        Assert.Equal(1, colors["version"]!.GetValue<int>());
        Assert.Equal("#123456", colors["clockText"]!.GetValue<string>());
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(ProfileJson.Serialize(original))!["profile"]!["semesterSets"],
            JsonNode.Parse(bytes)!["profile"]!["semesterSets"]));
    }

    [Fact]
    public void ProfileWithoutColorsLoadsDefaults()
    {
        var node = JsonNode.Parse(ProfileJson.Serialize(ProfileStorageTests.Sample()))!;
        node["profile"]!["display"]!["settings"]!.AsObject().Remove("colors");
        Assert.Equal(DisplayColors.Default, ProfileJson.Deserialize(Encoding.UTF8.GetBytes(node.ToJsonString())).Display.Colors);
    }

    [Theory]
    [InlineData("version")] [InlineData("null")] [InlineData("missing")] [InlineData("extra")]
    [InlineData("format")] [InlineData("alpha")] [InlineData("number")]
    public void InvalidOrFutureColorsPreserveOriginalAndBlockSave(string kind)
    {
        using var temp = new TempProfile();
        var node = JsonNode.Parse(ProfileJson.Serialize(ProfileStorageTests.Sample()))!;
        var settings = node["profile"]!["display"]!["settings"]!;
        var colors = settings["colors"]!;
        switch (kind)
        {
            case "version": colors["version"] = 2; break;
            case "null": settings["colors"] = null; break;
            case "missing": colors.AsObject().Remove("cellText"); break;
            case "extra": colors["futureColor"] = "#000000"; break;
            case "format": colors["border"] = "red"; break;
            case "alpha": colors["border"] = "#80FFFFFF"; break;
            case "number": colors["border"] = 16777215; break;
        }
        var original = Encoding.UTF8.GetBytes(node.ToJsonString()); File.WriteAllBytes(temp.File, original);
        using var store = new JsonProfileStore(temp.Directory); var profile = new ProfileSession(store);
        Assert.False(profile.LoadResult.CanWrite); Assert.NotNull(profile.SaveDisplay(DisplayPresets.Create(DisplayPreset.Digital)));
        Assert.Equal(original, File.ReadAllBytes(temp.File));
    }

    [Fact]
    public void TableOnlyProfileIsBackedUpExactlyBeforeFirstColorsExtension()
    {
        using var temp = new TempProfile();
        var node = JsonNode.Parse(ProfileJson.Serialize(ProfileBackupFileTests.Sample()))!;
        node["profile"]!["display"]!["settings"]!.AsObject().Remove("colors");
        foreach (var preset in node["profile"]!["displayPresets"]!.AsArray()) preset!["settings"]!.AsObject().Remove("colors");
        var original = Encoding.UTF8.GetBytes(node.ToJsonString()); File.WriteAllBytes(temp.File, original);
        using var store = new JsonProfileStore(temp.Directory); var session = new ProfileSession(store);
        Assert.Null(session.SaveLunch(session.Current.ShowLunch));
        Assert.Equal(original, File.ReadAllBytes(Assert.Single(Directory.GetFiles(temp.Directory, "profile-before-colors-*.json"))));
        Assert.Empty(Directory.GetFiles(temp.Directory, "profile-before-table-*.json"));
        Assert.Null(session.SaveLunch(!session.Current.ShowLunch));
        Assert.Single(Directory.GetFiles(temp.Directory, "profile-before-colors-*.json"));
    }

    [Fact]
    public void PresetFileRoundTripsColorsAndTreatsMissingColorsAsDefault()
    {
        var display = DisplayPresets.Create(DisplayPreset.Standard) with { Colors = Custom };
        var preset = new UserDisplayPreset(Guid.Parse("4f1c6a52-1a3e-4f0e-9c55-2b8f3d0e6a11"), "색상 프리셋", display);
        var bytes = DisplayPresetFile.Export(preset);
        Assert.Equal(Custom, DisplayPresetFile.Import(bytes).Display.Colors);
        var old = JsonNode.Parse(bytes)!;
        old["preset"]!["settings"]!.AsObject().Remove("colors");
        Assert.Equal(DisplayColors.Default, DisplayPresetFile.Import(Encoding.UTF8.GetBytes(old.ToJsonString())).Display.Colors);
        foreach (JsonNode? invalid in new JsonNode?[] { new JsonObject(), null })
        {
            old["preset"]!["settings"]!["colors"] = invalid?.DeepClone();
            Assert.Throws<InvalidDataException>(() => DisplayPresetFile.Import(Encoding.UTF8.GetBytes(old.ToJsonString())));
        }
        var badValue = JsonNode.Parse(bytes)!;
        badValue["preset"]!["settings"]!["colors"]!["cellText"] = "#12345";
        Assert.Throws<InvalidDataException>(() => DisplayPresetFile.Import(Encoding.UTF8.GetBytes(badValue.ToJsonString())));
    }

    [Fact]
    public void BackupPreservesColors()
    {
        var original = ProfileBackupFileTests.Sample();
        var candidate = new ProfileSnapshot(original.SemesterSets, original.ActiveSemesterId, original.ShowLunch,
            original.Display with { Colors = Custom }, original.DisplayPresets);
        Assert.Equal(Custom, ProfileBackupFile.Import(ProfileBackupFile.Export(candidate)).Display.Colors);
    }

    [Fact]
    public void ColorDraftsPreviewApplyAndCancelLikeOtherDisplaySettings() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        Assert.Equal(8, session.Colors.Count);
        session.Colors[2].Text = "#102030";
        Assert.Equal(new RgbColor(0x10, 0x20, 0x30), owner.Current.Colors.CellBackground);
        Assert.True(session.TryApply()); var baseline = owner.Committed;
        Assert.Equal(new RgbColor(0x10, 0x20, 0x30), baseline.Colors.CellBackground);
        session.Colors[7].Text = "#FFFFFF";
        Assert.Equal(new RgbColor(0xFF, 0xFF, 0xFF), owner.Current.Colors.ClockText);
        session.Cancel(); Assert.Equal(baseline, owner.Current);
    });

    [Fact]
    public void InvalidColorTextKeepsLastPreviewAndBlocksApply() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        var committed = owner.Committed;
        session.Colors[0].Text = "#ABCDEF";
        var previewed = owner.Current;
        session.Colors[0].Text = "#ABC";
        Assert.Equal("#ABCDEF", session.Colors[0].Swatch);
        Assert.Contains("머리글 배경", session.ErrorText);
        Assert.Equal(previewed, owner.Current);
        Assert.False(session.TryApply());
        Assert.Equal(committed, owner.Committed);
        session.Cancel();
    });

    [Fact]
    public void ColorsReachTableBrushesTextAndClockWithoutFadingText() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var header = new CurrentStatusHeaderViewModel();
        var window = new MainWindow(header, model, display: owner);
        try
        {
            header.SetDisplay(owner.Current);
            var session = owner.Open();
            string[] texts = ["#102030", "#405060", "#010203", "#AABBCC", "#FEDCBA", "#FFFFFF", "#F0E0D0", "#123456"];
            for (var i = 0; i < texts.Length; i++) session.Colors[i].Text = texts[i];
            header.SetDisplay(owner.Current);
            var table = (WeeklyTimetableView)window.FindName("Timetable");
            Layout((FrameworkElement)window.Content, 800, 600);
            var cell = Descendants<Border>(table).First(b => b.Child is TextBlock && b.DataContext is TimetableCellViewModel);
            Assert.Equal(Color.FromRgb(0x01, 0x02, 0x03), ((SolidColorBrush)cell.Background).Color);
            Assert.Equal(Color.FromRgb(0xFE, 0xDC, 0xBA), ((SolidColorBrush)cell.BorderBrush).Color);
            var cellText = (SolidColorBrush)((TextBlock)cell.Child).Foreground;
            Assert.Equal(Color.FromRgb(0xF0, 0xE0, 0xD0), cellText.Color); Assert.Equal(1, cellText.Opacity);
            Assert.Equal(Color.FromRgb(0x10, 0x20, 0x30), ((SolidColorBrush)((Border)table.FindName("CornerHeader")).Background).Color);
            var headerText = Descendants<TextBlock>(table).First(t => ReferenceEquals(t.Style, table.FindResource("TableHeaderText")));
            Assert.Equal(Color.FromRgb(0xFF, 0xFF, 0xFF), ((SolidColorBrush)headerText.Foreground).Color);
            model.Cells[0].SetIsCurrent(true); Drain(table);
            Assert.Equal(Color.FromRgb(0xAA, 0xBB, 0xCC), ((SolidColorBrush)cell.Background).Color);
            var status = (CurrentStatusHeaderView)window.FindName("StatusHeader");
            Assert.Equal(Color.FromRgb(0x12, 0x34, 0x56), ((SolidColorBrush)status.Foreground).Color);
            session.Cancel();
        }
        finally { window.Close(); }
    });

    private static void Drain(DispatcherObject target) => target.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void Layout(FrameworkElement view, double width, double height)
    {
        Drain(view);
        view.Measure(new Size(width, height));
        view.Arrange(new Rect(0, 0, width, height));
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
