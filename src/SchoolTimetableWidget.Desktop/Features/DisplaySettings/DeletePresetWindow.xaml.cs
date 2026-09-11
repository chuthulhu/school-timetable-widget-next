using System.Windows;
using System.Windows.Controls;

namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

public partial class DeletePresetWindow : Window
{
    private readonly DisplaySettingsSession _session;
    public DeletePresetWindow(DisplaySettingsSession session)
    {
        _session = session;
        InitializeComponent();
        DeletePresetList.ItemsSource = session.Presets.Items;
        DeletePrompt.Text = "사용 중인 프리셋은 다른 표시 스타일로 전환한 뒤 삭제할 수 있습니다.";
    }
    private void SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeletePresetList.SelectedItem is not UserDisplayPreset selected) return;
        ConfirmDeleteButton.IsEnabled = _session.CanDelete(selected.Id);
        DeletePrompt.Text = ConfirmDeleteButton.IsEnabled ? $"‘{selected.Name}’ 프리셋을 삭제할까요?"
            : "사용 중인 프리셋입니다. 표시 설정에서 다른 스타일을 선택한 뒤 삭제해 주세요.";
    }
    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (DeletePresetList.SelectedItem is UserDisplayPreset selected && _session.TryDelete(selected.Id)) Close();
        else DeletePrompt.Text = _session.ErrorText;
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
