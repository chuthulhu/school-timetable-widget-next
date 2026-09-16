using System.IO;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;

namespace SchoolTimetableWidget.Desktop.Features.Persistence;

public static class BackupCommands
{
    public static RoutedUICommand Backup { get; } = new("데이터 백업", nameof(Backup), typeof(BackupCommands));
    public static RoutedUICommand Restore { get; } = new("데이터 복원", nameof(Restore), typeof(BackupCommands));
    public static RoutedUICommand Recover { get; } = new("복원 전 상태로 되돌리기", nameof(Recover), typeof(BackupCommands));
}
public interface IBackupDialogs
{
    string? SavePath(string suggestedName);
    string? OpenPath();
    bool ConfirmRestore(string summary);
    bool ConfirmRecovery();
    void Message(string text);
}
public sealed class WindowsBackupDialogs(Window owner) : IBackupDialogs
{
    internal static SaveFileDialog CreateSave(string name) => new()
    {
        Title = "백업 파일 만들기", Filter = "시간표 위젯 데이터 백업 (*.stwbackup)|*.stwbackup",
        DefaultExt = ".stwbackup", AddExtension = true, OverwritePrompt = true, FileName = name
    };
    internal static OpenFileDialog CreateOpen() => new()
    {
        Title = "백업에서 복원하기", Filter = "시간표 위젯 데이터 백업 (*.stwbackup)|*.stwbackup",
        DefaultExt = ".stwbackup", CheckFileExists = true, Multiselect = false
    };
    public string? SavePath(string suggestedName)
    { var dialog = CreateSave(suggestedName); return dialog.ShowDialog(owner) == true ? dialog.FileName : null; }
    public string? OpenPath()
    { var dialog = CreateOpen(); return dialog.ShowDialog(owner) == true ? dialog.FileName : null; }
    public bool ConfirmRestore(string summary) => new BackupRestorePreviewWindow(summary) { Owner = owner }.ShowDialog() == true;
    public bool ConfirmRecovery() => MessageBox.Show(owner,
        "보관된 자료를 사용해 복원 시작 전 상태로 되돌립니다. 진행할까요?", "복원 전 상태로 되돌리기",
        MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK;
    public void Message(string text) => MessageBox.Show(owner, text, "시간표 데이터", MessageBoxButton.OK, MessageBoxImage.Information);
}
public sealed class BackupRestorePreviewWindow : Window
{
    public BackupRestorePreviewWindow(string summary)
    {
        Title = "데이터 복원"; Width = 540; Height = 600; MinWidth = 420; MinHeight = 350;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var layout = new DockPanel { Margin = new Thickness(20) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "취소", IsCancel = true, MinWidth = 80, Margin = new Thickness(4), Padding = new Thickness(8) };
        var restore = new Button { Content = "복원", MinWidth = 80, Margin = new Thickness(4), Padding = new Thickness(8) };
        restore.Click += (_, _) => DialogResult = true;
        buttons.Children.Add(cancel); buttons.Children.Add(restore); DockPanel.SetDock(buttons, Dock.Bottom); layout.Children.Add(buttons);
        layout.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new TextBlock { Text = summary, TextWrapping = TextWrapping.Wrap, FontSize = 16, Margin = new Thickness(0, 0, 0, 16) } });
        Content = layout;
    }
}
/// <summary>Dialogs and confirmation surround the pure parser and durable runtime transaction.</summary>
public sealed class ProfileBackupActions(ProfileRuntime runtime, IApplicationClock clock, IBackupDialogs dialogs,
    Func<bool>? hasModal = null)
{
    private bool _busy;
    private bool Ready => !_busy && runtime.CanReplace && !(hasModal?.Invoke() ?? false);
    public bool CanBackup => Ready && runtime.Session.LoadResult.CanWrite;
    public bool CanRestore => Ready && runtime.Session.CanRestore;
    public bool CanRecover => Ready && runtime.Session.CanRecover;
    public void Backup()
    {
        if (!CanBackup) return;
        Run(() =>
        {
            dialogs.Message("적용되어 저장된 현재 데이터를 백업합니다. 백업 파일에는 시간표와 설정 정보가 포함됩니다.");
            var name = "SchoolTimetableWidget-Backup-" + clock.GetSnapshot().LocalTime.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture) + ProfileBackupFile.Extension;
            var path = dialogs.SavePath(name);
            if (path is null) return;
            runtime.Session.ExportBackup(path);
            dialogs.Message("백업 파일을 만들었습니다.");
        });
    }
    public void Restore()
    {
        if (!CanRestore) return;
        Run(() =>
        {
            var path = dialogs.OpenPath(); if (path is null) return;
            var candidate = ProfileBackupFile.Read(path);
            if (!dialogs.ConfirmRestore(Summary(candidate))) return;
            var error = runtime.Restore(candidate);
            dialogs.Message(error ?? "시간표와 설정을 백업의 데이터로 복원했습니다.");
        });
    }
    public void Recover()
    {
        if (!CanRecover) return;
        Run(() =>
        {
            if (!dialogs.ConfirmRecovery()) return;
            var error = runtime.Recover();
            dialogs.Message(error ?? (runtime.Session.LoadResult.CanWrite ? "이전 정상 데이터로 복구했습니다. 편집과 저장을 사용할 수 있습니다." :
                "복원 전 상태로 돌아왔습니다. 기존 저장 데이터는 손상되었거나 이 버전에서 지원하지 않아 편집과 저장은 계속 사용할 수 없습니다. 정상적인 백업 파일을 선택해 다시 복원할 수 있습니다."));
        });
    }
    public string Summary(ProfileSnapshot candidate)
    {
        var style = candidate.Display.Preset.UserId is { } id ? "내 프리셋 · " + candidate.DisplayPresets.Get(id).Name :
            candidate.Display.Preset.BuiltIn switch { DisplayPreset.Digital => "디지털", DisplayPreset.Compact => "컴팩트", DisplayPreset.Minimal => "미니멀", _ => "표준" };
        var configurations = new[] { candidate.Display }.Concat(candidate.DisplayPresets.Items.Select(p => p.Display));
        var fonts = configurations.SelectMany(d => new[] { d.Time.Font, d.Date.Font, d.Weekday.Font, d.Status.Font }).Distinct();
        var lines = fonts.Select(font => font.Family + " — " + new PresetImportFont("", font, runtime.Display.Fonts.IsAvailable(font)).Status);
        return $"이 백업에는 다음 데이터가 있습니다.\n\n기본 시간표: 35칸\n기본 일과: 7교시\n날짜별 변경: {candidate.Overrides.Count}일\n사용자 프리셋: {candidate.DisplayPresets.Items.Count}개\n표시 스타일: {style}\n점심시간 표시: {(candidate.ShowLunch ? "켬" : "끔")}\n\n글꼴(표시 설정과 모든 프리셋):\n" +
            string.Join("\n", lines) + "\n\n온라인 글꼴은 자동 다운로드하지 않습니다. 필요한 글꼴은 표시 설정에서 다운로드할 수 있습니다.\n\n현재 데이터를 이 백업으로 교체합니다. 현재 보고 있는 주는 유지됩니다.";
    }
    private void Run(Action action)
    {
        _busy = true; CommandManager.InvalidateRequerySuggested();
        try { action(); }
        catch (InvalidDataException error)
        {
            System.Diagnostics.Debug.WriteLine(error);
            dialogs.Message("이 백업 파일을 불러올 수 없습니다. " + error.Message);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            System.Diagnostics.Debug.WriteLine(error);
            dialogs.Message("작업을 완료하지 못했습니다. 파일 위치와 접근 권한을 확인한 뒤 다시 시도해 주세요.");
        }
        finally { _busy = false; CommandManager.InvalidateRequerySuggested(); }
    }
}
