using System.Text.Json.Nodes;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.Persistence;

namespace SchoolTimetableWidget.Tests.DisplaySettings;

public class UserPresetPersistenceTests
{
    internal static byte[] Fixture(int version)
    {
        using var stream = typeof(UserPresetPersistenceTests).Assembly.GetManifestResourceStream(
            $"SchoolTimetableWidget.Tests.DisplaySettings.profile-v{version}.json")!;
        using var memory = new MemoryStream(); stream.CopyTo(memory); return memory.ToArray();
    }
    [Theory]
    [InlineData(1)] [InlineData(2)]
    public void FixedOldProfileLoadsWritableWithoutRewriteThenUpgradesWithAllInputsExact(int version)
    {
        using var temp = new TempProfile(); var bytes = Fixture(version); File.WriteAllBytes(temp.File, bytes);
        var modified = File.GetLastWriteTimeUtc(temp.File);
        using var store = new JsonProfileStore(temp.Directory); var profile = new ProfileSession(store);
        Assert.Equal(ProfileLoadState.Loaded, profile.LoadResult.State); Assert.True(profile.LoadResult.CanWrite);
        Assert.Empty(profile.Current.DisplayPresets.Items); Assert.Empty(profile.LoadResult.Notice);
        Assert.Equal(bytes, File.ReadAllBytes(temp.File)); Assert.Equal(modified, File.GetLastWriteTimeUtc(temp.File));
        var expected = JsonNode.Parse(bytes)!["profile"]!.AsObject();
        if (version == 2)
        {
            var sourceDisplay = expected["display"]!.DeepClone().AsObject();
            var builtIn = sourceDisplay["preset"]!.GetValue<string>(); sourceDisplay.Remove("preset");
            var actual = JsonNode.Parse(ProfileJson.Serialize(profile.Current))!["profile"]!["display"]!;
            Assert.Equal(builtIn, actual["preset"]!["builtIn"]!.GetValue<string>());
            foreach (var key in new[] { "time", "date", "weekday", "status" })
                sourceDisplay[key]!["font"]!["familyId"] = sourceDisplay[key]!["font"]!["family"]!.DeepClone();
            Assert.True(JsonNode.DeepEquals(sourceDisplay, actual["settings"]));
        }
        expected.Remove("display");
        Assert.Null(profile.SaveLunch(profile.Current.ShowLunch));
        var saved = JsonNode.Parse(File.ReadAllBytes(temp.File))!;
        Assert.Equal(4, saved["schemaVersion"]!.GetValue<int>());
        var actualProfile = saved["profile"]!.AsObject(); actualProfile.Remove("display"); actualProfile.Remove("displayPresets");
        Assert.True(JsonNode.DeepEquals(expected, actualProfile));
    }
    [Fact]
    public void ApplyRestartSelectAndResetRestoreExactPayloadAndKeepMissingFontIdentity()
    {
        using var temp = new TempProfile(); Guid id; DisplayConfiguration saved;
        using (var store = new JsonProfileStore(temp.Directory))
        {
            File.WriteAllBytes(temp.File, Fixture(2));
            var profile = new ProfileSession(store); var runtime = new ProfileRuntime(profile, () => { }, _ => { });
            var session = runtime.Display.Open();
            Assert.True(session.TrySaveAs("교무실 시계 😀")); id = session.Preset.UserId!.Value;
            Assert.True(session.TrySaveAs("노트북용"));
            session.Preset = DisplayPresetReference.User(id); saved = runtime.Display.Current;
            Assert.True(session.TryAccept());
        }
        var bytes = File.ReadAllBytes(temp.File); var modified = File.GetLastWriteTimeUtc(temp.File);
        using var restartStore = new JsonProfileStore(temp.Directory); var restart = new ProfileSession(restartStore);
        Assert.Equal(ProfileLoadState.Loaded, restart.LoadResult.State); Assert.Equal(saved, restart.Current.Display);
        Assert.Equal(2, restart.Current.DisplayPresets.Items.Count);
        Assert.Equal(bytes, File.ReadAllBytes(temp.File)); Assert.Equal(modified, File.GetLastWriteTimeUtc(temp.File));
        var owner = new ProfileRuntime(restart, () => { }, _ => { }).Display; var editor = owner.Open();
        editor.Preset = DisplayPreset.Standard; editor.Preset = DisplayPresetReference.User(id);
        Assert.Equal(saved, owner.Current); Assert.Equal("Missing Portable Family", owner.Current.Time.Font.Family);
        editor.Elements[0].SizeText = "75"; editor.Reset(); Assert.Equal(saved, owner.Current);
        Assert.Equal(bytes, ProfileJson.Serialize(restart.Current));
        var json = JsonNode.Parse(bytes)!;
        Assert.All(json["profile"]!["displayPresets"]!.AsArray(), p =>
            Assert.Null(p!["settings"]!["preset"])); // payloads contain no reference graph or built-in definitions
    }
    internal static byte[] CustomProfile()
    {
        var sample = ProfileStorageTests.Sample(); var display = DisplayPresets.Create(DisplayPreset.Digital);
        var a = new UserDisplayPreset(Guid.Parse("4e5d72c1-f046-4333-a2d2-84c8e65bb3ad"), "교무실 시계", display);
        var b = new UserDisplayPreset(Guid.Parse("59b40a0e-b5f0-4c4d-a08b-7ff7330f4d5e"), "노트북용", display);
        return ProfileJson.Serialize(new(sample.Timetable, sample.Schedule, sample.Overrides, sample.ShowLunch,
            a.Display, new([a, b])));
    }
    public static IEnumerable<object[]> InvalidPresets()
    {
        (string, Action<JsonNode>)[] cases =
        [
            ("missing library", p => p.AsObject().Remove("displayPresets")),
            ("null library", p => p["displayPresets"] = null),
            ("null entry", p => p["displayPresets"]![0] = null),
            ("invalid id", p => p["displayPresets"]![0]!["id"] = "not-id"),
            ("empty id", p => p["displayPresets"]![0]!["id"] = Guid.Empty.ToString()),
            ("duplicate id", p => p["displayPresets"]![1]!["id"] = p["displayPresets"]![0]!["id"]!.DeepClone()),
            ("duplicate normalized name", p => p["displayPresets"]![1]!["name"] = " 교무실 시계 "),
            ("case duplicate", p => { p["displayPresets"]![0]!["name"] = "Clock"; p["displayPresets"]![1]!["name"] = " CLOCK "; }),
            ("unicode duplicate", p => { p["displayPresets"]![0]!["name"] = "Café"; p["displayPresets"]![1]!["name"] = "Cafe\u0301"; }),
            ("blank name", p => p["displayPresets"]![0]!["name"] = " "),
            ("long name", p => p["displayPresets"]![0]!["name"] = new string('a', 61)),
            ("null name", p => p["displayPresets"]![0]!["name"] = null),
            ("missing payload", p => p["displayPresets"]![0]!.AsObject().Remove("settings")),
            ("null payload", p => p["displayPresets"]![0]!["settings"] = null),
            ("bad payload", p => p["displayPresets"]![0]!["settings"]!["time"]!["size"] = 97),
            ("missing payload boolean", p => p["displayPresets"]![0]!["settings"]!.AsObject().Remove("showStatus")),
            ("invalid font source", p => p["displayPresets"]![0]!["settings"]!["time"]!["font"]!["source"] = "Online"),
            ("invalid font identity", p => p["displayPresets"]![0]!["settings"]!["time"]!["font"]!["family"] = "C:/font.ttf"),
            ("font binary", p => p["displayPresets"]![0]!["fontData"] = "base64"),
            ("dangling reference", p => p["display"]!["preset"]!["userId"] = Guid.NewGuid().ToString()),
            ("unknown reference kind", p => p["display"]!["preset"]!["kind"] = "Other"),
            ("mixed reference", p => p["display"]!["preset"]!["builtIn"] = "Digital"),
            ("wrong kind", p => p["display"]!["preset"]!["kind"] = "BuiltIn"),
            ("null reference", p => p["display"]!["preset"] = null),
            ("missing reference field", p => p["display"]!["preset"]!.AsObject().Remove("builtIn"))
        ];
        foreach (var (name, mutate) in cases)
        {
            var json = JsonNode.Parse(CustomProfile())!; mutate(json["profile"]!);
            yield return [name, json.ToJsonString()];
        }
    }
    [Theory]
    [MemberData(nameof(InvalidPresets))]
    public void InvalidLibraryOrReferenceRejectsWholeProfileAndPreservesOriginal(string name, string json)
    {
        Assert.NotEmpty(name); using var temp = new TempProfile(); File.WriteAllText(temp.File, json);
        var bytes = File.ReadAllBytes(temp.File); var modified = File.GetLastWriteTimeUtc(temp.File);
        using var store = new JsonProfileStore(temp.Directory); var profile = new ProfileSession(store);
        Assert.Equal(ProfileLoadState.Invalid, profile.LoadResult.State); Assert.False(profile.LoadResult.CanWrite);
        Assert.Empty(profile.Current.DisplayPresets.Items);
        var session = new ProfileRuntime(profile, () => { }, _ => { }).Display.Open();
        Assert.True(session.TrySaveAs("임시")); Assert.False(session.TryApply()); session.Cancel();
        Assert.Equal(bytes, File.ReadAllBytes(temp.File)); Assert.Equal(modified, File.GetLastWriteTimeUtc(temp.File));
    }
    [Fact]
    public void V2StillRejectsV3LibraryAsUnknownField()
    {
        var json = JsonNode.Parse(Fixture(2))!; json["profile"]!["displayPresets"] = new JsonArray();
        Assert.Throws<System.Text.Json.JsonException>(() => ProfileJson.Deserialize(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString())));
    }
}
