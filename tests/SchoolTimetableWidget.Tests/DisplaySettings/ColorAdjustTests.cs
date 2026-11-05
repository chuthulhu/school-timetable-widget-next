using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.DisplaySettings;

// ADR 0035: in-app color adjustment window. Pure math, the model, and unshown WPF objects only;
// the popup is never opened, so no native window or input is involved.
public class ColorAdjustTests
{
    private const int CellText = 6, Border = 4, ClockText = 7, CellBackground = 2;

    [Theory]
    [InlineData("#000000")] [InlineData("#FFFFFF")] [InlineData("#0F6E66")] [InlineData("#FFF3CD")] [InlineData("#1D4ED8")] [InlineData("#A0225A")]
    public void HsvRoundTripsEveryRgbExactly(string hex)
    {
        var color = RgbColor.Parse(hex);
        Assert.Equal(color, ColorMath.ToRgb(ColorMath.ToHsv(color)));
    }

    [Fact]
    public void HsvKnownValuesAndGrayKeepsTheFallbackHue()
    {
        Assert.Equal(RgbColor.Parse("#FF0000"), ColorMath.ToRgb(new(0, 1, 1)));
        Assert.Equal(RgbColor.Parse("#00FF00"), ColorMath.ToRgb(new(120, 1, 1)));
        Assert.Equal(RgbColor.Parse("#0000FF"), ColorMath.ToRgb(new(240, 1, 1)));
        Assert.Equal(RgbColor.Parse("#FF0000"), ColorMath.ToRgb(new(360, 1, 1)));
        var gray = ColorMath.ToHsv(RgbColor.Parse("#808080"), fallbackHue: 210);
        Assert.Equal(210, gray.Hue); Assert.Equal(0, gray.Saturation);
    }

    [Fact]
    public void ContrastRatioMatchesWcagReferencePoints()
    {
        Assert.Equal(21, ColorMath.ContrastRatio(RgbColor.Parse("#000000"), RgbColor.Parse("#FFFFFF")), 2);
        Assert.Equal(1, ColorMath.ContrastRatio(RgbColor.Parse("#0F6E66"), RgbColor.Parse("#0F6E66")), 6);
        Assert.Equal(4.54, ColorMath.ContrastRatio(RgbColor.Parse("#767676"), RgbColor.Parse("#FFFFFF")), 2);
    }

    [Fact]
    public void AdjustingWritesTheDraftAndPreviewsWithoutSaving() => HighlightTestDispatcher.Run(() =>
    {
        var (owner, session) = Open();
        var model = new ColorAdjustModel(session, CellText, new RecentColors());
        Assert.Equal("셀 글자 색 조정", model.Title);
        Assert.Equal(RgbColor.Parse("#000000"), model.Before);
        model.SetHue(240); model.SetSaturationValue(1, 0.5);
        Assert.Equal("#000080", session.Colors[CellText].Text);
        Assert.Equal(RgbColor.Parse("#000080"), owner.Current.Colors.CellText); // previewed
        model.Pick(RgbColor.Parse("#0F6E66"));
        Assert.Equal("#0F6E66", session.Colors[CellText].Text);
        Assert.Equal("#0F6E66", model.HexText);
        Assert.False(session.IsClosed);
    });

    [Fact]
    public void CancelRestoresTheOpeningTextAndConfirmRemembersTheColor() => HighlightTestDispatcher.Run(() =>
    {
        var (_, session) = Open();
        var recent = new RecentColors();
        session.Colors[CellText].Text = "#12345"; // invalid text in the box; the swatch still shows #000000
        var cancelled = new ColorAdjustModel(session, CellText, recent);
        Assert.Equal(RgbColor.Parse("#000000"), cancelled.Before);
        cancelled.Pick(RgbColor.Parse("#FF0000"));
        cancelled.Cancel();
        Assert.Equal("#12345", session.Colors[CellText].Text);
        Assert.Empty(recent.Items);
        cancelled.Pick(RgbColor.Parse("#00FF00")); // finished: no further Draft writes
        Assert.Equal("#12345", session.Colors[CellText].Text);

        var confirmed = new ColorAdjustModel(session, CellText, recent);
        confirmed.Pick(RgbColor.Parse("#1D4ED8"));
        confirmed.Confirm(); confirmed.Cancel(); // the second call does nothing
        Assert.Equal("#1D4ED8", session.Colors[CellText].Text);
        Assert.Equal(new[] { RgbColor.Parse("#1D4ED8") }, recent.Items);
    });

    [Fact]
    public void RecentColorsKeepEightNewestFirstWithoutDuplicates()
    {
        var recent = new RecentColors();
        for (var i = 0; i < 10; i++) recent.Add(new((byte)i, 0, 0));
        recent.Add(new(5, 0, 0));
        Assert.Equal(RecentColors.Capacity, recent.Items.Count);
        Assert.Equal(new RgbColor(5, 0, 0), recent.Items[0]);
        Assert.Equal(new RgbColor(9, 0, 0), recent.Items[1]);
        Assert.Single(recent.Items, c => c == new RgbColor(5, 0, 0));
    }

    [Fact]
    public void InvalidHexReportsWithoutTouchingTheDraft() => HighlightTestDispatcher.Run(() =>
    {
        var (_, session) = Open();
        var model = new ColorAdjustModel(session, CellText, new RecentColors());
        model.HexText = "#00ZZ00";
        Assert.NotEmpty(model.HexError);
        Assert.Equal("#000000", session.Colors[CellText].Text);
        model.HexText = "#abcdef";
        Assert.Empty(model.HexError);
        Assert.Equal("#ABCDEF", session.Colors[CellText].Text);
    });

    [Fact]
    public void ContrastWarnsButNeverBlocksAndNotesTransparentBackgrounds() => HighlightTestDispatcher.Run(() =>
    {
        var (_, session) = Open();
        var model = new ColorAdjustModel(session, CellText, new RecentColors());
        var checks = model.Contrast;
        Assert.Equal(2, checks.Count);
        Assert.All(checks, c => Assert.Equal(ContrastLevel.Good, c.Level));
        Assert.StartsWith("일반 셀 배경 대비 21.0 : 1", checks[0].Title);
        Assert.Contains("0%", checks[0].OpacityNote); // the default cell background is fully transparent
        Assert.Empty(checks[1].OpacityNote);         // the current-period background is opaque

        model.Pick(RgbColor.Parse("#F0F0F0"));
        Assert.Equal(ContrastLevel.Poor, model.Contrast[0].Level);
        Assert.True(session.TryApply()); // a warning, not a block

        var background = new ColorAdjustModel(session, CellBackground, new RecentColors());
        Assert.Single(background.Contrast);
        Assert.Empty(background.ContrastNote);
        Assert.Empty(new ColorAdjustModel(session, Border, new RecentColors()).Contrast);
        Assert.NotEmpty(new ColorAdjustModel(session, Border, new RecentColors()).ContrastNote);
        Assert.Contains("시계 글자", new ColorAdjustModel(session, ClockText, new RecentColors()).ContrastNote);
    });

    [Fact]
    public void PanelPointerKeysChoicesAndEscDriveTheModel() => HighlightTestDispatcher.Run(() =>
    {
        var (_, session) = Open();
        var panel = new ColorAdjustPanel();
        var model = new ColorAdjustModel(session, CellText, new RecentColors());
        panel.DataContext = model;
        Layout(panel);
        panel.HueAt(new Point(panel.HueBar.Width / 3, 8));
        panel.PadAt(new Point(panel.SaturationValuePad.Width, 0)); // top right: full saturation and brightness
        Assert.Equal(1, model.Saturation); Assert.Equal(1, model.Value);
        Assert.Equal("#00FF00", session.Colors[CellText].Text); // a third of the bar is green

        Press(panel.SaturationValuePad, Key.Down);
        Assert.Equal(0.98, model.Value, 3);
        var hue = model.Hue; Press(panel.HueBar, Key.Right);
        Assert.Equal(hue + 3, model.Hue, 3);

        var chip = Descendants<Button>((ItemsControl)panel.FindName("RecommendedColors")).First();
        chip.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.Equal("#000000", session.Colors[CellText].Text);

        var finished = 0; panel.Finished += (_, _) => finished++;
        model.Pick(RgbColor.Parse("#A0225A"));
        panel.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, new FakeSource(), 0, System.Windows.Input.Key.Escape)
            { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        Assert.Equal(1, finished);
        Assert.True(model.IsFinished);
        Assert.Equal("#000000", session.Colors[CellText].Text); // Esc restores the opening color
    });

    [Fact]
    public void SettingsWindowSwatchesOpenOneAdjusterAtATime() => HighlightTestDispatcher.Run(() =>
    {
        var (_, session) = Open();
        var recent = new RecentColors();
        var dialog = new DisplaySettingsWindow(session, recentColors: recent);
        try
        {
            dialog.ShowSection(DisplaySettingsSection.Colors);
            Layout((FrameworkElement)dialog.Content);
            var swatches = Descendants<Button>((DependencyObject)dialog.FindName("ColorPage"))
                .Where(b => b.Tag is string).ToArray();
            Assert.Equal(Enumerable.Range(0, 8).Select(i => i.ToString()), swatches.Select(b => (string)b.Tag));

            var first = dialog.BeginColorAdjust(CellText);
            var panel = (ColorAdjustPanel)dialog.FindName("ColorAdjust");
            Assert.Same(first, panel.Model);
            first.Pick(RgbColor.Parse("#1D4ED8"));
            var second = dialog.BeginColorAdjust(CellBackground); // the first is kept, as by an outside click
            Assert.True(first.IsFinished);
            Assert.Equal("#1D4ED8", session.Colors[CellText].Text);
            Assert.Equal(new[] { RgbColor.Parse("#1D4ED8") }, recent.Items);
            Assert.Same(second, panel.Model);
            Assert.False(((Popup)dialog.FindName("ColorPopup")).IsOpen);
        }
        finally { dialog.Close(); }
    });

    private static (RuntimeDisplaySettings Owner, DisplaySettingsSession Session) Open()
    {
        var owner = DisplayModelTests.Owner();
        return (owner, owner.Open());
    }
    private static void Press(UIElement target, Key key) =>
        target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, new FakeSource(), 0, key) { RoutedEvent = Keyboard.KeyDownEvent });
    private static void Drain(DispatcherObject target) => target.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void Layout(FrameworkElement root)
    { Drain(root); root.Measure(new Size(760, 1400)); root.Arrange(new Rect(0, 0, 760, 1400)); root.UpdateLayout(); Drain(root); }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private sealed class FakeSource : PresentationSource
    {
        protected override CompositionTarget GetCompositionTargetCore() => null!;
        public override Visual RootVisual { get; set; } = null!;
        public override bool IsDisposed => false;
    }
}
