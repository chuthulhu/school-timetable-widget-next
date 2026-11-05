using CommunityToolkit.Mvvm.ComponentModel;

namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

/// <summary>One editable row of the clock layout (ADR 0037). Changes write the Draft and preview at once.</summary>
public sealed class ClockSlotDraft : ObservableObject
{
    private readonly DisplaySettingsSession _owner;
    private int _line;
    private ClockPosition _position;
    internal ClockSlotDraft(DisplaySettingsSession owner, ClockSlot slot, bool canMoveUp, bool canMoveDown)
    {
        _owner = owner;
        Element = slot.Element;
        _line = slot.Line;
        _position = slot.Position;
        CanMoveUp = canMoveUp;
        CanMoveDown = canMoveDown;
    }
    public ClockElement Element { get; }
    public string Label => Element switch
    {
        ClockElement.Date => "날짜",
        ClockElement.Weekday => "요일",
        ClockElement.Time => "시간",
        _ => "상태"
    };
    public bool CanMoveUp { get; }
    public bool CanMoveDown { get; }
    public int Line
    {
        get => _line;
        set { if (value is >= 1 and <= ClockLayout.MaxLines && SetProperty(ref _line, value)) _owner.UpdateClockSlot(Element, _line, _position); }
    }
    public ClockPosition Position
    {
        get => _position;
        set { if (Enum.IsDefined(value) && SetProperty(ref _position, value)) _owner.UpdateClockSlot(Element, _line, _position); }
    }
}
