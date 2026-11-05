using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;

namespace SchoolTimetableWidget.Desktop.Features.CurrentStatus;

/// <summary>Reconfigures existing elements only when display inputs change; ordinary ticks only update text.</summary>
public partial class CurrentStatusHeaderView : UserControl
{
    private FontLibrary _fonts = FontLibrary.LocalOnly;
    public FontLibrary Fonts
    {
        get => _fonts;
        set
        {
            WeakEventManager<FontLibrary, EventArgs>.RemoveHandler(_fonts, nameof(FontLibrary.Changed), FontsChanged);
            _fonts = value;
            WeakEventManager<FontLibrary, EventArgs>.AddHandler(_fonts, nameof(FontLibrary.Changed), FontsChanged);
            Configure();
        }
    }
    private void FontsChanged(object? sender, EventArgs e) => Configure();
    public CurrentStatusHeaderView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is CurrentStatusHeaderViewModel old) PropertyChangedEventManager.RemoveHandler(old, OnDisplayChanged, nameof(old.Display));
            if (e.NewValue is CurrentStatusHeaderViewModel model) PropertyChangedEventManager.AddHandler(model, OnDisplayChanged, nameof(model.Display));
            Configure();
        };
        Configure();
    }
    private void OnDisplayChanged(object? sender, PropertyChangedEventArgs e) => Configure();
    private void Configure()
    {
        var display = (DataContext as CurrentStatusHeaderViewModel)?.Display ?? DisplayPresets.Create(DisplayPreset.Standard);
        var clock = display.Colors.ClockText;
        var foreground = new SolidColorBrush(Color.FromRgb(clock.R, clock.G, clock.B));
        foreground.Freeze();
        Foreground = foreground;
        ApplyType(CurrentTimeTextBlock, display.Time);
        ApplyType(AmPmTextBlock, display.Time);
        ApplyType(CurrentDateTextBlock, display.Date);
        ApplyType(WeekdayTextBlock, display.Weekday);
        ApplyType(StatusTextBlock, display.Status);
        CurrentDateTextBlock.Visibility = Visible(display.ShowDate);
        WeekdayTextBlock.Visibility = Visible(display.ShowWeekday);
        StatusTextBlock.Visibility = Visible(display.ShowStatus);
        AmPmTextBlock.Visibility = Visible(!display.Use24Hour);
        // Reserve worst-digit/date/weekday widths once per configuration, independent of the current text.
        CurrentDateTextBlock.Width = Reserve(CurrentDateTextBlock, "0000년 00월 00일");
        WeekdayTextBlock.Width = new[] { "월요일", "화요일", "수요일", "목요일", "금요일", "토요일", "일요일" }.Max(s => Measure(WeekdayTextBlock, s));
        CurrentTimeTextBlock.Width = Reserve(CurrentTimeTextBlock, display.ShowSeconds ? "00:00:00" : "00:00");
        AmPmTextBlock.Width = Math.Max(Measure(AmPmTextBlock, "오전"), Measure(AmPmTextBlock, "오후")) + 8;
        ArrangeClock(display);
        if (Window.GetWindow(this) is { IsLoaded: true } window) WindowContentMinimum.Refresh(window);
    }
    private const double ElementGap = 16, DateWeekdayGap = 8, PositionGap = 16, StatusMinimum = 120;

    /// <summary>
    /// Places the four elements by the clock layout (ADR 0037): one row per line and, in each row, left,
    /// center and right cells in list order. Only the status may shrink (ellipsis) when it is not centered;
    /// the minimum width keeps the time, date and weekday whole.
    /// </summary>
    private void ArrangeClock(DisplayConfiguration display)
    {
        var clock = display.Clock;
        foreach (var element in new FrameworkElement[] { CurrentDateTextBlock, WeekdayTextBlock, TimeGroup, StatusTextBlock })
            if (element.Parent is Panel parent) parent.Children.Remove(element);
        HeaderLayout.Children.Clear();
        HeaderLayout.RowDefinitions.Clear();
        HeaderLayout.ColumnDefinitions.Clear();
        var lines = clock.LineCount;
        var multiLine = lines > 1;
        double minWidth = 0, height = 0;
        for (var line = 1; line <= lines; line++)
        {
            // Hidden elements stay placed (collapsed) so their bindings keep the data context.
            var placed = clock.Slots.Where(slot => slot.Line == line).ToArray();
            var slots = placed.Where(slot => IsShown(slot.Element, display)).ToArray();
            var rowHeight = slots.Length == 0 ? 0 : slots.Max(slot => LineHeight(slot.Element));
            if (multiLine && rowHeight > 0) rowHeight += line == 1 ? 8 : 6;
            HeaderLayout.RowDefinitions.Add(new RowDefinition
            { Height = multiLine ? new GridLength(rowHeight) : new GridLength(1, GridUnitType.Star) });
            height += rowHeight;
            var row = new Grid();
            Grid.SetRow(row, line - 1);
            HeaderLayout.Children.Add(row);
            var widths = new Dictionary<ClockPosition, double>();
            var hasStatus = slots.FirstOrDefault(slot => slot.Element == ClockElement.Status)?.Position;
            var centered = slots.Any(slot => slot.Position == ClockPosition.Center);
            foreach (var position in new[] { ClockPosition.Left, ClockPosition.Center, ClockPosition.Right })
            {
                var cellSlots = placed.Where(slot => slot.Position == position).ToArray();
                var stretch = hasStatus == position && position != ClockPosition.Center;
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = ColumnWidth(position, centered, hasStatus) });
                widths[position] = cellSlots.Length == 0 ? 0 : FillCell(row, (int)position, cellSlots, position, stretch, display);
            }
            var sides = Math.Max(widths[ClockPosition.Left], widths[ClockPosition.Right]);
            var rowWidth = centered
                ? widths[ClockPosition.Center] + 2 * sides + (sides > 0 ? 2 * PositionGap : 0)
                : widths[ClockPosition.Left] + widths[ClockPosition.Right] +
                  (widths[ClockPosition.Left] > 0 && widths[ClockPosition.Right] > 0 ? PositionGap : 0);
            minWidth = Math.Max(minWidth, rowWidth);
        }
        HeaderLayout.MinWidth = minWidth;
        Height = multiLine ? height + 48
            : height + (display.Layout == DisplayLayout.Inline ? 28 : 40);
    }

    // Centered rows keep the middle truly centered; otherwise the side holding the status (or the left) fills.
    private static GridLength ColumnWidth(ClockPosition position, bool centered, ClockPosition? status)
    {
        if (position == ClockPosition.Center) return GridLength.Auto;
        if (centered) return new GridLength(1, GridUnitType.Star);
        var fill = status is ClockPosition.Right ? ClockPosition.Right : ClockPosition.Left;
        return position == fill ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
    }

    private double FillCell(Grid row, int column, ClockSlot[] slots, ClockPosition position, bool stretch, DisplayConfiguration display)
    {
        var cell = new Grid
        {
            HorizontalAlignment = stretch ? HorizontalAlignment.Stretch
                : position switch { ClockPosition.Center => HorizontalAlignment.Center, ClockPosition.Right => HorizontalAlignment.Right, _ => HorizontalAlignment.Left }
        };
        Grid.SetColumn(cell, column);
        row.Children.Add(cell);
        double width = 0;
        ClockElement? previous = null; // the previous shown element in this cell
        for (var i = 0; i < slots.Length; i++)
        {
            var element = Element(slots[i].Element);
            var shown = IsShown(slots[i].Element, display);
            var gap = !shown || previous is null ? 0
                : previous == ClockElement.Date && slots[i].Element == ClockElement.Weekday ? DateWeekdayGap : ElementGap;
            if (shown) previous = slots[i].Element;
            var isStatus = slots[i].Element == ClockElement.Status;
            cell.ColumnDefinitions.Add(new ColumnDefinition
            { Width = isStatus && stretch ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });
            element.Margin = new Thickness(gap, 0, 0, 0);
            element.HorizontalAlignment = HorizontalAlignment.Stretch;
            Grid.SetColumn(element, i);
            cell.Children.Add(element);
            var alignment = position switch { ClockPosition.Center => TextAlignment.Center, ClockPosition.Right => TextAlignment.Right, _ => TextAlignment.Left };
            if (element is TextBlock block) block.TextAlignment = alignment;
            else CurrentTimeTextBlock.TextAlignment = alignment;
            if (isStatus)
                StatusTextBlock.Width = stretch ? double.NaN : Reserve(StatusTextBlock, "점심시간 · 7교시까지 23시간 59분");
            if (shown) width += gap + ElementWidth(slots[i].Element, stretch);
        }
        return width;
    }

    private FrameworkElement Element(ClockElement element) => element switch
    {
        ClockElement.Date => CurrentDateTextBlock,
        ClockElement.Weekday => WeekdayTextBlock,
        ClockElement.Time => TimeGroup,
        _ => StatusTextBlock
    };
    private static bool IsShown(ClockElement element, DisplayConfiguration display) => element switch
    {
        ClockElement.Date => display.ShowDate,
        ClockElement.Weekday => display.ShowWeekday,
        ClockElement.Status => display.ShowStatus,
        _ => true
    };
    private double ElementWidth(ClockElement element, bool stretch) => element switch
    {
        ClockElement.Date => CurrentDateTextBlock.Width,
        ClockElement.Weekday => WeekdayTextBlock.Width,
        ClockElement.Time => CurrentTimeTextBlock.Width + (AmPmTextBlock.Visibility == Visibility.Visible ? AmPmTextBlock.Width : 0),
        _ => stretch ? StatusMinimum : StatusTextBlock.Width
    };
    private double LineHeight(ClockElement element) => element switch
    {
        ClockElement.Date => LineHeight(CurrentDateTextBlock),
        ClockElement.Weekday => LineHeight(WeekdayTextBlock),
        ClockElement.Time => LineHeight(CurrentTimeTextBlock),
        _ => LineHeight(StatusTextBlock)
    };

    private static Visibility Visible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    private void ApplyType(TextBlock block, ElementTypography type)
    {
        block.FontFamily = Fonts.Resolve(type.Font);
        block.FontSize = type.Size;
        block.FontWeight = SystemFontCatalog.Weight(type.Weight);
        block.FontStyle = SystemFontCatalog.Style(type.Style);
    }
    private static double LineHeight(TextBlock block) => Math.Ceiling(Math.Max(block.FontFamily.LineSpacing * block.FontSize, block.FontSize * 1.5));
    private static double Measure(TextBlock block, string value) =>
        Math.Ceiling(new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(block.FontFamily, block.FontStyle, block.FontWeight, block.FontStretch),
            block.FontSize, Brushes.Black, 1).WidthIncludingTrailingWhitespace) + 2;
    private static double Reserve(TextBlock block, string pattern)
    {
        var digit = "0123456789".Max(c => Measure(block, c.ToString()));
        return pattern.Sum(c => char.IsAsciiDigit(c) ? digit : Measure(block, c.ToString())) + 4;
    }
}
