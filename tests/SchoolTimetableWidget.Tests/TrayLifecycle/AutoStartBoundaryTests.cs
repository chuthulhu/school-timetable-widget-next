using System.Runtime.CompilerServices;
using SchoolTimetableWidget.Desktop.Development;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using SchoolTimetableWidget.Tests.Persistence;

namespace SchoolTimetableWidget.Tests.TrayLifecycle;

// Source boundary evidence: never opens the real user's registry or desktop.
public class AutoStartBoundaryTests
{
    [Fact]
    public void WindowsAdapterCanOnlyAddressTheOwnedValue()
    {
        Assert.Equal(@"Software\Microsoft\Windows\CurrentVersion\Run", WindowsAutoStartRegistrationStore.KeyPath);
        Assert.Equal("SchoolTimetableWidget", WindowsAutoStartRegistrationStore.ValueName);
        var source = Read("Infrastructure/Windows/WindowsAutoStartRegistrationStore.cs");
        Assert.Contains("Registry.CurrentUser.OpenSubKey(KeyPath, writable: false)", source);
        Assert.Contains("Registry.CurrentUser.CreateSubKey(KeyPath, writable: true)", source);
        Assert.Contains("Registry.CurrentUser.OpenSubKey(KeyPath, writable: true)", source);
        Assert.Contains("GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames)", source);
        Assert.Contains("SetValue(ValueName, command, RegistryValueKind.String)", source);
        Assert.Contains("DeleteValue(ValueName, throwOnMissingValue: false)", source);
        foreach (var forbidden in new[] { "LocalMachine", "GetValueNames", "GetSubKeyNames", "DeleteSubKey", "StartupApproved", "Process.Start" })
            Assert.DoesNotContain(forbidden, source);
        Assert.Contains("FromExecutable(Environment.ProcessPath)", source);
    }

    [Fact]
    public void StartupRemainsVisibleAndRegistrationExistsOnlyInsidePrimaryGate()
    {
        var source = Read("App.xaml.cs");
        var primary = source.IndexOf("private void InitializePrimary", StringComparison.Ordinal);
        var startup = source[..primary];
        Assert.Contains("SingleInstanceStartup.Run", startup);
        Assert.DoesNotContain("AutoStartRegistration", startup);
        Assert.Contains("MainWindow.Show();", source);
        Assert.DoesNotContain("MainWindow.Hide", source);
        Assert.DoesNotContain("autostart.Status", source);
        Assert.DoesNotContain("autostart.SetEnabled", source);
        Assert.DoesNotContain("Thread.Sleep", source);
        Assert.True(source.IndexOf("new WindowPlacementController", StringComparison.Ordinal) < source.IndexOf("MainWindow.Show();", StringComparison.Ordinal));
    }

    [Fact]
    public void PersistenceAndLifecycleHaveNoAutostartDependency()
    {
        foreach (var relative in new[] { "Features/Persistence", "Infrastructure/Persistence", "Features/TrayLifecycle", "Features/WindowPlacement" })
        foreach (var file in Directory.GetFiles(Path.Combine(DesktopRoot(), relative), "*.cs"))
        {
            var source = File.ReadAllText(file);
            Assert.DoesNotContain("AutoStartRegistration", source);
            Assert.DoesNotContain("Registry.", source);
        }
        var trayState = Read("Infrastructure/Persistence/JsonTrayNoticeStore.cs");
        Assert.DoesNotContain("IsVisible", trayState);
        Assert.DoesNotContain("Hidden", trayState);
    }

    [Fact]
    public void ExactCommandDiagnosticStorageIsTemporaryAndArgumentHasPrecedence()
    {
        using var first = new TempProfile(); using var second = new TempProfile();
#if DEBUG
        Assert.Equal(first.Directory, DevelopmentProfileLocation.FromArguments([], first.Directory));
        Assert.Equal(second.Directory, DevelopmentProfileLocation.FromArguments(
            ["--dev-profile-directory=" + second.Directory], first.Directory));
        Assert.Throws<ArgumentException>(() => DevelopmentProfileLocation.FromArguments([], "relative"));
        Assert.Throws<ArgumentException>(() => DevelopmentProfileLocation.FromArguments([], Path.GetPathRoot(first.Directory)));
#else
        Assert.Null(DevelopmentProfileLocation.FromArguments([], first.Directory));
#endif
    }

    private static string Read(string relative) => File.ReadAllText(Path.Combine(DesktopRoot(), relative));
    private static string DesktopRoot([CallerFilePath] string file = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "../../../src/SchoolTimetableWidget.Desktop"));
}
