using CommunityToolkit.Mvvm.ComponentModel;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;

namespace SchoolTimetableWidget.Desktop.Features.CurrentStatus;

/// <summary>Display configuration and independent texts. Preview reformats the last captured facts, never reads a clock.</summary>
public sealed class CurrentStatusHeaderViewModel : ObservableObject
{
    private CurrentStatusHeaderText? _last;
    private string _currentDateText = "";
    private string _currentTimeText = "";
    private string _statusText = "";
    private string _weekdayText = "";
    private string _amPmText = "";
    public DisplayConfiguration Display { get; private set; } = DisplayPresets.Create(DisplayPreset.Standard);
    public string CurrentDateText => _currentDateText;
    public string CurrentTimeText => _currentTimeText;
    public string StatusText => _statusText;
    public string WeekdayText => _weekdayText;
    public string AmPmText => _amPmText;

    public void SetDisplay(DisplayConfiguration value)
    {
        value.Validate();
        if (Display == value) return;
        Display = value;
        if (_last is not null) Apply(_last);
        OnPropertyChanged(nameof(Display));
    }

    public void Apply(CurrentStatusHeaderText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _last = text;
        var time = Display.Use24Hour ? text.CurrentTimeText : text.Time12Text;
        if (!Display.ShowSeconds && time.Length >= 3) time = time[..^3];
        var amPm = Display.Use24Hour ? "" : text.AmPmText;
        var dateChanged = _currentDateText != text.CurrentDateText;
        var timeChanged = _currentTimeText != time;
        var statusChanged = _statusText != text.StatusText;
        var weekdayChanged = _weekdayText != text.WeekdayText;
        var amPmChanged = _amPmText != amPm;
        // Publish the entire same-snapshot projection before any notification.
        _currentDateText = text.CurrentDateText;
        _currentTimeText = time;
        _statusText = text.StatusText;
        _weekdayText = text.WeekdayText;
        _amPmText = amPm;
        if (dateChanged) OnPropertyChanged(nameof(CurrentDateText));
        if (timeChanged) OnPropertyChanged(nameof(CurrentTimeText));
        if (statusChanged) OnPropertyChanged(nameof(StatusText));
        if (weekdayChanged) OnPropertyChanged(nameof(WeekdayText));
        if (amPmChanged) OnPropertyChanged(nameof(AmPmText));
    }
}
