using System.Collections.ObjectModel;
using System.Globalization;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Core.Features.TimetableImport;

public static class CanonicalTimetableImporter
{
    public static ReadOnlyCollection<string> Headers { get; } = Array.AsReadOnly(new[]
    {
        "교시", "월-교과", "월-반", "화-교과", "화-반", "수-교과", "수-반", "목-교과", "목-반", "금-교과", "금-반"
    });

    public static TimetableImportCandidate Import(ClipboardTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        if (table.RowCount != 8 || table.ColumnCount != 11)
            throw new FormatException("표준 양식은 머리글 포함 정확히 8행 × 11열이어야 합니다.");
        if (!table.Rows[0].SequenceEqual(Headers))
            throw new FormatException("표준 양식 머리글이 일치하지 않습니다. ‘표준 양식 복사’로 양식을 받아 주세요.");
        for (var period = 1; period <= 7; period++)
            if (table[period, 0] != period.ToString(CultureInfo.InvariantCulture))
                throw new FormatException("첫 열의 교시는 순서대로 1부터 7까지 정확히 한 번씩 있어야 합니다.");
        var week = new WeeklyTimetable(
            from period in Enumerable.Range(1, 7)
            from day in Enum.GetValues<SchoolDay>()
            select new TimetableCell(day, period,
                new(table[period, 1 + (int)day * 2], table[period, 2 + (int)day * 2])));
        return new("표준 양식 · 35셀", "8행 × 11열 · 월~금 / 1~7교시", week);
    }

    public static string CreateTemplate() => new ClipboardTable(
        new[] { Headers.ToArray() }.Concat(Enumerable.Range(1, 7).Select(period =>
            new[] { period.ToString(CultureInfo.InvariantCulture) }.Concat(Enumerable.Repeat("", 10))))).ToTsv();
}
