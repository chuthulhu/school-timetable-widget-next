using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;

namespace SchoolTimetableWidget.Desktop.Features.Persistence;

/// <summary>Independent file contract containing canonical durable inputs only.</summary>
public static class ProfileBackupFile
{
    public const string Extension = ".stwbackup";
    public const int Version = 1;
    public const int MaximumBytes = 4 * 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true, RespectNullableAnnotations = true,
        AllowDuplicateProperties = false
    };
    private sealed record Envelope(int BackupFileVersion, int ProfileSchemaVersion, JsonElement Profile);

    public static byte[] Export(ProfileSnapshot committed)
    {
        using var profile = JsonDocument.Parse(ProfileJson.Serialize(committed));
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new Envelope(Version, 4, profile.RootElement.GetProperty("profile")), Options);
        if (bytes.Length > MaximumBytes) throw new InvalidDataException("백업 데이터가 파일 크기 제한(4 MiB)을 초과합니다.");
        _ = Import(bytes);
        return bytes;
    }

    public static ProfileSnapshot Import(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0 || bytes.Length > MaximumBytes)
            throw new InvalidDataException("백업 파일은 비어 있지 않은 4 MiB 이하 파일이어야 합니다.");
        try
        {
            var envelope = JsonSerializer.Deserialize<Envelope>(bytes, Options) ?? throw new JsonException();
            if (envelope.BackupFileVersion != Version || envelope.ProfileSchemaVersion != 4)
                throw new InvalidDataException("이 버전의 앱에서 지원하지 않는 백업 파일입니다.");
            using var output = new MemoryStream();
            using (var writer = new Utf8JsonWriter(output))
            {
                writer.WriteStartObject(); writer.WriteNumber("schemaVersion", 4);
                writer.WritePropertyName("profile"); envelope.Profile.WriteTo(writer); writer.WriteEndObject();
            }
            return ProfileJson.Deserialize(output.ToArray());
        }
        catch (Exception error) when (error is JsonException or ArgumentException or FormatException or InvalidOperationException)
        { throw new InvalidDataException("백업 파일의 내용이 올바르지 않습니다. 현재 데이터는 변경하지 않았습니다.", error); }
    }

    public static ProfileSnapshot Read(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > MaximumBytes) throw new InvalidDataException("백업 파일이 너무 큽니다(최대 4 MiB).");
        var bytes = new byte[stream.Length]; stream.ReadExactly(bytes);
        return Import(bytes);
    }
}
