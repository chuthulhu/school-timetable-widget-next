using SchoolTimetableWidget.Core.Features.TimetableImport;

namespace SchoolTimetableWidget.Desktop.Development;

/// <summary>Opt-in deterministic, anonymous import samples. Never writes the clipboard.</summary>
internal static class BulkImportPreviewData
{
    public static string Create(TimetableImportMode mode)
    {
        if (mode == TimetableImportMode.Canonical)
        {
            var rows = ClipboardTable.Parse(CanonicalTimetableImporter.CreateTemplate()).Rows.Select(row => row.ToArray()).ToArray();
            for (var period = 1; period <= 7; period++)
                for (var day = 0; day < 5; day++)
                {
                    rows[period][1 + day * 2] = period == 7 ? "" : "표준 교과";
                    rows[period][2 + day * 2] = period == 7 ? "" : $"{day + 1}-{period}";
                }
            rows[1][1] = "물리학Ⅱ\n실험";
            rows[2][3] = "  공백 유지  ";
            return new ClipboardTable(rows).ToTsv();
        }
        var school = Enumerable.Range(0, 6).Select(_ => Enumerable.Repeat("", 41).ToArray()).ToArray();
        school[0][0] = "번호";
        school[0][1] = "교사";
        school[0][2] = "구분";
        school[0][38] = "시수";
        school[0][39] = "담임";
        school[0][40] = "비고";
        var days = new[] { "월", "화", "수", "목", "금" };
        for (var day = 0; day < 5; day++)
        {
            school[0][3 + day * 7] = days[day];
            for (var period = 0; period < 7; period++) school[1][3 + day * 7 + period] = (period + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        for (var teacher = 0; teacher < 2; teacher++)
        {
            var row = 2 + teacher * 2;
            school[row][0] = (teacher + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            school[row][1] = teacher == 0 ? "샘플 교사 A" : "샘플 교사 B";
            school[row][2] = "교과";
            school[row + 1][2] = "반";
            for (var i = 0; i < 35; i++)
            {
                school[row][3 + i] = i % 7 == 6 ? "" : teacher == 0 ? "물리학Ⅱ" : "국어";
                school[row + 1][3 + i] = i % 7 == 6 ? "" : $"{teacher + 2}-{i % 5 + 1}";
            }
            school[row][3] += "\n실험";
            school[row][4] = "창체";
            school[row + 1][4] = "";
            school[row][5] = "";
            school[row + 1][5] = "반만 표시";
            school[row][10] = "<b>과목</b>";
            school[row][17] = "  앞뒤 공백  ";
        }
        return new ClipboardTable(school).ToTsv();
    }
}
