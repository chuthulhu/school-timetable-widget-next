using System.Globalization;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Features.TimetableImport;

namespace SchoolTimetableWidget.Tests.TimetableImport;

public class ImporterTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(3, 3)]
    [InlineData(8, 5)]
    public void SchoolFindsAllSlotsAtDifferentColumnsAndKeepsMetadataOut(int prefix, int suffix)
    {
        var source = ImportFixtures.School(prefix, suffix);
        var candidates = SchoolTimetableImporter.Import(new(source));
        Assert.Equal(2, candidates.Count);
        for (var teacher = 0; teacher < 2; teacher++)
        {
            Assert.Contains($"교사 {teacher + 1}", candidates[teacher].Label);
            var week = candidates[teacher].Timetable;
            Assert.Equal(35, week.Cells.Count);
            for (var i = 0; i < 35; i++)
            {
                var cell = week[(SchoolDay)(i / 7), i % 7 + 1];
                Assert.Equal(source[2 + teacher * 2][prefix + i], cell.Value.SubjectText);
                Assert.Equal(source[3 + teacher * 2][prefix + i], cell.Value.ClassText);
            }
            Assert.Equal(Enumerable.Range(1, 7).SelectMany(p => Enum.GetValues<SchoolDay>().Select(d => (d, p))),
                week.Cells.Select(c => (c.Day, c.PeriodNumber)));
        }
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("창체", "")]
    [InlineData("", "반만")]
    [InlineData(" \t ", "  ")]
    [InlineData("  한글 Ω 🎵 e\u0301  ", " 반 \r\n둘째\r셋째\n ")]
    [InlineData("<b>과목</b>", "a\"b\tc")]
    public void SchoolPreservesEmptyRepeatedAndExactUserData(string subject, string classText)
    {
        var source = ImportFixtures.School(teachers: 1);
        for (var i = 0; i < 35; i++) { source[2][3 + i] = subject; source[3][3 + i] = classText; }
        var candidate = Assert.Single(SchoolTimetableImporter.Import(ClipboardTable.Parse(new ClipboardTable(source).ToTsv())));
        Assert.All(candidate.Timetable.Cells, cell => Assert.Equal(new(subject, classText), cell.Value));
        Assert.NotSame(candidate.Timetable.Cells[0], candidate.Timetable.Cells[1]);
    }

    [Fact]
    public void HeaderWhitespaceOnlyIsNormalized()
    {
        var source = ImportFixtures.School();
        source[0][3] = " 월 "; source[1][3] = " 1 ";
        var candidates = SchoolTimetableImporter.Import(new(source));
        Assert.Equal(source[2][3], candidates[0].Timetable[SchoolDay.Monday, 1].Value.SubjectText);
    }

    [Fact]
    public void MissingWeekdayEvidenceIsExplicitAndNotAnAutomaticRejection()
    {
        var candidates = SchoolTimetableImporter.Import(new(ImportFixtures.School(weekday: false)));
        Assert.All(candidates, c => Assert.Contains("요일 머리글 없음", c.Evidence));
    }

    [Fact]
    public void IsolatedBarePairHasExplicitRowAndWeekdayWarnings()
    {
        var source = ImportFixtures.School(0, 0, 1, false).Skip(1);
        var candidate = Assert.Single(SchoolTimetableImporter.Import(new(source)));
        Assert.Equal("행 2–3", candidate.Label);
        Assert.Contains("행 구분 표시 없음", candidate.Evidence);
        Assert.Contains("요일 머리글 없음", candidate.Evidence);
    }

    [Fact]
    public void ExplicitPairMarkersWorkWithoutTeacherNames()
    {
        var source = ImportFixtures.School();
        source[0][0] = source[0][1] = "";
        source[2][0] = source[4][0] = "교과";
        source[3][0] = source[5][0] = "반";
        var candidates = SchoolTimetableImporter.Import(new(source));
        Assert.Equal(2, candidates.Count);
        Assert.Equal("행 3–4", candidates[0].Label);
    }

    [Fact]
    public void RepeatedDayLabelsAndLeadingTitleRowsAreSupported()
    {
        var source = ImportFixtures.School();
        for (var day = 0; day < 5; day++)
            for (var p = 0; p < 7; p++) source[0][3 + day * 7 + p] = new[] { "월요일", "화요일", "수요일", "목요일", "금요일" }[day];
        var title = Enumerable.Repeat("", source[0].Length).ToArray(); title[0] = "익명 시간표";
        var candidates = SchoolTimetableImporter.Import(new(new[] { title }.Concat(source)));
        Assert.Contains("행 4–5", candidates[0].Label);
    }

    // Shape of a real user's copy: no weekday row, Wednesday ends at period 6, multi-line quoted subjects.
    private static string ShortWednesdayText(bool weekdayRow = false)
    {
        int[] lengths = [7, 7, 6, 7, 7];
        var header = lengths.SelectMany(n => Enumerable.Range(1, n).Select(p => p.ToString(CultureInfo.InvariantCulture))).ToArray();
        var subjects = Enumerable.Range(0, header.Length).Select(i => $"\"통합\n과학{i} \"").ToArray();
        var classes = Enumerable.Range(0, header.Length).Select(i => $"{i}-9 ").ToArray();
        var rows = new List<string[]> { header, subjects, classes };
        if (weekdayRow)
            rows.Insert(0, lengths.SelectMany((n, day) => Enumerable.Range(0, n)
                .Select(p => p == 0 ? new[] { "월", "화", "수", "목", "금" }[day] : "")).ToArray());
        return string.Join("\r\n", rows.Select(row => string.Join("\t", row))) + "\r\n";
    }

    [Fact]
    public void ShortDayHeaderImportsMissingPeriodsAsEmptySlots()
    {
        var candidate = Assert.Single(SchoolTimetableImporter.Import(ClipboardTable.Parse(ShortWednesdayText())));
        Assert.Contains("수 6교시까지", candidate.Evidence);
        Assert.Contains("요일 머리글 없음", candidate.Evidence);
        Assert.Contains("열 1–34", candidate.Evidence);
        TimetableCellValue Value(SchoolDay day, int period) => candidate.Timetable[day, period].Value;
        Assert.Equal(new TimetableCellValue("통합\n과학0 ", "0-9 "), Value(SchoolDay.Monday, 1));
        Assert.Equal(new TimetableCellValue("통합\n과학19 ", "19-9 "), Value(SchoolDay.Wednesday, 6));
        Assert.Equal(new TimetableCellValue("", ""), Value(SchoolDay.Wednesday, 7));
        Assert.Equal(new TimetableCellValue("통합\n과학20 ", "20-9 "), Value(SchoolDay.Thursday, 1));
        Assert.Equal(new TimetableCellValue("통합\n과학33 ", "33-9 "), Value(SchoolDay.Friday, 7));
        Assert.Equal(35, candidate.Timetable.Cells.Count);
    }

    [Fact]
    public void WeekdayLabelsFollowShortDayGroups()
    {
        var candidate = Assert.Single(SchoolTimetableImporter.Import(ClipboardTable.Parse(ShortWednesdayText(weekdayRow: true))));
        Assert.Contains("월~금 머리글 확인", candidate.Evidence);
        var shifted = ClipboardTable.Parse(ShortWednesdayText(weekdayRow: true)).Rows.Select(r => r.ToArray()).ToArray();
        (shifted[0][20], shifted[0][19]) = ("", "목");
        Assert.Throws<FormatException>(() => SchoolTimetableImporter.Import(new(shifted)));
    }

    [Theory]
    [InlineData("1\t2\t3\t4\t5\t6\t7\t1\t2\t3\t4\t5\t6\t7\t1\t2\t3\t4\t5\t6\t7\t1\t2\t3\t4\t5\t6\t7\t1\t2\t3\t4\t5\t6\t7\t1\t2\t3")]
    [InlineData("1\t2\t3\t4\t5\t6\t7\t1\t2\t3\t4\t5\t6\t7\t1\t2\t3\t4\t5\t6\t7\t1\t2\t3\t4\t5\t6\t7\t1\t2\t3\t4\t5\t6\t7\t8")]
    [InlineData("1\t2\t3\t4\t5\t6\t7\t1\t2\t3\t4\t5\t6\t7\t1\t2\t3\t4\t5\t6\t7\t1\t2\t3\t4\t5\t6\t7")]
    public void SixthDayEighthPeriodOrFourDaysAreNotAMondayToFridayHeader(string header)
    {
        var width = header.Split('\t').Length;
        var body = string.Join("\t", Enumerable.Repeat("과목", width)) + "\r\n" + string.Join("\t", Enumerable.Repeat("1-1", width));
        Assert.Throws<FormatException>(() => SchoolTimetableImporter.Import(ClipboardTable.Parse(header + "\r\n" + body)));
    }

    [Theory]
    [InlineData("period")]
    [InlineData("day")]
    [InlineData("odd")]
    [InlineData("two teachers single rows")]
    [InlineData("ambiguous pairs")]
    [InlineData("repeated header")]
    [InlineData("second region")]
    public void MalformedOrAmbiguousSchoolStructureRejectsEntireInput(string defect)
    {
        var source = ImportFixtures.School();
        switch (defect)
        {
            case "period": source[1][13] = "6"; break;
            case "day": source[0][10] = "금"; break;
            case "odd": source = source[..^1]; break;
            case "two teachers single rows": source[3][0] = "별도 교사"; source = source[..4]; break;
            case "ambiguous pairs": source[0][0] = source[0][1] = ""; break;
            case "repeated header": source = source.Concat(source).ToArray(); break;
            case "second region": source = source.Select(row => row.Concat(row).ToArray()).ToArray(); break;
        }
        Assert.Throws<FormatException>(() => SchoolTimetableImporter.Import(new(source)));
    }

    [Fact]
    public void NumericDataAndSubjectNamesAreNotStructuralEvidence()
    {
        Assert.Throws<FormatException>(() => SchoolTimetableImporter.Import(ClipboardTable.Parse("교사\t물리\t3-5\n교사2\t국어\t1-1")));
        var source = ImportFixtures.School(0, 0, 2);
        Assert.Throws<FormatException>(() => SchoolTimetableImporter.Import(new(source)));
    }

    [Fact]
    public void EmptyClassRowRemainsRequiredAndMayBeCompletelyEmpty()
    {
        var source = ImportFixtures.School(teachers: 1);
        Array.Fill(source[3], "");
        Assert.All(Assert.Single(SchoolTimetableImporter.Import(ClipboardTable.Parse(new ClipboardTable(source).ToTsv()))).Timetable.Cells,
            c => Assert.Equal("", c.Value.ClassText));
    }

    [Fact]
    public void CanonicalTemplateIsExactlyEightByElevenAndRoundTripsEmptyWeek()
    {
        var table = ClipboardTable.Parse(CanonicalTimetableImporter.CreateTemplate());
        Assert.Equal(8, table.RowCount); Assert.Equal(11, table.ColumnCount);
        Assert.Equal(CanonicalTimetableImporter.Headers, table.Rows[0]);
        Assert.All(CanonicalTimetableImporter.Import(table).Timetable.Cells, c => Assert.Equal(new("", ""), c.Value));
    }

    [Fact]
    public void CanonicalMapsEveryFieldAndPreservesQuotedContent()
    {
        var source = ImportFixtures.Canonical();
        for (var p = 1; p <= 7; p++)
            for (var d = 0; d < 5; d++)
            {
                source[p][1 + d * 2] = $"  한글 {d}/{p}\n\"교과\"\t ";
                source[p][2 + d * 2] = p == 7 ? "" : " \r\n반\r ";
            }
        var week = CanonicalTimetableImporter.Import(ClipboardTable.Parse(new ClipboardTable(source).ToTsv())).Timetable;
        Assert.Equal(35, week.Cells.Count);
        foreach (var cell in week.Cells)
        {
            Assert.Equal(source[cell.PeriodNumber][1 + (int)cell.Day * 2], cell.Value.SubjectText);
            Assert.Equal(source[cell.PeriodNumber][2 + (int)cell.Day * 2], cell.Value.ClassText);
        }
    }

    [Theory]
    [InlineData("header")]
    [InlineData("header space")]
    [InlineData("missing row")]
    [InlineData("duplicate")]
    [InlineData("order")]
    [InlineData("period space")]
    [InlineData("extra row")]
    [InlineData("extra column")]
    public void CanonicalRejectsAnyUnexpectedStructure(string defect)
    {
        var source = ImportFixtures.Canonical();
        switch (defect)
        {
            case "header": source[0][1] = "월-과목"; break;
            case "header space": source[0][1] += " "; break;
            case "missing row": source = source[..^1]; break;
            case "duplicate": source[2][0] = "1"; break;
            case "order": source[1][0] = "2"; source[2][0] = "1"; break;
            case "period space": source[1][0] = " 1 "; break;
            case "extra row": source = source.Concat(new[] { source[7] }).ToArray(); break;
            case "extra column": source = source.Select(row => row.Concat(new[] { "" }).ToArray()).ToArray(); break;
        }
        Assert.Throws<FormatException>(() => CanonicalTimetableImporter.Import(new(source)));
    }

    [Fact]
    public void ModesNeverAutoFallbackToEachOther()
    {
        Assert.Throws<FormatException>(() => SchoolTimetableImporter.Import(new(ImportFixtures.Canonical())));
        Assert.Throws<FormatException>(() => CanonicalTimetableImporter.Import(new(ImportFixtures.School())));
    }
}
