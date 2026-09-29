using System.IO;

namespace SchoolTimetableWidget.Tests.Time;

/// <summary>Source architecture guards; these do not claim native startup/OS input evidence.</summary>
public class ClockCompositionTests
{
    [Fact]
    public void NetworkStartsOnlyAfterPrimaryShowAndListen()
    {
        var source = File.ReadAllText(Path.Combine(Root(), "src/SchoolTimetableWidget.Desktop/App.xaml.cs"));
        Assert.Contains("SingleInstanceStartup.Run(_instance, () => InitializePrimary", source);
        var show = source.IndexOf("MainWindow.Show();", StringComparison.Ordinal);
        var start = source.IndexOf("_clockSynchronization.Start()", StringComparison.Ordinal);
        Assert.True(start > show);
        Assert.True(start > source.IndexOf("_instance!.Listen", StringComparison.Ordinal));
        Assert.Contains("if (!preview)", source);
        Assert.Contains("_clockSynchronization?.Dispose();", source[source.IndexOf("protected override void OnSessionEnding", StringComparison.Ordinal)..]);
    }

    [Fact]
    public void ProductionNeverMutatesWindowsTimeAndNetworkIsOutsideFeatures()
    {
        foreach (var path in Directory.EnumerateFiles(Path.Combine(Root(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains("\\obj\\") && !p.Contains("\\bin\\")))
        {
            var source = File.ReadAllText(path);
            foreach (var forbidden in new[] { "SetSystemTime(", "SetLocalTime(", "w32tm", "Services\\W32Time" })
                Assert.DoesNotContain(forbidden, source, StringComparison.OrdinalIgnoreCase);
            if (path.Contains("\\Features\\"))
            {
                Assert.DoesNotContain("NtpClient", source);
                Assert.DoesNotContain("DateTimeOffset.Now", source);
                Assert.DoesNotContain("DateTime.Now", source);
            }
        }
    }

    [Fact]
    public void TimeInfrastructureCannotWriteProfileOrRegisterAutostart()
    {
        foreach (var path in Directory.EnumerateFiles(Path.Combine(Root(), "src/SchoolTimetableWidget.Desktop/Infrastructure/Time"), "*.cs"))
        {
            var source = File.ReadAllText(path);
            foreach (var forbidden in new[] { "ProfileSession", "JsonProfile", "Registry", "File.Write", "MessageBox", "WindowState", "TrayNotice" })
                Assert.DoesNotContain(forbidden, source);
        }
    }

    internal static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SchoolTimetableWidget.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root unavailable.");
    }
}
