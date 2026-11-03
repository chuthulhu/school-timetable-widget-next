using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using Velopack;

namespace SchoolTimetableWidget.Desktop;

/// <summary>
/// Process entry. Velopack install/update/uninstall hooks run and exit here, before WPF,
/// single-instance election or any profile access (ADR 0029).
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        VelopackApp.Build()
            .OnBeforeUninstallFastCallback(_ => InstallerHooks.BeforeUninstall())
            .Run();
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
