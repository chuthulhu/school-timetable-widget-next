using CommunityToolkit.Mvvm.ComponentModel;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Desktop.Features.Timetable;

public sealed class TimetableCellViewModel : ObservableObject
{
    private bool _isCurrent;
    private TimetableCellValue _value;
    private string _displayText;

    public TimetableCellViewModel(TimetableCellValue value, string slotLabel = "시간표 셀")
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(slotLabel);
        _value = value;
        _displayText = TimetableCellFormatter.Format(value);
        SlotLabel = slotLabel;
    }

    public TimetableCellValue Value => _value;
    public string DisplayText => _displayText;
    public string SlotLabel { get; }
    public bool IsCurrent => _isCurrent;

    internal void SetIsCurrent(bool value) => SetProperty(ref _isCurrent, value, nameof(IsCurrent));

    internal void SetValue(TimetableCellValue value)
    {
        if (_value == value) return;
        var displayText = TimetableCellFormatter.Format(value);
        var displayChanged = _displayText != displayText;
        _value = value;
        _displayText = displayText;
        // Both fields and the projection are already coherent for any observer.
        OnPropertyChanged(nameof(Value));
        if (displayChanged) OnPropertyChanged(nameof(DisplayText));
    }
}
