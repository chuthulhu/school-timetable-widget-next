using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

/// <summary>P2 Draft, valid live preview and last successful Apply baseline.</summary>
public sealed class DisplaySettingsSession : ObservableObject
{
    private readonly RuntimeDisplaySettings _owner;
    private DisplayConfiguration _baseline;
    private DisplayConfiguration _configuration;
    private UserDisplayPresetLibrary _baselinePresets;
    private string _errorText = "";
    private ElementTypographyDraft[] _elements = [];
    internal DisplaySettingsSession(RuntimeDisplaySettings owner)
    {
        _owner = owner;
        _baseline = _configuration = owner.Committed;
        _baselinePresets = Presets = owner.CommittedPresets;
        Load(_baseline);
    }
    public IReadOnlyList<ElementTypographyDraft> Elements => _elements;
    public DisplayPresetReference Preset { get => _configuration.Preset; set { if (value is not null && value != Preset && !IsClosed) Load(Presets.Resolve(value)); } }
    public UserDisplayPresetLibrary Presets { get; private set; }
    public bool CanManageSelected => !IsClosed && Preset.UserId is not null;
    public bool HasUserPresets => !IsClosed && Presets.Items.Count > 0;
    public IReadOnlyList<DisplayChoice<DisplayPresetReference>> PresetChoices =>
        DisplayChoices.Presets.Select(p => new DisplayChoice<DisplayPresetReference>(p.Value, "기본 제공 · " + p.Label))
            .Concat(Presets.Items.Select(p => new DisplayChoice<DisplayPresetReference>(DisplayPresetReference.User(p.Id), "내 프리셋 · " + p.Name))).ToArray();
    public bool Use24Hour { get => _configuration.Use24Hour; set => Change(_configuration with { Use24Hour = value }); }
    public bool ShowSeconds { get => _configuration.ShowSeconds; set => Change(_configuration with { ShowSeconds = value }); }
    public bool ShowDate { get => _configuration.ShowDate; set => Change(_configuration with { ShowDate = value }); }
    public bool ShowWeekday { get => _configuration.ShowWeekday; set => Change(_configuration with { ShowWeekday = value }); }
    public bool ShowStatus { get => _configuration.ShowStatus; set => Change(_configuration with { ShowStatus = value }); }
    public string ErrorText { get => _errorText; private set => SetProperty(ref _errorText, value); }
    public bool IsClosed { get; private set; }
    public void Reset() { if (!IsClosed) Load(Presets.Resolve(Preset)); }
    public bool TryApply()
    {
        if (IsClosed || !TryCandidate(out var candidate)) return false;
        var error = _owner.Commit(candidate!, Presets);
        if (error is not null) { ErrorText = error; return false; }
        _baseline = candidate!;
        _baselinePresets = Presets;
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
        Presets = _baselinePresets;
        Load(_baseline);
        IsClosed = true;
    }
    public bool TrySaveAs(string name) => EditLibrary(() =>
    {
        if (!TryCandidate(out var candidate)) return false;
        var preset = new UserDisplayPreset(Guid.NewGuid(), name, candidate!);
        var library = new UserDisplayPresetLibrary(Presets.Items.Append(preset));
        Presets = library;
        Load(preset.Display);
        return true;
    });
    public bool TryRename(string name) => EditLibrary(() =>
    {
        var selected = SelectedUser();
        Replace(new(selected.Id, name, selected.Display));
        return true;
    });
    public bool TryUpdate() => EditLibrary(() =>
    {
        var selected = SelectedUser();
        if (!TryCandidate(out var candidate)) return false;
        Replace(new(selected.Id, selected.Name, candidate!));
        return true;
    });
    public bool CanDelete(Guid id) => !IsClosed && Preset.UserId != id && Presets.Items.Any(p => p.Id == id);
    public bool TryDelete(Guid id) => EditLibrary(() =>
    {
        if (!CanDelete(id)) throw new ArgumentException("사용 중인 프리셋입니다. 다른 표시 스타일을 선택한 뒤 삭제해 주세요.");
        Presets = new(Presets.Items.Where(p => p.Id != id));
        return true;
    });
    private UserDisplayPreset SelectedUser() => Preset.UserId is { } id ? Presets.Get(id)
        : throw new ArgumentException("기본 제공 스타일은 변경할 수 없습니다.");
    private void Replace(UserDisplayPreset value) =>
        Presets = new(Presets.Items.Select(p => p.Id == value.Id ? value : p));
    private bool EditLibrary(Func<bool> edit)
    {
        if (IsClosed) return false;
        try
        {
            if (!edit()) return false;
            ErrorText = "";
            OnPropertyChanged(nameof(Presets));
            OnPropertyChanged(nameof(PresetChoices));
            OnPropertyChanged(nameof(CanManageSelected));
            OnPropertyChanged(nameof(HasUserPresets));
            return true;
        }
        catch (ArgumentException error) { ErrorText = error.Message; return false; }
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
