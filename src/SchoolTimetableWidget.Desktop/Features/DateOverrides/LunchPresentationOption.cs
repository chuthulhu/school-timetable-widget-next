using CommunityToolkit.Mvvm.ComponentModel;
namespace SchoolTimetableWidget.Desktop.Features.DateOverrides;

/// <summary>Run-local, default-OFF preference. No Settings/persistence contract.</summary>
public sealed class LunchPresentationOption(Action refresh) : ObservableObject
{
    private bool _enabled;
    public bool Enabled
    {
        get => _enabled;
        set { if (SetProperty(ref _enabled, value)) refresh(); }
    }
}
