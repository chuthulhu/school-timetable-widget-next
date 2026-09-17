using Microsoft.Win32;
using SchoolTimetableWidget.Desktop.Features.Autostart;
using System.IO;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

internal sealed class WindowsAutoStartRegistrationStore : IAutoStartRegistrationStore
{
    internal const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal const string ValueName = "SchoolTimetableWidget";

    public object? Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: false);
        return key?.GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
    }

    public void Write(string command)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true);
        key.SetValue(ValueName, command, RegistryValueKind.String);
    }

    public void Remove()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}

internal static class AutoStartCommand
{
    public static string? ForCurrentProcess() => FromExecutable(Environment.ProcessPath);

    internal static string? FromExecutable(string? path)
    {
        // Refuse dotnet/testhost/DLL launches and command text. Only an apphost can be registered.
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) ||
            path.IndexOfAny(['"', '\r', '\n', '\0']) >= 0 ||
            !string.Equals(Path.GetFileName(path), "SchoolTimetableWidget.Desktop.exe", StringComparison.OrdinalIgnoreCase))
            return null;
        var command = "\"" + path + "\"";
        // Windows Run documents a maximum command-line length of 260 characters.
        return command.Length <= 260 ? command : null;
    }
}
