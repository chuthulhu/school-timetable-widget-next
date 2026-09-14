using System.IO;
using Microsoft.Win32;

namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

public interface IPresetFileDialogs
{
    string? ChooseExportPath(string suggestedFileName);
    string? ChooseImportPath();
}

public sealed class WindowsPresetFileDialogs : IPresetFileDialogs
{
    private const string Filter = "시간표 위젯 프리셋 (*.stwpreset)|*.stwpreset";
    internal static SaveFileDialog CreateExportDialog(string suggestedFileName) => new()
    {
        Title = "프리셋 내보내기", Filter = Filter, DefaultExt = DisplayPresetFile.Extension,
        AddExtension = true, FileName = suggestedFileName, OverwritePrompt = true
    };
    internal static OpenFileDialog CreateImportDialog() => new()
    {
        Title = "프리셋 가져오기", Filter = Filter, DefaultExt = DisplayPresetFile.Extension,
        Multiselect = false, CheckFileExists = true
    };
    public string? ChooseExportPath(string suggestedFileName)
    {
        var dialog = CreateExportDialog(suggestedFileName);
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
    public string? ChooseImportPath()
    {
        var dialog = CreateImportDialog();
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}

public static class PresetFileStorage
{
    public static byte[] Read(string path)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length > DisplayPresetFile.MaximumBytes) throw new InvalidDataException("프리셋 파일이 너무 큽니다.");
        var bytes = new byte[file.Length];
        file.ReadExactly(bytes);
        return bytes;
    }
    public static void Write(string path, byte[] bytes)
    {
        if (bytes.Length > DisplayPresetFile.MaximumBytes) throw new InvalidDataException("프리셋 파일이 너무 큽니다.");
        using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        file.Write(bytes);
        file.Flush(true);
    }
}
