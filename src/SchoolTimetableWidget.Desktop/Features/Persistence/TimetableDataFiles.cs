using System.IO;
using System.Text;
using SchoolTimetableWidget.Core.Features.DataInterchange;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;

namespace SchoolTimetableWidget.Desktop.Features.Persistence;

/// <summary>Bounded file access; source reads never create, repair, move or rewrite files.</summary>
public static class TimetableDataFiles
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static LegacyImportCandidate ReadLegacy(string timetablePath, string? schedulePath = null) =>
        LegacyTimetableImporter.Import(ReadText(timetablePath), schedulePath is null ? null : ReadText(schedulePath));

    public static TimetableDataPackage ReadShared(string path) => TimetableShareFile.Import(ReadBytes(path));

    public static void ExportShared(string path, TimetableDataPackage package) =>
        AtomicProfileFile.Write(path, TimetableShareFile.Export(package));

    private static string ReadText(string path)
    {
        var bytes = ReadBytes(path).AsSpan();
        if (bytes.StartsWith(Encoding.UTF8.Preamble)) bytes = bytes[Encoding.UTF8.Preamble.Length..];
        try { return StrictUtf8.GetString(bytes); }
        catch (DecoderFallbackException error) { throw new FormatException("가져올 파일은 올바른 UTF-8이어야 합니다.", error); }
    }

    private static byte[] ReadBytes(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > TimetableShareFile.MaximumBytes) throw new FormatException("가져올 파일은 4 MiB 이하여야 합니다.");
        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        if (stream.ReadByte() != -1) throw new IOException("읽는 중 파일이 변경되었습니다. 다시 가져와 주세요.");
        return bytes;
    }
}
