using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.Persistence;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.DisplaySettings;

// Unshown WPF controls, bindings and routed events. Not native keyboard/IME/focus evidence.
public class UserPresetViewTests
{
    [Fact]
    public void ImportExportButtonsUseFileBoundaryPreviewAndKeepImportedPresetInactive() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); var exportPath = Path.Combine(temp.Directory, "out.stwpreset");
        var owner = DisplayModelTests.Owner(); var session = owner.Open(); Assert.True(session.TrySaveAs("교무실 시계"));
        var originalId = session.Preset.UserId!.Value; var files = new FakePresetFileDialogs { ExportPath = exportPath };
        var previewCalls = 0;
        var dialog = new DisplaySettingsWindow(session, child =>
        {
            previewCalls++; var preview = Assert.IsType<ImportPresetWindow>(child);
            try
            {
                Assert.Contains("같은 프리셋", ((TextBlock)preview.FindName("CollisionText")).Text);
                Assert.Equal(Visibility.Visible, Button(preview, "UpdateButton").Visibility);
                Assert.Equal(Visibility.Visible, Button(preview, "CopyButton").Visibility);
                Assert.Equal(Visibility.Collapsed, Button(preview, "ImportButton").Visibility);
                ((TextBox)preview.FindName("NameInput")).Text = "교무실 시계 복사";
                Click(preview, "CopyButton");
            }
            finally { preview.Close(); }
        }, files);
        try
        {
            Assert.True(Button(dialog, "ExportPresetButton").IsEnabled);
            Assert.True(Button(dialog, "ImportPresetButton").IsEnabled);
            Click(dialog, "ExportPresetButton"); Assert.True(File.Exists(exportPath));
            Assert.EndsWith(DisplayPresetFile.Extension, files.SuggestedName);
            files.ImportPath = exportPath; Click(dialog, "ImportPresetButton");
            Assert.Equal(1, previewCalls); Assert.Equal(2, session.Presets.Items.Count);
            Assert.Equal(originalId, session.Preset.UserId); Assert.Equal(owner.Current.Preset, session.Preset);
            dialog.Close(); Assert.Empty(owner.CommittedPresets.Items);
        }
        finally { dialog.Close(); }
    });

    [Fact]
    public void ExportIsDisabledForBuiltInAndNameCollisionPreviewSuggestsEditableCopyName() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        var dialog = new DisplaySettingsWindow(session);
        try
        {
            Drain(dialog);
            Assert.False(Button(dialog, "ExportPresetButton").IsEnabled);
            Assert.True(Button(dialog, "ImportPresetButton").IsEnabled);
            Assert.True(session.TrySaveAs("A"));
            var incoming = new UserDisplayPreset(Guid.NewGuid(), "A", DisplayPresets.Create(DisplayPreset.Digital));
            var preview = new ImportPresetWindow(session, session.InspectImport(incoming));
            try
            {
                Assert.Equal("A (복사본)", ((TextBox)preview.FindName("NameInput")).Text);
                Assert.Equal(Visibility.Collapsed, Button(preview, "UpdateButton").Visibility);
                Assert.Equal(Visibility.Visible, Button(preview, "CopyButton").Visibility);
                Assert.Contains("같은 이름", ((TextBlock)preview.FindName("CollisionText")).Text);
            }
            finally { preview.Close(); }
        }
        finally { dialog.Close(); }
    });
    [Fact]
    public void SaveNameDialogValidatesThenSelectorAndManagementReflectUserPreset() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open(); var calls = 0;
        var dialog = new DisplaySettingsWindow(session, child =>
        {
            calls++; Assert.IsType<PresetNameWindow>(child);
            try
            {
                var input = (TextBox)child.FindName("NameInput");
                input.Text = " "; Click(child, "SaveNameButton");
                Assert.NotEmpty(((TextBlock)child.FindName("NameError")).Text);
                Assert.Empty(session.Presets.Items);
                input.Text = "교무실 시계"; Click(child, "SaveNameButton");
            }
            finally { child.Close(); }
        });
        try
        {
            Drain(dialog);
            Assert.False(Button(dialog, "RenamePresetButton").IsEnabled);
            Assert.False(Button(dialog, "UpdatePresetButton").IsEnabled);
            Assert.False(Button(dialog, "DeletePresetButton").IsEnabled);
            Click(dialog, "SaveAsPresetButton"); Assert.Equal(1, calls);
            var selector = (ComboBox)dialog.FindName("PresetSelector"); Assert.Equal(5, selector.Items.Count);
            Assert.Equal(session.Preset, selector.SelectedValue);
            Assert.Contains(selector.Items.Cast<DisplayChoice<DisplayPresetReference>>(), p => p.Label == "내 프리셋 · 교무실 시계");
            Assert.True(Button(dialog, "RenamePresetButton").IsEnabled);
            Assert.True(Button(dialog, "UpdatePresetButton").IsEnabled);
            Assert.True(Button(dialog, "DeletePresetButton").IsEnabled);
            var id = session.Preset.UserId!.Value; session.Elements[0].SizeText = "58";
            Click(dialog, "UpdatePresetButton"); Assert.Equal(58, session.Presets.Get(id).Display.Time.Size);
            selector.SelectedValue = DisplayPresetReference.BuiltInPreset(DisplayPreset.Digital); Drain(dialog);
            Assert.False(Button(dialog, "RenamePresetButton").IsEnabled);
            Assert.False(Button(dialog, "UpdatePresetButton").IsEnabled);
            selector.SelectedValue = DisplayPresetReference.User(id); Drain(dialog);
            Assert.Equal(58, owner.Current.Time.Size);
            Click(dialog, "CancelButton"); Assert.Empty(owner.CommittedPresets.Items);
            Assert.Equal(owner.Committed, owner.Current);
        }
        finally { dialog.Close(); }
    });
    [Fact]
    public void RenameDialogPreservesIdAndCancelAfterApplyRestoresLibraryAndDisk() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var profile = new ProfileSession(store); var runtime = new ProfileRuntime(profile, () => { }, _ => { });
        var session = runtime.Display.Open(); session.TrySaveAs("교무실 시계"); var id = session.Preset.UserId;
        var dialog = new DisplaySettingsWindow(session, child =>
        {
            try
            {
                Assert.Equal("교무실 시계", ((TextBox)child.FindName("NameInput")).Text);
                ((TextBox)child.FindName("NameInput")).Text = "큰 시계"; Click(child, "SaveNameButton");
            }
            finally { child.Close(); }
        });
        try
        {
            Click(dialog, "ApplyButton"); var before = File.ReadAllBytes(temp.File);
            Click(dialog, "RenamePresetButton"); Assert.Equal(id, session.Preset.UserId);
            Assert.Equal("큰 시계", session.Presets.Items[0].Name);
            Assert.Equal(session.Preset, ((ComboBox)dialog.FindName("PresetSelector")).SelectedValue);
            Assert.Equal(before, File.ReadAllBytes(temp.File));
            dialog.Close(); Assert.Equal("교무실 시계", session.Presets.Items[0].Name);
            Assert.Equal(before, File.ReadAllBytes(temp.File));
        }
        finally { dialog.Close(); }
    });
    [Fact]
    public void DeletionPickerRequiresExplicitInactiveSelectionAndConfirmation() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open(); session.TrySaveAs("A");
        var a = session.Presets.Items[0]; session.TrySaveAs("B"); var b = session.Presets.Items[1];
        var dialog = new DisplaySettingsWindow(session, child =>
        {
            try
            {
                Assert.IsType<DeletePresetWindow>(child);
                var list = (ListBox)child.FindName("DeletePresetList"); Assert.Equal(2, list.Items.Count);
                Assert.False(Button(child, "ConfirmDeleteButton").IsEnabled);
                list.SelectedItem = b; Drain(child); Assert.False(Button(child, "ConfirmDeleteButton").IsEnabled);
                Assert.Contains("다른 스타일", ((TextBlock)child.FindName("DeletePrompt")).Text);
                list.SelectedItem = a; Drain(child); Assert.True(Button(child, "ConfirmDeleteButton").IsEnabled);
                Assert.Contains("‘A’", ((TextBlock)child.FindName("DeletePrompt")).Text);
                Assert.Equal(b.Id, session.Preset.UserId); Assert.Equal(2, session.Presets.Items.Count);
                Click(child, "ConfirmDeleteButton"); Assert.Single(session.Presets.Items);
                Assert.Equal(b.Id, session.Preset.UserId);
            }
            finally { child.Close(); }
        });
        try { Click(dialog, "DeletePresetButton"); Assert.Equal(b, Assert.Single(session.Presets.Items)); }
        finally { dialog.Close(); }
    });
    [Fact]
    public void NameAndDeleteDialogXDoNotMutateDraft() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open(); session.TrySaveAs("A");
        var baseline = session.Presets;
        var dialog = new DisplaySettingsWindow(session, child => child.Close());
        try
        {
            Click(dialog, "SaveAsPresetButton"); Click(dialog, "RenamePresetButton"); Click(dialog, "DeletePresetButton");
            Assert.Same(baseline, session.Presets);
        }
        finally { dialog.Close(); }
    });
    private static Button Button(Window window, string name) => (Button)window.FindName(name);
    private static void Click(Window window, string name)
    { Button(window, name).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Drain(window); }
    private static void Drain(Window window) => window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
}

internal sealed class FakePresetFileDialogs : IPresetFileDialogs
{
    public string? ExportPath { get; set; }
    public string? ImportPath { get; set; }
    public string SuggestedName { get; private set; } = "";
    public string? ChooseExportPath(string suggestedFileName) { SuggestedName = suggestedFileName; return ExportPath; }
    public string? ChooseImportPath() => ImportPath;
}
