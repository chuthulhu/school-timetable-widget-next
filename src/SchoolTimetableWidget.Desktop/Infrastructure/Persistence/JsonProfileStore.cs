using System.IO;
using System.Text.Json;
using SchoolTimetableWidget.Desktop.Features.Persistence;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Persistence;

/// <summary>One leased writer per directory. Whole-document same-directory rename, never destination truncation.</summary>
public sealed class JsonProfileStore : IProfileStore, IDisposable
{
    private readonly Action<ProfileWriteStage>? _checkpoint;
    private FileStream? _lease;
    private byte[]? _expectedBytes;
    private bool _loaded;
    private bool _writable;
    private bool _disposed;
    public JsonProfileStore(string directory) : this(directory, null) { }
    internal JsonProfileStore(string directory, Action<ProfileWriteStage>? checkpoint)
    {
        DirectoryPath = Path.GetFullPath(directory);
        FilePath = Path.Combine(DirectoryPath, "profile.json");
        _checkpoint = checkpoint;
    }
    public string DirectoryPath { get; }
    public string FilePath { get; }

    public ProfileLoadResult Load()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_loaded) throw new InvalidOperationException("Load once per store lifetime.");
        _loaded = true;
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            _lease = new FileStream(Path.Combine(DirectoryPath, "profile.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            try { _expectedBytes = File.ReadAllBytes(FilePath); }
            catch (FileNotFoundException)
            {
                _writable = true;
                return new(ProfileSnapshot.Defaults(), ProfileLoadState.Missing, FilePath);
            }
            var snapshot = ProfileJson.Deserialize(_expectedBytes);
            _writable = true;
            return new(snapshot, ProfileLoadState.Loaded, FilePath);
        }
        catch (UnsupportedProfileVersionException) { return Failure(ProfileLoadState.Unsupported); }
        catch (Exception error) when (error is JsonException or ArgumentException or FormatException or InvalidOperationException)
        {
            System.Diagnostics.Debug.WriteLine(error);
            return Failure(ProfileLoadState.Invalid);
        }
        catch (Exception error) when (IsAccessFailure(error))
        {
            System.Diagnostics.Debug.WriteLine(error);
            return Failure(ProfileLoadState.Unavailable);
        }
    }

    public void Save(ProfileSnapshot snapshot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_writable || _lease is null) throw new IOException("This profile is not writable.");
        var bytes = ProfileJson.Serialize(snapshot);
        VerifyUnchanged();
        var temporary = Path.Combine(DirectoryPath, $".profile-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                _checkpoint?.Invoke(ProfileWriteStage.Created);
                var half = bytes.Length / 2;
                stream.Write(bytes.AsSpan(0, half));
                _checkpoint?.Invoke(ProfileWriteStage.PartialWrite);
                stream.Write(bytes.AsSpan(half));
                _checkpoint?.Invoke(ProfileWriteStage.BeforeFlush);
                stream.Flush(flushToDisk: true);
            }
            _checkpoint?.Invoke(ProfileWriteStage.BeforeRename);
            VerifyUnchanged();
            // Both names have the same parent: Windows MoveFileEx replaces by rename,
            // with no cross-volume copy and no delete-then-move fallback.
            File.Move(temporary, FilePath, overwrite: _expectedBytes is not null);
            _expectedBytes = bytes;
        }
        finally
        {
            try { File.Delete(temporary); }
            catch (Exception error) when (IsAccessFailure(error)) { System.Diagnostics.Debug.WriteLine(error); }
        }
    }

    private void VerifyUnchanged()
    {
        byte[]? actual;
        try { actual = File.ReadAllBytes(FilePath); }
        catch (FileNotFoundException) { actual = null; }
        if (actual is null ? _expectedBytes is not null : _expectedBytes is null || !actual.AsSpan().SequenceEqual(_expectedBytes))
            throw new IOException("The profile changed outside this session. Refusing to overwrite it.");
    }
    private ProfileLoadResult Failure(ProfileLoadState state) => new(ProfileSnapshot.Defaults(), state, FilePath);
    private static bool IsAccessFailure(Exception error) => error is IOException or UnauthorizedAccessException or System.Security.SecurityException;
    public void Dispose()
    {
        _disposed = true;
        _writable = false;
        _lease?.Dispose();
        _lease = null;
    }
}

internal enum ProfileWriteStage { Created, PartialWrite, BeforeFlush, BeforeRename }
