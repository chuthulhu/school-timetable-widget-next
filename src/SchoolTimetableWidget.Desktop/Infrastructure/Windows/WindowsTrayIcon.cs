using SchoolTimetableWidget.Desktop.Features.TrayLifecycle;
using Forms = System.Windows.Forms;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

/// <summary>Uses the WPF UI thread's Windows message pump; no extra UI thread or package.</summary>
internal sealed class WindowsTrayIcon : ITrayIcon
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ContextMenuStrip _menu;
    private readonly Forms.ToolStripMenuItem _toggle;
    private readonly Forms.ToolStripMenuItem _exit;
    private readonly System.Drawing.Icon _image;
    private bool _disposed;
    public event Action? ToggleRequested;
    public event Action? ExitRequested;

    internal Forms.NotifyIcon Icon => _icon;
    internal Forms.ContextMenuStrip Menu => _menu;

    // Tests construct actual Forms objects without publishing a shell icon.
    public WindowsTrayIcon(bool publish = true)
    {
        _image = (System.Drawing.Icon)System.Drawing.SystemIcons.Application.Clone();
        _toggle = new("위젯 숨기기");
        _exit = new("종료");
        _menu = new();
        _menu.Items.AddRange([_toggle, new Forms.ToolStripSeparator(), _exit]);
        _icon = new() { Text = "School Timetable Widget", Icon = _image, ContextMenuStrip = _menu };
        _toggle.Click += Toggle;
        _exit.Click += Exit;
        _icon.MouseDoubleClick += DoubleClick;
        _icon.Visible = publish;
    }

    public void SetWindowVisible(bool visible) => _toggle.Text = visible ? "위젯 숨기기" : "위젯 보이기";
    private void Toggle(object? sender, EventArgs e) => ToggleRequested?.Invoke();
    private void Exit(object? sender, EventArgs e) => ExitRequested?.Invoke();
    private void DoubleClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left) ToggleRequested?.Invoke();
    }
    public void RequestCloseNotice() => Notice("창을 닫아도 앱은 알림 영역에서 계속 실행됩니다.\n완전히 종료하려면 트레이 아이콘을 우클릭해 '종료'를 선택하세요.");
    public void RequestEditorNotice() => Notice("열린 편집 창을 먼저 닫아 주세요. 저장 또는 취소 후 다시 종료할 수 있습니다.");
    private void Notice(string text)
    {
        try { _icon.ShowBalloonTip(5000, "School Timetable Widget", text, Forms.ToolTipIcon.Info); }
        catch (System.ComponentModel.Win32Exception error) { System.Diagnostics.Debug.WriteLine(error); }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _icon.Visible = false;
        _icon.MouseDoubleClick -= DoubleClick;
        _toggle.Click -= Toggle; _exit.Click -= Exit;
        _icon.Dispose(); _menu.Dispose(); _image.Dispose();
    }
}
