using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.Persistence;

namespace SchoolTimetableWidget.Tests.DisplaySettings;

public class UserPresetTransactionTests
{
    [Fact]
    public void CreateCancelDoesNotWriteAndSuccessfulApplyThenCancelRestoresBothBaselines()
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var profile = new ProfileSession(store); var runtime = new ProfileRuntime(profile, () => { }, _ => { });
        var session = runtime.Display.Open(); Assert.True(session.TrySaveAs("A")); session.Cancel();
        Assert.Empty(runtime.Display.CommittedPresets.Items); Assert.False(File.Exists(temp.File));
        session = runtime.Display.Open(); session.Preset = DisplayPreset.Digital; session.Elements[0].SizeText = "56";
        Assert.True(session.TrySaveAs("교무실 시계")); Assert.True(session.TryApply()); Assert.False(session.IsClosed);
        var saved = profile.Current; var bytes = File.ReadAllBytes(temp.File);
        Assert.True(session.TryRename("B")); session.Elements[0].SizeText = "64"; Assert.True(session.TryUpdate());
        Assert.True(session.TrySaveAs("C")); Assert.True(session.TryDelete(saved.Display.Preset.UserId!.Value));
        session.Cancel();
        Assert.Equal(saved.Display, runtime.Display.Current); Assert.Same(saved.DisplayPresets, session.Presets);
        Assert.Same(saved, profile.Current); Assert.Equal(bytes, File.ReadAllBytes(temp.File));
    }
    [Theory]
    [InlineData("rename")] [InlineData("update")] [InlineData("delete")] [InlineData("create")]
    public void EachMutationRollsBackToLastApply(string mutation)
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open(); Assert.True(session.TrySaveAs("A"));
        Assert.True(session.TryApply()); var baseline = owner.CommittedPresets;
        var id = baseline.Items[0].Id;
        switch (mutation)
        {
            case "rename": Assert.True(session.TryRename("B")); break;
            case "update": session.Elements[0].SizeText = "70"; Assert.True(session.TryUpdate()); break;
            case "delete": session.Preset = DisplayPreset.Standard; Assert.True(session.TryDelete(id)); break;
            case "create": Assert.True(session.TrySaveAs("B")); break;
        }
        session.Cancel(); Assert.Same(baseline, session.Presets); Assert.Same(baseline, owner.CommittedPresets);
        Assert.Equal(owner.Committed, owner.Current);
    }
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void AtomicFailureLeavesBothCommittedValuesAndDiskOldWithRetryableDraft(int failureStage)
    {
        using var temp = new TempProfile();
        using (var seed = new JsonProfileStore(temp.Directory)) { seed.Load(); seed.Save(ProfileStorageTests.Sample()); }
        var fail = false;
        using var store = new JsonProfileStore(temp.Directory, stage =>
        { if (fail && (int)stage == failureStage) throw new IOException("Injected"); });
        var profile = new ProfileSession(store); var runtime = new ProfileRuntime(profile, () => { }, _ => { });
        var session = runtime.Display.Open(); Assert.True(session.TrySaveAs("A")); Assert.True(session.TryApply());
        var old = profile.Current; var bytes = File.ReadAllBytes(temp.File); var modified = File.GetLastWriteTimeUtc(temp.File);
        Assert.True(session.TryRename("B")); session.Elements[0].SizeText = "67"; Assert.True(session.TryUpdate());
        Assert.True(session.TrySaveAs("C")); fail = true;
        Assert.False(session.TryAccept()); Assert.False(session.IsClosed); Assert.NotEmpty(session.ErrorText);
        Assert.Same(old, profile.Current); Assert.Equal(old.Display, runtime.Display.Committed);
        Assert.Same(old.DisplayPresets, runtime.Display.CommittedPresets); Assert.Equal(2, session.Presets.Items.Count);
        Assert.Equal(bytes, File.ReadAllBytes(temp.File)); Assert.Equal(modified, File.GetLastWriteTimeUtc(temp.File));
        fail = false; Assert.True(session.TryAccept());
        Assert.Equal(2, profile.Current.DisplayPresets.Items.Count); Assert.Equal(67, profile.Current.Display.Time.Size);
        Assert.Equal(profile.Current.Display, ProfileJson.Deserialize(File.ReadAllBytes(temp.File)).Display);
        Assert.Equal(profile.Current.DisplayPresets.Items, ProfileJson.Deserialize(File.ReadAllBytes(temp.File)).DisplayPresets.Items);
    }
    [Fact]
    public void EveryOtherFeatureSavePreservesCommittedLibraryAndDisplaySavePreservesLatestInputs()
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var profile = new ProfileSession(store); var runtime = new ProfileRuntime(profile, () => { }, _ => { });
        var session = runtime.Display.Open(); Assert.True(session.TrySaveAs("A")); Assert.True(session.TryApply());
        var baseline = profile.Current; Assert.True(session.TryRename("B")); Assert.True(session.TrySaveAs("C"));
        var sample = ProfileStorageTests.Sample();
        Assert.Null(profile.SaveTimetable(sample.Timetable)); Assert.Null(profile.SaveSchedule(sample.Schedule));
        Assert.Null(profile.SaveOverrides(sample.Overrides)); Assert.Null(profile.SaveLunch(true));
        Assert.Same(baseline.DisplayPresets, profile.Current.DisplayPresets); Assert.Equal(baseline.Display, profile.Current.Display);
        Assert.True(session.TryApply()); Assert.Same(sample.Timetable, profile.Current.Timetable);
        Assert.Same(sample.Schedule, profile.Current.Schedule); Assert.Equal(sample.Overrides, profile.Current.Overrides);
        Assert.True(profile.Current.ShowLunch); Assert.Equal(2, profile.Current.DisplayPresets.Items.Count);
    }
}
