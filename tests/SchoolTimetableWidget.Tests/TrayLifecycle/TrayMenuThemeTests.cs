using System.Drawing;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using Forms = System.Windows.Forms;

namespace SchoolTimetableWidget.Tests.TrayLifecycle;

// ADR 0038 tray menu look. Unpublished Forms objects and an in-memory bitmap only; no shell icon or menu is shown.
public class TrayMenuThemeTests
{
    [Fact]
    public void TrayMenuUsesTheWidgetMenuLookWithoutChangingItsItems()
    {
        using var tray = new WindowsTrayIcon(publish: false);
        var menu = tray.Menu;
        Assert.IsType<WidgetTrayMenuRenderer>(menu.Renderer);
        Assert.Equal(WidgetTrayMenuRenderer.FontSize, menu.Font.SizeInPoints, 2);
        Assert.False(menu.ShowImageMargin);
        Assert.True(menu.ShowCheckMargin);
        Assert.Equal(WidgetTrayMenuRenderer.Surface.ToArgb(), menu.BackColor.ToArgb());
        var items = menu.Items.OfType<Forms.ToolStripMenuItem>().ToArray();
        Assert.Equal(new[] { "위젯 숨기기", "Windows 시작 시 실행", "업데이트 확인", "업데이트 후 다시 시작", "종료" }, items.Select(i => i.Text));
        Assert.All(items, item => Assert.Equal(WidgetTrayMenuRenderer.ItemPadding, item.Padding));
        Assert.All(items, item => Assert.True(item.AutoSize)); // widths still follow the text
        Assert.Equal(2, menu.Items.OfType<Forms.ToolStripSeparator>().Count());
    }

    [Fact]
    public void RendererPaintsTheLightSurfaceAndBorder()
    {
        using var tray = new WindowsTrayIcon(publish: false);
        var menu = tray.Menu;
        menu.Size = new Size(200, 120);
        using var bitmap = new Bitmap(200, 120);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            menu.Renderer.DrawToolStripBackground(new Forms.ToolStripRenderEventArgs(graphics, menu));
            menu.Renderer.DrawToolStripBorder(new Forms.ToolStripRenderEventArgs(graphics, menu));
        }
        Assert.Equal(WidgetTrayMenuRenderer.Surface.ToArgb(), bitmap.GetPixel(100, 60).ToArgb());
        Assert.Equal(WidgetTrayMenuRenderer.Border.ToArgb(), bitmap.GetPixel(0, 60).ToArgb());
    }

    [Fact]
    public void RoundedHighlightStaysInsideItsBounds()
    {
        using var path = WidgetTrayMenuRenderer.Rounded(new Rectangle(2, 1, 180, 30), 4);
        var bounds = path.GetBounds();
        Assert.Equal(2, bounds.X, 1); Assert.Equal(1, bounds.Y, 1);
        Assert.Equal(180, bounds.Width, 1); Assert.Equal(30, bounds.Height, 1);
        Assert.False(path.IsVisible(2, 1)); // the corner is cut
        Assert.True(path.IsVisible(92, 16));
    }
}
