using System.Globalization;
using System.IO;
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
    public static IReadOnlyList<DisplayChoice<FontSourceKind>> FontSources { get; } =
        [new(FontSourceKind.Bundled, "앱 제공 글꼴"), new(FontSourceKind.System, "Windows 글꼴"), new(FontSourceKind.OnlineDownloaded, "온라인 글꼴")];
    public static IReadOnlyList<DisplayChoice<DisplayPreset>> Presets { get; } =
        [new(DisplayPreset.Standard, "표준"), new(DisplayPreset.Digital, "디지털"),
         new(DisplayPreset.Compact, "컴팩트"), new(DisplayPreset.Minimal, "미니멀")];
    public static IReadOnlyList<DisplayChoice<DisplayFontWeight>> Weights { get; } =
        [new(DisplayFontWeight.Thin, "얇게"), new(DisplayFontWeight.Normal, "보통"),
         new(DisplayFontWeight.Medium, "중간"), new(DisplayFontWeight.Bold, "굵게")];
    public static IReadOnlyList<DisplayChoice<int>> ClockLines { get; } = [new(1, "1줄"), new(2, "2줄"), new(3, "3줄")];
    public static IReadOnlyList<DisplayChoice<ClockPosition>> ClockPositions { get; } =
        [new(ClockPosition.Left, "왼쪽"), new(ClockPosition.Center, "가운데"), new(ClockPosition.Right, "오른쪽")];
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

/// <summary>Paints an opaque display color, e.g. the theme preview strips.</summary>
public sealed class RgbBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not RgbColor color) return System.Windows.Media.Brushes.Transparent;
        var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
        brush.Freeze();
        return brush;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>The left-list sections of the settings window (ADR 0034), in list order.</summary>
public enum DisplaySettingsSection { Style, Clock, TableFonts, Colors }

public partial class DisplaySettingsWindow : Window
{
    public DisplaySettingsSession Session { get; }
    private readonly Action<Window> _showPresetDialog;
    private readonly IPresetFileDialogs _fileDialogs;
    private readonly RecentColors _recentColors;
    public DisplaySettingsWindow(DisplaySettingsSession session, Action<Window>? showPresetDialog = null,
        IPresetFileDialogs? fileDialogs = null, RecentColors? recentColors = null)
    {
        if (session.IsClosed) throw new ArgumentException("닫힌 설정입니다.", nameof(session));
        Session = session;
        _showPresetDialog = showPresetDialog ?? (dialog => { dialog.Owner = this; dialog.ShowDialog(); });
        _fileDialogs = fileDialogs ?? new WindowsPresetFileDialogs();
        _recentColors = recentColors ?? RecentColors.Shared;
        InitializeComponent();
        DataContext = session;
        ColorAdjust.Finished += (_, _) => ColorPopup.IsOpen = false;
        ShowSection(DisplaySettingsSection.Style);
    }

    public DisplaySettingsSection Section => (DisplaySettingsSection)Math.Max(0, SectionList.SelectedIndex);

    /// <summary>Shows one section. Hidden sections keep their Draft; switching never previews or saves.</summary>
    public void ShowSection(DisplaySettingsSection section)
    {
        if (SectionList.SelectedIndex != (int)section) { SectionList.SelectedIndex = (int)section; return; }
        StylePage.Visibility = section == DisplaySettingsSection.Style ? Visibility.Visible : Visibility.Collapsed;
        ClockPage.Visibility = section == DisplaySettingsSection.Clock ? Visibility.Visible : Visibility.Collapsed;
        TableFontPage.Visibility = section == DisplaySettingsSection.TableFonts ? Visibility.Visible : Visibility.Collapsed;
        ColorPage.Visibility = section == DisplaySettingsSection.Colors ? Visibility.Visible : Visibility.Collapsed;
        SectionScroll.ScrollToTop();
    }
    private void SectionList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Selecting nothing (e.g. a cleared selection) keeps the current section.
        if (SectionList.SelectedIndex < 0) { SectionList.SelectedIndex = 0; return; }
        if (StylePage is not null) ShowSection((DisplaySettingsSection)SectionList.SelectedIndex);
    }
    private void PresetSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Collection resets can clear WPF selection; only an actual selected stable ID is an edit.
        if (PresetSelector.SelectedItem is DisplayChoice<DisplayPresetReference> choice) Session.Preset = choice.Value;
    }
    private void LoadTheme_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DisplayTheme theme }) Session.LoadTheme(theme);
    }
    private async void DownloadFont_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ElementTypographyDraft element }) await element.DownloadAsync();
    }
    private void SaveAsPreset_Click(object sender, RoutedEventArgs e) =>
        _showPresetDialog(new PresetNameWindow("내 프리셋으로 저장", "", Session.TrySaveAs, () => Session.ErrorText));
    private void RenamePreset_Click(object sender, RoutedEventArgs e)
    {
        if (Session.Preset.UserId is not { } id) return;
        _showPresetDialog(new PresetNameWindow("이름 변경", Session.Presets.Get(id).Name, Session.TryRename, () => Session.ErrorText));
    }
    private void UpdatePreset_Click(object sender, RoutedEventArgs e) => Session.TryUpdate();
    private void DeletePreset_Click(object sender, RoutedEventArgs e) => _showPresetDialog(new DeletePresetWindow(Session));
    private void ExportPreset_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var preset = Session.SelectedUserPreset();
            var invalid = Path.GetInvalidFileNameChars();
            var safeName = string.Concat(preset.Name.Select(c => invalid.Contains(c) ? '_' : c));
            var path = _fileDialogs.ChooseExportPath(safeName + DisplayPresetFile.Extension);
            if (path is not null) { PresetFileStorage.Write(path, DisplayPresetFile.Export(preset)); Session.ShowError(""); }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            System.Security.SecurityException or ArgumentException or NotSupportedException)
        {
            System.Diagnostics.Debug.WriteLine(error);
            Session.ShowError("프리셋 파일을 저장하지 못했습니다. 저장 위치와 파일 이름을 확인해 주세요.");
        }
    }
    private void ImportPreset_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = _fileDialogs.ChooseImportPath();
            if (path is null) return;
            var preset = DisplayPresetFile.Import(PresetFileStorage.Read(path));
            _showPresetDialog(new ImportPresetWindow(Session, Session.InspectImport(preset)));
        }
        catch (UnsupportedPresetFileVersionException)
        {
            Session.ShowError("이 앱에서 지원하지 않는 버전의 프리셋 파일입니다.");
        }
        catch (InvalidDataException error)
        {
            System.Diagnostics.Debug.WriteLine(error);
            Session.ShowError("이 프리셋 파일을 불러올 수 없습니다. " + error.Message);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            System.Security.SecurityException or ArgumentException or NotSupportedException)
        {
            System.Diagnostics.Debug.WriteLine(error);
            Session.ShowError("이 프리셋 파일을 불러올 수 없습니다. 파일이 올바른지 확인해 주세요.");
        }
    }
    private void Swatch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag } swatch || !int.TryParse(tag, out var index)) return;
        BeginColorAdjust(index);
        ColorPopup.PlacementTarget = swatch;
        ColorPopup.IsOpen = true;
        ColorAdjust.SaturationValuePad.Focus();
    }
    /// <summary>Starts adjusting one Draft color (ADR 0035). A window still open for another color keeps its color.</summary>
    internal ColorAdjustModel BeginColorAdjust(int index)
    {
        ColorAdjust.Finish(confirm: true);
        var model = new ColorAdjustModel(Session, index, _recentColors);
        ColorAdjust.DataContext = model;
        return model;
    }
    // Clicking outside closes the popup and keeps the previewed color, like 선택.
    private void ColorPopup_Closed(object? sender, EventArgs e) => ColorAdjust.Finish(confirm: true);
    private void MoveClockUp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ClockSlotDraft slot }) Session.MoveClockSlot(slot.Element, -1);
    }
    private void MoveClockDown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ClockSlotDraft slot }) Session.MoveClockSlot(slot.Element, 1);
    }
    private void Reset_Click(object sender, RoutedEventArgs e) => Session.Reset();
    private void Apply_Click(object sender, RoutedEventArgs e) => Session.TryApply();
    private void Accept_Click(object sender, RoutedEventArgs e) { if (Session.TryAccept()) Close(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    protected override void OnClosed(EventArgs e) { Session.Cancel(); base.OnClosed(e); }
}
