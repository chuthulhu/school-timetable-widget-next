using System.Text;
using System.Text.Json.Nodes;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Features.Fonts;

namespace SchoolTimetableWidget.Tests.DisplaySettings;

public class DisplayPresetFileTests
{
    private static UserDisplayPreset Sample(Guid? id = null, string name = "교무실 시계")
    {
        var display = DisplayPresets.Create(DisplayPreset.Digital) with
        {
            Time = DisplayPresets.Create(DisplayPreset.Digital).Time with
                { Font = FontCatalog.Get(FontSourceKind.OnlineDownloaded, "orbitron").Selection, Size = 56 },
            Date = DisplayPresets.Create(DisplayPreset.Digital).Date with
                { Font = FontCatalog.Get(FontSourceKind.Bundled, "pretendard").Selection },
            Status = DisplayPresets.Create(DisplayPreset.Digital).Status with
                { Font = new(FontSourceKind.System, "맑은 고딕"), Size = 15 },
            Use24Hour = false, ShowSeconds = false, ShowWeekday = true
        };
        return new(id ?? Guid.Parse("b9a0d238-e916-4aaf-a871-f40390e594fa"), name, display);
    }

    [Fact]
    public void ExportAndImportPreserveExactIdentityNameConfigurationAndUtf8()
    {
        var preset = Sample(); var bytes = DisplayPresetFile.Export(preset);
        Assert.Contains("교무실 시계", Encoding.UTF8.GetString(bytes));
        var imported = DisplayPresetFile.Import(bytes);
        Assert.Equal(preset.Id, imported.Id); Assert.Equal(preset.Name, imported.Name);
        Assert.Equal(preset.Display, imported.Display);
        Assert.Equal(bytes, DisplayPresetFile.Export(imported));
    }

    [Fact]
    public void FileContainsOnlyVersionPresetSettingsAndPortableFontReferences()
    {
        var json = JsonNode.Parse(DisplayPresetFile.Export(Sample()))!;
        Assert.Equal(1, json["presetFileVersion"]!.GetValue<int>());
        Assert.Equal(new[] { "presetFileVersion", "preset" }, json.AsObject().Select(p => p.Key));
        var preset = json["preset"]!; Assert.Equal(new[] { "id", "name", "settings" }, preset.AsObject().Select(p => p.Key));
        var text = json.ToJsonString();
        Assert.Contains("OnlineDownloaded", text); Assert.Contains("orbitron", text);
        Assert.Contains("Bundled", text); Assert.Contains("pretendard", text); Assert.Contains("System", text);
        foreach (var forbidden in new[] { "timetable", "subjectText", "classText", "periodSchedule", "dateOverrides",
            "showLunch", "profile", "cache", "path", "username", "base64", ".ttf", ".otf" })
            Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("version")][InlineData("missing")][InlineData("id")][InlineData("name")]
    [InlineData("source")][InlineData("bundled")][InlineData("online")][InlineData("system-id")]
    [InlineData("size")][InlineData("weight")][InlineData("style")][InlineData("layout")][InlineData("extra")]
    public void StrictImporterRejectsInvalidDocuments(string kind)
    {
        var node = JsonNode.Parse(DisplayPresetFile.Export(Sample()))!;
        var preset = node["preset"]!; var settings = preset["settings"]!; var font = settings["time"]!["font"]!;
        switch (kind)
        {
            case "version": node["presetFileVersion"] = 2; break;
            case "missing": settings.AsObject().Remove("showStatus"); break;
            case "id": preset["id"] = Guid.Empty.ToString("D"); break;
            case "name": preset["name"] = " "; break;
            case "source": font["source"] = "LocalFile"; break;
            case "bundled": font["source"] = "Bundled"; font["familyId"] = "missing"; font["family"] = "Missing"; break;
            case "online": font["familyId"] = "missing"; break;
            case "system-id": font["source"] = "System"; font["family"] = "Segoe UI"; font["familyId"] = "wrong"; break;
            case "size": settings["time"]!["size"] = 1000; break;
            case "weight": settings["time"]!["weight"] = "Heavy"; break;
            case "style": settings["time"]!["style"] = "Oblique"; break;
            case "layout": settings["layout"] = "Future"; break;
            case "extra": preset["payload"] = "unexpected"; break;
        }
        var bytes = Encoding.UTF8.GetBytes(node.ToJsonString());
        if (kind == "version") Assert.Throws<UnsupportedPresetFileVersionException>(() => DisplayPresetFile.Import(bytes));
        else Assert.Throws<InvalidDataException>(() => DisplayPresetFile.Import(bytes));
    }

    [Fact]
    public void MalformedDuplicateAndOversizedInputsAreRejected()
    {
        Assert.Throws<InvalidDataException>(() => DisplayPresetFile.Import("{"u8));
        Assert.Throws<InvalidDataException>(() => DisplayPresetFile.Import("}"u8));
        Assert.Throws<InvalidDataException>(() => DisplayPresetFile.Import("[]"u8));
        var text = Encoding.UTF8.GetString(DisplayPresetFile.Export(Sample()));
        var duplicate = text.Replace("\"presetFileVersion\": 1", "\"presetFileVersion\": 1,\n  \"presetFileVersion\": 1");
        Assert.Throws<InvalidDataException>(() => DisplayPresetFile.Import(Encoding.UTF8.GetBytes(duplicate)));
        Assert.Throws<InvalidDataException>(() => DisplayPresetFile.Import(new byte[DisplayPresetFile.MaximumBytes + 1]));
    }

    [Fact]
    public void WindowsDialogBoundaryUsesClearTitlesAndDedicatedExtension()
    {
        var save = WindowsPresetFileDialogs.CreateExportDialog("교무실 시계.stwpreset");
        Assert.Equal("프리셋 내보내기", save.Title); Assert.Equal("stwpreset", save.DefaultExt);
        Assert.True(save.AddExtension); Assert.True(save.OverwritePrompt); Assert.Contains("*.stwpreset", save.Filter);
        var open = WindowsPresetFileDialogs.CreateImportDialog();
        Assert.Equal("프리셋 가져오기", open.Title); Assert.Equal("stwpreset", open.DefaultExt);
        Assert.False(open.Multiselect); Assert.True(open.CheckFileExists); Assert.Contains("*.stwpreset", open.Filter);
    }
}
