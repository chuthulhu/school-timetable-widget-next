using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

public sealed class ElementTypographyDraft : ObservableObject
{
    private string _family;
    private string _sizeText;
    private DisplayFontWeight _weight;
    private DisplayFontStyle _style;
    private readonly FontSourceKind _source;
    public ElementTypographyDraft(string label, ElementTypography value)
    {
        Label = label;
        _source = value.Font.Source;
        _family = value.Font.Family;
        _sizeText = value.Size.ToString(CultureInfo.CurrentCulture);
        _weight = value.Weight;
        _style = value.Style;
    }
    public string Label { get; }
    public string Family { get => _family; set => SetProperty(ref _family, value); }
    public string SizeText { get => _sizeText; set => SetProperty(ref _sizeText, value); }
    public DisplayFontWeight Weight { get => _weight; set => SetProperty(ref _weight, value); }
    public DisplayFontStyle Style { get => _style; set => SetProperty(ref _style, value); }
    public ElementTypography Create()
    {
        if (!double.TryParse(SizeText, NumberStyles.Float, CultureInfo.CurrentCulture, out var size))
            throw new ArgumentException($"{Label} 글자 크기를 숫자로 입력해 주세요.");
        return new(new(_source, Family), size, Weight, Style);
    }
}
