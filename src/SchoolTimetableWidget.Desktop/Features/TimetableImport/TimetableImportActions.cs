using System.Windows;
using SchoolTimetableWidget.Core.Features.TimetableImport;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;

namespace SchoolTimetableWidget.Desktop.Features.TimetableImport;

/// <summary>Clipboard and modal window integration, separate from parser and timetable ownership.</summary>
public sealed class TimetableImportActions(ISpreadsheetClipboard clipboard, Func<TimetableImportMode, string>? developmentSample = null)
{
    public TimetableImportSession CreateSession(WeeklyTimetableViewModel target, TimetableImportMode mode)
    {
        var baseline = target.CommittedTimetable;
        return new(mode, next => target.TryReplaceTimetable(baseline, next), () => target.CommitError);
    }

    public void ReadClipboard(TimetableImportSession session)
    {
        try { session.LoadText(clipboard.ReadText()); }
        catch (Exception error) when (WindowsSpreadsheetClipboard.IsAccessFailure(error))
        { session.ReportReadError("클립보드에 접근하지 못했습니다. 다른 앱의 복사 작업이 끝난 뒤 다시 읽어 주세요."); }
    }

    public string CopyTemplate()
    {
        try
        {
            clipboard.WriteText(CanonicalTimetableImporter.CreateTemplate());
            return "표준 양식을 복사했습니다. 스프레드시트 A1에 붙여넣고 작성한 뒤, 8행 × 11열 전체를 복사해 표준 양식으로 가져오세요.";
        }
        catch (Exception error) when (WindowsSpreadsheetClipboard.IsAccessFailure(error))
        { return "클립보드에 복사하지 못했습니다. 다른 앱의 복사 작업이 끝난 뒤 다시 시도해 주세요."; }
    }

    public void ShowImport(Window owner, WeeklyTimetableViewModel target, TimetableImportMode mode)
    {
        if (target.Editor.ActiveSession is not null) return;
        var session = CreateSession(target, mode);
        if (developmentSample is null) ReadClipboard(session);
        else session.LoadText(developmentSample(mode));
        try
        {
            var dialog = new TimetableImportWindow(session, () => ReadClipboard(session)) { Owner = owner };
            if (developmentSample is not null) dialog.Title = session.ModeLabel + " — 개발 샘플로 시작";
            dialog.ShowDialog();
        }
        finally
        {
            session.Cancel();
            if (session.IsApplied) WindowContentMinimum.Refresh(owner);
        }
    }
}
