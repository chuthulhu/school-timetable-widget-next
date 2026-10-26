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
        }
    }
    /// <summary>The last valid color, so the sample never shows an unparsed value.</summary>
    public string Swatch { get => _swatch; private set => SetProperty(ref _swatch, value); }
    public RgbColor Create() => RgbColor.TryParse(Text, out var value) ? value
        : throw new ArgumentException($"{Label} 색상은 #RRGGBB 형식으로 입력해 주세요.");
}
