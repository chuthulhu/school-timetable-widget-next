using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SchoolTimetableWidget.Core.Features.DataInterchange;

/// <summary>Strict JSON primitives shared only by the data-interchange formats.</summary>
internal static class DataJson
{
    internal const int MaximumBytes = 4 * 1024 * 1024;
    internal const string TimeFormat = "HH:mm:ss.fffffff";
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal static JsonDocument Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            if (StrictUtf8.GetByteCount(json) > MaximumBytes)
                throw new FormatException("가져올 데이터는 UTF-8 기준 4 MiB 이하여야 합니다.");
        }
        catch (EncoderFallbackException error) { throw new FormatException("JSON 문자열의 Unicode가 올바르지 않습니다.", error); }
        try { return JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 }); }
        catch (JsonException error) { throw new FormatException("JSON 데이터의 형식이 올바르지 않습니다.", error); }
    }

    internal static Dictionary<string, JsonElement> Object(JsonElement element, string path, params string[] allowed)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new FormatException($"{path}: 객체가 필요합니다.");
        var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        try
        {
            foreach (var property in element.EnumerateObject())
            {
                if (!fields.TryAdd(property.Name, property.Value))
                    throw new FormatException($"{path}.{property.Name}: 중복 항목입니다.");
                if (!allowed.Contains(property.Name, StringComparer.Ordinal))
                    throw new FormatException($"{path}.{property.Name}: 지원하지 않는 항목입니다.");
            }
        }
        catch (InvalidOperationException error) { throw new FormatException($"{path}: JSON 항목의 Unicode가 올바르지 않습니다.", error); }
        return fields;
    }

    internal static JsonElement Required(Dictionary<string, JsonElement> fields, string name, string path) =>
        fields.TryGetValue(name, out var value) ? value : throw new FormatException($"{path}.{name}: 필수 항목이 없습니다.");

    internal static string Text(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.String) throw new FormatException($"{path}: 문자열이 필요합니다.");
        try { return value.GetString()!; }
        catch (InvalidOperationException error) { throw new FormatException($"{path}: 문자열의 Unicode가 올바르지 않습니다.", error); }
    }

    internal static int Number(JsonElement value, string path) => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
        ? number : throw new FormatException($"{path}: 정수가 필요합니다.");

    internal static TimeOnly NativeTime(JsonElement value, string path)
    {
        var text = Text(value, path);
        return TimeOnly.TryParseExact(text, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? time : throw new FormatException($"{path}: 교시 시각 형식이 올바르지 않습니다.");
    }
}
