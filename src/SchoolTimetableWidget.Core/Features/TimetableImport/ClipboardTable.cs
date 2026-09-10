using System.Collections.ObjectModel;
using System.Text;

namespace SchoolTimetableWidget.Core.Features.TimetableImport;

/// <summary>Immutable rectangular text table. No platform clipboard dependency.</summary>
public sealed class ClipboardTable
{
    public ClipboardTable(IEnumerable<IEnumerable<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var copy = rows.Select(row => Array.AsReadOnly(row.Select(value =>
            value ?? throw new ArgumentException("Null is not a cell value.", nameof(rows))).ToArray())).ToArray();
        if (copy.Length == 0 || copy[0].Count == 0 || copy.Any(row => row.Count != copy[0].Count))
            throw new FormatException("표의 모든 행은 같은 수의 열을 가져야 합니다. 빈 마지막 열도 함께 복사해 주세요.");
        Rows = Array.AsReadOnly(copy);
    }

    public ReadOnlyCollection<ReadOnlyCollection<string>> Rows { get; }
    public int RowCount => Rows.Count;
    public int ColumnCount => Rows[0].Count;
    public string this[int row, int column] => Rows[row][column];

    public static ClipboardTable Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var rows = new List<string[]>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        var closedQuote = false;
        var fieldStart = true;
        var endedRow = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            endedRow = false;
            if (quoted)
            {
                if (ch != '"') field.Append(ch);
                else if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else { quoted = false; closedQuote = true; }
                continue;
            }
            if (ch is '\t' or '\r' or '\n')
            {
                row.Add(field.ToString());
                field.Clear();
                closedQuote = false;
                fieldStart = true;
                if (ch != '\t')
                {
                    if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    rows.Add(row.ToArray());
                    row.Clear();
                    endedRow = true;
                }
            }
            else if (ch == '"' && fieldStart) { quoted = true; fieldStart = false; }
            else
            {
                if (closedQuote || ch == '"')
                    throw new FormatException("따옴표 형식이 잘못되었습니다. 표 전체를 스프레드시트에서 다시 복사해 주세요.");
                field.Append(ch);
                fieldStart = false;
            }
        }
        if (quoted) throw new FormatException("닫히지 않은 따옴표가 있습니다.");
        // A final record separator terminates the last row; it does not create an extra row.
        if (!endedRow) { row.Add(field.ToString()); rows.Add(row.ToArray()); }
        return new ClipboardTable(rows);
    }

    public string ToTsv() => string.Join("\r\n", Rows.Select(row => string.Join("\t", row.Select(Escape))));

    private static string Escape(string value) => value.IndexOfAny(['\t', '\r', '\n', '"']) < 0
        ? value : "\"" + value.Replace("\"", "\"\"") + "\"";
}
