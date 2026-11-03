using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using Forms = System.Windows.Forms;

namespace SchoolTimetableWidget.Tests.TrayLifecycle;

// ADR 0031 icon resources. Unpublished Forms objects only; no shell icon is shown.
public class TrayIconImageTests
{
    [Fact]
    public void TrayIconUsesTheEmbeddedImageAtTheSmallIconSize()
    {
        using var image = WindowsTrayIcon.LoadTrayImage();
        Assert.Equal(Forms.SystemInformation.SmallIconSize, image.Size);
        // SystemIcons are shared process-wide; never dispose them.
        var generic = System.Drawing.SystemIcons.Application;
        using var tray = new WindowsTrayIcon(publish: false);
        Assert.NotNull(tray.Icon.Icon);
        Assert.NotEqual(generic.Handle, tray.Icon.Icon!.Handle);
    }

    [Fact]
    public void TrayResourceCarriesEveryDpiFrame()
    {
        using var stream = typeof(WindowsTrayIcon).Assembly.GetManifestResourceStream(WindowsTrayIcon.TrayIconResource);
        Assert.NotNull(stream);
        using var reader = new BinaryReader(stream!);
        Assert.Equal(0, reader.ReadUInt16()); Assert.Equal(1, reader.ReadUInt16());
        var count = reader.ReadUInt16();
        var sizes = Enumerable.Range(0, count).Select(_ =>
        {
            var size = reader.ReadByte(); reader.ReadBytes(15);
            return size == 0 ? 256 : size;
        }).ToArray();
        Assert.Equal(new[] { 16, 20, 24, 32, 40, 48, 64 }, sizes);
    }

    [Fact]
    public void ExecutableCarriesTheApplicationIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "SchoolTimetableWidget.Desktop.exe");
        using var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
        var generic = System.Drawing.SystemIcons.Application;
        Assert.NotNull(icon);
        using var mine = icon!.ToBitmap();
        using var theirs = generic.ToBitmap();
        // The centre tile is the amber current period; the generic icon has no such pixel there.
        Assert.Equal(System.Drawing.Color.FromArgb(242, 184, 75), mine.GetPixel(mine.Width / 2, mine.Height / 2));
        Assert.NotEqual(theirs.GetPixel(theirs.Width / 2, theirs.Height / 2), mine.GetPixel(mine.Width / 2, mine.Height / 2));
    }
}
