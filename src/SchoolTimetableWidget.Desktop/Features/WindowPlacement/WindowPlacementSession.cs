namespace SchoolTimetableWidget.Desktop.Features.WindowPlacement;

internal interface IWindowStateStore
{
    PreferredWindowBounds? Load();
    bool Save(PreferredWindowBounds bounds);
}

/// <summary>Only completed native gestures or explicit reset change intent. Layout has no write path.</summary>
internal sealed class WindowPlacementSession(IWindowStateStore? store)
{
    public PreferredWindowBounds Preferred { get; private set; } = store?.Load() ?? PreferredWindowBounds.Default;
    public bool IsInteracting { get; private set; }
    private PreferredWindowBounds? _start;
    private bool _move;
    private int _edge;
    private bool _dirty;

    public void Begin(PreferredWindowBounds observed)
    {
        IsInteracting = true;
        _start = observed;
        _move = false;
        _edge = 0;
    }

    public void Moving() { if (IsInteracting) _move = true; }
    public void Sizing(int edge) { if (IsInteracting && edge is >= 1 and <= 8) _edge = edge; }

    public void End(PreferredWindowBounds? observed)
    {
        if (!IsInteracting) return;
        IsInteracting = false;
        if (observed is not { IsValid: true } || _start is null) return;
        var moved = _move && (observed.Left != _start.Left || observed.Top != _start.Top || observed.MonitorHint != _start.MonitorHint);
        var horizontal = _edge is 1 or 2 or 4 or 5 or 7 or 8;
        var vertical = _edge is >= 3 and <= 8;
        var resizedWidth = horizontal && Math.Abs(observed.Width - _start.Width) > 0.5;
        var resizedHeight = vertical && Math.Abs(observed.Height - _start.Height) > 0.5;
        if (!moved && !resizedWidth && !resizedHeight) return;
        var monitorChanged = observed.MonitorHint != _start.MonitorHint;
        Preferred = Preferred with
        {
            Width = resizedWidth ? observed.Width : Preferred.Width,
            Height = resizedHeight ? observed.Height : Preferred.Height,
            Left = moved || monitorChanged || (resizedWidth && _edge is 1 or 4 or 7) ? observed.Left : Preferred.Left,
            Top = moved || monitorChanged || (resizedHeight && _edge is 3 or 4 or 5) ? observed.Top : Preferred.Top,
            MonitorHint = observed.MonitorHint
        };
        _dirty = true;
        Flush();
    }

    public void Reset(string? monitorHint)
    {
        Preferred = PreferredWindowBounds.Default with { MonitorHint = monitorHint };
        _dirty = true;
        Flush();
    }

    public void Flush()
    {
        if (_dirty && store?.Save(Preferred) == true) _dirty = false;
    }
}
