using System.Text.Json.Nodes;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.Persistence;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.DisplaySettings;

// ADR 0028 built-in themes. Draft checks use unshown WPF objects only; no native input or rendering.
public class DisplayThemeTests
{
    [Fact]
    public void CatalogHasSixValidThemesWithUniqueIdsAndLabels()
    {
        Assert.Equal(new[] { "default", "legacy-custom", "slate-light", "slate-dark", "blue-light", "blue-dark" },
            DisplayThemes.All.Select(t => t.Id));
        Assert.Equal(DisplayThemes.All.Count, DisplayThemes.All.Select(t => t.Label).Distinct().Count());
        foreach (var theme in DisplayThemes.All)
            (TableAppearance.Default with { HeaderOpacity = theme.HeaderOpacity, CellOpacity = theme.CellOpacity,
                CurrentOpacity = theme.CurrentOpacity, BorderOpacity = theme.BorderOpacity }).Validate();
    }

    [Fact]
    public void DefaultThemeRestoresTheDefaultLook()
    {
        var theme = DisplayThemes.Default;
        Assert.Equal(DisplayColors.Default, theme.Colors);
        Assert.Equal((TableAppearance.Default.HeaderOpacity, TableAppearance.Default.CellOpacity,
            TableAppearance.Default.CurrentOpacity, TableAppearance.Default.BorderOpacity),
            (theme.HeaderOpacity, theme.CellOpacity, theme.CurrentOpacity, theme.BorderOpacity));
    }

    [Fact]
    public void LegacyThemeCarriesTheUsersOwnLegacyValuesExactly()
    {
        var theme = DisplayThemes.Legacy;
        Assert.Equal("#6464C8", theme.Colors.HeaderBackground.ToString());
        Assert.Equal("#6464C8", theme.Colors.TodayHeaderBackground.ToString());
        Assert.Equal("#000000", theme.Colors.CellBackground.ToString());
        Assert.Equal("#FFD700", theme.Colors.CurrentBackground.ToString());
        Assert.Equal("#FFFFFF", theme.Colors.Border.ToString());
        Assert.All(new[] { theme.Colors.HeaderText, theme.Colors.CellText, theme.Colors.ClockText },
            c => Assert.Equal("#FFFFFF", c.ToString()));
        Assert.Equal(102d / 255, theme.HeaderOpacity); Assert.Equal(51d / 255, theme.CellOpacity);
        Assert.Equal(130d / 255, theme.CurrentOpacity); Assert.Equal(7d / 255, theme.BorderOpacity);
    }

    [Theory]
    [InlineData("slate-light")] [InlineData("slate-dark")] [InlineData("blue-light")] [InlineData("blue-dark")]
    public void GuidePaletteThemesAreOpaqueAndKeepTableTextAtLeastFourPointFiveToOne(string id)
    {
        var theme = DisplayThemes.All.Single(t => t.Id == id);
        Assert.Equal((1d, 1d, 1d, 1d), (theme.HeaderOpacity, theme.CellOpacity, theme.CurrentOpacity, theme.BorderOpacity));
        var c = theme.Colors;
        Assert.Equal(c.HeaderText, c.CellText); Assert.Equal(c.HeaderText, c.ClockText);
        foreach (var (text, background) in new[] { (c.HeaderText, c.HeaderBackground), (c.HeaderText, c.TodayHeaderBackground),
            (c.CellText, c.CellBackground), (c.CellText, c.CurrentBackground) })
            Assert.True(Contrast(text, background) >= 4.5, $"{id}: {text} on {background} = {Contrast(text, background):0.00}");
        Assert.NotEqual(c.CellBackground, c.CurrentBackground);
    }

    [Fact]
    public void LoadingAThemeFillsTheDraftPreviewsAndCancelRestores() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        var committed = owner.Committed;
        session.LoadTheme(DisplayThemes.SlateDark);
        Assert.Equal(DisplayThemes.SlateDark.Colors.CellBackground.ToString(), session.Colors[2].Text);
        Assert.Equal(1, session.CellOpacity);
        Assert.Equal(DisplayThemes.SlateDark.Colors, owner.Current.Colors);
        Assert.Equal(1, owner.Current.Table.CellOpacity);
        Assert.Equal(committed, owner.Committed);
        session.Cancel();
        Assert.Equal(committed, owner.Current);
    });

    [Fact]
    public void ThemeValuesStayEditableAndApplyStoresOnlyValues() => HighlightTestDispatcher.Run(() =>
    {
        var owner = DisplayModelTests.Owner(); var session = owner.Open();
        session.LoadTheme(DisplayThemes.Legacy);
        session.Colors[7].Text = "#123456";
        Assert.True(session.TryApply());
        Assert.Equal(DisplayThemes.Legacy.Colors with { ClockText = new(0x12, 0x34, 0x56) }, owner.Committed.Colors);
        Assert.Equal(102d / 255, owner.Committed.Table.HeaderOpacity);
        var original = ProfileStorageTests.Sample();
        var snapshot = new ProfileSnapshot(original.SemesterSets, original.ActiveSemesterId, original.ShowLunch,
            owner.Committed, original.DisplayPresets);
        var settings = JsonNode.Parse(ProfileJson.Serialize(snapshot))!["profile"]!["display"]!["settings"]!.AsObject();
        Assert.DoesNotContain(settings, p => p.Key.Contains("theme", StringComparison.OrdinalIgnoreCase));
        session.Cancel();
    });

    private static double Contrast(RgbColor a, RgbColor b)
    {
        static double Channel(byte value)
        {
            var c = value / 255d;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        static double Luminance(RgbColor c) => 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
        var (high, low) = (Math.Max(Luminance(a), Luminance(b)), Math.Min(Luminance(a), Luminance(b)));
        return (high + 0.05) / (low + 0.05);
    }
}
