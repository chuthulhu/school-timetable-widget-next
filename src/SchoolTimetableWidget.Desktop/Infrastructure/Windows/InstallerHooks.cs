using SchoolTimetableWidget.Desktop.Features.Autostart;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

/// <summary>Installer lifecycle callbacks. User data is never touched (ADR 0029).</summary>
internal static class InstallerHooks
{
    public static void BeforeUninstall()
    {
        // Velopack ends the hook after 30 seconds; a registry failure must not block uninstall.
        try { RemoveOwnedAutoStart(new WindowsAutoStartRegistrationStore(), AutoStartCommand.ForCurrentProcess()); }
        catch (Exception) { }
    }

    /// <summary>Removes the Run value only while it still launches this installed executable.</summary>
    internal static bool RemoveOwnedAutoStart(IAutoStartRegistrationStore store, string? currentCommand)
    {
        if (currentCommand is null || store.Read() is not string registered ||
            !string.Equals(registered, currentCommand, StringComparison.OrdinalIgnoreCase))
            return false;
        store.Remove();
        return true;
    }
}
