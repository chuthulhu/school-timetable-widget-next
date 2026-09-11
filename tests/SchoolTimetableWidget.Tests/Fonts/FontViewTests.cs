using System.Globalization;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Features.Fonts;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Fonts;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using SchoolTimetableWidget.Tests.DisplaySettings;
using SchoolTimetableWidget.Tests.Persistence;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.Fonts;

// WPF object, binding, glyph-run and offscreen render evidence. No foreground/native input.
[Collection("WPF font resource verification")]
public class FontViewTests
{
    private static FontSelection Font(string id) => FontCatalog.Entries.Single(e => e.Id == id).Selection;
    [Theory]
    [InlineData("pretendard")][InlineData("dseg7-modern")][InlineData("dseg7-classic")]
    public void BundledResourceHashAndPrivateGlyphTypefaceLoad(string id) => HighlightTestDispatcher.Run(() =>
    {
        var entry = FontCatalog.Entries.Single(e => e.Id == id);
        var resource = Application.GetResourceStream(new Uri($"/SchoolTimetableWidget.Desktop;component/Assets/Fonts/{id}/{entry.FileName}", UriKind.Relative));
        using (var stream = resource.Stream) Assert.Equal(entry.Sha256, Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant());
        var library = new FontLibrary(); Assert.True(library.IsAvailable(entry.Selection));
        var face = new Typeface(library.Resolve(entry.Selection), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal); Assert.True(face.TryGetGlyphTypeface(out var glyph));
        Assert.Contains(entry.FamilyName, glyph.FamilyNames.Values);
        Assert.All("0123456789:", c => Assert.True(glyph.CharacterToGlyphMap.ContainsKey(c)));
        Assert.True(glyph.FontUri.ToString().Contains("component", StringComparison.OrdinalIgnoreCase));
    });
    [Theory]
    [InlineData("dseg7-modern", "0123456789:23")]
    [InlineData("dseg7-classic", "0123456789:23")]
    [InlineData("pretendard", "2026년 09월 11일 수업")]
    [InlineData("dseg7-modern", "수업 종료")]
    public void WpfDrawsNonzeroGlyphsIncludingKoreanFallback(string id, string text) => HighlightTestDispatcher.Run(() =>
    {
        Assert.True(new FontLibrary().IsAvailable(Font(id)));
        var family = new FontLibrary().Resolve(Font(id));
        var formatted = new FormattedText(text, CultureInfo.GetCultureInfo("ko-KR"), FlowDirection.LeftToRight,
            new Typeface(family, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 32, Brushes.Black, 1);
        var drawing = new DrawingGroup(); using (var context = drawing.Open()) context.DrawText(formatted, new Point());
        var runs = Glyphs(drawing).ToArray(); Assert.NotEmpty(runs);
        Assert.All(runs.SelectMany(g => g.GlyphIndices), index => Assert.NotEqual(0, index));
        Assert.False(formatted.BuildGeometry(new Point()).IsEmpty());
        if (id.StartsWith("dseg") && text.Contains('수'))
            Assert.Contains(runs, r => !r.GlyphTypeface.FamilyNames.Values.Any(n => n.StartsWith("DSEG")));
        var visual = new DrawingVisual(); using (var context = visual.RenderOpen()) context.DrawDrawing(drawing);
        var bitmap = new RenderTargetBitmap(600, 90, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
        var pixels = new byte[600 * 90 * 4]; bitmap.CopyPixels(pixels, 600 * 4, 0); Assert.Contains(pixels, b => b != 0);
    });
    [Fact]
    public void SourcePickerUpdatesOnlyTimeAndOffersExplicitOnlineAction() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); using var cleanup = new SessionScope(owner.Open()); var session = cleanup.Session;
        var dialog = new DisplaySettingsWindow(session);
        try
        {
            Layout((FrameworkElement)dialog.Content);
            var editors = (ItemsControl)dialog.FindName("ElementEditors");
            var sources = Descendants<ComboBox>(editors).Where(c => c.Name == "FontSourceSelector").ToArray();
            Assert.Equal(4, sources.Length); Assert.Equal(3, sources[0].Items.Count);
            sources[0].SelectedValue = FontSourceKind.Bundled; Drain();
            var families = Descendants<ComboBox>(editors).Where(c => c.Name == "FontSelector").ToArray();
            families[0].SelectedItem = "DSEG7 Modern"; Drain();
            Assert.Equal(Font("dseg7-modern"), owner.Current.Time.Font);
            Assert.Equal(FontSourceKind.System, owner.Current.Date.Font.Source);
            sources[0].SelectedValue = FontSourceKind.OnlineDownloaded; Drain();
            Assert.Equal(Font("dseg7-modern"), owner.Current.Time.Font);
            Assert.True(session.Elements[0].CanDownload); Assert.Contains("이전 글꼴", session.Elements[0].FontStatus);
            Assert.Contains(Descendants<Button>(editors), b => b.Name == "DownloadFontButton" && b.IsEnabled);
            Assert.True(session.TryApply()); Assert.Equal(Font("dseg7-modern"), owner.Committed.Time.Font);
        }
        finally { dialog.Close(); }
    });
    [Fact]
    public void SuccessfulDownloadPreviewsCancelRestoresButCacheRemains() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); var transport = new FakeFontTransport(); var cache = new DownloadedFontCache(temp.Directory, transport);
        var fonts = new FontLibrary(cache); var baseline = DisplayPresets.Create(DisplayPreset.Digital);
        var owner = new RuntimeDisplaySettings(baseline, UserDisplayPresetLibrary.Empty, (_, _) => null, fonts);
        var session = owner.Open(); session.Elements[0].Source = FontSourceKind.OnlineDownloaded;
        Assert.Equal(baseline, owner.Current); Pump(session.Elements[0].DownloadAsync());
        Assert.Equal(Font("orbitron"), owner.Current.Time.Font); Assert.Equal(baseline.Date, owner.Current.Date);
        Assert.True(fonts.IsAvailable(Font("orbitron"))); session.Cancel();
        Assert.Equal(baseline, owner.Current); Assert.NotNull(cache.FindValid(Font("orbitron"))); Assert.Equal(1, transport.Calls);
    });
    [Fact]
    public void DownloadFailureKeepsDraftPreviewCommitAndReportsError() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); var transport = new FakeFontTransport { Fail = true };
        var baseline = DisplayPresets.Create(DisplayPreset.Standard);
        var owner = new RuntimeDisplaySettings(baseline, UserDisplayPresetLibrary.Empty, (_, _) => null,
            new FontLibrary(new DownloadedFontCache(temp.Directory, transport)));
        var session = owner.Open(); session.Elements[0].Source = FontSourceKind.OnlineDownloaded;
        Pump(session.Elements[0].DownloadAsync()); Assert.Contains("못했습니다", session.Elements[0].FontStatus);
        Assert.Equal(baseline, owner.Current); Assert.Equal(baseline, owner.Committed);
        transport.Fail = false; Pump(session.Elements[0].DownloadAsync()); Assert.Equal(Font("orbitron"), owner.Current.Time.Font);
        session.Cancel();
    });
    [Fact]
    public void ClosingOrReplacingDraftDuringDownloadCannotChangeNewDraftOrBaseline() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); var transport = new FakeFontTransport { Wait = new() };
        var baseline = DisplayPresets.Create(DisplayPreset.Standard);
        var owner = new RuntimeDisplaySettings(baseline, UserDisplayPresetLibrary.Empty, (_, _) => null,
            new FontLibrary(new DownloadedFontCache(temp.Directory, transport)));
        var session = owner.Open(); session.Elements[0].Source = FontSourceKind.OnlineDownloaded;
        var task = session.Elements[0].DownloadAsync(); session.Reset(); Pump(task); Assert.Equal(baseline, owner.Current);
        session.Elements[0].Source = FontSourceKind.OnlineDownloaded; var next = session.Elements[0].DownloadAsync(); session.Cancel(); Pump(next);
        Assert.Equal(baseline, owner.Current); Assert.Equal(baseline, owner.Committed);
    });
    [Fact]
    public void MixedFontsAndUserPresetPersistRestartOfflineFallbackAndRedownload() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); var transport = new FakeFontTransport(); var cache = new DownloadedFontCache(temp.Directory, transport);
        var fonts = new FontLibrary(cache); DisplayConfiguration expected;
        using (var store = new JsonProfileStore(temp.Directory))
        {
            var runtime = new ProfileRuntime(new ProfileSession(store), () => { }, _ => { }, fonts);
            var session = runtime.Display.Open(); session.Preset = DisplayPreset.Digital;
            session.Elements[0].Source = FontSourceKind.OnlineDownloaded; Pump(session.Elements[0].DownloadAsync());
            session.Elements[1].Source = FontSourceKind.Bundled; session.Elements[1].Family = "Pretendard";
            session.Elements[2].Source = FontSourceKind.Bundled; session.Elements[2].Family = "DSEG7 Classic";
            Assert.True(session.TrySaveAs("혼합 글꼴 시계")); Assert.True(session.TryAccept()); expected = runtime.Display.Committed;
        }
        var original = File.ReadAllBytes(temp.File); var modified = File.GetLastWriteTimeUtc(temp.File);
        transport.Fail = true;
        using var restartStore = new JsonProfileStore(temp.Directory); var profile = new ProfileSession(restartStore);
        Assert.True(profile.LoadResult.CanWrite); Assert.Equal(expected, profile.Current.Display);
        Assert.Equal(expected, profile.Current.DisplayPresets.Items.Single().Display);
        Assert.True(new FontLibrary(cache).IsAvailable(expected.Time.Font)); Assert.Equal(1, transport.Calls);
        File.Delete(cache.PathFor(expected.Time.Font));
        var missingFonts = new FontLibrary(cache); Assert.False(missingFonts.IsAvailable(expected.Time.Font));
        Assert.Equal("Segoe UI", missingFonts.Resolve(expected.Time.Font).Source);
        var owner = new RuntimeDisplaySettings(expected, profile.Current.DisplayPresets, profile.SaveDisplay, missingFonts);
        var edit = owner.Open();
        Assert.Equal(original, File.ReadAllBytes(temp.File)); Assert.Equal(modified, File.GetLastWriteTimeUtc(temp.File));
        Assert.Contains("다시 다운로드", edit.Elements[0].FontStatus); edit.Elements[0].SizeText = "50";
        Assert.True(edit.TryApply()); edit.Reset(); Assert.Equal(expected, owner.Current); edit.Cancel();
        // Apply above legitimately writes; a missing-cache load itself does not rewrite.
        Assert.Equal(expected.Time.Font, profile.Current.Display.Time.Font); Assert.True(profile.LoadResult.CanWrite);
        transport.Fail = false; var recovery = owner.Open(); Pump(recovery.Elements[0].DownloadAsync());
        Assert.True(missingFonts.IsAvailable(expected.Time.Font)); Assert.Equal(expected.Time.Font, owner.Current.Time.Font); recovery.Cancel();
        Assert.DoesNotContain(temp.Directory, System.Text.Encoding.UTF8.GetString(original));
        Assert.DoesNotContain("https://", System.Text.Encoding.UTF8.GetString(original));
        Assert.Equal(2, transport.Calls);
    });
    [Theory]
    [InlineData(FontSourceKind.System)]
    [InlineData(FontSourceKind.Bundled)]
    [InlineData(FontSourceKind.OnlineDownloaded)]
    public void SaveAsSelectionSurvivesLibraryRefreshTransactionsResetAndReopen(FontSourceKind source) => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile();
        var original = ProfileJson.Serialize(ProfileStorageTests.Sample()); File.WriteAllBytes(temp.File, original);
        var transport = new FakeFontTransport(); var fonts = new FontLibrary(new DownloadedFontCache(temp.Directory, transport));
        DisplayConfiguration saved; Guid savedId;
        using (var store = new JsonProfileStore(temp.Directory))
        {
            var profile = new ProfileSession(store);
            var owner = new RuntimeDisplaySettings(profile.Current.Display, profile.Current.DisplayPresets, profile.SaveDisplay, fonts);
            var session = owner.Open(); var dialog = new DisplaySettingsWindow(session);
            try
            {
                Drain(); session.Preset = DisplayPreset.Digital;
                var element = session.Elements[0]; element.Source = source;
                if (source == FontSourceKind.Bundled) element.Family = "DSEG7 Modern";
                if (source == FontSourceKind.OnlineDownloaded) Pump(element.DownloadAsync());
                element.SizeText = "57"; var typography = owner.Current.Time;
                Assert.True(session.TrySaveAs("교무실 시계")); savedId = session.Preset.UserId!.Value;
                AssertPresetSelection(dialog, session.Preset);
                Assert.Equal(typography, owner.Current.Time);
                Assert.Equal(typography, session.Presets.Get(savedId).Display.Time);
                var choices = session.PresetChoices; Assert.Same(choices, session.PresetChoices);
                System.Windows.Data.CollectionViewSource.GetDefaultView(((ComboBox)dialog.FindName("PresetSelector")).ItemsSource).Refresh();
                AssertPresetSelection(dialog, session.Preset);
                Assert.True(session.TryRename("큰 교무실 시계"));
                Assert.NotSame(choices, session.PresetChoices); AssertPresetSelection(dialog, DisplayPresetReference.User(savedId));
                session.Elements[0].SizeText = "61"; session.Reset();
                Assert.Equal(typography, owner.Current.Time); AssertPresetSelection(dialog, DisplayPresetReference.User(savedId));
                Assert.Equal(original, File.ReadAllBytes(temp.File));
                session.Cancel(); AssertPresetSelection(dialog, owner.Committed.Preset);
                Assert.Empty(session.Presets.Items); Assert.Equal(original, File.ReadAllBytes(temp.File));
            }
            finally { dialog.Close(); }
            session = owner.Open(); dialog = new DisplaySettingsWindow(session);
            try
            {
                session.Preset = DisplayPreset.Digital; session.Elements[0].Source = source;
                if (source == FontSourceKind.Bundled) session.Elements[0].Family = "DSEG7 Modern";
                if (source == FontSourceKind.OnlineDownloaded) session.Elements[0].Family = "Orbitron";
                session.Elements[0].SizeText = "57";
                Assert.True(session.TrySaveAs("교무실 시계")); savedId = session.Preset.UserId!.Value;
                AssertPresetSelection(dialog, DisplayPresetReference.User(savedId));
                Assert.Equal(original, File.ReadAllBytes(temp.File));
                Assert.True(session.TryApply()); saved = owner.Committed;
                AssertPresetSelection(dialog, DisplayPresetReference.User(savedId));
                var disk = File.ReadAllBytes(temp.File); var choices = session.PresetChoices;
                session.ShowSeconds = !session.ShowSeconds; session.Elements[0].SizeText = "62";
                AssertPresetSelection(dialog, DisplayPresetReference.User(savedId)); Assert.Same(choices, session.PresetChoices);
                var selector = (ComboBox)dialog.FindName("PresetSelector");
                for (var round = 0; round < 3; round++)
                {
                    foreach (var builtIn in Enum.GetValues<DisplayPreset>())
                    {
                        selector.SelectedValue = DisplayPresetReference.BuiltInPreset(builtIn);
                        AssertPresetSelection(dialog, DisplayPresetReference.BuiltInPreset(builtIn));
                        selector.SelectedValue = DisplayPresetReference.User(savedId);
                        AssertPresetSelection(dialog, DisplayPresetReference.User(savedId));
                        Assert.Equal(saved.Time, owner.Current.Time);
                    }
                }
                session.Cancel(); AssertPresetSelection(dialog, DisplayPresetReference.User(savedId));
                Assert.Equal(saved, owner.Current); Assert.Equal(disk, File.ReadAllBytes(temp.File));
            }
            finally { dialog.Close(); }
        }
        using var restartStore = new JsonProfileStore(temp.Directory); var restart = new ProfileSession(restartStore);
        var reopened = new RuntimeDisplaySettings(restart.Current.Display, restart.Current.DisplayPresets, restart.SaveDisplay, fonts).Open();
        var window = new DisplaySettingsWindow(reopened);
        try
        {
            AssertPresetSelection(window, DisplayPresetReference.User(savedId));
            Assert.Equal(saved, restart.Current.Display); Assert.Equal(saved, reopened.Presets.Get(savedId).Display);
        }
        finally { window.Close(); }
    });
    [Fact]
    public void NewOnlineSelectionMustStillBeAvailableAtApply() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); var cache = new DownloadedFontCache(temp.Directory, new FakeFontTransport());
        var baseline = DisplayPresets.Create(DisplayPreset.Digital); var saves = 0;
        var owner = new RuntimeDisplaySettings(baseline, UserDisplayPresetLibrary.Empty, (_, _) => { saves++; return null; }, new FontLibrary(cache));
        var session = owner.Open(); session.Elements[0].Source = FontSourceKind.OnlineDownloaded;
        Pump(session.Elements[0].DownloadAsync()); File.Delete(cache.PathFor(Font("orbitron")));
        Assert.False(session.TryApply()); Assert.Contains("먼저 다운로드", session.ErrorText);
        Assert.Equal(0, saves); Assert.Equal(baseline, owner.Committed); session.Cancel();
    });
    [Theory]
    [InlineData(false)][InlineData(true)]
    public void MissingOrCorruptCacheKeepsProfileWritableAndUnchanged(bool corrupt) => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); var sample = ProfileStorageTests.Sample();
        var display = sample.Display with { Time = sample.Display.Time with { Font = Font("orbitron") } };
        var bytes = ProfileJson.Serialize(new(sample.Timetable, sample.Schedule, sample.Overrides, sample.ShowLunch, display));
        File.WriteAllBytes(temp.File, bytes); var modified = File.GetLastWriteTimeUtc(temp.File);
        var transport = new FakeFontTransport { Fail = true }; var cache = new DownloadedFontCache(temp.Directory, transport);
        if (corrupt) { var path = cache.PathFor(Font("orbitron")); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, "invalid"); }
        using var store = new JsonProfileStore(temp.Directory); var profile = new ProfileSession(store);
        Assert.Equal(ProfileLoadState.Loaded, profile.LoadResult.State); Assert.True(profile.LoadResult.CanWrite);
        var fonts = new FontLibrary(cache); Assert.False(fonts.IsAvailable(display.Time.Font));
        Assert.Equal("Segoe UI", fonts.Resolve(display.Time.Font).Source);
        Assert.Equal(display, profile.Current.Display); Assert.Equal(bytes, File.ReadAllBytes(temp.File));
        Assert.Equal(modified, File.GetLastWriteTimeUtc(temp.File)); Assert.Equal(0, transport.Calls);
    });
    private static void AssertPresetSelection(DisplaySettingsWindow dialog, DisplayPresetReference expected)
    {
        Drain(); var selector = (ComboBox)dialog.FindName("PresetSelector");
        Assert.NotNull(selector.SelectedItem); Assert.NotNull(selector.SelectedValue);
        Assert.Equal(expected, selector.SelectedValue);
        Assert.Same(selector.Items.Cast<DisplayChoice<DisplayPresetReference>>().Single(p => p.Value == expected), selector.SelectedItem);
        Assert.Equal(expected, dialog.Session.Preset);
    }
    private static IEnumerable<GlyphRun> Glyphs(Drawing drawing)
    {
        if (drawing is GlyphRunDrawing run) yield return run.GlyphRun;
        if (drawing is DrawingGroup group) foreach (var child in group.Children) foreach (var glyph in Glyphs(child)) yield return glyph;
    }
    private static void Pump(Task task)
    {
        var timeout = System.Diagnostics.Stopwatch.StartNew();
        while (!task.IsCompleted)
        {
            Drain(); if (timeout.Elapsed > TimeSpan.FromSeconds(20)) throw new TimeoutException(); Thread.Sleep(1);
        }
        task.GetAwaiter().GetResult(); Drain();
    }
    private static void Drain() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void Layout(FrameworkElement root) { Drain(); root.Measure(new Size(690, 1000)); root.Arrange(new Rect(0, 0, 690, 1000)); root.UpdateLayout(); Drain(); }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i); if (child is T item) yield return item;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private sealed class SessionScope(DisplaySettingsSession session) : IDisposable
    { public DisplaySettingsSession Session => session; public void Dispose() => session.Cancel(); }
}

// WPF packages and font caches are process-wide; resource probes must not race other STA view tests.
[CollectionDefinition("WPF font resource verification", DisableParallelization = true)]
public sealed class FontResourceCollection;
