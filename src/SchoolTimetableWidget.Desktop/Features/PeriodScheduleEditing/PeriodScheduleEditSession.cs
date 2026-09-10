using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using SchoolTimetableWidget.Core.Features.Periods;

namespace SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;

/// <summary>Draft and validation only. The owner callback accepts a complete immutable candidate.</summary>
public sealed class PeriodScheduleEditSession : ObservableObject
{
    private readonly Func<PeriodSchedule, bool> _tryCommit;
    private string _errorText = string.Empty;

    public PeriodScheduleEditSession(PeriodSchedule baseline, Func<PeriodSchedule, bool> tryCommit)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(tryCommit);
        _tryCommit = tryCommit;
        Rows = Array.AsReadOnly(baseline.Periods.Select(period => new PeriodDraftRow(period)).ToArray());
    }

    public ReadOnlyCollection<PeriodDraftRow> Rows { get; }
    public string ErrorText { get => _errorText; private set => SetProperty(ref _errorText, value); }
    public bool IsClosed { get; private set; }
    public bool IsApplied { get; private set; }

    public bool TryApply()
    {
        if (IsClosed) return false;
        var starts = new TimeOnly[7];
        var ends = new TimeOnly[7];
        // Do not construct even a candidate interval until every field parses.
        for (var i = 0; i < Rows.Count; i++)
        {
            if (!TryTime(Rows[i].StartText, out starts[i]))
                return Reject($"{i + 1}교시 시작 시간이 올바르지 않습니다. 24시간제 HH:mm으로 입력하세요.");
            if (!TryTime(Rows[i].EndText, out ends[i]))
                return Reject($"{i + 1}교시 종료 시간이 올바르지 않습니다. 24시간제 HH:mm으로 입력하세요.");
        }
        for (var i = 0; i < Rows.Count; i++)
        {
            if (starts[i] >= ends[i])
                return Reject($"{i + 1}교시 종료 시간은 시작 시간보다 늦어야 합니다.");
            if (i > 0 && ends[i - 1] > starts[i])
                return Reject($"{i}교시와 {i + 1}교시의 시간 순서가 올바르지 않거나 시간이 겹칩니다. {i + 1}교시는 {i}교시 종료 시각과 같거나 늦게 시작해야 합니다.");
        }
        var candidate = new PeriodSchedule(Enumerable.Range(0, 7)
            .Select(i => new PeriodDefinition(i + 1, starts[i], ends[i])));
        if (!_tryCommit(candidate))
            return Reject("일과 시간이 다른 편집에서 변경되었습니다. 취소 후 다시 열어 주세요.");
        IsApplied = true;
        IsClosed = true;
        ErrorText = string.Empty;
        return true;
    }

    public void Cancel() => IsClosed = true;

    private bool Reject(string message) { ErrorText = message; return false; }

    private static bool TryTime(string? text, out TimeOnly value) =>
        TimeOnly.TryParseExact(text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
}
