namespace SchoolTimetableWidget.Desktop.Features.TrayLifecycle;

internal interface IWidgetWindow : IDisposable
{
    bool IsVisible { get; }
    event Action? VisibilityChanged;
    event Func<bool>? CloseRequested;
    bool TryActivateDialog();
    void ShowAndActivate();
    void Hide();
}

internal interface ITrayIcon : IDisposable
{
    event Action? ToggleRequested;
    event Action? ExitRequested;
    void SetWindowVisible(bool visible);
    void RequestCloseNotice();
    void RequestEditorNotice();
}

internal interface ITrayNoticeStore
{
    bool WasRequested();
    bool SaveRequested();
}

/// <summary>Close, visibility and exit policy; neither profile data nor timers belong here.</summary>
internal sealed class WidgetTrayLifecycle : IDisposable
{
    private readonly IWidgetWindow _window;
    private readonly ITrayIcon _tray;
    private readonly ITrayNoticeStore? _noticeStore;
    private readonly Action _shutdown;
    private bool _noticeRequested;
    private bool _disposed;
    public bool AllowClose { get; private set; }

    public WidgetTrayLifecycle(IWidgetWindow window, ITrayIcon tray, ITrayNoticeStore? noticeStore, Action shutdown)
    {
        _window = window; _tray = tray; _noticeStore = noticeStore; _shutdown = shutdown;
        _noticeRequested = noticeStore?.WasRequested() ?? false;
        window.CloseRequested += RequestClose;
        window.VisibilityChanged += UpdateTray;
        tray.ToggleRequested += Toggle;
        tray.ExitRequested += Exit;
        UpdateTray();
    }

    public bool RequestClose()
    {
        if (AllowClose || _disposed) return false;
        if (Hide() && !_noticeRequested)
        {
            _noticeRequested = true; // A request is enough; shell display is not guaranteed.
            _tray.RequestCloseNotice();
            _noticeStore?.SaveRequested(); // Failure must not repeat the notice in this run.
        }
        return true;
    }

    public void Show()
    {
        if (_disposed || AllowClose || _window.TryActivateDialog()) return;
        _window.ShowAndActivate();
        UpdateTray();
    }

    public bool Hide()
    {
        if (_disposed || AllowClose || _window.TryActivateDialog()) return false;
        _window.Hide();
        UpdateTray();
        return true;
    }

    public void Toggle()
    {
        if (_window.IsVisible) Hide(); else Show();
    }

    public void Exit()
    {
        if (_disposed || AllowClose) return;
        if (_window.TryActivateDialog())
        {
            _tray.RequestEditorNotice();
            return;
        }
        AllowClose = true;
        _shutdown();
    }

    public void SessionEnding() => AllowClose = true;

    private void UpdateTray()
    {
        if (!_disposed) _tray.SetWindowVisible(_window.IsVisible);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _window.CloseRequested -= RequestClose;
        _window.VisibilityChanged -= UpdateTray;
        _tray.ToggleRequested -= Toggle;
        _tray.ExitRequested -= Exit;
        try { _tray.Dispose(); }
        finally { _window.Dispose(); }
    }
}
