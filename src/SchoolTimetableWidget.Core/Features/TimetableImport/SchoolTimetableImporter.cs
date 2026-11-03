using System.Collections.ObjectModel;
using System.Globalization;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Core.Features.TimetableImport;

/// <summary>Explicit structural recognizer, never infers rows from subject/class vocabulary.</summary>
public static class SchoolTimetableImporter
{
    private static readonly string[] Days = ["월", "화", "수", "목", "금"];

    public static ReadOnlyCollection<TimetableImportCandidate> Import(ClipboardTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        var signatures = new List<(int Row, int Column, int[] Lengths)>();
        for (var row = 0; row < table.RowCount; row++)
            for (var column = 0; column < table.ColumnCount; column++)
                if (DayLengths(table, row, column) is { } lengths) signatures.Add((row, column, lengths));
        if (signatures.Count != 1)
            throw new FormatException(signatures.Count == 0
                ? "월~금 다섯 요일의 교시 머리글(요일마다 1부터 이어지는 숫자, 최대 7교시)을 찾지 못했습니다. 머리글과 교과/반 행을 함께 복사해 주세요."
                : "교시 머리글 영역이 여러 개입니다. 시간표 영역 하나만 복사하거나 표준 양식을 사용해 주세요.");

        var (headerRow, start, dayLengths) = signatures[0];
        var offsets = new int[5];
        for (var day = 1; day < 5; day++) offsets[day] = offsets[day - 1] + dayLengths[day - 1];
        var width = offsets[4] + dayLengths[4];
        var weekdayEvidence = ValidateWeekdays(table, headerRow, start, offsets, dayLengths);
        var dataStart = headerRow + 1;
        var remaining = table.RowCount - dataStart;
        if (remaining == 0 || remaining % 2 != 0)
            throw new FormatException("교시 머리글 아래에 교과 행과 반 행을 두 행씩 함께 복사해 주세요. 마지막 반이 비어 있어도 행은 필요합니다.");

        var outside = Enumerable.Range(0, table.ColumnCount).Where(c => c < start || c >= start + width).ToArray();
        var identityColumns = outside.Where(c => Enumerable.Range(0, headerRow + 1)
            .Any(r => Header(table[r, c]) is "교사" or "교사명" or "성명" or "번호")).ToArray();
        var nameColumns = identityColumns.Where(c => Enumerable.Range(0, headerRow + 1)
            .Any(r => Header(table[r, c]) is "교사" or "교사명" or "성명")).ToArray();
        var candidates = new List<TimetableImportCandidate>();
        for (var row = dataStart; row < table.RowCount; row += 2)
        {
            var explicitPair = outside.Any(c => Header(table[row, c]) == "교과" && Header(table[row + 1, c]) == "반");
            var mergedIdentity = identityColumns.Any(c => Header(table[row, c]).Length > 0) &&
                identityColumns.All(c => Header(table[row + 1, c]).Length == 0);
            // Two isolated rows can be offered for explicit user confirmation, never silently accepted.
            // A second populated teacher identity contradicts the pair even in a two-row selection.
            if (!explicitPair && !mergedIdentity &&
                (remaining != 2 || identityColumns.Any(c => Header(table[row + 1, c]).Length > 0)))
                throw new FormatException($"행 {row + 1}–{row + 2}의 교과/반 대응이 불명확합니다. 교과/반 표시 또는 병합된 교사 정보가 있는 두 행씩 복사하거나 표준 양식을 사용해 주세요.");
            if (identityColumns.Any(c => Header(table[row + 1, c]).Length > 0))
                throw new FormatException($"행 {row + 2}에 별도 교사 정보가 있습니다. 서로 다른 교사를 교과/반으로 합칠 수 없습니다.");
            var name = nameColumns.Select(c => table[row, c]).FirstOrDefault(value => Header(value).Length > 0);
            var label = (name is null ? "" : name + " · ") + $"행 {row + 1}–{row + 2}";
            var evidence = $"열 {start + 1}–{start + width} · 위 행=교과 / 아래 행=반 · " + ShortDayNotice(dayLengths) +
                (weekdayEvidence ? "월~금 머리글 확인" : "요일 머리글 없음: 월~금 순서를 확인해 주세요") +
                (!explicitPair && !mergedIdentity ? " · 행 구분 표시 없음: 교과/반 순서를 확인해 주세요" : "");
            var subjectRow = row;
            var week = new WeeklyTimetable(
                from period in Enumerable.Range(1, 7)
                from day in Enum.GetValues<SchoolDay>()
                let column = start + offsets[(int)day] + period - 1
                // A period the source day does not have imports as an empty slot.
                select new TimetableCell(day, period, period > dayLengths[(int)day] ? new("", "")
                    : new(table[subjectRow, column], table[subjectRow + 1, column])));
            candidates.Add(new(label, evidence, week));
        }
        return candidates.AsReadOnly();
    }

    /// <summary>
    /// Five contiguous day groups starting at <paramref name="column"/>, each numbered 1, 2, … up to at most 7.
    /// A group ends where the next value is not the following number; the next group must restart at 1.
    /// </summary>
    private static int[]? DayLengths(ClipboardTable table, int row, int column)
    {
        // Starting inside a longer run (e.g. a Saturday-inclusive header) would misalign the days.
        if (column > 0 && Header(table[row, column - 1]) is "1" or "2" or "3" or "4" or "5" or "6" or "7") return null;
        var lengths = new int[5];
        for (var day = 0; day < 5; day++)
        {
            var length = 0;
            while (length < 7 && column < table.ColumnCount &&
                Header(table[row, column]) == (length + 1).ToString(CultureInfo.InvariantCulture)) { length++; column++; }
            if (length == 0) return null;
            lengths[day] = length;
        }
        // A sixth group or an 8th period continuing the run is not a Monday–Friday seven-period header.
        return column < table.ColumnCount && Header(table[row, column]) is "1" or "8" ? null : lengths;
    }

    private static string ShortDayNotice(int[] lengths)
    {
        var shortDays = Enumerable.Range(0, 5).Where(day => lengths[day] < 7)
            .Select(day => $"{Days[day]} {lengths[day]}교시까지").ToArray();
        return shortDays.Length == 0 ? "" : string.Join(", ", shortDays) + ": 이후 교시는 빈 칸 · ";
    }

    private static bool ValidateWeekdays(ClipboardTable table, int row, int start, int[] offsets, int[] lengths)
    {
        if (row == 0) return false;
        var width = offsets[4] + lengths[4];
        var values = Enumerable.Range(0, width).Select(c => Header(table[row - 1, start + c])).ToArray();
        if (!values.Any(value => Days.Contains(value) || Days.Any(day => value == day + "요일"))) return false;
        for (var day = 0; day < 5; day++)
            for (var period = 0; period < lengths[day]; period++)
            {
                var value = values[offsets[day] + period];
                if (value != Days[day] && value != Days[day] + "요일" && !(period > 0 && value.Length == 0))
                    throw new FormatException("요일 머리글이 월~금 순서 또는 요일별 교시 구간과 일치하지 않습니다.");
            }
        return true;
    }

    // Structure comparisons only. Values passed to TimetableCellValue are never normalized.
    private static string Header(string text) => text.Trim();
}
