using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SchoolTimetableWidget.Desktop.Features.WindowPlacement;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Persistence;

internal sealed class JsonWindowStateStore(string directory) : IWindowStateStore
{
    internal const string FileName = "window-state.json";
    internal string FilePath { get; } = Path.Combine(directory, FileName);
    internal Action? BeforeRename { get; set; }
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true
    };

    public PreferredWindowBounds? Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            using var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > 4096) return null;
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().GroupBy(p => p.Name).Any(g => g.Count() != 1)) return null;
            var dto = root.Deserialize<StateDto>(Options);
            if (dto is null || dto.WindowStateVersion != 1) return null;
            var bounds = new PreferredWindowBounds(dto.PreferredWidth, dto.PreferredHeight, dto.Left, dto.Top, dto.MonitorHint);
            return bounds.IsValid ? bounds : null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or JsonException)
        { System.Diagnostics.Debug.WriteLine(error); return null; }
    }

    public bool Save(PreferredWindowBounds bounds)
    {
        if (!bounds.IsValid) return false;
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new StateDto
            {
                WindowStateVersion = 1, PreferredWidth = bounds.Width, PreferredHeight = bounds.Height,
                Left = bounds.Left, Top = bounds.Top, MonitorHint = bounds.MonitorHint
            }, Options);
            Directory.CreateDirectory(directory);
            AtomicProfileFile.Write(FilePath, bytes, BeforeRename);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { System.Diagnostics.Debug.WriteLine(error); return false; }
    }

    private sealed class StateDto
    {
        public required int WindowStateVersion { get; init; }
        public required double PreferredWidth { get; init; }
        public required double PreferredHeight { get; init; }
        public required double Left { get; init; }
        public required double Top { get; init; }
        public required string? MonitorHint { get; init; }
    }
}
