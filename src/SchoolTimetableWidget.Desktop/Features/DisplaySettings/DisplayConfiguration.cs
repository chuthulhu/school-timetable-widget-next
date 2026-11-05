namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

public enum DisplayPreset { Standard, Digital, Compact, Minimal }
public enum DisplayLayout { Standard, Digital, Inline }
public enum ClockElement { Date, Weekday, Time, Status }
public enum ClockPosition { Left, Center, Right }

/// <summary>Where one clock-area element sits: line 1–3 and a horizontal position (ADR 0037).</summary>
public sealed record ClockSlot(ClockElement Element, int Line, ClockPosition Position);

/// <summary>
/// Clock-area arrangement (ADR 0037). Every element appears exactly once; list order is the order
/// within the same line and position. Visibility stays in the Show* flags of the configuration.
/// </summary>
public sealed class ClockLayout : IEquatable<ClockLayout>
{
    public const int MaxLines = 3;
    public IReadOnlyList<ClockSlot> Slots { get; }
    public ClockLayout(IEnumerable<ClockSlot> slots)
    {
        ArgumentNullException.ThrowIfNull(slots);
        Slots = slots.ToArray();
    }
    public int LineCount => Slots.Count == 0 ? 0 : Slots.Max(slot => slot.Line);

    /// <summary>The arrangement each built-in style always had, so existing settings keep their look.</summary>
    public static ClockLayout For(DisplayLayout layout) => layout switch
    {
        DisplayLayout.Standard => Line(ClockElement.Date, ClockElement.Weekday, ClockElement.Time, ClockElement.Status),
        DisplayLayout.Inline => Line(ClockElement.Time, ClockElement.Date, ClockElement.Weekday, ClockElement.Status),
        DisplayLayout.Digital => new([new(ClockElement.Time, 1, ClockPosition.Center), new(ClockElement.Date, 2, ClockPosition.Center),
            new(ClockElement.Weekday, 2, ClockPosition.Center), new(ClockElement.Status, 3, ClockPosition.Center)]),
        _ => throw new ArgumentOutOfRangeException(nameof(layout))
    };
    private static ClockLayout Line(params ClockElement[] order) =>
        new(order.Select(element => new ClockSlot(element, 1, ClockPosition.Left)));

    public void Validate()
    {
        if (Slots.Count != 4 || Slots.Any(slot => slot is null) || Slots.Select(slot => slot.Element).Distinct().Count() != 4 ||
            Slots.Any(slot => !Enum.IsDefined(slot.Element) || !Enum.IsDefined(slot.Position) || slot.Line is < 1 or > MaxLines))
            throw new ArgumentException("시계 배치를 확인해 주세요.");
    }

    public bool Equals(ClockLayout? other) => other is not null && Slots.SequenceEqual(other.Slots);
    public override bool Equals(object? obj) => Equals(obj as ClockLayout);
    public override int GetHashCode() => Slots.Aggregate(17, (hash, slot) => HashCode.Combine(hash, slot));
    public static bool operator ==(ClockLayout? left, ClockLayout? right) => left is null ? right is null : left.Equals(right);
    public static bool operator !=(ClockLayout? left, ClockLayout? right) => !(left == right);
}
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
    private readonly ClockLayout? _clock;
    /// <summary>Clock-area arrangement; without an explicit one, the arrangement of <see cref="Layout"/> (ADR 0037).</summary>
    public ClockLayout Clock { get => _clock ?? ClockLayout.For(Layout); init => _clock = value; }

    // Compares the effective clock layout, so "derived from Layout" equals the same layout stored explicitly.
    public bool Equals(DisplayConfiguration? other) => other is not null &&
        Preset == other.Preset && Layout == other.Layout && Time == other.Time && Date == other.Date &&
        Weekday == other.Weekday && Status == other.Status && Use24Hour == other.Use24Hour &&
        ShowSeconds == other.ShowSeconds && ShowDate == other.ShowDate && ShowWeekday == other.ShowWeekday &&
        ShowStatus == other.ShowStatus && Table == other.Table && Colors == other.Colors && Clock == other.Clock;
    public override int GetHashCode() => HashCode.Combine(
        HashCode.Combine(Preset, Layout, Time, Date, Weekday, Status),
        HashCode.Combine(Use24Hour, ShowSeconds, ShowDate, ShowWeekday, ShowStatus), Table, Colors, Clock);

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
        Clock.Validate();
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
        var value = preset switch
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
        return value with { Clock = ClockLayout.For(value.Layout) };
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
