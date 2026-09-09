using CommunityToolkit.Mvvm.ComponentModel;

namespace SchoolTimetableWidget.Desktop.Features.CurrentStatus;

/// <summary>Only the two observable display texts; calculation and lifecycle belong to the loop.</summary>
public sealed class CurrentStatusHeaderViewModel : ObservableObject
{
    private string _currentTimeText = string.Empty;
    private string _statusText = string.Empty;

    public string CurrentTimeText => _currentTimeText;

    public string StatusText => _statusText;

    /// <summary>Applies calculated text on the owning UI thread; unchanged values do not notify.</summary>
    public void Apply(CurrentStatusHeaderText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        SetProperty(ref _currentTimeText, text.CurrentTimeText, nameof(CurrentTimeText));
        SetProperty(ref _statusText, text.StatusText, nameof(StatusText));
    }
}
