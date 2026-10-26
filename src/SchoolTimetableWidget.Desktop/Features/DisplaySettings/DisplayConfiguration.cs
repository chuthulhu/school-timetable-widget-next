namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

public enum DisplayPreset { Standard, Digital, Compact, Minimal }
public enum DisplayLayout { Standard, Digital, Inline }
public enum FontSourceKind { System, Bundled, OnlineDownloaded }
public enum DisplayFontWeight { Thin, Normal, Medium, Bold }
public enum DisplayFontStyle { Normal, Italic }
public sealed record FontSelection(FontSourceKind Source, string Family, string FamilyId)
{
    // Installed family names are Windows' portable logical identity, never a file path.
    public FontSelection(FontSourceKind source, string family) : this(source, family, family) { }
}
public sealed record ElementTypography(FontSelection Font, double Size, DisplayFontWeight Weight, DisplayFontStyle Style);

/// <summary>Immutable Desktop display inputs, independent of WPF and storage DTOs.</summary>
public sealed record DisplayConfiguration(DisplayPresetReference Preset, DisplayLayout Layout,
    ElementTypography Time, ElementTypography Date, ElementTypography Weekday, ElementTypography Status,
    bool Use24Hour, bool ShowSeconds, bool ShowDate, bool ShowWeekday, bool ShowStatus)
{
    public TableAppearance Table { get; init; } = TableAppearance.Default;
    public DisplayColors Colors { get; init; } = DisplayColors.Default;

    public void Validate()
    {
        if (Preset is null || !Enum.IsDefined(Layout)) throw new ArgumentException("표시 스타일을 확인해 주세요.");
        ValidateElement(Time, 10, 96);
        ValidateElement(Date, 8, 48);
        ValidateElement(Weekday, 8, 48);
        ValidateElement(Status, 8, 48);
        if (Table is null) throw new ArgumentException("표 표시 설정을 확인해 주세요.");
        Table.Validate();
        if (Colors is null) throw new ArgumentException("색상 설정을 확인해 주세요.");
    }

    internal static void ValidateElement(ElementTypography? element, double min, double max)
    {
        if (element?.Font is not { } font || !Enum.IsDefined(font.Source) ||
            string.IsNullOrWhiteSpace(font.Family) || font.Family.Length > 200 ||
            font.Family.IndexOfAny(['/', '\\', ':', '#']) >= 0 || font.Family.Any(char.IsControl))
            throw new ArgumentException("사용할 글꼴 이름을 확인해 주세요.");
        if (font.Source == FontSourceKind.System ? font.FamilyId != font.Family :
            !Fonts.FontCatalog.Contains(font))
            throw new ArgumentException("등록된 글꼴을 선택해 주세요.");
        if (!double.IsFinite(element.Size) || element.Size < min || element.Size > max)
            throw new ArgumentException($"글자 크기는 {min}~{max} 사이로 입력해 주세요.");
        if (!Enum.IsDefined(element.Weight) || !Enum.IsDefined(element.Style))
            throw new ArgumentException("글자 굵기와 기울임을 확인해 주세요.");
    }
}

public static class DisplayPresets
{
    public static DisplayConfiguration Create(DisplayPreset preset)
    {
        var standard = new DisplayConfiguration(preset, DisplayLayout.Standard,
            Type(16), Type(16), Type(14), Type(16), true, true, true, false, true);
        return preset switch
        {
            DisplayPreset.Standard => standard,
            DisplayPreset.Digital => standard with { Layout = DisplayLayout.Digital,
                Time = Type(48, DisplayFontWeight.Medium), Date = Type(14), Status = Type(15) },
            DisplayPreset.Compact => standard with { Layout = DisplayLayout.Inline,
                Time = Type(16, DisplayFontWeight.Medium), Status = Type(14), ShowSeconds = false, ShowDate = false },
            DisplayPreset.Minimal => standard with { Layout = DisplayLayout.Inline,
                Time = Type(24), ShowSeconds = false, ShowDate = false, ShowStatus = false },
            _ => throw new ArgumentOutOfRangeException(nameof(preset))
        };
    }
    private static ElementTypography Type(double size, DisplayFontWeight weight = DisplayFontWeight.Normal) =>
        new(new(FontSourceKind.System, "Segoe UI"), size, weight, DisplayFontStyle.Normal);
}

/// <summary>Brush opacity is a finite 0–1 value; typography always remains opaque.</summary>
public sealed record TableAppearance(ElementTypography Header, ElementTypography Cell,
    double HeaderOpacity, double CellOpacity, double CurrentOpacity, double BorderOpacity)
{
    public static TableAppearance Default { get; } = new(
        new(new(FontSourceKind.System, "Segoe UI"), 15, DisplayFontWeight.Medium, DisplayFontStyle.Normal),
        new(new(FontSourceKind.System, "Segoe UI"), 16, DisplayFontWeight.Normal, DisplayFontStyle.Normal), 1, 0, 1, 1);
    public void Validate()
    {
        DisplayConfiguration.ValidateElement(Header, 8, 48);
        DisplayConfiguration.ValidateElement(Cell, 8, 48);
        foreach (var opacity in new[] { HeaderOpacity, CellOpacity, CurrentOpacity, BorderOpacity })
            if (!double.IsFinite(opacity) || opacity is < 0 or > 1)
                throw new ArgumentException("표 투명도는 0~100% 사이로 선택해 주세요.");
    }
}

/// <summary>Opaque 24-bit RGB. Transparency stays in <see cref="TableAppearance"/> brush opacity.</summary>
public readonly record struct RgbColor(byte R, byte G, byte B)
{
    public static RgbColor Parse(string? text) => TryParse(text, out var value) ? value
        : throw new ArgumentException("색상은 #RRGGBB 형식으로 입력해 주세요.");
    public static bool TryParse(string? text, out RgbColor value)
    {
        value = default;
        if (text is not { Length: 7 } || text[0] != '#' || text.AsSpan(1).ContainsAnyExcept("0123456789ABCDEFabcdef".AsSpan())) return false;
        var rgb = Convert.ToInt32(text[1..], 16);
        value = new((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        return true;
    }
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
}

/// <summary>Table brushes/text and the clock area text color; the default reproduces the prior fixed look.</summary>
public sealed record DisplayColors(RgbColor HeaderBackground, RgbColor TodayHeaderBackground, RgbColor CellBackground,
    RgbColor CurrentBackground, RgbColor Border, RgbColor HeaderText, RgbColor CellText, RgbColor ClockText)
{
    public static DisplayColors Default { get; } = new(new(0xF0, 0xF0, 0xF0), new(0xE5, 0xEF, 0xF6), new(0xFF, 0xFF, 0xFF),
        new(0xFF, 0xF3, 0xCD), new(0xE3, 0xE3, 0xE3), new(0x00, 0x00, 0x00), new(0x00, 0x00, 0x00), new(0x00, 0x00, 0x00));
}
