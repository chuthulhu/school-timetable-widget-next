using SchoolTimetableWidget.Core.Features.TimetableImport;

namespace SchoolTimetableWidget.Tests.TimetableImport;

public class ClipboardTableTests
{
    [Theory]
    [InlineData("a\tb\nc\td")]
    [InlineData("a\tb\r\nc\td")]
    [InlineData("a\tb\rc\td")]
    [InlineData("a\tb\r\nc\td\r\n")]
    public void RectangularRowsAndOptionalFinalTerminator(string text)
    {
        var table = ClipboardTable.Parse(text);
        Assert.Equal(2, table.RowCount);
        Assert.Equal(2, table.ColumnCount);
        Assert.Equal("a", table[0, 0]);
        Assert.Equal("d", table[1, 1]);
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData("\t", 2)]
    [InlineData("\t\t", 3)]
    [InlineData("\"\"\t\"\"", 2)]
    public void EmptyFieldsAreData(string text, int columns)
    {
        var table = ClipboardTable.Parse(text);
        Assert.Equal(columns, table.ColumnCount);
        Assert.All(table.Rows[0], value => Assert.Equal("", value));
    }

    [Theory]
    [InlineData("a\t\t\r\nb\t\t")]
    [InlineData("a\t\t\nb\t\t\n")]
    public void TrailingEmptyColumnsPreserved(string text)
    {
        var table = ClipboardTable.Parse(text);
        Assert.Equal(2, table.RowCount);
        Assert.Equal(3, table.ColumnCount);
        Assert.Equal("", table[1, 2]);
    }

    [Theory]
    [InlineData("\"교과\"", "교과")]
    [InlineData("\"a\"\"b\"", "a\"b")]
    [InlineData("\"한글\n둘째\"", "한글\n둘째")]
    [InlineData("\"\r\n한글\r둘째\n\"", "\r\n한글\r둘째\n")]
    [InlineData("\"a\tb\"", "a\tb")]
    [InlineData("  한글 Ω 🎵 e\u0301  ", "  한글 Ω 🎵 e\u0301  ")]
    [InlineData("\" \t \"", " \t ")]
    [InlineData("<b>과목</b>", "<b>과목</b>")]
    public void ExactDecodedCellValue(string text, string expected) => Assert.Equal(expected, ClipboardTable.Parse(text)[0, 0]);

    [Theory]
    [InlineData("\"unterminated")]
    [InlineData("\"closed\"junk")]
    [InlineData("\"closed\" ")]
    [InlineData("un\"quoted")]
    [InlineData(" \"quote\"")]
    [InlineData("\"\"\"")]
    [InlineData("a\tb\nc")]
    [InlineData("a\tb\n\n")]
    public void InvalidInputRejects(string text) => Assert.Throws<FormatException>(() => ClipboardTable.Parse(text));

    [Fact]
    public void WriterRoundTripsEveryTextIncludingQuotesAndTrailingFields()
    {
        string[] values = ["", "\"", "\t", "\n", "\r\n", "\r", "  교과  ", "한글 🧪 e\u0301", "<b>과목</b>", "   "];
        var table = new ClipboardTable(new[] { values, values.Reverse().ToArray() });
        var copy = ClipboardTable.Parse(table.ToTsv());
        Assert.Equal(table.RowCount, copy.RowCount);
        for (var row = 0; row < table.RowCount; row++) Assert.Equal(table.Rows[row], copy.Rows[row]);
        values[0] = "mutated";
        Assert.Equal("", table[0, 0]);
    }

    [Fact]
    public void EmptyTrailingRecordIsNotSilentlyDropped()
    {
        var table = ClipboardTable.Parse("a\n\n");
        Assert.Equal(2, table.RowCount);
        Assert.Equal("", table[1, 0]);
    }
}
