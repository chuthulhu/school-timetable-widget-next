using System.Text;
using System.Text.Json.Nodes;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Features.Fonts;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;

namespace SchoolTimetableWidget.Tests.Persistence;

public class ProfileBackupFileTests
{
    internal static ProfileSnapshot Sample()
    {
        var data = ProfileStorageTests.Sample();
        var display = DisplayPresets.Create(DisplayPreset.Digital);
        display = display with
        {
            Time = display.Time with { Font = FontCatalog.Get(FontSourceKind.OnlineDownloaded, "orbitron").Selection },
            Date = display.Date with { Font = FontCatalog.Get(FontSourceKind.Bundled, "pretendard").Selection },
            Status = display.Status with { Font = new(FontSourceKind.System, "Unavailable Test Family") }
        };
        var preset = new UserDisplayPreset(Guid.Parse("1206b7ed-93a5-40c6-8f47-cc067501208b"), "교무실", display);
        return new(data.Timetable, data.Schedule, data.Overrides, data.ShowLunch,
            preset.Display, new([preset]));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CompleteCanonicalRoundTrip(bool populated)
    {
        var original = populated ? Sample() : ProfileSnapshot.Defaults();
        var bytes = ProfileBackupFile.Export(original);
        var restored = ProfileBackupFile.Import(bytes);
        Assert.Equal(ProfileJson.Serialize(original), ProfileJson.Serialize(restored));
        Assert.Equal(bytes, ProfileBackupFile.Export(restored));
        Assert.False(bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble));
        var root = JsonNode.Parse(bytes)!;
        Assert.Equal(new[] { "backupFileVersion", "profileSchemaVersion", "profile" }, root.AsObject().Select(p => p.Key));
        Assert.Equal(1, root["backupFileVersion"]!.GetValue<int>());
        Assert.Equal(4, root["profileSchemaVersion"]!.GetValue<int>());
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void OlderLoadedProfilesExportCanonicalBackupWithoutRewritingSource(int version)
    {
        var assembly = typeof(ProfileBackupFileTests).Assembly;
        var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith($"profile-v{version}.json"));
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var buffer = new MemoryStream(); stream.CopyTo(buffer);
        var original = buffer.ToArray();
        var loaded = ProfileJson.Deserialize(original);
        var backup = ProfileBackupFile.Export(loaded);
        Assert.Equal(ProfileJson.Serialize(loaded), ProfileJson.Serialize(ProfileBackupFile.Import(backup)));
        Assert.Equal(version, JsonNode.Parse(original)!["schemaVersion"]!.GetValue<int>());
    }

    [Fact]
    public void FileShapeExcludesRuntimeMachineAndBinaryMetadata()
    {
        var root = JsonNode.Parse(ProfileBackupFile.Export(Sample()))!;
        var profile = root["profile"]!;
        Assert.Equal(new[] { "timetable", "periodSchedule", "dateOverrides", "presentation", "display", "displayPresets" },
            profile.AsObject().Select(p => p.Key));
        var text = root.ToJsonString();
        foreach (var forbidden in new[] { "currentTime", "currentStatus", "countdown", "highlight", "viewedWeek",
            "cache", "path", "username", "localappdata", "base64", ".ttf", ".otf", "http:", "https:" })
            Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
        foreach (var element in new[] { "time", "date", "weekday", "status" })
            Assert.Equal(new[] { "source", "family", "familyId" },
                profile["display"]!["settings"]![element]!["font"]!.AsObject().Select(p => p.Key));
    }

    [Theory]
    [InlineData("malformed")] [InlineData("empty")] [InlineData("oversized")]
    [InlineData("future")] [InlineData("profileVersion")] [InlineData("missing")]
    [InlineData("timetable")] [InlineData("period")] [InlineData("override")]
    [InlineData("display")] [InlineData("font")] [InlineData("duplicateId")]
    [InlineData("duplicateName")] [InlineData("dangling")] [InlineData("duplicateProperty")]
    [InlineData("unknownProperty")] [InlineData("profileFile")]
    public void InvalidBackupRejectsEntireCandidate(string kind)
    {
        var node = JsonNode.Parse(ProfileBackupFile.Export(Sample()))!;
        var profile = node["profile"]!;
        byte[]? bytes = null;
        switch (kind)
        {
            case "malformed": bytes = "{"u8.ToArray(); break;
            case "empty": bytes = []; break;
            case "oversized": bytes = new byte[ProfileBackupFile.MaximumBytes + 1]; break;
            case "future": node["backupFileVersion"] = 2; break;
            case "profileVersion": node["profileSchemaVersion"] = 5; break;
            case "missing": profile.AsObject().Remove("presentation"); break;
            case "timetable": profile["timetable"]!.AsArray().RemoveAt(0); break;
            case "period": profile["periodSchedule"]![0]!["end"] = "10:30:00.0000000"; break;
            case "override": profile["dateOverrides"]![0]!["date"] = "2026-02-30"; break;
            case "display": profile["display"]!["settings"]!["time"]!["size"] = 1000; break;
            case "font": profile["display"]!["settings"]!["time"]!["font"]!["familyId"] = "unknown"; break;
            case "duplicateId": profile["displayPresets"]!.AsArray().Add(profile["displayPresets"]![0]!.DeepClone()); break;
            case "duplicateName":
                var copy = profile["displayPresets"]![0]!.DeepClone(); copy["id"] = Guid.NewGuid().ToString("D");
                profile["displayPresets"]!.AsArray().Add(copy); break;
            case "dangling": profile["display"]!["preset"]!["userId"] = Guid.NewGuid().ToString("D"); break;
            case "duplicateProperty": bytes = Encoding.UTF8.GetBytes(node.ToJsonString().Replace("\"backupFileVersion\":1", "\"backupFileVersion\":1,\"backupFileVersion\":1")); break;
            case "unknownProperty": profile["currentStatus"] = "unexpected"; break;
            case "profileFile": bytes = ProfileJson.Serialize(Sample()); break;
        }
        bytes ??= Encoding.UTF8.GetBytes(node.ToJsonString());
        using var temp = new TempProfile();
        using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store); Assert.Null(session.SaveLunch(true));
        var before = File.ReadAllBytes(temp.File); var committed = session.Current;
        Assert.Throws<InvalidDataException>(() => ProfileBackupFile.Import(bytes));
        Assert.Same(committed, session.Current); Assert.Equal(before, File.ReadAllBytes(temp.File));
    }

    [Fact]
    public void ExportAndAtomicWriteDoNotMutateCommittedProfileOrDraft()
    {
        using var temp = new TempProfile();
        using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store, Sample()); Assert.Null(session.SaveLunch(true));
        var before = File.ReadAllBytes(temp.File); var committed = session.Current;
        var runtime = new ProfileRuntime(session, () => { }, _ => { });
        var editor = runtime.Timetable.Editor.BeginEdit(runtime.Timetable.Cells[0]); editor.SubjectText = "UNAPPLIED";
        var destination = Path.Combine(temp.Directory, "export.stwbackup");
        AtomicProfileFile.Write(destination, ProfileBackupFile.Export(session.Current));
        Assert.Equal(before, File.ReadAllBytes(temp.File)); Assert.Same(committed, session.Current);
        Assert.Equal(before, ProfileJson.Serialize(ProfileBackupFile.Read(destination)));
        Assert.Equal("UNAPPLIED", editor.SubjectText); Assert.False(Directory.Exists(Path.Combine(temp.Directory, "fonts")));
        editor.Cancel();
    }

    [Fact]
    public void FailedBackupReplacementPreservesExistingDestinationAndCleansOnlyOwnedTemp()
    {
        using var temp = new TempProfile();
        var destination = Path.Combine(temp.Directory, "export.stwbackup");
        var old = ProfileBackupFile.Export(Sample()); File.WriteAllBytes(destination, old);
        var unrelated = Path.Combine(temp.Directory, ".unrelated.tmp"); File.WriteAllText(unrelated, "keep");
        Assert.Throws<IOException>(() => AtomicProfileFile.Write(destination,
            ProfileBackupFile.Export(ProfileSnapshot.Defaults()), () => throw new IOException("injected")));
        Assert.Equal(old, File.ReadAllBytes(destination)); Assert.Equal("keep", File.ReadAllText(unrelated));
        Assert.Empty(Directory.GetFiles(temp.Directory, ".stw-*.tmp"));
    }

    [Fact]
    public void OversizedFileIsRejectedAtReadBoundary()
    {
        using var temp = new TempProfile(); var path = Path.Combine(temp.Directory, "large.stwbackup");
        using (var file = File.Create(path)) file.SetLength(ProfileBackupFile.MaximumBytes + 1L);
        Assert.Throws<InvalidDataException>(() => ProfileBackupFile.Read(path));
    }
}
