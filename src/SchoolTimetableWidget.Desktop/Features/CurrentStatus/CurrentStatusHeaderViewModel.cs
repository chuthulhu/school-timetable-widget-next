using CommunityToolkit.Mvvm.ComponentModel;

namespace SchoolTimetableWidget.Desktop.Features.CurrentStatus;

/// <summary>Separate display texts; calculation and lifecycle belong to the loop.</summary>
public sealed class CurrentStatusHeaderViewModel : ObservableObject
{
    private string _currentDateText = string.Empty;
    private string _currentTimeText = string.Empty;
    private string _statusText = string.Empty;

    public string CurrentDateText => _currentDateText;

    public string CurrentTimeText => _currentTimeText;

    public string StatusText => _statusText;

    /// <summary>Applies calculated text on the owning UI thread; unchanged values do not notify.</summary>
    public void Apply(CurrentStatusHeaderText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var dateChanged = _currentDateText != text.CurrentDateText;
        var timeChanged = _currentTimeText != text.CurrentTimeText;
        var statusChanged = _statusText != text.StatusText;
        // Publish all values before notifying observers, including at midnight.
        _currentDateText = text.CurrentDateText;
        _currentTimeText = text.CurrentTimeText;
        _statusText = text.StatusText;
        if (dateChanged) OnPropertyChanged(nameof(CurrentDateText));
        if (timeChanged) OnPropertyChanged(nameof(CurrentTimeText));
        if (statusChanged) OnPropertyChanged(nameof(StatusText));
    }
}
