using System.Runtime.InteropServices;
using System.Windows;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

/// <summary>Small STA Windows boundary; injectable without touching the user's clipboard.</summary>
public interface ISpreadsheetClipboard
{
    string ReadText();
    void WriteText(string text);
}

public sealed class WindowsSpreadsheetClipboard : ISpreadsheetClipboard
{
    public string ReadText() => Clipboard.GetText(TextDataFormat.UnicodeText);
    public void WriteText(string text) => Clipboard.SetText(text, TextDataFormat.UnicodeText);

    public static bool IsAccessFailure(Exception error) => error is ExternalException or System.Threading.ThreadStateException;
}
