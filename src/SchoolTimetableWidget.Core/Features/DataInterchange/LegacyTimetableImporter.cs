using System.Globalization;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Core.Features.DataInterchange;

/// <summary>Reads raw legacy timetable/time JSON without importing legacy storage semantics.</summary>
public static class LegacyTimetableImporter
{
    private static readonly string[] Days = ["월", "화", "수", "목", "금"];
    private static readonly string[] Periods = Enumerable.Range(1, 7).Select(p => p.ToString(CultureInfo.InvariantCulture)).ToArray();

    public static LegacyImportCandidate Import(string timetableJson, string? scheduleJson = null)
    {
        using var timetableDocument = DataJson.Parse(timetableJson);
        var days = DataJson.Object(timetableDocument.RootElement, "timetable", Days);
        var reports = new List<LegacyImportReport>();
        var cells = new List<TimetableCell>();
        for (var dayIndex = 0; dayIndex < Days.Length; dayIndex++)
        {
            var day = Days[dayIndex];
            var values = days.TryGetValue(day, out var dayValue)
                ? DataJson.Object(dayValue, $"timetable.{day}", Periods) : [];
            for (var period = 1; period <= 7; period++)
            {
                var key = Periods[period - 1];
                var path = $"timetable.{day}.{key}";
                var text = "";
                if (values.TryGetValue(key, out var value)) text = DataJson.Text(value, path);
                else reports.Add(new("MissingCell", path, "누락된 셀을 빈 셀로 보충합니다."));
                cells.Add(new((SchoolDay)dayIndex, period, new(text, "")));
            }
        }

        using var scheduleDocument = DataJson.Parse(scheduleJson ?? "{}");
        var scheduleValues = DataJson.Object(scheduleDocument.RootElement, "schedule", Periods);
        var periods = new List<PeriodDefinition>();
        for (var period = 1; period <= 7; period++)
        {
            var key = Periods[period - 1];
            var path = $"schedule.{key}";
            if (!scheduleValues.TryGetValue(key, out var value))
            {
                periods.Add(DefaultPeriodSchedule.Periods[period - 1]);
                reports.Add(new("MissingPeriod", path, "누락된 교시를 기본 일과 시각으로 보충합니다."));
                continue;
            }
            var fields = DataJson.Object(value, path, "start", "end");
            var start = ReadTime(DataJson.Text(DataJson.Required(fields, "start", path), path + ".start"), path + ".start", reports);
            var end = ReadTime(DataJson.Text(DataJson.Required(fields, "end", path), path + ".end"), path + ".end", reports);
            if (start >= end) throw new FormatException($"{path}: 시작 시각은 종료 시각보다 빨라야 합니다.");
            periods.Add(new(period, start, end));
        }
        try { return new(new(cells), new(periods), reports); }
        catch (ArgumentException error) { throw new FormatException("교시 시각이 순서대로 이어지고 서로 겹치지 않아야 합니다.", error); }
    }

    private static TimeOnly ReadTime(string text, string path, List<LegacyImportReport> reports)
    {
        if (!TimeOnly.TryParseExact(text, ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            throw new FormatException($"{path}: H:mm 또는 HH:mm 형식의 시각이 필요합니다.");
        var normalized = time.ToString("HH:mm", CultureInfo.InvariantCulture);
        if (text != normalized) reports.Add(new("NormalizedTime", path, $"시각 '{text}'을 '{normalized}'으로 변환합니다."));
        return time;
    }
}
