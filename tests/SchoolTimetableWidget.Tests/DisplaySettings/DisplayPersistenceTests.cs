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

    [Fact]
    public void V1LoadsAllInputsExactlyWithoutRewriteOrDegradationAndNextSaveWritesV3()
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
            var restored = JsonNode.Parse(ProfileJson.Serialize(profile.Current))!["profile"]!;
            restored.AsObject().Remove("display");
            restored.AsObject().Remove("displayPresets");
            Assert.True(JsonNode.DeepEquals(expected, restored)); // all cells, schedules, dates, lunch and precision
            Assert.Equal(original, File.ReadAllBytes(temp.File)); Assert.Equal(modified, File.GetLastWriteTimeUtc(temp.File));
            Assert.Null(profile.SaveDisplay(DisplayPresets.Create(DisplayPreset.Digital)));
            var saved = JsonNode.Parse(File.ReadAllBytes(temp.File))!;
            Assert.Equal(3, saved["schemaVersion"]!.GetValue<int>());
            saved["profile"]!.AsObject().Remove("display");
            saved["profile"]!.AsObject().Remove("displayPresets");
            Assert.True(JsonNode.DeepEquals(expected, saved["profile"]));
        }
        using var restart = new JsonProfileStore(temp.Directory);
        Assert.Equal(DisplayPreset.Digital, restart.Load().Snapshot.Display.Preset.BuiltIn);
    }

    [Theory]
    [InlineData(DisplayPreset.Standard)] [InlineData(DisplayPreset.Digital)]
    [InlineData(DisplayPreset.Compact)] [InlineData(DisplayPreset.Minimal)]
    public void V3RoundTripPreservesAllOverridesAndLogicalMissingFont(DisplayPreset preset)
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
    public void InvalidV3FailsClosedWithoutPartialLoadOrOverwrite(string name, string json)
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
