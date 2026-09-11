using System.IO;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

public static class ProfileLocation
{
    public static string ForCurrentUser()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(local)) throw new IOException("Local application data is unavailable.");
        return Path.Combine(local, "SchoolTimetableWidget");
    }
}
