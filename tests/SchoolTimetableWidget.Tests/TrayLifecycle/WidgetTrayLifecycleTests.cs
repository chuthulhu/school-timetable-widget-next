using SchoolTimetableWidget.Desktop.Features.TrayLifecycle;

namespace SchoolTimetableWidget.Tests.TrayLifecycle;

public class WidgetTrayLifecycleTests
{
    [Fact]
    public void NormalCloseHidesWithoutShuttingDownAndShowReusesWindow()
    {
        var window = new FakeWidgetWindow(); var tray = new FakeTrayIcon(); var exits = 0;
        using var lifecycle = new WidgetTrayLifecycle(window, tray, null, () => exits++);
        Assert.True(tray.Visible);
        Assert.True(window.Close());
        Assert.False(window.IsVisible); Assert.False(tray.Visible);
        Assert.False(lifecycle.AllowClose); Assert.Equal(0, exits);
        lifecycle.Show();
        Assert.True(window.IsVisible); Assert.True(tray.Visible); Assert.Equal(1, window.Shows);
    }

    [Fact]
    public void MenuOrDoubleClickToggleDoesNotRequestCloseNotice()
    {
        var window = new FakeWidgetWindow(); var tray = new FakeTrayIcon();
        using var lifecycle = new WidgetTrayLifecycle(window, tray, null, () => { });
        tray.Toggle(); Assert.False(window.IsVisible);
        tray.Toggle(); Assert.True(window.IsVisible);
        Assert.Equal(0, tray.CloseNotices);
        window.SetVisible(false); Assert.False(tray.Visible);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void NoticeRequestedOnceEvenWhenPersistenceFails(bool saveSucceeds)
    {
        var window = new FakeWidgetWindow(); var tray = new FakeTrayIcon();
        var store = new MemoryNotice { SaveSucceeds = saveSucceeds };
        using var lifecycle = new WidgetTrayLifecycle(window, tray, store, () => { });
        Assert.True(window.Close()); lifecycle.Show(); Assert.True(window.Close());
        Assert.Equal(1, tray.CloseNotices); Assert.Equal(1, store.Writes);
    }

    [Fact]
    public void ExistingReceiptSuppressesNotice()
    {
        var window = new FakeWidgetWindow(); var tray = new FakeTrayIcon();
        var store = new MemoryNotice { Requested = true };
        using var lifecycle = new WidgetTrayLifecycle(window, tray, store, () => { });
        window.Close();
        Assert.Equal(0, tray.CloseNotices); Assert.Equal(0, store.Writes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ExitFromVisibleOrHiddenAllowsCloseAndShutsDownOnce(bool hidden)
    {
        var window = new FakeWidgetWindow(); var tray = new FakeTrayIcon(); var exits = 0;
        using var lifecycle = new WidgetTrayLifecycle(window, tray, null, () => exits++);
        if (hidden) lifecycle.Hide();
        tray.Exit(); tray.Exit();
        Assert.True(lifecycle.AllowClose); Assert.False(window.Close()); Assert.Equal(1, exits);
        lifecycle.Show(); Assert.Equal(0, window.Shows);
        Assert.Equal(0, tray.CloseNotices);
    }

    [Fact]
    public void ModalDialogPreventsHideCloseAndExitWithoutLosingDraft()
    {
        var window = new FakeWidgetWindow { DialogOpen = true }; var tray = new FakeTrayIcon(); var exits = 0;
        var draft = new object(); window.Draft = draft;
        using var lifecycle = new WidgetTrayLifecycle(window, tray, null, () => exits++);
        tray.Toggle(); Assert.True(window.IsVisible);
        Assert.True(window.Close()); lifecycle.Show(); tray.Exit();
        Assert.Equal(4, window.DialogActivations);
        Assert.Equal(0, window.Hides); Assert.Equal(0, window.Shows); Assert.Equal(0, exits);
        Assert.Same(draft, window.Draft); Assert.Equal(1, tray.EditorNotices);
        Assert.Equal(0, tray.CloseNotices); Assert.False(lifecycle.AllowClose);
        window.DialogOpen = false; tray.Exit(); Assert.Equal(1, exits);
    }

    [Fact]
    public void SessionEndingAllowsCloseEvenWithEditorWithoutNoticeOrConfirmation()
    {
        var window = new FakeWidgetWindow { DialogOpen = true }; var tray = new FakeTrayIcon();
        using var lifecycle = new WidgetTrayLifecycle(window, tray, null, () => throw new Exception("Not explicit exit"));
        lifecycle.SessionEnding();
        Assert.True(lifecycle.AllowClose); Assert.False(window.Close());
        Assert.Equal(0, window.DialogActivations); Assert.Equal(0, tray.EditorNotices);
        Assert.Equal(0, tray.CloseNotices); Assert.Equal(0, window.Hides);
    }

    [Fact]
    public void DisposeUnsubscribesAndDisposesResourcesExactlyOnce()
    {
        var window = new FakeWidgetWindow(); var tray = new FakeTrayIcon(); var exits = 0;
        var lifecycle = new WidgetTrayLifecycle(window, tray, null, () => exits++);
        lifecycle.Dispose(); lifecycle.Dispose();
        tray.Toggle(); tray.Exit(); Assert.False(window.Close());
        lifecycle.Show(); lifecycle.Hide();
        Assert.Equal(1, window.Disposals); Assert.Equal(1, tray.Disposals);
        Assert.Equal(0, window.Hides); Assert.Equal(0, window.Shows); Assert.Equal(0, exits);
    }

    [Fact]
    public void UnexpectedShutdownDefectIsNotSwallowed()
    {
        using var lifecycle = new WidgetTrayLifecycle(new FakeWidgetWindow(), new FakeTrayIcon(), null,
            () => throw new InvalidOperationException("defect"));
        Assert.Throws<InvalidOperationException>(lifecycle.Exit);
    }
}

internal sealed class FakeWidgetWindow : IWidgetWindow
{
    public bool IsVisible { get; private set; } = true;
    public bool DialogOpen { get; set; }
    public object? Draft { get; set; }
    public int Shows, Hides, Disposals, DialogActivations;
    public Action? OnShow { get; set; }
    public event Action? VisibilityChanged;
    public event Func<bool>? CloseRequested;
    public bool Close() => CloseRequested?.Invoke() ?? false;
    public bool TryActivateDialog() { if (DialogOpen) DialogActivations++; return DialogOpen; }
    public void ShowAndActivate() { Shows++; OnShow?.Invoke(); SetVisible(true); }
    public void Hide() { Hides++; SetVisible(false); }
    public void SetVisible(bool value) { IsVisible = value; VisibilityChanged?.Invoke(); }
    public void Dispose() => Disposals++;
}
internal sealed class FakeTrayIcon : ITrayIcon
{
    public bool Visible;
    public int CloseNotices, EditorNotices, Disposals;
    public event Action? ToggleRequested;
    public event Action? ExitRequested;
    public void Toggle() => ToggleRequested?.Invoke();
    public void Exit() => ExitRequested?.Invoke();
    public void SetWindowVisible(bool visible) => Visible = visible;
    public void RequestCloseNotice() => CloseNotices++;
    public void RequestEditorNotice() => EditorNotices++;
    public void Dispose() => Disposals++;
}
internal sealed class MemoryNotice : ITrayNoticeStore
{
    public bool Requested, SaveSucceeds = true;
    public int Writes;
    public bool WasRequested() => Requested;
    public bool SaveRequested() { Writes++; if (SaveSucceeds) Requested = true; return SaveSucceeds; }
}
