using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;

namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

public enum PresetImportCollision { None, SameId, SameName }
public enum PresetImportAction { Add, UpdateExisting, ImportAsCopy }

public sealed record PresetImportFont(string Element, FontSelection Font, bool IsAvailable)
{
    public string Status => IsAvailable ? "사용 가능" : Font.Source switch
    {
        FontSourceKind.System => "이 PC에 없음 · 기본 글꼴로 표시",
        FontSourceKind.OnlineDownloaded => "다운로드 필요 · 지금은 기본 글꼴로 표시",
        _ => "사용할 수 없음"
    };
}

public sealed record PresetImportCandidate(UserDisplayPreset Preset, PresetImportCollision Collision,
    IReadOnlyList<PresetImportFont> Fonts);

/// <summary>Strict, standalone display-preset file format. It never contains profile or font bytes.</summary>
public static class DisplayPresetFile
{
    public const int Version = 1;
    public const int MaximumBytes = 64 * 1024;
    public const string Extension = ".stwpreset";
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static byte[] Export(UserDisplayPreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(Document.From(preset), Options);
        if (bytes.Length > MaximumBytes) throw new InvalidDataException("프리셋 파일이 너무 큽니다.");
        return bytes;
    }

    public static UserDisplayPreset Import(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0) throw new InvalidDataException("파일이 비어 있습니다.");
        if (bytes.Length > MaximumBytes) throw new InvalidDataException("프리셋 파일이 너무 큽니다.");
        try
        {
            RejectDuplicateProperties(bytes);
            if (ReadVersion(bytes) != Version) throw new UnsupportedPresetFileVersionException();
            ValidateRequiredShape(bytes);
            var document = JsonSerializer.Deserialize<Document>(bytes, Options)
                ?? throw new JsonException("Missing document.");
            return document.Preset.ToValue();
        }
        catch (UnsupportedPresetFileVersionException) { throw; }
        catch (Exception error) when (error is JsonException or ArgumentException or FormatException or OverflowException or NullReferenceException)
        {
            throw new InvalidDataException("파일 내용이 올바르지 않습니다.", error);
        }
    }

    private static int ReadVersion(ReadOnlySpan<byte> bytes)
    {
        using var json = JsonDocument.Parse(bytes.ToArray());
        return json.RootElement.ValueKind == JsonValueKind.Object &&
            json.RootElement.TryGetProperty("presetFileVersion", out var version) &&
            version.TryGetInt32(out var value) ? value : throw new JsonException("Missing file version.");
    }

    private static void ValidateRequiredShape(ReadOnlySpan<byte> bytes)
    {
        using var json = JsonDocument.Parse(bytes.ToArray());
        Require(json.RootElement, "presetFileVersion", "preset");
        var preset = json.RootElement.GetProperty("preset");
        Require(preset, "id", "name", "settings");
        var settings = preset.GetProperty("settings");
        Require(settings, "layout", "time", "date", "weekday", "status", "use24Hour", "showSeconds", "showDate", "showWeekday", "showStatus");
        foreach (var element in new[] { "time", "date", "weekday", "status" })
        {
            var typography = settings.GetProperty(element);
            Require(typography, "font", "size", "weight", "style");
            Require(typography.GetProperty("font"), "source", "familyId", "family");
        }
    }
    private static void Require(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object || names.Any(name => !element.TryGetProperty(name, out _)))
            throw new JsonException("Missing required field.");
    }

    private static void RejectDuplicateProperties(ReadOnlySpan<byte> bytes)
    {
        var reader = new Utf8JsonReader(bytes, new JsonReaderOptions { CommentHandling = JsonCommentHandling.Disallow });
        var properties = new Stack<HashSet<string>>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject) properties.Push(new(StringComparer.Ordinal));
            else if (reader.TokenType == JsonTokenType.EndObject) properties.Pop();
            else if (reader.TokenType == JsonTokenType.PropertyName &&
                !properties.Peek().Add(reader.GetString()!))
                throw new JsonException("Duplicate property.");
        }
    }

    private sealed record Document(int PresetFileVersion, PresetDto Preset)
    {
        public static Document From(UserDisplayPreset value) => new(Version, PresetDto.From(value));
    }
    private sealed record PresetDto(string Id, string Name, SettingsDto Settings)
    {
        public static PresetDto From(UserDisplayPreset value) =>
            new(value.Id.ToString("D"), value.Name, SettingsDto.From(value.Display));
        public UserDisplayPreset ToValue()
        {
            if (!Guid.TryParseExact(Id, "D", out var id) || id == Guid.Empty)
                throw new JsonException("Invalid preset ID.");
            return new(id, Name, Settings.ToValue(DisplayPresetReference.User(id)));
        }
    }
    private sealed record FontDto(string Source, string FamilyId, string Family)
    {
        public static FontDto From(FontSelection value) => new(value.Source.ToString(), value.FamilyId, value.Family);
        public FontSelection ToValue() => new(Parse<FontSourceKind>(Source), Family, FamilyId);
    }
    private sealed record TypographyDto(FontDto Font, double Size, string Weight, string Style)
    {
        public static TypographyDto From(ElementTypography value) =>
            new(FontDto.From(value.Font), value.Size, value.Weight.ToString(), value.Style.ToString());
        public ElementTypography ToValue() => new(Font.ToValue(), Size, Parse<DisplayFontWeight>(Weight), Parse<DisplayFontStyle>(Style));
    }
    private sealed record SettingsDto(string Layout, TypographyDto Time, TypographyDto Date,
        TypographyDto Weekday, TypographyDto Status, bool Use24Hour, bool ShowSeconds,
        bool ShowDate, bool ShowWeekday, bool ShowStatus)
    {
        public static SettingsDto From(DisplayConfiguration value) => new(value.Layout.ToString(),
            TypographyDto.From(value.Time), TypographyDto.From(value.Date), TypographyDto.From(value.Weekday),
            TypographyDto.From(value.Status), value.Use24Hour, value.ShowSeconds, value.ShowDate,
            value.ShowWeekday, value.ShowStatus);
        public DisplayConfiguration ToValue(DisplayPresetReference preset)
        {
            var result = new DisplayConfiguration(preset, Parse<DisplayLayout>(Layout), Time.ToValue(),
                Date.ToValue(), Weekday.ToValue(), Status.ToValue(), Use24Hour, ShowSeconds,
                ShowDate, ShowWeekday, ShowStatus);
            result.Validate();
            return result;
        }
    }
    private static T Parse<T>(string value) where T : struct, Enum =>
        Enum.TryParse<T>(value, out var parsed) && Enum.IsDefined(parsed) && parsed.ToString() == value
            ? parsed : throw new JsonException("Invalid display value.");
}

public sealed class UnsupportedPresetFileVersionException : Exception;
