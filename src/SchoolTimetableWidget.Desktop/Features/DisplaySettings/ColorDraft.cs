using CommunityToolkit.Mvvm.ComponentModel;

namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

/// <summary>Editable #RRGGBB text for one display color; invalid text blocks the candidate.</summary>
public sealed class ColorDraft : ObservableObject
{
    private string _text;
    private string _swatch;
    public ColorDraft(string label, RgbColor value)
    {
        Label = label;
        _text = _swatch = value.ToString();
    }
    public string Label { get; }
    public string Text
    {
        get => _text;
        set
        {
            if (!SetProperty(ref _text, value ?? "")) return;
            if (RgbColor.TryParse(_text, out var color)) Swatch = color.ToString();
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(ErrorText));
        }
    }
    public bool IsValid => RgbColor.TryParse(_text, out _);
    /// <summary>Inline message next to the field (ADR 0034); empty while the text is valid.</summary>
    public string ErrorText => IsValid ? "" : "#RRGGBB 형식으로 입력해 주세요 (예: #1F2937). 미리보기는 마지막 올바른 색을 유지합니다.";
    /// <summary>The last valid color, so the sample never shows an unparsed value.</summary>
    public string Swatch { get => _swatch; private set => SetProperty(ref _swatch, value); }
    public RgbColor Create() => RgbColor.TryParse(Text, out var value) ? value
        : throw new ArgumentException($"{Label} 색상은 #RRGGBB 형식으로 입력해 주세요.");
}
