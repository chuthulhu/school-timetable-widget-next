using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

/// <summary>P2 Draft, valid live preview and last successful Apply baseline.</summary>
public sealed class DisplaySettingsSession : ObservableObject
{
    private readonly RuntimeDisplaySettings _owner;
    private DisplayConfiguration _baseline;
    private DisplayConfiguration _configuration;
    private string _errorText = "";
    private ElementTypographyDraft[] _elements = [];
    internal DisplaySettingsSession(RuntimeDisplaySettings owner)
    {
        _owner = owner;
        _baseline = _configuration = owner.Committed;
        Load(_baseline);
    }
    public IReadOnlyList<ElementTypographyDraft> Elements => _elements;
    public DisplayPreset Preset { get => _configuration.Preset; set { if (value != Preset && !IsClosed) Load(DisplayPresets.Create(value)); } }
    public bool Use24Hour { get => _configuration.Use24Hour; set => Change(_configuration with { Use24Hour = value }); }
    public bool ShowSeconds { get => _configuration.ShowSeconds; set => Change(_configuration with { ShowSeconds = value }); }
    public bool ShowDate { get => _configuration.ShowDate; set => Change(_configuration with { ShowDate = value }); }
    public bool ShowWeekday { get => _configuration.ShowWeekday; set => Change(_configuration with { ShowWeekday = value }); }
    public bool ShowStatus { get => _configuration.ShowStatus; set => Change(_configuration with { ShowStatus = value }); }
    public string ErrorText { get => _errorText; private set => SetProperty(ref _errorText, value); }
    public bool IsClosed { get; private set; }
    public void Reset() { if (!IsClosed) Load(DisplayPresets.Create(Preset)); }
    public bool TryApply()
    {
        if (IsClosed || !TryCandidate(out var candidate)) return false;
        var error = _owner.Commit(candidate!);
        if (error is not null) { ErrorText = error; return false; }
        _baseline = candidate!;
        ErrorText = "";
        return true;
    }
    public bool TryAccept()
    {
        if (!TryApply()) return false;
        IsClosed = true;
        return true;
    }
    public void Cancel()
    {
        if (IsClosed) return;
        Load(_baseline);
        IsClosed = true;
    }
    private void Load(DisplayConfiguration value)
    {
        foreach (var element in _elements) element.PropertyChanged -= ElementChanged;
        _configuration = value;
        _elements = [new("시간", value.Time), new("날짜", value.Date), new("요일", value.Weekday), new("상태", value.Status)];
        foreach (var element in _elements) element.PropertyChanged += ElementChanged;
        Preview();
        OnPropertyChanged(string.Empty);
    }
    private void Change(DisplayConfiguration value)
    {
        if (IsClosed || value == _configuration) return;
        _configuration = value;
        Preview();
        OnPropertyChanged(string.Empty);
    }
    private void ElementChanged(object? sender, PropertyChangedEventArgs e) { if (!IsClosed) Preview(); }
    private void Preview() { if (TryCandidate(out var candidate)) _owner.Preview(candidate!); }
    private bool TryCandidate(out DisplayConfiguration? candidate)
    {
        candidate = null;
        try
        {
            var value = _configuration with { Time = _elements[0].Create(), Date = _elements[1].Create(),
                Weekday = _elements[2].Create(), Status = _elements[3].Create() };
            value.Validate();
            candidate = value;
            ErrorText = "";
            return true;
        }
        catch (ArgumentException error) { ErrorText = error.Message; return false; }
    }
}
