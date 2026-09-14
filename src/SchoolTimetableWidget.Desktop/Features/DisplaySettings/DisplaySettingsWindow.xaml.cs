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
    private readonly Action<Window> _showPresetDialog;
    private readonly IPresetFileDialogs _fileDialogs;
    public DisplaySettingsWindow(DisplaySettingsSession session, Action<Window>? showPresetDialog = null,
        IPresetFileDialogs? fileDialogs = null)
    {
        if (session.IsClosed) throw new ArgumentException("닫힌 설정입니다.", nameof(session));
        Session = session;
        _showPresetDialog = showPresetDialog ?? (dialog => { dialog.Owner = this; dialog.ShowDialog(); });
        _fileDialogs = fileDialogs ?? new WindowsPresetFileDialogs();
        InitializeComponent();
        DataContext = session;
    }
    private void PresetSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Collection resets can clear WPF selection; only an actual selected stable ID is an edit.
        if (PresetSelector.SelectedItem is DisplayChoice<DisplayPresetReference> choice) Session.Preset = choice.Value;
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
    private void Reset_Click(object sender, RoutedEventArgs e) => Session.Reset();
    private void Apply_Click(object sender, RoutedEventArgs e) => Session.TryApply();
    private void Accept_Click(object sender, RoutedEventArgs e) { if (Session.TryAccept()) Close(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    protected override void OnClosed(EventArgs e) { Session.Cancel(); base.OnClosed(e); }
}
