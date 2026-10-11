using System.Text;
using System.Text.Json.Nodes;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.Persistence;

namespace SchoolTimetableWidget.Tests.DisplaySettings;

public class DisplayPersistenceTests
{
    // Fixed v1 envelope follows ADR 0012 exactly, with full precision and independent overrides.
    private static byte[] V1()
    {
        using var resource = typeof(DisplayPersistenceTests).Assembly.GetManifestResourceStream(
            "SchoolTimetableWidget.Tests.DisplaySettings.profile-v1.json")!;
        using var memory = new MemoryStream();
        resource.CopyTo(memory);
        return memory.ToArray();
    }

    [Theory]
    [InlineData("version")] [InlineData("null")] [InlineData("missing")] [InlineData("extra")]
    [InlineData("opacity")] [InlineData("font")]
    public void InvalidOrFutureTableSettingsPreserveOriginalAndBlockSave(string kind)
    {
        using var temp = new TempProfile();
        var node = JsonNode.Parse(ProfileJson.Serialize(ProfileStorageTests.Sample()))!;
        var settings = node["profile"]!["display"]!["settings"]!;
        var table = settings["table"]!;
        switch (kind)
        {
            case "version": table["version"] = 2; break;
            case "null": settings["table"] = null; break;
            case "missing": table.AsObject().Remove("currentOpacity"); break;
            case "extra": table["futureSetting"] = true; break;
            case "opacity": table["headerOpacity"] = 1.1; break;
            case "font": table["cell"]!["font"]!["source"] = "FutureFont"; break;
        }
        var original = Encoding.UTF8.GetBytes(node.ToJsonString()); File.WriteAllBytes(temp.File, original);
        using var store = new JsonProfileStore(temp.Directory); var profile = new ProfileSession(store);
        Assert.False(profile.LoadResult.CanWrite); Assert.NotNull(profile.SaveDisplay(DisplayPresets.Create(DisplayPreset.Digital)));
        Assert.Equal(original, File.ReadAllBytes(temp.File));
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void OldProfileIsBackedUpExactlyBeforeFirstSettingsExtension(int version)
    {
        using var stream = typeof(DisplayPersistenceTests).Assembly.GetManifestResourceStream(
            $"SchoolTimetableWidget.Tests.DisplaySettings.profile-v{version}.json")!;
        using var memory = new MemoryStream(); stream.CopyTo(memory); var original = memory.ToArray();
        using var temp = new TempProfile(); File.WriteAllBytes(temp.File, original);
        using var store = new JsonProfileStore(temp.Directory); var profile = new ProfileSession(store);
        Assert.Null(profile.SaveLunch(profile.Current.ShowLunch));
        Assert.Equal(original, File.ReadAllBytes(Assert.Single(Directory.GetFiles(temp.Directory, "profile-before-table-*.json"))));
        Assert.Null(profile.SaveLunch(!profile.Current.ShowLunch));
        Assert.Single(Directory.GetFiles(temp.Directory, "profile-before-table-*.json"));
    }

    [Fact]
    public void TableAppearanceRoundTripsWithoutChangingSemesterInputs()
    {
        var original = ProfileStorageTests.Sample();
        var display = original.Display with { Table = TableAppearance.Default with
        {
            HeaderOpacity = 120d / 255, CellOpacity = 30d / 255,
            CurrentOpacity = 150d / 255, BorderOpacity = 200d / 255,
            Header = TableAppearance.Default.Header with { Font = new(FontSourceKind.System, "없는 머리글 글꼴") },
            Cell = TableAppearance.Default.Cell with { Font = new(FontSourceKind.System, "없는 본문 글꼴") }
        }};
        var candidate = new ProfileSnapshot(original.SemesterSets, original.ActiveSemesterId,
            original.ShowLunch, display, original.DisplayPresets);
        var restored = ProfileJson.Deserialize(ProfileJson.Serialize(candidate));
        Assert.Equal(display, restored.Display);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(ProfileJson.Serialize(original))!["profile"]!["semesterSets"],
            JsonNode.Parse(ProfileJson.Serialize(restored))!["profile"]!["semesterSets"]));
    }

    [Fact]
    public void V1LoadsAllInputsExactlyWithoutRewriteOrDegradationAndNextSaveWritesV4()
    {
        using var temp = new TempProfile();
        var original = V1(); File.WriteAllBytes(temp.File, original);
        var modified = File.GetLastWriteTimeUtc(temp.File);
        using (var store = new JsonProfileStore(temp.Directory))
        {
            var profile = new ProfileSession(store);
            Assert.Equal(ProfileLoadState.Loaded, profile.LoadResult.State);
            Assert.True(profile.LoadResult.CanWrite); Assert.Empty(profile.LoadResult.Notice);
            Assert.Equal(DisplayPresets.Create(DisplayPreset.Standard), profile.Current.Display);
            var expected = JsonNode.Parse(original)!["profile"];
            var restored = Semesters.SemesterTests.LegacyFields(JsonNode.Parse(ProfileJson.Serialize(profile.Current))!["profile"]!);
            restored.AsObject().Remove("display");
            restored.AsObject().Remove("displayPresets");
            Assert.True(JsonNode.DeepEquals(expected, restored)); // all cells, schedules, dates, lunch and precision
            Assert.Equal(original, File.ReadAllBytes(temp.File)); Assert.Equal(modified, File.GetLastWriteTimeUtc(temp.File));
            Assert.Null(profile.SaveDisplay(DisplayPresets.Create(DisplayPreset.Digital)));
            var saved = JsonNode.Parse(File.ReadAllBytes(temp.File))!;
            Assert.Equal(5, saved["schemaVersion"]!.GetValue<int>());
            saved["profile"]!.AsObject().Remove("display");
            saved["profile"]!.AsObject().Remove("displayPresets");
            Assert.True(JsonNode.DeepEquals(expected, Semesters.SemesterTests.LegacyFields(saved["profile"]!)));
        }
        using var restart = new JsonProfileStore(temp.Directory);
        Assert.Equal(DisplayPreset.Digital, restart.Load().Snapshot.Display.Preset.BuiltIn);
    }

    [Theory]
    [InlineData(DisplayPreset.Standard)] [InlineData(DisplayPreset.Digital)]
    [InlineData(DisplayPreset.Compact)] [InlineData(DisplayPreset.Minimal)]
    public void V4RoundTripPreservesAllOverridesAndLogicalMissingFont(DisplayPreset preset)
    {
        var sample = ProfileStorageTests.Sample();
        var display = DisplayPresets.Create(preset);
        display = display with { Time = display.Time with { Font = new(FontSourceKind.System, "Missing Portable Family"),
            Size = 56, Weight = DisplayFontWeight.Bold, Style = DisplayFontStyle.Italic },
            Weekday = display.Weekday with { Size = 21 }, Use24Hour = false, ShowSeconds = false, ShowWeekday = true };
        var value = new ProfileSnapshot(sample.Timetable, sample.Schedule, sample.Overrides, sample.ShowLunch, display);
        var bytes = ProfileJson.Serialize(value);
        var restored = ProfileJson.Deserialize(bytes);
        Assert.Equal(display, restored.Display);
        Assert.Equal(bytes, ProfileJson.Serialize(restored));
        using var temp = new TempProfile(); File.WriteAllBytes(temp.File, bytes);
        using var store = new JsonProfileStore(temp.Directory);
        Assert.Equal(ProfileLoadState.Loaded, store.Load().State);
    }

    public static IEnumerable<object[]> InvalidDisplay()
    {
        (string Name, Action<JsonObject> Mutate)[] cases =
        [
            ("missing display", p => p.Remove("display")),
            ("null display", p => p["display"] = null),
            ("missing time", p => p["display"]!["settings"]!.AsObject().Remove("time")),
            ("null time", p => p["display"]!["settings"]!["time"] = null),
            ("null font", p => p["display"]!["settings"]!["time"]!["font"] = null),
            ("missing font source", p => p["display"]!["settings"]!["time"]!["font"]!.AsObject().Remove("source")),
            ("future source", p => p["display"]!["settings"]!["time"]!["font"]!["source"] = "OnlineDownloaded"),
            ("path family", p => p["display"]!["settings"]!["time"]!["font"]!["family"] = "C:\\font.ttf"),
            ("empty family", p => p["display"]!["settings"]!["date"]!["font"]!["family"] = ""),
            ("null family", p => p["display"]!["settings"]!["date"]!["font"]!["family"] = null),
            ("zero size", p => p["display"]!["settings"]!["time"]!["size"] = 0),
            ("oversize", p => p["display"]!["settings"]!["weekday"]!["size"] = 49),
            ("invalid size type", p => p["display"]!["settings"]!["status"]!["size"] = "large"),
            ("invalid weight", p => p["display"]!["settings"]!["status"]!["weight"] = "Heavy"),
            ("invalid style", p => p["display"]!["settings"]!["date"]!["style"] = "0"),
            ("unknown layout", p => p["display"]!["settings"]!["layout"] = "Other"),
            ("numeric preset", p => p["display"]!["preset"] = 0),
            ("missing seconds", p => p["display"]!["settings"]!.AsObject().Remove("showSeconds")),
            ("null visibility", p => p["display"]!["settings"]!["showDate"] = null),
            ("extra setting", p => p["display"]!["settings"]!["theme"] = "dark")
        ];
        foreach (var (name, mutate) in cases)
        {
            var document = JsonNode.Parse(ProfileJson.Serialize(ProfileStorageTests.Sample()))!;
            mutate(document["profile"]!.AsObject());
            yield return [name, document.ToJsonString()];
        }
    }

    [Theory]
    [MemberData(nameof(InvalidDisplay))]
    public void InvalidV4FailsClosedWithoutPartialLoadOrOverwrite(string name, string json)
    {
        Assert.NotEmpty(name);
        using var temp = new TempProfile(); File.WriteAllText(temp.File, json);
        var bytes = File.ReadAllBytes(temp.File); var modified = File.GetLastWriteTimeUtc(temp.File);
        using (var store = new JsonProfileStore(temp.Directory))
        {
            var profile = new ProfileSession(store);
            Assert.Equal(ProfileLoadState.Invalid, profile.LoadResult.State);
            var runtime = new RuntimeDisplaySettings(profile.Current.Display, profile.Current.DisplayPresets, profile.SaveDisplay);
            var session = runtime.Open(); session.Preset = DisplayPreset.Digital;
            Assert.False(session.TryAccept()); Assert.False(session.IsClosed);
            session.Cancel();
            Assert.Equal(DisplayPresets.Create(DisplayPreset.Standard), runtime.Current);
        }
        Assert.Equal(bytes, File.ReadAllBytes(temp.File)); Assert.Equal(modified, File.GetLastWriteTimeUtc(temp.File));
    }

    [Fact]
    public void V1RemainsStrictAboutUnknownDisplayField()
    {
        var document = JsonNode.Parse(V1())!;
        document["profile"]!["display"] = new JsonObject();
        Assert.Throws<System.Text.Json.JsonException>(() => ProfileJson.Deserialize(Encoding.UTF8.GetBytes(document.ToJsonString())));
    }
}
