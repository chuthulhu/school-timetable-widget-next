using SchoolTimetableWidget.Desktop.Features.DisplaySettings;

namespace SchoolTimetableWidget.Tests.DisplaySettings;

public class UserPresetTests
{
    [Fact]
    public void SaveAsSelectsStableIdentityAndCopiesEveryInputWithoutMutableDraftSharing()
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        session.Preset = DisplayPreset.Digital;
        foreach (var element in session.Elements)
        {
            element.SizeText = element.Label == "시간" ? "56" : "23";
            element.Family = "Missing Portable Font";
            element.Weight = DisplayFontWeight.Bold; element.Style = DisplayFontStyle.Italic;
        }
        session.Use24Hour = false; session.ShowSeconds = false; session.ShowDate = false;
        session.ShowWeekday = true; session.ShowStatus = false;
        var before = owner.Current;
        Assert.True(session.TrySaveAs(" 교무실 시계 "));
        var preset = Assert.Single(session.Presets.Items);
        Assert.NotEqual(Guid.Empty, preset.Id); Assert.Equal("교무실 시계", preset.Name);
        Assert.Equal(preset.Id, session.Preset.UserId); Assert.Null(session.Preset.BuiltIn);
        Assert.Equal(before with { Preset = session.Preset }, preset.Display);
        session.Elements[0].SizeText = "60";
        Assert.Equal(56, preset.Display.Time.Size);
        session.Reset(); Assert.Equal(preset.Display, owner.Current);
        session.Preset = DisplayPreset.Minimal;
        session.Preset = DisplayPresetReference.User(preset.Id);
        Assert.Equal(preset.Display, owner.Current);
        Assert.True(session.TryRename("큰 시계"));
        Assert.Equal(preset.Id, Assert.Single(session.Presets.Items).Id);
        Assert.Equal(preset.Display, session.Presets.Get(preset.Id).Display);
        session.Elements[0].SizeText = "64";
        Assert.True(session.TryUpdate());
        Assert.Equal(64, session.Presets.Get(preset.Id).Display.Time.Size);
        Assert.Equal(DisplayPresets.Create(DisplayPreset.Standard), owner.Committed);
        session.Elements[0].SizeText = "72"; session.Reset();
        Assert.Equal(64, owner.Current.Time.Size);
    }

    [Theory]
    [InlineData("")] [InlineData("   ")] [InlineData("\t\r\n")] [InlineData("a\nb")]
    public void InvalidNamesLeaveLibraryAndPreviewUntouched(string name)
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open(); var before = owner.Current;
        Assert.False(session.TrySaveAs(name)); Assert.NotEmpty(session.ErrorText);
        Assert.Empty(session.Presets.Items); Assert.Equal(before, owner.Current);
    }
    [Fact]
    public void NameLengthAndNormalizedDuplicateRulesApplyToCreateAndRename()
    {
        var session = DisplayModelTests.Owner().Open();
        Assert.False(session.TrySaveAs(new string('가', 61)));
        Assert.True(session.TrySaveAs(new string('가', 60)));
        Assert.True(session.TrySaveAs("Café"));
        Assert.False(session.TrySaveAs(" CAFÉ "));
        Assert.False(session.TrySaveAs("Cafe\u0301"));
        Assert.True(session.TrySaveAs("B")); var id = session.Preset.UserId;
        Assert.False(session.TryRename(" café "));
        Assert.Equal("B", session.Presets.Get(id!.Value).Name);
        Assert.False(session.TryRename(" "));
        Assert.True(session.TryRename(" b "));
        Assert.Equal(id, session.Preset.UserId); Assert.Equal("b", session.Presets.Get(id.Value).Name);
    }
    [Theory]
    [InlineData(DisplayPreset.Standard)] [InlineData(DisplayPreset.Digital)]
    [InlineData(DisplayPreset.Compact)] [InlineData(DisplayPreset.Minimal)]
    public void BuiltInsCannotBeRenamedUpdatedOrDeleted(DisplayPreset builtIn)
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open(); session.Preset = builtIn;
        var original = DisplayPresets.Create(builtIn);
        Assert.False(session.CanManageSelected);
        Assert.False(session.TryRename("다른 이름")); Assert.False(session.TryUpdate());
        Assert.False(session.TryDelete(Guid.NewGuid())); Assert.Empty(session.Presets.Items);
        session.Elements[0].SizeText = "88";
        Assert.Equal(original, DisplayPresets.Create(builtIn));
        session.Reset(); Assert.Equal(original, owner.Current);
    }
    [Fact]
    public void ActiveDeletionIsBlockedAndInactiveDeletionDoesNotChangeDisplay()
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        Assert.True(session.TrySaveAs("A")); var id = session.Preset.UserId!.Value;
        Assert.False(session.CanDelete(id)); Assert.False(session.TryDelete(id));
        session.Preset = DisplayPreset.Compact; var before = owner.Current;
        Assert.True(session.CanDelete(id)); Assert.True(session.TryDelete(id));
        Assert.Equal(before, owner.Current); Assert.Empty(session.Presets.Items);
        Assert.Throws<ArgumentException>(() => session.Preset = DisplayPresetReference.User(id));
    }
    [Fact]
    public void LibraryCopiesInputAndRejectsDuplicateIdsAndDanglingReference()
    {
        var preset = new UserDisplayPreset(Guid.NewGuid(), "A", DisplayPresets.Create(DisplayPreset.Digital));
        var input = new[] { preset }; var library = new UserDisplayPresetLibrary(input);
        input[0] = new(Guid.NewGuid(), "B", preset.Display);
        Assert.Same(preset, library.Items[0]);
        Assert.Throws<ArgumentException>(() => new UserDisplayPresetLibrary([preset, new(preset.Id, "B", preset.Display)]));
        Assert.Throws<ArgumentException>(() => new UserDisplayPreset(Guid.Empty, "A", preset.Display));
        Assert.Throws<ArgumentException>(() => DisplayPresetReference.BuiltInPreset((DisplayPreset)123));
        Assert.Throws<ArgumentException>(() => UserDisplayPresetLibrary.Empty.ValidateReference(preset.Display));
    }
    [Fact]
    public void InvalidDraftCannotBeCapturedOrUpdatedAndClosedSessionCannotMutateLibrary()
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        session.Elements[0].SizeText = "invalid"; Assert.False(session.TrySaveAs("A"));
        session.Reset(); Assert.True(session.TrySaveAs("A")); var old = session.Presets;
        session.Elements[0].SizeText = "invalid"; Assert.False(session.TryUpdate()); Assert.Same(old, session.Presets);
        session.Cancel();
        Assert.False(session.TrySaveAs("B")); Assert.False(session.TryRename("B"));
        Assert.False(session.TryUpdate()); Assert.False(session.TryDelete(old.Items[0].Id));
        Assert.Empty(session.Presets.Items); Assert.Empty(owner.CommittedPresets.Items);
    }
}
