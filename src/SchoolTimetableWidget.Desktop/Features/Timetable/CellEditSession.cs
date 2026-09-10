using CommunityToolkit.Mvvm.ComponentModel;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Desktop.Features.Timetable;

/// <summary>Two-field draft with a captured commit target; independent of weekly/date models.</summary>
public sealed class CellEditSession : ObservableObject
{
    private Func<TimetableCellValue, bool>? _tryCommit;
    private string _subjectText;
    private string _classText;
    private string _errorText = "";
    private bool _applying;

    public CellEditSession(string targetLabel, TimetableCellValue initialValue, Func<TimetableCellValue, bool> tryCommit)
    {
        ArgumentNullException.ThrowIfNull(targetLabel);
        ArgumentNullException.ThrowIfNull(initialValue);
        ArgumentNullException.ThrowIfNull(tryCommit);
        TargetLabel = targetLabel;
        OriginalValue = initialValue;
        _subjectText = initialValue.SubjectText;
        _classText = initialValue.ClassText;
        _tryCommit = tryCommit;
    }

    public string TargetLabel { get; }
    public TimetableCellValue OriginalValue { get; }
    public string SubjectText
    {
        get => _subjectText;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (!IsClosed && !_applying) SetProperty(ref _subjectText, value);
        }
    }
    public string ClassText
    {
        get => _classText;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (!IsClosed && !_applying) SetProperty(ref _classText, value);
        }
    }
    public string ErrorText => _errorText;
    public bool IsClosed { get; private set; }
    public bool IsApplied { get; private set; }
    public event EventHandler? Completed;

    public bool TryApply()
    {
        if (IsClosed || _applying) return false;
        _applying = true;
        try
        {
            var candidate = new TimetableCellValue(SubjectText, ClassText);
            if (!_tryCommit!(candidate))
            {
                SetProperty(ref _errorText, "편집 대상이 변경되어 적용하지 못했습니다. 취소 후 다시 열어 주세요.", nameof(ErrorText));
                return false;
            }
            IsApplied = true;
            Complete();
            return true;
        }
        finally { _applying = false; }
    }

    public void Cancel()
    {
        if (!IsClosed && !_applying) Complete();
    }

    private void Complete()
    {
        IsClosed = true;
        _tryCommit = null;
        Completed?.Invoke(this, EventArgs.Empty);
        Completed = null;
    }
}
