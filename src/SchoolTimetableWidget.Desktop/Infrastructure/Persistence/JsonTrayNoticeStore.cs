using System.IO;
using System.Text.Json;
using SchoolTimetableWidget.Desktop.Features.TrayLifecycle;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Persistence;

/// <summary>Small machine-local notice receipt, independent of profile and geometry versions.</summary>
internal sealed class JsonTrayNoticeStore(string directory) : ITrayNoticeStore
{
    internal string FilePath { get; } = Path.Combine(directory, "tray-state.json");
    internal Action? BeforeRename { get; set; }

    public bool WasRequested()
    {
        try
        {
            if (!File.Exists(FilePath)) return false;
            using var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > 1024) return false;
            using var json = JsonDocument.Parse(stream);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;
            var fields = root.EnumerateObject().ToArray();
            return fields.Length == 2 && fields.Select(f => f.Name).Distinct().Count() == 2 &&
                root.TryGetProperty("trayStateVersion", out var version) && version.ValueKind == JsonValueKind.Number && version.TryGetInt32(out var number) && number == 1 &&
                root.TryGetProperty("closeNoticeRequested", out var notice) && notice.ValueKind == JsonValueKind.True;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or JsonException)
        { System.Diagnostics.Debug.WriteLine(error); return false; }
    }

    public bool SaveRequested()
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new { trayStateVersion = 1, closeNoticeRequested = true });
            Directory.CreateDirectory(directory);
            AtomicProfileFile.Write(FilePath, bytes, BeforeRename);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { System.Diagnostics.Debug.WriteLine(error); return false; }
    }
}
