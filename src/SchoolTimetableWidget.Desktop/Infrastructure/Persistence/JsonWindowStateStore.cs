using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SchoolTimetableWidget.Desktop.Features.WindowPlacement;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Persistence;

internal sealed class JsonWindowStateStore(string directory) : IWindowStateStore
{
    internal const string FileName = "window-state.json";
    internal string FilePath { get; } = Path.Combine(directory, FileName);
    private bool _loaded;
    private bool _writable = true;
    private byte[]? _expectedBytes;
    internal Action? BeforeRename { get; set; }
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
        RespectNullableAnnotations = true,
        AllowDuplicateProperties = false
    };

    public PreferredWindowBounds? Load()
    {
        _loaded = true;
        _writable = false;
        try
        {
            if (!File.Exists(FilePath)) { _expectedBytes = null; _writable = true; return null; }
            using var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > 4096) return null;
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().GroupBy(p => p.Name).Any(g => g.Count() != 1)) return null;
            var dto = root.Deserialize<StateDto>(Options);
            if (dto is null || dto.WindowStateVersion != 1) return null;
            var bounds = new PreferredWindowBounds(dto.PreferredWidth, dto.PreferredHeight, dto.Left, dto.Top, dto.MonitorHint)
                { MoveLocked = dto.MoveLocked };
            if (!bounds.IsValid) return null;
            stream.Position = 0;
            _expectedBytes = new byte[stream.Length]; stream.ReadExactly(_expectedBytes);
            _writable = true;
            return bounds;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or JsonException)
        { System.Diagnostics.Debug.WriteLine(error); return null; }
    }

    public bool Save(PreferredWindowBounds bounds)
    {
        if (!_loaded) Load();
        if (!_writable || !bounds.IsValid) return false;
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new StateDto
            {
                WindowStateVersion = 1, PreferredWidth = bounds.Width, PreferredHeight = bounds.Height,
                Left = bounds.Left, Top = bounds.Top, MonitorHint = bounds.MonitorHint, MoveLocked = bounds.MoveLocked
            }, Options);
            Directory.CreateDirectory(directory);
            VerifyUnchanged();
            if (_expectedBytes is not null)
            {
                using var old = JsonDocument.Parse(_expectedBytes);
                if (!old.RootElement.TryGetProperty("moveLocked", out _))
                {
                    var backupPath = Path.Combine(directory, $"window-state-before-lock-{Guid.NewGuid():N}.json");
                    using var backup = new FileStream(backupPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    backup.Write(_expectedBytes); backup.Flush(flushToDisk: true);
                }
            }
            AtomicProfileFile.Write(FilePath, bytes, () => { BeforeRename?.Invoke(); VerifyUnchanged(); });
            _expectedBytes = bytes;
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { System.Diagnostics.Debug.WriteLine(error); return false; }
    }

    private void VerifyUnchanged()
    {
        byte[]? actual;
        try { actual = File.ReadAllBytes(FilePath); }
        catch (FileNotFoundException) { actual = null; }
        if (actual is null ? _expectedBytes is not null : _expectedBytes is null || !actual.AsSpan().SequenceEqual(_expectedBytes))
            throw new IOException("Window settings changed outside this session. Refusing to overwrite them.");
    }

    private sealed class StateDto
    {
        public bool MoveLocked { get; init; }
        public required int WindowStateVersion { get; init; }
        public required double PreferredWidth { get; init; }
        public required double PreferredHeight { get; init; }
        public required double Left { get; init; }
        public required double Top { get; init; }
        public required string? MonitorHint { get; init; }
    }
}
