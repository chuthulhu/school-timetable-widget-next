using SchoolTimetableWidget.Desktop.Features.Autostart;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;

namespace SchoolTimetableWidget.Tests.Installer;

// ADR 0029 uninstall cleanup against an in-memory store; the real registry is never touched.
public class InstallerHooksTests
{
    private const string Installed = "\"C:\\Users\\교사\\AppData\\Local\\SchoolTimetableWidgetNext\\current\\SchoolTimetableWidget.Desktop.exe\"";

    [Fact]
    public void RemovesTheRunValueThatLaunchesThisInstall()
    {
        var store = new Store { Value = Installed };
        Assert.True(InstallerHooks.RemoveOwnedAutoStart(store, Installed));
        Assert.Null(store.Value); Assert.Equal(1, store.Removes);
    }

    [Fact]
    public void PathCaseDifferencesStillCountAsThisInstall()
    {
        var store = new Store { Value = Installed.ToUpperInvariant() };
        Assert.True(InstallerHooks.RemoveOwnedAutoStart(store, Installed));
        Assert.Null(store.Value);
    }

    [Theory]
    [InlineData("\"D:\\Codex\\school-timetable-widget-next\\bin\\SchoolTimetableWidget.Desktop.exe\"")]
    [InlineData("\"C:\\Users\\교사\\AppData\\Local\\SchoolTimetableWidgetNext\\current\\SchoolTimetableWidget.Desktop.exe\" --hidden")]
    [InlineData("")]
    public void LeavesAnotherCommandUntouched(string other)
    {
        var store = new Store { Value = other };
        Assert.False(InstallerHooks.RemoveOwnedAutoStart(store, Installed));
        Assert.Equal(other, store.Value); Assert.Equal(0, store.Removes);
    }

    [Fact]
    public void MissingNonStringOrUnknownCurrentCommandRemovesNothing()
    {
        Assert.False(InstallerHooks.RemoveOwnedAutoStart(new Store(), Installed));
        var binary = new Store { Value = new byte[] { 1, 2 } };
        Assert.False(InstallerHooks.RemoveOwnedAutoStart(binary, Installed)); Assert.Equal(0, binary.Removes);
        var registered = new Store { Value = Installed };
        Assert.False(InstallerHooks.RemoveOwnedAutoStart(registered, null)); Assert.Equal(Installed, registered.Value);
    }

    private sealed class Store : IAutoStartRegistrationStore
    {
        public object? Value;
        public int Removes;
        public object? Read() => Value;
        public void Write(string command) => Value = command;
        public void Remove() { Removes++; Value = null; }
    }
}
