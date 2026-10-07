using System.Text.Json;
using SchoolTimetableWidget.Core.Features.DataInterchange;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Tests.DataInterchange;

public class LegacyImportTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" \t ")]
    [InlineData(" 물리학\r\n3-2\n ")]
    [InlineData("😀 e\u0301 <b>실험</b> {Binding X}")]
    public void OriginalStringBecomesSubjectWithoutParsingOrTrimming(string text)
    {
        var json = JsonSerializer.Serialize(new Dictionary<string, Dictionary<string, string>>
            { ["월"] = new() { ["1"] = text, ["2"] = text } });
        var result = LegacyTimetableImporter.Import(json);
        Assert.Equal(new(text, ""), result.Timetable[SchoolDay.Monday, 1].Value);
        Assert.Equal(new(text, ""), result.Timetable[SchoolDay.Monday, 2].Value);
        Assert.Equal(35, result.Timetable.Cells.Count);
        Assert.NotSame(result.Timetable[SchoolDay.Monday, 1], result.Timetable[SchoolDay.Monday, 2]);
        Assert.Equal(33, result.Reports.Count(r => r.Code == "MissingCell"));
        Assert.Equal(7, result.Reports.Count(r => r.Code == "MissingPeriod"));
        Assert.All(result.Timetable.Cells.Where(c => c.Day != SchoolDay.Monday || c.PeriodNumber > 2),
            c => Assert.Equal(new("", ""), c.Value));
    }

    [Fact]
    public void AllDefaultsAreExplicitlyReportedForEmptyRawObjects()
    {
        var result = LegacyTimetableImporter.Import("{}", "{}");
        Assert.Equal(35, result.Reports.Count(r => r.Code == "MissingCell"));
        Assert.Contains(result.Reports, r => r.Code == "MissingCell" && r.Path == "timetable.월.1");
        Assert.Equal(7, result.Reports.Count(r => r.Code == "MissingPeriod"));
        Assert.Equal(DefaultPeriodSchedule.Periods.Select(p => (p.Start, p.End)),
            result.Schedule.Periods.Select(p => (p.Start, p.End)));
        Assert.Throws<NotSupportedException>(() => ((IList<LegacyImportReport>)result.Reports).Clear());
    }

    [Fact]
    public void LegacySingleDigitHourIsNormalizedAndMissingPeriodsUseApprovedDefaults()
    {
        var result = LegacyTimetableImporter.Import("{}", """
            {"1":{"start":"9:00","end":"09:50"},"5":{"start":"13:00","end":"13:50"}}
            """);
        Assert.Equal(new TimeOnly(9, 0), result.Schedule.Periods[0].Start);
        Assert.Equal(new TimeOnly(13, 0), result.Schedule.Periods[4].Start);
        var normalized = Assert.Single(result.Reports, r => r.Code == "NormalizedTime");
        Assert.Equal("schedule.1.start", normalized.Path);
        Assert.Contains("09:00", normalized.Message);
        Assert.Equal(5, result.Reports.Count(r => r.Code == "MissingPeriod"));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"timetable\":{}}")]
    [InlineData("{\"토\":{\"1\":\"국어\"}}")]
    [InlineData("{\"월\":null}")]
    [InlineData("{\"월\":[]}")]
    [InlineData("{\"월\":{\"1\":null}}")]
    [InlineData("{\"월\":{\"1\":123}}")]
    [InlineData("{\"월\":{\"1\":\"\\uD800\"}}")]
    [InlineData("{\"월\":{\"8\":\"국어\"}}")]
    [InlineData("{\"월\":{\"01\":\"국어\"}}")]
    [InlineData("{\"월\":{},\"월\":{}}")]
    [InlineData("{\"월\":{\"1\":\"a\",\"1\":\"a\"}}")]
    [InlineData("{\"월\":{\"1\":\"a\",}}")]
    [InlineData("{/*comment*/}")]
    [InlineData("{")]
    public void InvalidRawTimetableCannotBecomeAnEmptySuccessfulCandidate(string json) =>
        Assert.Throws<FormatException>(() => LegacyTimetableImporter.Import(json));

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"8\":{\"start\":\"17:00\",\"end\":\"17:50\"}}")]
    [InlineData("{\"1\":{\"start\":\"09:00\"}}")]
    [InlineData("{\"1\":{\"start\":null,\"end\":\"09:50\"}}")]
    [InlineData("{\"1\":{\"start\":\"09:00:00\",\"end\":\"09:50\"}}")]
    [InlineData("{\"1\":{\"start\":\" 9:00\",\"end\":\"09:50\"}}")]
    [InlineData("{\"1\":{\"start\":\"24:00\",\"end\":\"09:50\"}}")]
    [InlineData("{\"1\":{\"start\":\"09:50\",\"end\":\"09:50\"}}")]
    [InlineData("{\"1\":{\"start\":\"10:00\",\"end\":\"09:50\"}}")]
    [InlineData("{\"1\":{\"start\":\"09:00\",\"end\":\"10:30\"}}")]
    [InlineData("{\"1\":{\"start\":\"09:00\",\"start\":\"09:00\",\"end\":\"09:50\"}}")]
    [InlineData("{\"1\":{},\"1\":{}}")]
    [InlineData("{\"1\":{\"start\":\"09:00\",\"end\":\"09:50\",\"extra\":true}}")]
    public void InvalidScheduleRejectsTheEntireOtherwiseValidTimetable(string json) =>
        Assert.Throws<FormatException>(() => LegacyTimetableImporter.Import("{\"월\":{\"1\":\"국어\"}}", json));

    [Fact]
    public void InputBoundUsesUtf8BytesRatherThanCharacterCount()
    {
        var json = JsonSerializer.Serialize(new { 월 = new Dictionary<string, string> { ["1"] = new('가', 1_500_000) } },
            new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        Assert.True(json.Length < 4 * 1024 * 1024);
        Assert.Throws<FormatException>(() => LegacyTimetableImporter.Import(json));
    }
}
