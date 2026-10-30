namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

/// <summary>A built-in color and brush-opacity bundle. Loading one only fills the Draft; no theme identity is stored.</summary>
public sealed record DisplayTheme(string Id, string Label, DisplayColors Colors,
    double HeaderOpacity, double CellOpacity, double CurrentOpacity, double BorderOpacity);

/// <summary>ADR 0028 built-in themes; values are copied, never looked up at runtime.</summary>
public static class DisplayThemes
{
    public static DisplayTheme Default { get; } = new("default", "기본", DisplayColors.Default,
        TableAppearance.Default.HeaderOpacity, TableAppearance.Default.CellOpacity,
        TableAppearance.Default.CurrentOpacity, TableAppearance.Default.BorderOpacity);

    // The user's own legacy style_settings.json at Golden Reference 84de32a. Legacy had no separate
    // today header or clock text color, so those reuse the header background and the text color.
    // Opacity bytes convert losslessly as n/255.
    public static DisplayTheme Legacy { get; } = new("legacy-custom", "이전 앱 내 설정", new(
        Rgb("#6464C8"), Rgb("#6464C8"), Rgb("#000000"), Rgb("#FFD700"), Rgb("#FFFFFF"),
        Rgb("#FFFFFF"), Rgb("#FFFFFF"), Rgb("#FFFFFF")), 102d / 255, 51d / 255, 130d / 255, 7d / 255);

    // physichu-platform guides/_core/tokens.json (_core@1.4.1-draft). Light: header soft, today accent-soft,
    // cell paper, current b-300, border line, text ink. Dark: header soft, today accent-soft, cell paper,
    // current b-600, border line, text ink. Fully opaque, as the flat palettes are contrast-checked opaque.
    public static DisplayTheme SlateLight { get; } = Flat("slate-light", "슬레이트 밝음",
        "#E0E1E4", "#E0ECEA", "#F9F8F5", "#A9D2CE", "#BABEC5", "#3F4958");
    public static DisplayTheme SlateDark { get; } = Flat("slate-dark", "슬레이트 어두움",
        "#4A5463", "#31504F", "#3F4958", "#2F6766", "#7F8792", "#E0E1E4");
    public static DisplayTheme BlueLight { get; } = Flat("blue-light", "파랑 밝음",
        "#EEF3FB", "#E6EEFE", "#FFFFFF", "#6B9BFF", "#D9E1EE", "#14213A");
    public static DisplayTheme BlueDark { get; } = Flat("blue-dark", "파랑 어두움",
        "#1B2540", "#1B2A4A", "#151E31", "#2563EB", "#26324A", "#F6F8FC");

    public static IReadOnlyList<DisplayTheme> All { get; } =
        [Default, Legacy, SlateLight, SlateDark, BlueLight, BlueDark];

    private static DisplayTheme Flat(string id, string label, string header, string today, string cell,
        string current, string border, string ink) =>
        new(id, label, new(Rgb(header), Rgb(today), Rgb(cell), Rgb(current), Rgb(border), Rgb(ink), Rgb(ink), Rgb(ink)), 1, 1, 1, 1);
    private static RgbColor Rgb(string text) => RgbColor.Parse(text);
}
