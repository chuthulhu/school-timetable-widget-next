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
        ApplyType(CurrentTimeTextBlock, display.Time);
        ApplyType(AmPmTextBlock, display.Time);
        ApplyType(CurrentDateTextBlock, display.Date);
        ApplyType(WeekdayTextBlock, display.Weekday);
        ApplyType(StatusTextBlock, display.Status);
        CurrentDateTextBlock.Visibility = Visible(display.ShowDate);
        WeekdayTextBlock.Visibility = Visible(display.ShowWeekday);
        StatusTextBlock.Visibility = Visible(display.ShowStatus);
        AmPmTextBlock.Visibility = Visible(!display.Use24Hour);
        DateGroup.Visibility = Visible(display.ShowDate || display.ShowWeekday);
        // Reserve worst-digit/date/weekday widths once per configuration, independent of the current text.
        CurrentDateTextBlock.Width = Reserve(CurrentDateTextBlock, "0000년 00월 00일");
        WeekdayTextBlock.Width = new[] { "월요일", "화요일", "수요일", "목요일", "금요일", "토요일", "일요일" }.Max(s => Measure(WeekdayTextBlock, s));
        WeekdayTextBlock.Margin = new(display.ShowDate ? 8 : 0, 0, 0, 0);
        CurrentTimeTextBlock.Width = Reserve(CurrentTimeTextBlock, display.ShowSeconds ? "00:00:00" : "00:00");
        AmPmTextBlock.Width = Math.Max(Measure(AmPmTextBlock, "오전"), Measure(AmPmTextBlock, "오후")) + 8;
        var dateWidth = (display.ShowDate ? CurrentDateTextBlock.Width : 0) +
            (display.ShowWeekday ? WeekdayTextBlock.Width + (display.ShowDate ? 8 : 0) : 0);
        var timeWidth = CurrentTimeTextBlock.Width + (display.Use24Hour ? 0 : AmPmTextBlock.Width);
        var dateHeight = Math.Max(display.ShowDate ? LineHeight(CurrentDateTextBlock) : 0,
            display.ShowWeekday ? LineHeight(WeekdayTextBlock) : 0);
        var timeHeight = LineHeight(CurrentTimeTextBlock);
        var statusHeight = display.ShowStatus ? LineHeight(StatusTextBlock) : 0;
        StatusTextBlock.Width = double.NaN;
        foreach (var text in new[] { CurrentTimeTextBlock, CurrentDateTextBlock, WeekdayTextBlock, StatusTextBlock })
            text.TextAlignment = display.Layout == DisplayLayout.Digital ? TextAlignment.Center : TextAlignment.Left;
        HeaderLayout.RowDefinitions.Clear();
        foreach (var child in new FrameworkElement[] { DateGroup, TimeGroup, StatusTextBlock })
        {
            Grid.SetRow(child, 0);
            Grid.SetColumnSpan(child, 1);
            child.HorizontalAlignment = HorizontalAlignment.Stretch;
            child.Margin = new Thickness(0);
        }
        if (display.Layout == DisplayLayout.Digital)
        {
            HeaderLayout.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            HeaderLayout.ColumnDefinitions[1].Width = new GridLength(0);
            HeaderLayout.ColumnDefinitions[2].Width = new GridLength(0);
            HeaderLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(timeHeight + 8) });
            HeaderLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(dateHeight > 0 ? dateHeight + 6 : 0) });
            HeaderLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(statusHeight > 0 ? statusHeight + 6 : 0) });
            Grid.SetColumn(TimeGroup, 0);
            Grid.SetColumn(DateGroup, 0);
            Grid.SetColumn(StatusTextBlock, 0);
            Grid.SetRow(DateGroup, 1);
            Grid.SetRow(StatusTextBlock, 2);
            TimeGroup.HorizontalAlignment = DateGroup.HorizontalAlignment = StatusTextBlock.HorizontalAlignment = HorizontalAlignment.Center;
            StatusTextBlock.Width = Reserve(StatusTextBlock, "점심시간 · 7교시까지 23시간 59분");
            HeaderLayout.MinWidth = Math.Max(timeWidth, Math.Max(dateWidth, display.ShowStatus ? StatusTextBlock.Width : 0));
            Height = timeHeight + (dateHeight > 0 ? dateHeight + 6 : 0) + (statusHeight > 0 ? statusHeight + 6 : 0) + 48;
        }
        else
        {
            HeaderLayout.RowDefinitions.Add(new RowDefinition());
            var standard = display.Layout == DisplayLayout.Standard;
            Grid.SetColumn(DateGroup, standard ? 0 : 1);
            Grid.SetColumn(TimeGroup, standard ? 1 : 0);
            Grid.SetColumn(StatusTextBlock, 2);
            var dateSlot = dateWidth > 0 ? dateWidth + 16 : 0;
            var timeSlot = timeWidth + (dateWidth > 0 || display.ShowStatus ? 16 : 0);
            HeaderLayout.ColumnDefinitions[0].Width = new GridLength(standard ? dateSlot : timeSlot);
            HeaderLayout.ColumnDefinitions[1].Width = new GridLength(standard ? timeSlot : dateSlot);
            HeaderLayout.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            HeaderLayout.MinWidth = dateSlot + timeSlot + (display.ShowStatus ? 120 : 0);
            Height = Math.Max(timeHeight, Math.Max(dateHeight, statusHeight)) + (standard ? 40 : 28);
        }
        if (Window.GetWindow(this) is { IsLoaded: true } window) WindowContentMinimum.Refresh(window);
    }
    private static Visibility Visible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    private static void ApplyType(TextBlock block, ElementTypography type)
    {
        block.FontFamily = SystemFontCatalog.Current.Resolve(type.Font);
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
