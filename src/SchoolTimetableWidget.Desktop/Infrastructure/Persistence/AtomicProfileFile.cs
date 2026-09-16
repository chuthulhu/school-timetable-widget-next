using System.IO;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Persistence;

/// <summary>Complete same-directory write and flush before destination replacement.</summary>
internal static class AtomicProfileFile
{
    internal static void Write(string path, byte[] bytes, Action? beforeRename = null)
    {
        var destination = Path.GetFullPath(path);
        var temporary = Path.Combine(Path.GetDirectoryName(destination)!, $".stw-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { stream.Write(bytes); stream.Flush(true); }
            beforeRename?.Invoke();
            File.Move(temporary, destination, true);
        }
        finally
        {
            try { File.Delete(temporary); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            { System.Diagnostics.Debug.WriteLine(error); }
        }
    }
}
