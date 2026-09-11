namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

/// <summary>One display editor owns preview; one save publishes display and library together.</summary>
public sealed class RuntimeDisplaySettings
{
    private readonly Func<DisplayConfiguration, UserDisplayPresetLibrary, string?> _save;
    private DisplaySettingsSession? _active;
    public RuntimeDisplaySettings(DisplayConfiguration initial, UserDisplayPresetLibrary presets,
        Func<DisplayConfiguration, UserDisplayPresetLibrary, string?> save)
    {
        presets.ValidateReference(initial);
        Committed = Current = initial;
        CommittedPresets = presets;
        _save = save;
    }
    public DisplayConfiguration Committed { get; private set; }
    public DisplayConfiguration Current { get; private set; }
    public UserDisplayPresetLibrary CommittedPresets { get; private set; }
    public event EventHandler? Changed;
    public DisplaySettingsSession Open()
    {
        if (_active is { IsClosed: false }) throw new InvalidOperationException("표시 설정이 이미 열려 있습니다.");
        return _active = new(this);
    }
    internal void Preview(DisplayConfiguration value)
    {
        value.Validate();
        if (Current == value) return;
        Current = value;
        Changed?.Invoke(this, EventArgs.Empty);
    }
    internal string? Commit(DisplayConfiguration candidate, UserDisplayPresetLibrary presets)
    {
        presets.ValidateReference(candidate);
        var error = _save(candidate, presets);
        if (error is not null) return error;
        Committed = candidate;
        CommittedPresets = presets;
        Preview(candidate);
        return null;
    }
}
