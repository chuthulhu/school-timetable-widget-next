using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SchoolTimetableWidget.Core.Features.Semesters;

namespace SchoolTimetableWidget.Desktop.Features.Semesters;

/// <summary>Compact selector. A candidate selection is reset to committed identity after the transaction.</summary>
public sealed class SemesterSelector : DockPanel
{
    public ComboBox Selector { get; } = new() { MinWidth = 120, MaxWidth = 260, DisplayMemberPath = nameof(SemesterSet.DisplayName),
        SelectedValuePath = nameof(SemesterSet.SemesterId), IsSynchronizedWithCurrentItem = false };
    public Button ManageButton { get; } = new() { Content = "학기 관리...", Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(8, 2, 8, 2) };
    private bool _updating;
    public SemesterSelector(SemesterManagement management, Window owner, Action<string>? reportError = null)
    {
        Margin = new Thickness(10, 2, 10, 6); HorizontalAlignment = HorizontalAlignment.Left;
        System.Windows.Automation.AutomationProperties.SetName(Selector, "현재 학기");
        Children.Add(Selector); Children.Add(ManageButton);
        Selector.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(SemesterManagement.Items)) { Source = management });
        Selector.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedValueProperty,
            new Binding(nameof(SemesterManagement.ActiveSemesterId)) { Source = management, Mode = BindingMode.OneWay });
        Selector.SelectionChanged += (_, _) =>
        {
            if (_updating || Selector.SelectedValue is not Guid id || id == management.ActiveSemesterId) return;
            _updating = true;
            try
            {
                var error = owner.OwnedWindows.Cast<Window>().Any(w => w.IsVisible)
                    ? "열린 편집 창을 먼저 닫아 주세요." : management.Activate(id);
                ResetSelection();
                if (error is not null)
                {
                    if (reportError is not null) reportError(error);
                    else MessageBox.Show(owner, error, "학기 선택", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            finally { _updating = false; }
        };
        ManageButton.Click += (_, _) => new SemesterManagementWindow(management) { Owner = owner }.ShowDialog();
    }
    public void ResetSelection() => Selector.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty)?.UpdateTarget();
}

public sealed class SemesterManagementWindow : Window
{
    public ListBox SemesterList { get; } = new() { DisplayMemberPath = nameof(SemesterSet.DisplayName), MinHeight = 120 };
    public Button NewButton { get; } = new() { Content = "새 학기 만들기" };
    public Button RenameButton { get; } = new() { Content = "이름 변경" };
    public Button DeleteButton { get; } = new() { Content = "삭제" };
    public TextBlock ActiveLabel { get; } = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) };
    public TextBlock ErrorLabel { get; } = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 8) };
    public SemesterManagementWindow(SemesterManagement management)
    {
        Title = "학기 관리"; Width = 480; Height = 420; MinWidth = 360; MinHeight = 320;
        ShowInTaskbar = false; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = new DockPanel { Margin = new Thickness(18) };
        DockPanel.SetDock(ActiveLabel, Dock.Top); root.Children.Add(ActiveLabel);
        var footer = new StackPanel(); DockPanel.SetDock(footer, Dock.Bottom);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var button in new[] { NewButton, RenameButton, DeleteButton })
        { button.Padding = new Thickness(10, 6, 10, 6); button.Margin = new Thickness(0, 8, 8, 0); buttons.Children.Add(button); }
        footer.Children.Add(buttons);
        footer.Children.Add(new TextBlock { Text = "현재 학기는 삭제할 수 없습니다. 삭제하려면 메인 화면에서 다른 학기를 먼저 선택해 주세요.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
        footer.Children.Add(ErrorLabel);
        var close = new Button { Content = "닫기", IsCancel = true, HorizontalAlignment = HorizontalAlignment.Right, Padding = new Thickness(12, 6, 12, 6) };
        close.Click += (_, _) => Close(); footer.Children.Add(close); root.Children.Add(footer); root.Children.Add(SemesterList); Content = root;
        void Refresh()
        {
            var id = (SemesterList.SelectedItem as SemesterSet)?.SemesterId ?? management.ActiveSemesterId;
            SemesterList.ItemsSource = management.Items;
            SemesterList.SelectedItem = management.Items.FirstOrDefault(s => s.SemesterId == id) ?? management.Items.First();
            ActiveLabel.Text = "현재 학기: " + management.Items.Single(s => s.SemesterId == management.ActiveSemesterId).DisplayName;
            DeleteButton.IsEnabled = SemesterList.SelectedItem is SemesterSet selected && management.CanDelete(selected.SemesterId);
            RenameButton.IsEnabled = management.CanChange && SemesterList.SelectedItem is not null;
            NewButton.IsEnabled = management.CanChange;
        }
        SemesterList.SelectionChanged += (_, _) =>
        {
            DeleteButton.IsEnabled = SemesterList.SelectedItem is SemesterSet item && management.CanDelete(item.SemesterId);
        };
        NewButton.Click += (_, _) =>
        {
            var dialog = new SemesterNameWindow(null, (name, copy) => management.Create(name, copy)) { Owner = this };
            dialog.ShowDialog(); Refresh();
        };
        RenameButton.Click += (_, _) =>
        {
            if (SemesterList.SelectedItem is not SemesterSet item) return;
            new SemesterNameWindow(item.DisplayName, (name, _) => management.Rename(item.SemesterId, name)) { Owner = this }.ShowDialog(); Refresh();
        };
        DeleteButton.Click += (_, _) =>
        {
            if (SemesterList.SelectedItem is not SemesterSet item || !management.CanDelete(item.SemesterId)) return;
            var confirmed = MessageBox.Show(this, $"‘{item.DisplayName}’ 학기를 삭제할까요?\n이 학기의 시간표와 날짜별 변경 내용이 모두 삭제됩니다.",
                "학기 삭제", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK;
            if (confirmed) ErrorLabel.Text = management.Delete(item.SemesterId, true) ?? "";
            Refresh();
        };
        Refresh();
    }
}

public sealed class SemesterNameWindow : Window
{
    public TextBox NameInput { get; } = new() { MaxLength = SemesterSet.MaximumNameLength, Margin = new Thickness(0, 6, 0, 12) };
    public CheckBox CopyOption { get; } = new() { Content = "현재 학기의 시간표를 복사", IsChecked = false, Margin = new Thickness(0, 0, 0, 12) };
    public TextBlock ErrorLabel { get; } = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 8) };
    public Button SaveButton { get; } = new() { Content = "저장", Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(8, 0, 0, 0) };
    public SemesterNameWindow(string? currentName, Func<string, bool, string?> save)
    {
        Title = currentName is null ? "새 학기 만들기" : "학기 이름 변경";
        Width = 430; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = new StackPanel { Margin = new Thickness(18) };
        root.Children.Add(new Label { Content = "학기 이름", Target = NameInput }); root.Children.Add(NameInput);
        NameInput.Text = currentName ?? "";
        CopyOption.Visibility = currentName is null ? Visibility.Visible : Visibility.Collapsed;
        root.Children.Add(CopyOption); root.Children.Add(ErrorLabel);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(new Button { Content = "취소", IsCancel = true, Padding = new Thickness(12, 6, 12, 6) });
        buttons.Children.Add(SaveButton); root.Children.Add(buttons); Content = root;
        SaveButton.Click += (_, _) =>
        {
            ErrorLabel.Text = save(NameInput.Text, CopyOption.IsChecked == true) ?? "";
            if (ErrorLabel.Text.Length == 0) DialogResult = true;
        };
    }
}
