using System.Globalization;
using System.Text;
using System.Text.Json;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Core.Features.DataInterchange;

/// <summary>Strict .stwshare v1 codec. No I/O, profile state, or legacy format guessing.</summary>
public static class TimetableShareFile
{
    public const int MaximumBytes = DataJson.MaximumBytes;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static byte[] Export(TimetableDataPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        // Utf8JsonWriter replaces lone surrogates. Reject them before writing, preserving text semantics.
        try
        {
            long textBytes = 0;
            foreach (var cell in package.Timetable?.Cells ?? [])
            {
                textBytes += StrictUtf8.GetByteCount(cell.Value.SubjectText);
                textBytes += StrictUtf8.GetByteCount(cell.Value.ClassText);
                if (textBytes > MaximumBytes) throw new FormatException("공유 데이터는 UTF-8 기준 4 MiB 이하여야 합니다.");
            }
        }
        catch (EncoderFallbackException error) { throw new FormatException("공유할 문자열의 Unicode가 올바르지 않습니다.", error); }
        using var output = new MemoryStream();
        using (var writer = new Utf8JsonWriter(output))
        {
            writer.WriteStartObject();
            writer.WriteNumber("shareFileVersion", 1);
            if (package.Timetable is { } timetable)
            {
                writer.WriteStartArray("timetable");
                foreach (var cell in timetable.Cells)
                {
                    writer.WriteStartObject();
                    writer.WriteString("schoolDay", cell.Day.ToString());
                    writer.WriteNumber("periodNumber", cell.PeriodNumber);
                    writer.WriteString("subjectText", cell.Value.SubjectText);
                    writer.WriteString("classText", cell.Value.ClassText);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            if (package.Schedule is { } schedule)
            {
                writer.WriteStartArray("periodSchedule");
                foreach (var period in schedule.Periods)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("periodNumber", period.PeriodNumber);
                    writer.WriteString("start", period.Start.ToString(DataJson.TimeFormat, CultureInfo.InvariantCulture));
                    writer.WriteString("end", period.End.ToString(DataJson.TimeFormat, CultureInfo.InvariantCulture));
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }
        if (output.Length > MaximumBytes) throw new FormatException("공유 데이터는 UTF-8 기준 4 MiB 이하여야 합니다.");
        return output.ToArray();
    }

    public static TimetableDataPackage Import(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length > MaximumBytes) throw new FormatException("공유 파일은 4 MiB 이하여야 합니다.");
        try
        {
            var content = bytes.AsSpan();
            if (content.StartsWith(Encoding.UTF8.Preamble)) content = content[Encoding.UTF8.Preamble.Length..];
            using var document = DataJson.Parse(StrictUtf8.GetString(content));
            var fields = DataJson.Object(document.RootElement, "share", "shareFileVersion", "timetable", "periodSchedule");
            if (DataJson.Number(DataJson.Required(fields, "shareFileVersion", "share"), "share.shareFileVersion") != 1)
                throw new FormatException("지원하지 않는 공유 파일 버전입니다.");
            var timetable = fields.TryGetValue("timetable", out var week) ? ReadTimetable(week) : null;
            var schedule = fields.TryGetValue("periodSchedule", out var periods) ? ReadSchedule(periods) : null;
            return new(timetable, schedule);
        }
        catch (DecoderFallbackException error) { throw new FormatException("공유 파일은 올바른 UTF-8이어야 합니다.", error); }
        catch (ArgumentException error) { throw new FormatException("공유 데이터의 시간표 또는 일과 시각이 올바르지 않습니다.", error); }
    }

    private static WeeklyTimetable ReadTimetable(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != WeeklyTimetable.CellCount)
            throw new FormatException("공유 시간표는 완전한 35셀이 필요합니다.");
        var cells = new List<TimetableCell>();
        foreach (var entry in value.EnumerateArray())
        {
            var path = $"share.timetable[{cells.Count}]";
            var fields = DataJson.Object(entry, path, "schoolDay", "periodNumber", "subjectText", "classText");
            var dayText = DataJson.Text(DataJson.Required(fields, "schoolDay", path), path + ".schoolDay");
            if (!Enum.GetNames<SchoolDay>().Contains(dayText, StringComparer.Ordinal))
                throw new FormatException($"{path}.schoolDay: 지원하지 않는 요일입니다.");
            var day = Enum.Parse<SchoolDay>(dayText);
            var period = DataJson.Number(DataJson.Required(fields, "periodNumber", path), path + ".periodNumber");
            var subject = DataJson.Text(DataJson.Required(fields, "subjectText", path), path + ".subjectText");
            var classText = DataJson.Text(DataJson.Required(fields, "classText", path), path + ".classText");
            cells.Add(new(day, period, new(subject, classText)));
        }
        return new(cells);
    }

    private static PeriodSchedule ReadSchedule(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 7)
            throw new FormatException("공유 일과 시각은 완전한 7교시가 필요합니다.");
        var periods = new List<PeriodDefinition>();
        foreach (var entry in value.EnumerateArray())
        {
            var path = $"share.periodSchedule[{periods.Count}]";
            var fields = DataJson.Object(entry, path, "periodNumber", "start", "end");
            periods.Add(new(
                DataJson.Number(DataJson.Required(fields, "periodNumber", path), path + ".periodNumber"),
                DataJson.NativeTime(DataJson.Required(fields, "start", path), path + ".start"),
                DataJson.NativeTime(DataJson.Required(fields, "end", path), path + ".end")));
        }
        return new(periods);
    }
}
