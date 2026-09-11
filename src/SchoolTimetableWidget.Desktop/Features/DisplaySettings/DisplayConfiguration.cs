namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

public enum DisplayPreset { Standard, Digital, Compact, Minimal }
public enum DisplayLayout { Standard, Digital, Inline }
// Additional source kinds can be introduced with their own resolver and schema support.
public enum FontSourceKind { System }
public enum DisplayFontWeight { Thin, Normal, Medium, Bold }
public enum DisplayFontStyle { Normal, Italic }
public sealed record FontSelection(FontSourceKind Source, string Family);
public sealed record ElementTypography(FontSelection Font, double Size, DisplayFontWeight Weight, DisplayFontStyle Style);

/// <summary>Immutable Desktop display inputs, independent of WPF and storage DTOs.</summary>
public sealed record DisplayConfiguration(DisplayPreset Preset, DisplayLayout Layout,
    ElementTypography Time, ElementTypography Date, ElementTypography Weekday, ElementTypography Status,
    bool Use24Hour, bool ShowSeconds, bool ShowDate, bool ShowWeekday, bool ShowStatus)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Preset) || !Enum.IsDefined(Layout)) throw new ArgumentException("표시 스타일을 확인해 주세요.");
        ValidateElement(Time, 10, 96);
        ValidateElement(Date, 8, 48);
        ValidateElement(Weekday, 8, 48);
        ValidateElement(Status, 8, 48);
    }

    private static void ValidateElement(ElementTypography? element, double min, double max)
    {
        if (element?.Font is not { } font || !Enum.IsDefined(font.Source) ||
            string.IsNullOrWhiteSpace(font.Family) || font.Family.Length > 200 ||
            font.Family.IndexOfAny(['/', '\\', ':', '#']) >= 0 || font.Family.Any(char.IsControl))
            throw new ArgumentException("사용할 글꼴 이름을 확인해 주세요.");
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
