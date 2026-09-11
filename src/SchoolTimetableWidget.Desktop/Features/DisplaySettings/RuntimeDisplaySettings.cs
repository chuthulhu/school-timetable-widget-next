namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

/// <summary>One display editor owns preview; persistence success precedes commitment.</summary>
public sealed class RuntimeDisplaySettings(DisplayConfiguration initial, Func<DisplayConfiguration, string?> save)
{
    private DisplaySettingsSession? _active;
    public DisplayConfiguration Committed { get; private set; } = Validated(initial);
    public DisplayConfiguration Current { get; private set; } = Validated(initial);
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
    internal string? Commit(DisplayConfiguration candidate)
    {
        candidate.Validate();
        var error = save(candidate);
        if (error is not null) return error;
        Committed = candidate;
        Preview(candidate);
        return null;
    }
    private static DisplayConfiguration Validated(DisplayConfiguration value) { value.Validate(); return value; }
}
