using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Fonts;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using SchoolTimetableWidget.Desktop.Features.Fonts;
using SchoolTimetableWidget.Tests.Persistence;

namespace SchoolTimetableWidget.Tests.DisplaySettings;

public class DisplayPresetImportTransactionTests
{
    private static UserDisplayPreset Preset(Guid id, string name, double size = 56) => new(id, name,
        DisplayPresets.Create(DisplayPreset.Digital) with
        { Time = DisplayPresets.Create(DisplayPreset.Digital).Time with { Size = size } });

    [Fact]
    public void NewImportUpdatesOnlyDraftLibraryAndDoesNotActivateDisplay()
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open(); var before = owner.Current;
        var imported = Preset(Guid.NewGuid(), "가져온 시계"); var candidate = session.InspectImport(imported);
        Assert.Equal(PresetImportCollision.None, candidate.Collision);
        Assert.True(session.TryImport(candidate, PresetImportAction.Add, imported.Name));
        Assert.Equal(imported, Assert.Single(session.Presets.Items));
        Assert.Equal(before, owner.Current); Assert.Equal(before.Preset, session.Preset);
        Assert.Empty(owner.CommittedPresets.Items);
        session.Cancel(); Assert.Empty(session.Presets.Items); Assert.Equal(before, owner.Current);
    }

    [Fact]
    public void ApplyCommitsImportAndFailedSaveRetainsRetryableDraft()
    {
        var fail = true; var owner = new RuntimeDisplaySettings(DisplayPresets.Create(DisplayPreset.Standard),
            UserDisplayPresetLibrary.Empty, (_, _) => fail ? "저장 실패" : null);
        var session = owner.Open(); var imported = Preset(Guid.NewGuid(), "A");
        Assert.True(session.TryImport(session.InspectImport(imported), PresetImportAction.Add, "A"));
        Assert.False(session.TryApply()); Assert.Single(session.Presets.Items); Assert.Empty(owner.CommittedPresets.Items);
        fail = false; Assert.True(session.TryApply()); Assert.Equal(imported, Assert.Single(owner.CommittedPresets.Items));
    }

    [Fact]
    public void SameIdSupportsUpdateCopyAndCancelWithoutSilentOverwrite()
    {
        var id = Guid.NewGuid(); var owner = DisplayModelTests.Owner(); var session = owner.Open();
        Assert.True(session.TryImport(session.InspectImport(Preset(id, "A", 40)), PresetImportAction.Add, "A"));
        Assert.True(session.TryApply()); var baseline = session.Presets;
        var incoming = Preset(id, "A", 72); var collision = session.InspectImport(incoming);
        Assert.Equal(PresetImportCollision.SameId, collision.Collision);
        Assert.False(session.TryImport(collision, PresetImportAction.Add, "A"));
        Assert.Equal(40, session.Presets.Get(id).Display.Time.Size);
        Assert.True(session.TryImport(collision, PresetImportAction.UpdateExisting, "A"));
        Assert.Equal(id, session.Presets.Get(id).Id); Assert.Equal(72, session.Presets.Get(id).Display.Time.Size);
        Assert.True(session.TryImport(collision, PresetImportAction.ImportAsCopy, "A (복사본)"));
        var copy = session.Presets.Items.Single(p => p.Id != id);
        Assert.NotEqual(id, copy.Id); Assert.Equal(incoming.Display with { Preset = DisplayPresetReference.User(copy.Id) }, copy.Display);
        session.Cancel(); Assert.Same(baseline, session.Presets); Assert.Single(owner.CommittedPresets.Items);
        Assert.Equal(40, owner.CommittedPresets.Get(id).Display.Time.Size);
    }

    [Fact]
    public void NameOnlyCollisionRequiresConfirmedUniqueName()
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        var first = Preset(Guid.NewGuid(), "같은 이름");
        Assert.True(session.TryImport(session.InspectImport(first), PresetImportAction.Add, first.Name));
        var second = Preset(Guid.NewGuid(), "같은 이름", 70); var collision = session.InspectImport(second);
        Assert.Equal(PresetImportCollision.SameName, collision.Collision);
        Assert.False(session.TryImport(collision, PresetImportAction.Add, second.Name));
        Assert.False(session.TryImport(collision, PresetImportAction.ImportAsCopy, second.Name));
        Assert.True(session.TryImport(collision, PresetImportAction.ImportAsCopy, "같은 이름 (복사본)"));
        Assert.Equal(2, session.Presets.Items.Count); Assert.Equal(70, session.Presets.Items[1].Display.Time.Size);
    }

    [Fact]
    public void BuiltInLabelsRemainAllowedButCannotBecomeBuiltInEntries()
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open(); var preset = Preset(Guid.NewGuid(), "표준");
        Assert.True(session.TryImport(session.InspectImport(preset), PresetImportAction.Add, "표준"));
        var imported = Assert.Single(session.Presets.Items);
        Assert.Equal(preset.Id, imported.Id); Assert.Null(imported.Display.Preset.BuiltIn);
        Assert.Equal(preset.Id, imported.Display.Preset.UserId);
    }

    [Fact]
    public void ExportReadsSavedDraftTemplateNotUncapturedControls()
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open(); session.Preset = DisplayPreset.Digital;
        Assert.True(session.TrySaveAs("A")); var savedSize = session.SelectedUserPreset().Display.Time.Size;
        session.Elements[0].SizeText = "72";
        var exported = DisplayPresetFile.Import(DisplayPresetFile.Export(session.SelectedUserPreset()));
        Assert.Equal(savedSize, exported.Display.Time.Size); Assert.Equal(72, owner.Current.Time.Size);
    }

    [Fact]
    public async Task ExportImportApplyRestartRestoresExactPresetAcrossFontAvailabilityWithoutActivation()
    {
        var baseDisplay = DisplayPresets.Create(DisplayPreset.Digital);
        var original = new UserDisplayPreset(Guid.NewGuid(), "온라인 시계", baseDisplay with
        {
            Time = baseDisplay.Time with { Font = FontCatalog.Get(FontSourceKind.OnlineDownloaded, "orbitron").Selection },
            Date = baseDisplay.Date with { Font = FontCatalog.Get(FontSourceKind.Bundled, "pretendard").Selection },
            Status = baseDisplay.Status with { Font = new(FontSourceKind.System, "Missing Font 7AC98B1E") }
        });
        var file = DisplayPresetFile.Export(original);

        using (var cachedDir = new TempProfile())
        {
            var cache = new DownloadedFontCache(cachedDir.Directory, new SchoolTimetableWidget.Tests.Fonts.FakeFontTransport());
            await cache.DownloadAsync(original.Display.Time.Font, TestContext.Current.CancellationToken);
            var cachedOwner = new RuntimeDisplaySettings(DisplayPresets.Create(DisplayPreset.Standard),
                UserDisplayPresetLibrary.Empty, (_, _) => null, new FontLibrary(cache));
            var cached = cachedOwner.Open();
            Assert.Contains(cached.InspectImport(original).Fonts,
                f => f.Font.Source == FontSourceKind.OnlineDownloaded && f.IsAvailable);
            cached.Cancel();
        }

        using var targetDir = new TempProfile();
        using (var targetStore = new JsonProfileStore(targetDir.Directory))
        {
            var targetProfile = new ProfileSession(targetStore); var target = new ProfileRuntime(targetProfile, () => { }, _ => { });
            var targetSession = target.Display.Open(); var imported = DisplayPresetFile.Import(file);
            var candidate = targetSession.InspectImport(imported);
            Assert.Contains(candidate.Fonts, f => f.Font.Source == FontSourceKind.OnlineDownloaded && !f.IsAvailable && f.Status.Contains("다운로드 필요"));
            Assert.Contains(candidate.Fonts, f => f.Font.Source == FontSourceKind.Bundled && f.IsAvailable);
            Assert.Contains(candidate.Fonts, f => f.Font.Source == FontSourceKind.System && !f.IsAvailable && f.Status.Contains("이 PC에 없음"));
            Assert.True(targetSession.TryImport(candidate, PresetImportAction.Add, imported.Name));
            Assert.Equal(DisplayPreset.Standard, target.Display.Current.Preset.BuiltIn); Assert.True(targetSession.TryApply());
        }
        using var reopenedStore = new JsonProfileStore(targetDir.Directory); var reopened = new ProfileSession(reopenedStore);
        var restored = Assert.Single(reopened.Current.DisplayPresets.Items);
        Assert.Equal(original, restored); Assert.Equal(DisplayPreset.Standard, reopened.Current.Display.Preset.BuiltIn);
        Assert.Equal(4, System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllBytes(targetDir.File))!["schemaVersion"]!.GetValue<int>());
    }
}
