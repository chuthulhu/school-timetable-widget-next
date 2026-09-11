using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;

namespace SchoolTimetableWidget.Desktop.Features.DateOverrides;

/// <summary>One fixed date, two independent Drafts, one complete validated commit.</summary>
public sealed class DateOverrideEditSession : ObservableObject
{
    private readonly Func<DateSpecificOverride?, bool> _tryCommit;
    private bool _useTimetable;
    private bool _useSchedule;
    private bool _applying;
    private string _error = "";
    private readonly Func<string?>? _getCommitError;

    public DateOverrideEditSession(DateOnly date, WeeklyTimetable baseWeek, PeriodSchedule baseSchedule,
        DateSpecificOverride? baseline, Func<DateSpecificOverride?, bool> tryCommit, Func<string?>? getCommitError = null)
    {
        _getCommitError = getCommitError;
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            throw new ArgumentException("Weekend overrides are not supported.", nameof(date));
        if (baseline is not null && baseline.Date != date) throw new ArgumentException("Wrong baseline date.");
        ArgumentNullException.ThrowIfNull(tryCommit);
        Date = date;
        _tryCommit = tryCommit;
        _useTimetable = baseline?.Timetable is not null;
        _useSchedule = baseline?.Schedule is not null;
        var day = baseline?.Timetable ?? DayTimetable.FromBase(baseWeek, (SchoolDay)((int)date.DayOfWeek - 1));
        TimetableRows = Array.AsReadOnly(day.Values.Select((v, i) => new DateTimetableDraftRow(i + 1, v)).ToArray());
        ScheduleDraft = new(baseline?.Schedule ?? baseSchedule, _ => throw new InvalidOperationException("Validate only."));
    }
    public DateOnly Date { get; }
    public string TargetLabel => Date.ToString("yyyy년 MM월 dd일", CultureInfo.InvariantCulture) + " 시간표 / 일과";
    public ReadOnlyCollection<DateTimetableDraftRow> TimetableRows { get; }
    public PeriodScheduleEditSession ScheduleDraft { get; }
    public bool UseTimetable { get => _useTimetable; set { if (!IsClosed && !_applying) SetProperty(ref _useTimetable, value); } }
    public bool UseSchedule { get => _useSchedule; set { if (!IsClosed && !_applying) SetProperty(ref _useSchedule, value); } }
    public string ErrorText { get => _error; private set => SetProperty(ref _error, value); }
    public bool IsClosed { get; private set; }
    public bool IsApplied { get; private set; }

    public bool TryApply()
    {
        if (IsClosed || _applying) return false;
        _applying = true;
        try
        {
            DayTimetable? timetable = null;
            PeriodSchedule? schedule = null;
            if (UseTimetable)
            {
                if (TimetableRows.Any(r => r.SubjectText is null || r.ClassText is null))
                    return Reject("교과와 반은 빈 문자열을 포함한 텍스트여야 합니다.");
                timetable = new(TimetableRows.Select(r => new TimetableCellValue(r.SubjectText, r.ClassText)));
            }
            if (UseSchedule && !ScheduleDraft.TryCreateCandidate(out schedule))
                return Reject(ScheduleDraft.ErrorText);
            var candidate = timetable is null && schedule is null ? null : new DateSpecificOverride(Date, timetable, schedule);
            if (!_tryCommit(candidate)) return Reject(_getCommitError?.Invoke() ?? "이 날짜의 예외 설정이 변경되었습니다. 취소 후 다시 열어 주세요.");
            IsApplied = true;
            IsClosed = true;
            ErrorText = "";
            return true;
        }
        finally { _applying = false; }
    }
    public void Cancel() { if (!_applying) IsClosed = true; }
    private bool Reject(string error) { ErrorText = error; return false; }
}
