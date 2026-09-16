using CommunityToolkit.Mvvm.ComponentModel;
namespace SchoolTimetableWidget.Desktop.Features.DateOverrides;

/// <summary>Default-OFF presentation preference; optional durable callback precedes publication.</summary>
public sealed class LunchPresentationOption(Action refresh, bool initial = false, Func<bool, string?>? persist = null) : ObservableObject
{
    private bool _enabled = initial;
    private string _errorText = "";
    public string ErrorText { get => _errorText; private set => SetProperty(ref _errorText, value); }
    internal void RestoreValue(bool value) => _enabled = value;
    internal void NotifyRestored() => OnPropertyChanged(nameof(Enabled));
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            ErrorText = persist?.Invoke(value) ?? "";
            if (ErrorText.Length != 0) { OnPropertyChanged(nameof(Enabled)); return; }
            if (SetProperty(ref _enabled, value)) refresh();
        }
    }
}
