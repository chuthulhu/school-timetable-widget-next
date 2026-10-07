using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using SchoolTimetableWidget.Core.Features.DataInterchange;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Semesters;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Tests.DataInterchange;

public class TimetableSharingTests
{
    internal static WeeklyTimetable Week(string subject = " 물리\r\n😀 e\u0301 ", string classText = "3-2") =>
        new(WeeklyTimetable.Empty().Cells.Select(c => new TimetableCell(c.Day, c.PeriodNumber, new(subject, classText))));
    internal static PeriodSchedule Schedule() => new(DefaultPeriodSchedule.Periods.Select(p =>
        new PeriodDefinition(p.PeriodNumber, p.Start.Add(TimeSpan.FromTicks(123)), p.End.Add(TimeSpan.FromTicks(456)))));

    [Theory]
    [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void SelectedComponentsRoundTripExactlyIncludingWhitespaceUnicodeAndSubsecondTicks(bool timetable, bool schedule)
    {
        var original = new TimetableDataPackage(timetable ? Week() : null, schedule ? Schedule() : null);
        var bytes = TimetableShareFile.Export(original);
        var copy = TimetableShareFile.Import(bytes);
        Assert.Equal(timetable, copy.Timetable is not null);
        Assert.Equal(schedule, copy.Schedule is not null);
        if (timetable) Assert.Equal(original.Timetable!.Cells.Select(c => c.Value), copy.Timetable!.Cells.Select(c => c.Value));
        if (schedule) Assert.Equal(original.Schedule!.Periods.Select(p => (p.Start, p.End)), copy.Schedule!.Periods.Select(p => (p.Start, p.End)));
        Assert.Equal(bytes, TimetableShareFile.Export(copy));
        Assert.False(bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble));
        Assert.Equal(1, (int)JsonNode.Parse(bytes)!["shareFileVersion"]!);
    }

    [Theory]
    [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void SelectionReplacesOnlyBaseComponentsAndRetainsIdentityAndDateOverrides(bool timetable, bool schedule)
    {
        var oldWeek = Week("原본", "1-1");
        var oldSchedule = new PeriodSchedule(DefaultPeriodSchedule.Periods);
        var date = new DateOnly(2026, 9, 7);
        var dateOverride = new DateSpecificOverride(date, DayTimetable.FromBase(oldWeek, SchoolDay.Monday), oldSchedule);
        var target = new SemesterSet(Guid.NewGuid(), "2026 2학기", oldWeek, oldSchedule, [dateOverride]);
        var package = new TimetableDataPackage(Week(), Schedule());
        var next = package.ApplyTo(target, timetable, schedule);
        Assert.Equal(target.SemesterId, next.SemesterId);
        Assert.Equal(target.DisplayName, next.DisplayName);
        Assert.Same(dateOverride, Assert.Single(next.Overrides));
        Assert.Same(timetable ? package.Timetable : oldWeek, next.Timetable);
        Assert.Same(schedule ? package.Schedule : oldSchedule, next.Schedule);
        Assert.Same(oldWeek, target.Timetable);
        Assert.Same(oldSchedule, target.Schedule);
    }

    [Fact]
    public void MissingOrEmptySelectionIsRejected()
    {
        Assert.Throws<ArgumentException>(() => new TimetableDataPackage(null, null));
        var target = new SemesterSet(Guid.NewGuid(), "학기", Week(), Schedule(), []);
        Assert.Throws<ArgumentException>(() => new TimetableDataPackage(Week(), null).ApplyTo(target, false, false));
        Assert.Throws<ArgumentException>(() => new TimetableDataPackage(Week(), null).ApplyTo(target, false, true));
        Assert.Throws<ArgumentException>(() => new TimetableDataPackage(null, Schedule()).ApplyTo(target, true, false));
    }

    [Theory]
    [InlineData("ko-KR")] [InlineData("ar-SA")] [InlineData("en-US")]
    public void SerializedTimesAndNumbersAreInvariant(string culture)
    {
        var prior = CultureInfo.CurrentCulture;
        var package = new TimetableDataPackage(Week(), Schedule());
        var baseline = TimetableShareFile.Export(package);
        try
        {
            CultureInfo.CurrentCulture = new(culture);
            Assert.Equal(baseline, TimetableShareFile.Export(package));
            Assert.Equal(baseline, TimetableShareFile.Export(TimetableShareFile.Import(baseline)));
        }
        finally { CultureInfo.CurrentCulture = prior; }
    }

    [Theory]
    [InlineData("null")] [InlineData("[]")] [InlineData("{}")]
    [InlineData("{\"shareFileVersion\":1}")]
    [InlineData("{\"shareFileVersion\":2,\"timetable\":[]}")]
    [InlineData("{\"shareFileVersion\":\"1\",\"timetable\":[]}")]
    [InlineData("{\"shareFileVersion\":1,\"shareFileVersion\":1,\"timetable\":[]}")]
    [InlineData("{\"shareFileVersion\":1,\"timetable\":null}")]
    [InlineData("{\"shareFileVersion\":1,\"timetable\":[]}")]
    [InlineData("{\"shareFileVersion\":1,\"periodSchedule\":[]}")]
    [InlineData("{\"shareFileVersion\":1,\"timetable\":{},\"extra\":true}")]
    [InlineData("{\"월\":{\"1\":\"국어\"}}")]
    [InlineData("{\"schemaVersion\":5,\"profile\":{}}")]
    public void InvalidEnvelopeIsNotMistakenForOtherFormats(string json) =>
        Assert.Throws<FormatException>(() => TimetableShareFile.Import(Encoding.UTF8.GetBytes(json)));

    public static IEnumerable<object[]> InvalidComponents()
    {
        (string Name, Action<JsonNode> Mutate)[] cases =
        [
            ("missing cell", p => p["timetable"]!.AsArray().RemoveAt(0)),
            ("duplicate cell", p => p["timetable"]![1] = p["timetable"]![0]!.DeepClone()),
            ("numeric weekday", p => p["timetable"]![0]!["schoolDay"] = "0"),
            ("unknown weekday", p => p["timetable"]![0]!["schoolDay"] = "Saturday"),
            ("wrong weekday type", p => p["timetable"]![0]!["schoolDay"] = 0),
            ("wrong period", p => p["timetable"]![0]!["periodNumber"] = 8),
            ("null text", p => p["timetable"]![0]!["subjectText"] = null),
            ("missing class", p => p["timetable"]![0]!.AsObject().Remove("classText")),
            ("extra cell field", p => p["timetable"]![0]!["span"] = 2),
            ("null cell", p => p["timetable"]![0] = null),
            ("schedule reversed", p => p["periodSchedule"]![0]!["start"] = "10:00:00.0000000"),
            ("schedule overlaps", p => p["periodSchedule"]![0]!["end"] = "10:30:00.0000000"),
            ("schedule partial", p => p["periodSchedule"]!.AsArray().RemoveAt(0)),
            ("schedule unordered", p => p["periodSchedule"]![0]!["periodNumber"] = 2),
            ("schedule time format", p => p["periodSchedule"]![0]!["start"] = "9:00"),
            ("schedule missing end", p => p["periodSchedule"]![0]!.AsObject().Remove("end")),
            ("null schedule", p => p["periodSchedule"] = null),
            ("schedule extra field", p => p["periodSchedule"]![0]!["label"] = "새 이름")
        ];
        foreach (var (name, mutate) in cases)
        {
            var node = JsonNode.Parse(TimetableShareFile.Export(new(Week(), Schedule())))!;
            mutate(node);
            yield return [name, Encoding.UTF8.GetBytes(node.ToJsonString())];
        }
    }

    [Theory]
    [MemberData(nameof(InvalidComponents))]
    public void EitherInvalidComponentRejectsTheCompleteFile(string name, byte[] bytes)
    {
        Assert.NotEmpty(name);
        Assert.Throws<FormatException>(() => TimetableShareFile.Import(bytes));
    }

    [Theory]
    [InlineData("\"subjectText\":\"x\",\"subjectText\":\"x\"")]
    [InlineData("\"periodNumber\":1,\"periodNumber\":1")]
    public void DuplicateCellFieldsAreRejectedEvenWhenIdentical(string duplicate)
    {
        var json = Encoding.UTF8.GetString(TimetableShareFile.Export(new(Week("x"), null)));
        var original = duplicate.StartsWith("\"subjectText") ? "\"subjectText\":\"x\"" : "\"periodNumber\":1";
        Assert.Throws<FormatException>(() => TimetableShareFile.Import(Encoding.UTF8.GetBytes(json.Replace(original, duplicate))));
    }

    [Fact]
    public void InvalidUtf8AndOversizedInputAreRejectedAndBomIsAccepted()
    {
        var bytes = TimetableShareFile.Export(new(Week(), null));
        var withBom = Encoding.UTF8.Preamble.ToArray().Concat(bytes).ToArray();
        Assert.Equal(bytes, TimetableShareFile.Export(TimetableShareFile.Import(withBom)));
        Assert.Throws<FormatException>(() => TimetableShareFile.Import([0x7B, 0x22, 0xFF, 0x22, 0x7D]));
        Assert.Throws<FormatException>(() => TimetableShareFile.Import(new byte[4 * 1024 * 1024 + 1]));
    }

    [Fact]
    public void ExportCannotCreateAFileThatImportRejectsForSize() =>
        Assert.Throws<FormatException>(() => TimetableShareFile.Export(new(Week(new string('x', 130_000)), null)));

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void ExportRejectsInvalidUnicodeInsteadOfSilentlyChangingOriginalText(bool subjectField)
    {
        // Construct at runtime: attribute metadata/test discovery can normalize invalid UTF-16.
        var invalid = new string((char)(subjectField ? 0xd800 : 0xdfff), 1);
        Assert.Throws<FormatException>(() => TimetableShareFile.Export(new(
            Week(subjectField ? invalid : "subject", subjectField ? "class" : invalid), null)));
    }
}
