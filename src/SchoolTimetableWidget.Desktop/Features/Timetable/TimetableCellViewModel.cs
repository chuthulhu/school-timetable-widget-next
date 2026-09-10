using CommunityToolkit.Mvvm.ComponentModel;

namespace SchoolTimetableWidget.Desktop.Features.Timetable;

public sealed class TimetableCellViewModel : ObservableObject
{
    private bool _isCurrent;

    public TimetableCellViewModel(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        Content = content;
    }

    public string Content { get; }
    public bool IsCurrent => _isCurrent;

    // Only the owning weekly presentation chooses a current slot.
    internal void SetIsCurrent(bool value) => SetProperty(ref _isCurrent, value, nameof(IsCurrent));
}
