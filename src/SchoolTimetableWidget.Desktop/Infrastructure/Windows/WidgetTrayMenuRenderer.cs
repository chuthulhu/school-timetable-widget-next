using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

/// <summary>
/// ADR 0038: draws the Windows Forms tray menu with the widget menu's light look (ADR 0036):
/// #F9F9F9 surface, #D6D6D6 border, rounded #E6E6E6 highlight, thin separators and a line check.
/// Behaviour, items and the Forms menu itself are unchanged.
/// </summary>
internal sealed class WidgetTrayMenuRenderer : Forms.ToolStripRenderer
{
    internal static readonly Color Surface = ColorTranslator.FromHtml("#F9F9F9");
    internal static readonly Color Border = ColorTranslator.FromHtml("#D6D6D6");
    internal static readonly Color Highlight = ColorTranslator.FromHtml("#E6E6E6");
    internal static readonly Color Separator = ColorTranslator.FromHtml("#E0E0E0");
    internal static readonly Color Text = ColorTranslator.FromHtml("#1A1A1A");
    internal static readonly Color DisabledText = ColorTranslator.FromHtml("#9A9A9A");
    internal const float FontSize = 10.5f; // 14 DIPs, as in the WPF menu
    internal static readonly Forms.Padding ItemPadding = new(2, 4, 10, 4);

    /// <summary>Applies the look to a menu and its current items; call again after adding items.</summary>
    internal static void Apply(Forms.ContextMenuStrip menu)
    {
        menu.Renderer = new WidgetTrayMenuRenderer();
        menu.Font = new Font("Segoe UI", FontSize, FontStyle.Regular, GraphicsUnit.Point);
        menu.Padding = new Forms.Padding(4);
        menu.ShowImageMargin = false;
        menu.ShowCheckMargin = true;
        menu.BackColor = Surface;
        menu.ForeColor = Text;
        foreach (Forms.ToolStripItem item in menu.Items)
            // Width stays automatic; vertical padding brings items to about 32 DIPs, as in the WPF menu.
            if (item is Forms.ToolStripMenuItem menuItem) menuItem.Padding = ItemPadding;
        menu.Opened -= RoundCorners;
        menu.Opened += RoundCorners;
    }

    // Windows 11 rounds the popup itself; earlier versions ignore the attribute and keep square corners.
    private static void RoundCorners(object? sender, EventArgs e)
    {
        if (sender is not Forms.ToolStripDropDown { IsHandleCreated: true } menu) return;
        var preference = DwmRound;
        _ = DwmSetWindowAttribute(menu.Handle, DwmWindowCornerPreference, ref preference, sizeof(int));
    }
    private const int DwmWindowCornerPreference = 33, DwmRound = 2;
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);

    protected override void OnRenderToolStripBackground(Forms.ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Surface);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }
    protected override void OnRenderToolStripBorder(Forms.ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Border);
        var bounds = new Rectangle(Point.Empty, e.ToolStrip.Size);
        e.Graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
    }
    protected override void OnRenderImageMargin(Forms.ToolStripRenderEventArgs e) { } // same surface, no grey strip
    protected override void OnRenderMenuItemBackground(Forms.ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected || !e.Item.Enabled) return;
        var bounds = new Rectangle(2, 1, e.Item.Width - 4, e.Item.Height - 2);
        using var path = Rounded(bounds, 4);
        using var brush = new SolidBrush(Highlight);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillPath(brush, path);
    }
    protected override void OnRenderSeparator(Forms.ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(Separator);
        var y = e.Item.Height / 2;
        e.Graphics.DrawLine(pen, 4, y, e.Item.Width - 4, y);
    }
    protected override void OnRenderItemText(Forms.ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Text : DisabledText;
        base.OnRenderItemText(e);
    }
    protected override void OnRenderItemCheck(Forms.ToolStripItemImageRenderEventArgs e)
    {
        var box = e.ImageRectangle;
        using var pen = new Pen(e.Item.Enabled ? Text : DisabledText, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        float x = box.X + box.Width / 2f - 6, y = box.Y + box.Height / 2f - 6;
        e.Graphics.DrawLines(pen, new[] { new PointF(x + 1, y + 6.5f), new PointF(x + 4.5f, y + 10), new PointF(x + 11, y + 2.5f) });
    }

    internal static GraphicsPath Rounded(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
