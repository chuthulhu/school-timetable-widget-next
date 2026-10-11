using System.Globalization;
using System.Windows;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;

namespace SchoolTimetableWidget.Desktop.Features.WindowPlacement;

/// <summary>Explicit, machine-local size/lock writes. Unapplied input has no runtime side effects.</summary>
public partial class WindowSettingsWindow : Window
{
    private readonly WindowPlacementController _controller;
    internal WindowSettingsWindow(WindowPlacementController controller)
    {
        _controller = controller;
        InitializeComponent();
        var preferred = controller.Session.Preferred;
        WidthInput.Text = preferred.Width.ToString("R", CultureInfo.CurrentCulture);
        HeightInput.Text = preferred.Height.ToString("R", CultureInfo.CurrentCulture);
        MovementLock.IsChecked = preferred.MoveLocked;
    }
    internal bool TryApply()
    {
        if (!double.TryParse(WidthInput.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var width) ||
            !double.TryParse(HeightInput.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var height) ||
            !(_controller.Session.Preferred with { Width = width, Height = height }).IsValid)
        {
            ErrorText.Text = "너비와 높이를 0보다 크고 32768 이하인 숫자로 입력해 주세요.";
            return false;
        }
        if (!_controller.TrySetPreferences(width, height, MovementLock.IsChecked == true))
        {
            ErrorText.Text = "창 설정을 저장하지 못했습니다. 원본 파일과 입력값을 보존했습니다. 다시 시도해 주세요.";
            return false;
        }
        ErrorText.Text = "";
        return true;
    }
    private void Apply_Click(object sender, RoutedEventArgs e) => TryApply();
    private void Accept_Click(object sender, RoutedEventArgs e) { if (TryApply()) Close(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
