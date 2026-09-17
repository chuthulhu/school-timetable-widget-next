using System.IO;
using System.Security;

namespace SchoolTimetableWidget.Desktop.Features.Autostart;

internal enum AutoStartStatus { Disabled, Enabled, StaleOrDifferent, Unavailable }

/// <summary>Only the application's single registration; no profile or general registry API.</summary>
internal interface IAutoStartRegistrationStore
{
    object? Read();
    void Write(string command);
    void Remove();
}

internal sealed class AutoStartRegistration(IAutoStartRegistrationStore store, string? command)
{
    public AutoStartStatus Status { get; private set; } = AutoStartStatus.Unavailable;
    public string? Error { get; private set; }

    public AutoStartStatus Refresh()
    {
        Error = null;
        try
        {
            var value = store.Read();
            Status = value is null ? AutoStartStatus.Disabled :
                command is not null && value is string text && StringComparer.OrdinalIgnoreCase.Equals(text, command)
                    ? AutoStartStatus.Enabled : AutoStartStatus.StaleOrDifferent;
        }
        catch (Exception error) when (IsAccessFailure(error))
        {
            Status = AutoStartStatus.Unavailable;
            Error = "Windows 시작 시 자동 실행 상태를 확인하지 못했습니다.";
        }
        return Status;
    }

    public bool SetEnabled(bool enabled)
    {
        // Never infer success from the click or a cached checkmark.
        if (Refresh() == AutoStartStatus.Unavailable) return false;
        if (enabled && command is null)
        {
            Error = "현재 실행 파일로 자동 실행을 설정할 수 없습니다. 앱의 EXE 파일에서 실행해 주세요.";
            return false;
        }
        try
        {
            if (enabled) store.Write(command!); else store.Remove();
        }
        catch (Exception error) when (IsAccessFailure(error))
        {
            Refresh();
            Error = FailureMessage(enabled);
            return false;
        }
        var actual = Refresh();
        if (actual == (enabled ? AutoStartStatus.Enabled : AutoStartStatus.Disabled)) return true;
        Error = FailureMessage(enabled) + " 등록 상태를 다시 확인해 주세요.";
        return false;
    }

    private static string FailureMessage(bool enabled) => enabled
        ? "Windows 시작 시 자동 실행을 설정하지 못했습니다."
        : "Windows 시작 시 자동 실행을 해제하지 못했습니다.";
    private static bool IsAccessFailure(Exception error) =>
        error is SecurityException or UnauthorizedAccessException or IOException or System.ComponentModel.Win32Exception;
}
