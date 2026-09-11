using System.IO;

namespace SchoolTimetableWidget.Desktop.Development;

/// <summary>Preview modes always isolate storage. Explicit restart paths are DEBUG-only and beneath TEMP.</summary>
internal static class DevelopmentProfileLocation
{
    public static string CreateTemporary() => Path.Combine(Path.GetTempPath(), "SchoolTimetableWidget-preview-" + Guid.NewGuid().ToString("N"));

    public static string? FromArguments(string[] args)
    {
#if DEBUG
        var argument = args.SingleOrDefault(a => a.StartsWith("--dev-profile-directory=", StringComparison.Ordinal));
        if (argument is null) return null;
        var supplied = argument["--dev-profile-directory=".Length..];
        if (!Path.IsPathFullyQualified(supplied)) throw new ArgumentException("Development profile directory must be absolute.");
        var directory = Path.GetFullPath(supplied);
        var temp = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())) + Path.DirectorySeparatorChar;
        if (!directory.StartsWith(temp, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Development profiles must be below TEMP.");
        return directory;
#else
        return null;
#endif
    }
}
