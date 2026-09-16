using System.IO;
using SchoolTimetableWidget.Desktop.Features.TrayLifecycle;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.Persistence;

namespace SchoolTimetableWidget.Tests.TrayLifecycle;

public class TrayNoticeStoreTests
{
    [Fact]
    public void FirstCloseReceiptSurvivesRestartWithoutTouchingProfileOrPlacement()
    {
        using var temp = new TempProfile();
        File.WriteAllText(temp.File, "profile sentinel");
        var geometry = Path.Combine(temp.Directory, "window-state.json");
        File.WriteAllText(geometry, "geometry sentinel");
        var store = new JsonTrayNoticeStore(temp.Directory);
        Assert.False(store.WasRequested()); Assert.False(File.Exists(store.FilePath));
        var first = new FakeWidgetWindow(); var tray = new FakeTrayIcon();
        using (var lifecycle = new WidgetTrayLifecycle(first, tray, store, () => { })) first.Close();
        Assert.True(new JsonTrayNoticeStore(temp.Directory).WasRequested());
        var next = new FakeWidgetWindow(); var nextTray = new FakeTrayIcon();
        using (var lifecycle = new WidgetTrayLifecycle(next, nextTray, new JsonTrayNoticeStore(temp.Directory), () => { })) next.Close();
        Assert.Equal(1, tray.CloseNotices); Assert.Equal(0, nextTray.CloseNotices);
        Assert.Equal("profile sentinel", File.ReadAllText(temp.File));
        Assert.Equal("geometry sentinel", File.ReadAllText(geometry));
        Assert.Empty(Directory.GetFiles(temp.Directory, "*.tmp"));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"trayStateVersion\":2,\"closeNoticeRequested\":true}")]
    [InlineData("{\"trayStateVersion\":\"1\",\"closeNoticeRequested\":true}")]
    [InlineData("{\"trayStateVersion\":1,\"closeNoticeRequested\":false}")]
    [InlineData("{\"trayStateVersion\":1,\"trayStateVersion\":1,\"closeNoticeRequested\":true}")]
    [InlineData("{\"trayStateVersion\":1,\"closeNoticeRequested\":true,\"extra\":1}")]
    public void InvalidReceiptIsIgnoredWithoutStartupRewrite(string data)
    {
        using var temp = new TempProfile();
        var store = new JsonTrayNoticeStore(temp.Directory);
        File.WriteAllText(store.FilePath, data);
        Assert.False(store.WasRequested()); Assert.Equal(data, File.ReadAllText(store.FilePath));
    }

    [Fact]
    public void OversizedReceiptIsIgnored()
    {
        using var temp = new TempProfile(); var store = new JsonTrayNoticeStore(temp.Directory);
        File.WriteAllText(store.FilePath, new string(' ', 1025));
        Assert.False(store.WasRequested());
    }

    [Fact]
    public void AtomicWriteFailurePreservesOldReceiptAndDoesNotRepeatDuringRun()
    {
        using var temp = new TempProfile(); var store = new JsonTrayNoticeStore(temp.Directory);
        File.WriteAllText(store.FilePath, "old invalid receipt");
        store.BeforeRename = () => throw new IOException("injected rename failure");
        var window = new FakeWidgetWindow(); var tray = new FakeTrayIcon();
        using var lifecycle = new WidgetTrayLifecycle(window, tray, store, () => { });
        window.Close(); lifecycle.Show(); window.Close();
        Assert.Equal(1, tray.CloseNotices); Assert.False(window.IsVisible);
        Assert.Equal("old invalid receipt", File.ReadAllText(store.FilePath));
        Assert.Empty(Directory.GetFiles(temp.Directory, "*.tmp"));
        store.BeforeRename = null; Assert.True(store.SaveRequested()); Assert.True(store.WasRequested());
    }

    [Fact]
    public void LockedReceiptReadOrWriteIsOptionalFailure()
    {
        using var temp = new TempProfile(); var store = new JsonTrayNoticeStore(temp.Directory);
        Assert.True(store.SaveRequested());
        using var locked = new FileStream(store.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.False(store.WasRequested()); Assert.False(store.SaveRequested());
    }

    [Fact]
    public void UnexpectedProgrammingDefectIsNotSwallowedByWriter()
    {
        using var temp = new TempProfile(); var store = new JsonTrayNoticeStore(temp.Directory);
        store.BeforeRename = () => throw new InvalidOperationException("defect");
        Assert.Throws<InvalidOperationException>(() => store.SaveRequested());
        Assert.Empty(Directory.GetFiles(temp.Directory, "*.tmp"));
    }
}
