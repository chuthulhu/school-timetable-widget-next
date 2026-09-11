using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;

namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

public static class DisplaySettingsCommands
{
    public static RoutedUICommand Open { get; } = new("표시 설정", nameof(Open), typeof(DisplaySettingsCommands),
        new InputGestureCollection { new KeyGesture(Key.OemComma, ModifierKeys.Control) });
}
public sealed record DisplayChoice<T>(T Value, string Label);
public static class DisplayChoices
{
    public static IReadOnlyList<DisplayChoice<DisplayPreset>> Presets { get; } =
        [new(DisplayPreset.Standard, "표준"), new(DisplayPreset.Digital, "디지털"),
         new(DisplayPreset.Compact, "컴팩트"), new(DisplayPreset.Minimal, "미니멀")];
    public static IReadOnlyList<DisplayChoice<DisplayFontWeight>> Weights { get; } =
        [new(DisplayFontWeight.Thin, "얇게"), new(DisplayFontWeight.Normal, "보통"),
         new(DisplayFontWeight.Medium, "중간"), new(DisplayFontWeight.Bold, "굵게")];
    public static IReadOnlyList<DisplayChoice<DisplayFontStyle>> Styles { get; } =
        [new(DisplayFontStyle.Normal, "보통"), new(DisplayFontStyle.Italic, "기울임")];
}
public sealed class MissingFontMessageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is string family && !SystemFontCatalog.Current.IsAvailable(family)
            ? "선택한 글꼴을 찾을 수 없어 기본 글꼴을 사용 중" : "";
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public partial class DisplaySettingsWindow : Window
{
    public DisplaySettingsSession Session { get; }
    public DisplaySettingsWindow(DisplaySettingsSession session)
    {
        if (session.IsClosed) throw new ArgumentException("닫힌 설정입니다.", nameof(session));
        Session = session;
        InitializeComponent();
        DataContext = session;
    }
    private void Reset_Click(object sender, RoutedEventArgs e) => Session.Reset();
    private void Apply_Click(object sender, RoutedEventArgs e) => Session.TryApply();
    private void Accept_Click(object sender, RoutedEventArgs e) { if (Session.TryAccept()) Close(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    protected override void OnClosed(EventArgs e) { Session.Cancel(); base.OnClosed(e); }
}
