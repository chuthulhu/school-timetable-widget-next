using System.Text;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.Persistence;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.DisplaySettings;

// ADR 0037 clock-area layout: model, storage, settings Draft and header placement (unshown WPF objects only).
public class ClockLayoutTests
{
    private static readonly ClockLayout Custom = new([
        new(ClockElement.Time, 1, ClockPosition.Center), new(ClockElement.Status, 1, ClockPosition.Right),
        new(ClockElement.Weekday, 2, ClockPosition.Left), new(ClockElement.Date, 2, ClockPosition.Left)]);

    [Fact]
    public void BuiltInStylesKeepTheirExistingArrangement()
    {
        var standard = DisplayPresets.Create(DisplayPreset.Standard).Clock;
        Assert.Equal(new[] { ClockElement.Date, ClockElement.Weekday, ClockElement.Time, ClockElement.Status },
            standard.Slots.Select(s => s.Element));
        Assert.All(standard.Slots, s => { Assert.Equal(1, s.Line); Assert.Equal(ClockPosition.Left, s.Position); });
        Assert.Equal(new[] { ClockElement.Time, ClockElement.Date, ClockElement.Weekday, ClockElement.Status },
            DisplayPresets.Create(DisplayPreset.Compact).Clock.Slots.Select(s => s.Element));
        var digital = DisplayPresets.Create(DisplayPreset.Digital).Clock;
        Assert.Equal(3, digital.LineCount);
        Assert.Equal(new[] { 1, 2, 2, 3 }, digital.Slots.Select(s => s.Line));
        Assert.All(digital.Slots, s => Assert.Equal(ClockPosition.Center, s.Position));
    }

    [Fact]
    public void DerivedAndExplicitLayoutsCompareEqualAndCustomDiffers()
    {
        var derived = new DisplayConfiguration(DisplayPreset.Digital, DisplayLayout.Digital,
            DisplayPresets.Create(DisplayPreset.Digital).Time, DisplayPresets.Create(DisplayPreset.Digital).Date,
            DisplayPresets.Create(DisplayPreset.Digital).Weekday, DisplayPresets.Create(DisplayPreset.Digital).Status,
            true, true, true, false, true);
        var preset = DisplayPresets.Create(DisplayPreset.Digital);
        Assert.Equal(preset, derived);
        Assert.Equal(preset.GetHashCode(), derived.GetHashCode());
        Assert.NotEqual(preset, preset with { Clock = Custom });
        Assert.Equal(preset with { Clock = Custom }, preset with { Clock = new ClockLayout(Custom.Slots) });
    }

    [Theory]
    [InlineData("duplicate")] [InlineData("missing")] [InlineData("line0")] [InlineData("line4")] [InlineData("position")]
    public void InvalidLayoutsAreRejected(string kind)
    {
        var slots = Custom.Slots.ToList();
        switch (kind)
        {
            case "duplicate": slots[3] = slots[3] with { Element = ClockElement.Time }; break;
            case "missing": slots.RemoveAt(3); break;
            case "line0": slots[0] = slots[0] with { Line = 0 }; break;
            case "line4": slots[0] = slots[0] with { Line = 4 }; break;
            case "position": slots[0] = slots[0] with { Position = (ClockPosition)7 }; break;
        }
        Assert.Throws<ArgumentException>(() => (DisplayPresets.Create(DisplayPreset.Standard) with { Clock = new ClockLayout(slots) }).Validate());
    }

    [Fact]
    public void ProfilePresetFileAndBackupRoundTripTheClockLayout()
    {
        var original = ProfileStorageTests.Sample();
        var preset = new UserDisplayPreset(Guid.NewGuid(), "배치", original.Display with { Clock = Custom });
        var candidate = new ProfileSnapshot(original.SemesterSets, original.ActiveSemesterId, original.ShowLunch,
            original.Display with { Clock = Custom }, new(original.DisplayPresets.Items.Append(preset)));
        var bytes = ProfileJson.Serialize(candidate);
        var restored = ProfileJson.Deserialize(bytes);
        Assert.Equal(Custom, restored.Display.Clock);
        Assert.Equal(Custom, restored.DisplayPresets.Get(preset.Id).Display.Clock);
        var clock = JsonNode.Parse(bytes)!["profile"]!["display"]!["settings"]!["clock"]!;
        Assert.Equal(1, clock["version"]!.GetValue<int>());
        Assert.Equal("Time", clock["elements"]![0]!["element"]!.GetValue<string>());
        Assert.Equal("Center", clock["elements"]![0]!["position"]!.GetValue<string>());
        Assert.Equal(Custom, DisplayPresetFile.Import(DisplayPresetFile.Export(preset)).Display.Clock);
        Assert.Equal(Custom, ProfileBackupFile.Import(ProfileBackupFile.Export(candidate)).Display.Clock);
    }

    [Fact]
    public void MissingClockLoadsTheStyleArrangement()
    {
        var display = DisplayPresets.Create(DisplayPreset.Digital);
        var source = ProfileStorageTests.Sample();
        var node = JsonNode.Parse(ProfileJson.Serialize(new ProfileSnapshot(source.SemesterSets, source.ActiveSemesterId,
            source.ShowLunch, display, source.DisplayPresets)))!;
        node["profile"]!["display"]!["settings"]!.AsObject().Remove("clock");
        var restored = ProfileJson.Deserialize(Encoding.UTF8.GetBytes(node.ToJsonString())).Display;
        Assert.Equal(ClockLayout.For(DisplayLayout.Digital), restored.Clock);
        var file = JsonNode.Parse(DisplayPresetFile.Export(new UserDisplayPreset(Guid.NewGuid(), "옛 파일", display)))!;
        file["preset"]!["settings"]!.AsObject().Remove("clock");
        Assert.Equal(ClockLayout.For(DisplayLayout.Digital),
            DisplayPresetFile.Import(Encoding.UTF8.GetBytes(file.ToJsonString())).Display.Clock);
    }

    [Theory]
    [InlineData("version")] [InlineData("null")] [InlineData("extra")] [InlineData("line")] [InlineData("duplicate")]
    [InlineData("name")] [InlineData("missingSlot")]
    public void InvalidOrFutureClockPreservesOriginalAndBlocksSave(string kind)
    {
        using var temp = new TempProfile();
        var node = JsonNode.Parse(ProfileJson.Serialize(ProfileStorageTests.Sample()))!;
        var settings = node["profile"]!["display"]!["settings"]!;
        var clock = settings["clock"]!;
        var elements = clock["elements"]!.AsArray();
        switch (kind)
        {
            case "version": clock["version"] = 2; break;
            case "null": settings["clock"] = null; break;
            case "extra": clock["rows"] = 2; break;
            case "line": elements[0]!["line"] = 4; break;
            case "duplicate": elements[1]!["element"] = elements[0]!["element"]!.GetValue<string>(); break;
            case "name": elements[0]!["position"] = "center"; break;
            case "missingSlot": elements.RemoveAt(3); break;
        }
        var original = Encoding.UTF8.GetBytes(node.ToJsonString()); File.WriteAllBytes(temp.File, original);
        using var store = new JsonProfileStore(temp.Directory); var profile = new ProfileSession(store);
        Assert.False(profile.LoadResult.CanWrite);
        Assert.NotNull(profile.SaveDisplay(DisplayPresets.Create(DisplayPreset.Digital)));
        Assert.Equal(original, File.ReadAllBytes(temp.File));
    }

    [Fact]
    public void InvalidClockInAPresetFileIsRejected()
    {
        var bytes = DisplayPresetFile.Export(new UserDisplayPreset(Guid.NewGuid(), "배치", DisplayPresets.Create(DisplayPreset.Standard)));
        foreach (var mutate in new Action<JsonNode>[]
                 {
                     n => n["preset"]!["settings"]!["clock"] = null,
                     n => n["preset"]!["settings"]!["clock"] = new JsonObject(),
                     n => n["preset"]!["settings"]!["clock"]!["elements"]![0]!["line"] = 0,
                     n => n["preset"]!["settings"]!["clock"]!["version"] = 2,
                 })
        {
            var node = JsonNode.Parse(bytes)!;
            mutate(node);
            Assert.Throws<InvalidDataException>(() => DisplayPresetFile.Import(Encoding.UTF8.GetBytes(node.ToJsonString())));
        }
    }

    [Fact]
    public void ColorsExtendedProfileIsBackedUpExactlyBeforeFirstClockExtension()
    {
        using var temp = new TempProfile();
        var node = JsonNode.Parse(ProfileJson.Serialize(ProfileBackupFileTests.Sample()))!;
        node["profile"]!["display"]!["settings"]!.AsObject().Remove("clock");
        foreach (var preset in node["profile"]!["displayPresets"]!.AsArray()) preset!["settings"]!.AsObject().Remove("clock");
        var original = Encoding.UTF8.GetBytes(node.ToJsonString()); File.WriteAllBytes(temp.File, original);
        using var store = new JsonProfileStore(temp.Directory); var session = new ProfileSession(store);
        Assert.Null(session.SaveLunch(session.Current.ShowLunch));
        Assert.Equal(original, File.ReadAllBytes(Assert.Single(Directory.GetFiles(temp.Directory, "profile-before-clock-*.json"))));
        Assert.Empty(Directory.GetFiles(temp.Directory, "profile-before-colors-*.json"));
        Assert.Empty(Directory.GetFiles(temp.Directory, "profile-before-table-*.json"));
        Assert.Null(session.SaveLunch(!session.Current.ShowLunch));
        Assert.Single(Directory.GetFiles(temp.Directory, "profile-before-clock-*.json"));
    }

    [Fact]
    public void SettingsDraftEditsPreviewReorderAndResetToTheStyle() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        Assert.Equal(new[] { "날짜", "요일", "시간", "상태" }, session.ClockSlots.Select(s => s.Label));
        Assert.False(session.ClockSlots[0].CanMoveUp); Assert.True(session.ClockSlots[3].CanMoveUp);
        var time = session.ClockSlots[2];
        time.Line = 2; time.Position = ClockPosition.Center;
        Assert.Equal(new ClockSlot(ClockElement.Time, 2, ClockPosition.Center),
            owner.Current.Clock.Slots.Single(s => s.Element == ClockElement.Time)); // previewed
        time.Line = 9; // out of range: ignored
        Assert.Equal(2, owner.Current.Clock.Slots.Single(s => s.Element == ClockElement.Time).Line);

        session.MoveClockSlot(ClockElement.Status, -1);
        Assert.Equal(new[] { ClockElement.Date, ClockElement.Weekday, ClockElement.Status, ClockElement.Time },
            owner.Current.Clock.Slots.Select(s => s.Element));
        Assert.Equal("상태", session.ClockSlots[2].Label);
        session.MoveClockSlot(ClockElement.Date, -1); // already first: no change
        Assert.Equal(ClockElement.Date, owner.Current.Clock.Slots[0].Element);

        Assert.True(session.TryApply());
        Assert.Equal(owner.Current.Clock, owner.Committed.Clock);
        session.Preset = DisplayPresetReference.BuiltInPreset(DisplayPreset.Digital);
        Assert.Equal(ClockLayout.For(DisplayLayout.Digital), owner.Current.Clock); // a style loads its arrangement
        session.Reset();
        Assert.Equal(ClockLayout.For(DisplayLayout.Digital), owner.Current.Clock);
        session.Cancel();
    });

    [Fact]
    public void HeaderPlacesElementsByLinePositionAndOrder() => HighlightTestDispatcher.Run(() =>
    {
        var model = new CurrentStatusHeaderViewModel(); model.Apply(DisplayModelTests.Text(9));
        var display = DisplayPresets.Create(DisplayPreset.Standard) with { Clock = Custom, ShowWeekday = true };
        model.SetDisplay(display);
        var header = new CurrentStatusHeaderView { DataContext = model };
        Layout(header);
        var layout = (Grid)header.FindName("HeaderLayout");
        Assert.Equal(2, layout.RowDefinitions.Count);
        Assert.Equal((0, ClockPosition.Center), Place(header, "TimeGroup"));
        Assert.Equal((0, ClockPosition.Right), Place(header, "StatusTextBlock"));
        Assert.Equal((1, ClockPosition.Left), Place(header, "WeekdayTextBlock"));
        Assert.Equal((1, ClockPosition.Left), Place(header, "CurrentDateTextBlock"));
        Assert.True(Grid.GetColumn((UIElement)header.FindName("WeekdayTextBlock")) < Grid.GetColumn((UIElement)header.FindName("CurrentDateTextBlock")));
        Assert.Equal(TextAlignment.Center, ((TextBlock)header.FindName("CurrentTimeTextBlock")).TextAlignment);
        Assert.Equal(TextAlignment.Right, ((TextBlock)header.FindName("StatusTextBlock")).TextAlignment);
        var standard = new CurrentStatusHeaderView();
        Assert.True(header.Height > standard.Height); // two lines are taller than one

        var before = Geometry(header);
        foreach (var hour in new[] { 0, 12, 13, 23 })
        {
            model.Apply(DisplayModelTests.Text(hour)); Layout(header);
            Assert.Equal(before, Geometry(header)); // ticks never move the layout
        }
        model.SetDisplay(display with { ShowDate = false });
        Layout(header);
        Assert.Equal(Visibility.Collapsed, ((TextBlock)header.FindName("CurrentDateTextBlock")).Visibility);
        Assert.NotNull(VisualTreeHelperParent(header, "CurrentDateTextBlock")); // hidden but still placed for its binding
    });

    private static (int Row, ClockPosition Position) Place(CurrentStatusHeaderView header, string name)
    {
        var element = (FrameworkElement)header.FindName(name);
        var cell = (Grid)element.Parent;
        var row = (Grid)cell.Parent;
        return (Grid.GetRow(row), (ClockPosition)Grid.GetColumn(cell));
    }
    private static object? VisualTreeHelperParent(CurrentStatusHeaderView header, string name) =>
        ((FrameworkElement)header.FindName(name)).Parent;
    private static Rect[] Geometry(CurrentStatusHeaderView header) =>
        new[] { "CurrentDateTextBlock", "WeekdayTextBlock", "CurrentTimeTextBlock", "StatusTextBlock" }.Select(name =>
        {
            var block = (TextBlock)header.FindName(name);
            return new Rect(block.TranslatePoint(new Point(), header), block.RenderSize);
        }).ToArray();
    private static void Drain(DispatcherObject target) => target.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void Layout(FrameworkElement root)
    { Drain(root); root.Measure(new Size(900, 400)); root.Arrange(new Rect(0, 0, 900, 400)); root.UpdateLayout(); Drain(root); }
}
