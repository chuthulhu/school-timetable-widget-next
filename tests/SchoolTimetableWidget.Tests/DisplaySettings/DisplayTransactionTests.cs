using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.Persistence;

namespace SchoolTimetableWidget.Tests.DisplaySettings;

public class DisplayTransactionTests
{
    [Fact]
    public void PresetPreviewAndCancelDoNotWrite()
    {
        var saves = 0;
        var owner = new RuntimeDisplaySettings(DisplayPresets.Create(DisplayPreset.Standard), _ => { saves++; return null; });
        var session = owner.Open();
        session.Preset = DisplayPreset.Digital;
        Assert.Equal(DisplayPreset.Digital, owner.Current.Preset); Assert.Equal(DisplayPreset.Standard, owner.Committed.Preset);
        session.Cancel();
        Assert.Equal(owner.Committed, owner.Current); Assert.Equal(DisplayPreset.Standard, session.Preset);
        Assert.Equal(0, saves); Assert.True(session.IsClosed);
    }

    [Fact]
    public void ApplyKeepsDialogAndCancelRestoresLastSuccessfulBaselineIncludingControls()
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        session.Preset = DisplayPreset.Digital;
        session.Elements[0].SizeText = "56";
        session.Elements[1].Weight = DisplayFontWeight.Bold;
        Assert.True(session.TryApply()); Assert.False(session.IsClosed);
        var baseline = owner.Committed;
        session.Preset = DisplayPreset.Minimal;
        session.Cancel();
        Assert.Equal(baseline, owner.Current); Assert.Equal(baseline, owner.Committed);
        Assert.Equal(DisplayPreset.Digital, session.Preset);
        Assert.Equal("56", session.Elements[0].SizeText);
        Assert.Equal(DisplayFontWeight.Bold, session.Elements[1].Weight);
    }

    [Fact]
    public void ResetUsesCurrentPresetAndIsCancelable()
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        session.Preset = DisplayPreset.Digital; session.Elements[0].SizeText = "56";
        Assert.True(session.TryApply());
        session.Reset(); Assert.Equal(DisplayPresets.Create(DisplayPreset.Digital), owner.Current);
        Assert.Equal(56, owner.Committed.Time.Size);
        session.Cancel(); Assert.Equal(56, owner.Current.Time.Size);
    }

    [Fact]
    public void FailureRetainsDraftAndBaselineThenRetryAcceptCloses()
    {
        var fail = true;
        var owner = new RuntimeDisplaySettings(DisplayPresets.Create(DisplayPreset.Standard), _ => fail ? "저장 실패" : null);
        var session = owner.Open(); session.Preset = DisplayPreset.Digital;
        Assert.False(session.TryAccept()); Assert.False(session.IsClosed);
        Assert.Equal("저장 실패", session.ErrorText); Assert.Equal(DisplayPreset.Standard, owner.Committed.Preset);
        Assert.Equal(DisplayPreset.Digital, session.Preset); Assert.Equal(DisplayPreset.Digital, owner.Current.Preset);
        fail = false;
        Assert.True(session.TryAccept()); Assert.True(session.IsClosed);
        session.Cancel(); Assert.Equal(DisplayPreset.Digital, owner.Current.Preset);
        Assert.False(session.TryApply());
    }

    [Fact]
    public void OtherFeatureSavesPreserveCommittedDisplayAndDoNotSavePreview()
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var profile = new ProfileSession(store);
        var runtime = new ProfileRuntime(profile, () => { }, _ => { });
        var session = runtime.Display.Open(); session.Preset = DisplayPreset.Digital;
        Assert.Null(profile.SaveLunch(true));
        Assert.Equal(DisplayPreset.Standard, profile.Current.Display.Preset);
        Assert.True(session.TryApply());
        Assert.True(profile.Current.ShowLunch);
        Assert.Null(profile.SaveTimetable(profile.Current.Timetable));
        Assert.Null(profile.SaveSchedule(profile.Current.Schedule));
        Assert.Null(profile.SaveOverrides(profile.Current.Overrides));
        Assert.Equal(DisplayPreset.Digital, ProfileJson.Deserialize(File.ReadAllBytes(temp.File)).Display.Preset);
        session.Preset = DisplayPreset.Minimal; session.Cancel();
        Assert.True(profile.Current.ShowLunch);
        Assert.Equal(DisplayPreset.Digital, runtime.Display.Current.Preset);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void EveryAtomicFailurePreservesDiskCommittedAndLastApply(int failureStage)
    {
        using var temp = new TempProfile();
        using (var seed = new JsonProfileStore(temp.Directory)) { seed.Load(); seed.Save(ProfileStorageTests.Sample()); }
        var before = File.ReadAllBytes(temp.File);
        var fail = true;
        using var store = new JsonProfileStore(temp.Directory, stage =>
        { if (fail && (int)stage == failureStage) throw new IOException("Injected"); });
        var profile = new ProfileSession(store);
        var owner = new RuntimeDisplaySettings(profile.Current.Display, profile.SaveDisplay);
        var session = owner.Open(); session.Preset = DisplayPreset.Digital;
        Assert.False(session.TryApply()); Assert.NotEmpty(session.ErrorText);
        Assert.Equal(before, File.ReadAllBytes(temp.File));
        Assert.Equal(DisplayPreset.Standard, owner.Committed.Preset); Assert.Equal(owner.Committed, profile.Current.Display);
        session.Cancel(); Assert.Equal(owner.Committed, owner.Current);
        fail = false; session = owner.Open(); session.Preset = DisplayPreset.Digital;
        Assert.True(session.TryApply());
        session.Elements[0].SizeText = "60"; session.Cancel();
        Assert.Equal(DisplayPresets.Create(DisplayPreset.Digital), owner.Current);
        Assert.Equal(owner.Current, ProfileJson.Deserialize(File.ReadAllBytes(temp.File)).Display);
    }

    [Fact]
    public void OnlyOneEditorOwnsPreviewAndClosedDraftCannotChangeRuntime()
    {
        var owner = DisplayModelTests.Owner(); var a = owner.Open();
        Assert.Throws<InvalidOperationException>(() => owner.Open());
        a.Cancel(); var b = owner.Open(); b.Preset = DisplayPreset.Digital;
        a.Elements[0].SizeText = "90"; a.Preset = DisplayPreset.Minimal; a.Reset(); a.Cancel();
        Assert.Equal(DisplayPreset.Digital, owner.Current.Preset);
    }
}
