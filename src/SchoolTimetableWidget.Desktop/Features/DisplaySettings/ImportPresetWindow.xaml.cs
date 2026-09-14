using System.Windows;

namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

public partial class ImportPresetWindow : Window
{
    private readonly DisplaySettingsSession _session;
    private readonly PresetImportCandidate _candidate;
    public ImportPresetWindow(DisplaySettingsSession session, PresetImportCandidate candidate)
    {
        _session = session;
        _candidate = candidate;
        InitializeComponent();
        NameInput.Text = candidate.Collision == PresetImportCollision.SameName
            ? SuggestCopyName(candidate.Preset.Name, session.Presets) : candidate.Preset.Name;
        LayoutText.Text = candidate.Preset.Display.Layout switch
        {
            DisplayLayout.Standard => "표준 배치", DisplayLayout.Digital => "디지털 배치", _ => "한 줄 배치"
        };
        OptionsText.Text = string.Join(" · ", new[]
        {
            candidate.Preset.Display.Use24Hour ? "24시간제" : "12시간제",
            candidate.Preset.Display.ShowSeconds ? "초 표시" : "초 숨김",
            candidate.Preset.Display.ShowDate ? "날짜 표시" : "날짜 숨김",
            candidate.Preset.Display.ShowWeekday ? "요일 표시" : "요일 숨김",
            candidate.Preset.Display.ShowStatus ? "상태 표시" : "상태 숨김"
        });
        var typography = new[] { candidate.Preset.Display.Time, candidate.Preset.Display.Date,
            candidate.Preset.Display.Weekday, candidate.Preset.Display.Status };
        FontList.ItemsSource = candidate.Fonts.Zip(typography, (font, type) => new
        {
            font.Element, Description = $"{font.Font.Family} / {type.Size:g}", font.Status
        });
        UpdateButton.Visibility = candidate.Collision == PresetImportCollision.SameId ? Visibility.Visible : Visibility.Collapsed;
        CopyButton.Visibility = candidate.Collision == PresetImportCollision.None ? Visibility.Collapsed : Visibility.Visible;
        ImportButton.Visibility = candidate.Collision == PresetImportCollision.None ? Visibility.Visible : Visibility.Collapsed;
        CollisionText.Text = candidate.Collision switch
        {
            PresetImportCollision.SameId => "같은 프리셋이 이미 있습니다. 기존 프리셋을 업데이트하거나 새 복사본으로 가져올 수 있습니다.",
            PresetImportCollision.SameName => "같은 이름의 내 프리셋이 있습니다. 제안된 새 이름을 확인하거나 수정해 주세요.",
            _ => "가져온 프리셋은 내 프리셋 목록에 추가되며 현재 화면에는 자동으로 적용되지 않습니다."
        };
    }
    private void Import_Click(object sender, RoutedEventArgs e) => Complete(PresetImportAction.Add);
    private void Update_Click(object sender, RoutedEventArgs e) => Complete(PresetImportAction.UpdateExisting);
    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        var normalized = "";
        try { normalized = UserDisplayPreset.NormalizeName(NameInput.Text); } catch (ArgumentException) { }
        if (_session.Presets.Items.Any(p => string.Equals(p.Name, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            NameInput.Text = SuggestCopyName(_candidate.Preset.Name, _session.Presets);
            ErrorText.Text = "새 복사본 이름을 확인하거나 수정한 뒤 다시 눌러 주세요.";
            NameInput.Focus(); NameInput.SelectAll();
            return;
        }
        Complete(PresetImportAction.ImportAsCopy);
    }
    private void Complete(PresetImportAction action)
    {
        if (_session.TryImport(_candidate, action, NameInput.Text)) Close();
        else ErrorText.Text = _session.ErrorText;
    }
    internal static string SuggestCopyName(string name, UserDisplayPresetLibrary library)
    {
        for (var number = 1; ; number++)
        {
            var suffix = number == 1 ? " (복사본)" : $" (복사본 {number})";
            var prefix = name[..Math.Min(name.Length, UserDisplayPreset.MaxNameLength - suffix.Length)];
            var candidate = prefix + suffix;
            if (!library.Items.Any(p => string.Equals(p.Name, candidate, StringComparison.OrdinalIgnoreCase))) return candidate;
        }
    }
}
