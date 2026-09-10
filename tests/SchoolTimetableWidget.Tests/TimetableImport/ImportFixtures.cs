using System.Globalization;
using SchoolTimetableWidget.Core.Features.TimetableImport;

namespace SchoolTimetableWidget.Tests.TimetableImport;

internal static class ImportFixtures
{
    // Anonymous structural fixture, with arbitrary front/back metadata and merged day labels.
    public static string[][] School(int prefix = 3, int suffix = 3, int teachers = 2, bool weekday = true)
    {
        var table = Enumerable.Range(0, 2 + teachers * 2).Select(_ => Enumerable.Repeat("", prefix + 35 + suffix).ToArray()).ToArray();
        if (prefix > 0) table[0][0] = "교사";
        if (prefix > 1) table[0][1] = "번호";
        if (suffix > 0) table[0][prefix + 35] = "시수";
        for (var day = 0; day < 5; day++)
        {
            if (weekday) table[0][prefix + day * 7] = new[] { "월", "화", "수", "목", "금" }[day];
            for (var period = 0; period < 7; period++) table[1][prefix + day * 7 + period] = (period + 1).ToString(CultureInfo.InvariantCulture);
        }
        for (var teacher = 0; teacher < teachers; teacher++)
        {
            var row = 2 + teacher * 2;
            if (prefix > 0) table[row][0] = $"교사 {teacher + 1}";
            if (prefix > 1) table[row][1] = (teacher + 1).ToString(CultureInfo.InvariantCulture);
            for (var i = 0; i < 35; i++)
            {
                table[row][prefix + i] = $" 교과 {teacher}/{i} \n둘째";
                table[row + 1][prefix + i] = $" 반 {teacher}/{i} ";
            }
            if (suffix > 0) table[row][prefix + 35] = "metadata ignored";
        }
        return table;
    }

    public static string SchoolText(int teachers = 2) => new ClipboardTable(School(teachers: teachers)).ToTsv();
    public static string[][] Canonical() => ClipboardTable.Parse(CanonicalTimetableImporter.CreateTemplate()).Rows.Select(row => row.ToArray()).ToArray();
}
