using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using SchoolTimetableWidget.Desktop.Features.Persistence;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Persistence;

/// <summary>One immutable pending recovery source, secured before candidate replacement.</summary>
internal sealed class ProfileRecoveryFiles(string directory)
{
    internal string SnapshotPath => Path.Combine(directory, "profile.pre-restore.json");
    internal string OriginalPath => Path.Combine(directory, "profile.recovery-original.json");
    internal string MarkerPath => Path.Combine(directory, "recovery-required.json");
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true,
        AllowDuplicateProperties = false
    };
    private sealed record Marker(int Version, string SnapshotSha256, string Stage, string Origin);
    internal sealed record Source(ProfileLoadState Origin, byte[] Bytes, ProfileSnapshot Snapshot);
    internal bool IsPending
    {
        get
        {
            try { using var stream = File.Open(MarkerPath, FileMode.Open, FileAccess.Read, FileShare.Read); return true; }
            catch (FileNotFoundException) { return false; }
        }
    }
    internal void Secure(ProfileSnapshot current) => Secure(current, ProfileLoadState.Loaded, null);
    internal void Secure(ProfileSnapshot current, ProfileLoadState origin, byte[]? original)
    {
        if (IsPending) throw new IOException("Recovery is already pending.");
        var degraded = origin is ProfileLoadState.Invalid or ProfileLoadState.Unsupported;
        if (!degraded && origin is not (ProfileLoadState.Loaded or ProfileLoadState.Missing))
            throw new IOException("No recoverable origin.");
        var bytes = degraded ? original ?? throw new IOException("Original unavailable.") : ProfileJson.Serialize(current);
        AtomicProfileFile.Write(degraded ? OriginalPath : SnapshotPath, bytes);
        AtomicProfileFile.Write(MarkerPath, JsonSerializer.SerializeToUtf8Bytes(
            new Marker(1, Convert.ToHexString(SHA256.HashData(bytes)), "replacement-pending", origin.ToString()), Options));
    }
    internal Source ReadSource()
    {
        using var markerStream = File.OpenRead(MarkerPath);
        if (markerStream.Length > 4096) throw new InvalidDataException("Invalid recovery record.");
        var marker = JsonSerializer.Deserialize<Marker>(markerStream, Options) ?? throw new InvalidDataException();
        if (marker.Version != 1 || marker.Stage != "replacement-pending" ||
            !Enum.TryParse<ProfileLoadState>(marker.Origin, out var origin) || origin.ToString() != marker.Origin ||
            origin is not (ProfileLoadState.Loaded or ProfileLoadState.Missing or ProfileLoadState.Invalid or ProfileLoadState.Unsupported))
            throw new InvalidDataException("Unsupported recovery record.");
        var degraded = origin is ProfileLoadState.Invalid or ProfileLoadState.Unsupported;
        var bytes = File.ReadAllBytes(degraded ? OriginalPath : SnapshotPath);
        if (Convert.ToHexString(SHA256.HashData(bytes)) != marker.SnapshotSha256)
            throw new InvalidDataException("Recovery source no longer matches its record.");
        return new(origin, bytes, degraded ? ProfileSnapshot.Defaults() : ProfileJson.Deserialize(bytes));
    }
    internal ProfileSnapshot ReadPrevious() => ReadSource().Snapshot;
    internal void Complete()
    {
        _ = ReadSource();
        File.Delete(MarkerPath);
    }
}
