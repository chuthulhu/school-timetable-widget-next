using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Features.TimetableImport;
using SchoolTimetableWidget.Desktop.Features.Timetable;

namespace SchoolTimetableWidget.Desktop.Features.TimetableImport;

/// <summary>Owns one import draft. Reads and previews cannot mutate the captured target.</summary>
public sealed class TimetableImportSession : ObservableObject
{
    private Func<WeeklyTimetable, bool>? _tryCommit;
    private IReadOnlyList<TimetableImportCandidate> _candidates = Array.Empty<TimetableImportCandidate>();
    private TimetableImportCandidate? _selectedCandidate;
    private WeeklyTimetableViewModel? _preview;
    private string _errorText = "";
    private bool _mappingConfirmed;
    private bool _applying;

    public TimetableImportSession(TimetableImportMode mode, Func<WeeklyTimetable, bool> tryCommit)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        ArgumentNullException.ThrowIfNull(tryCommit);
        Mode = mode;
        _tryCommit = tryCommit;
        ApplyCommand = new RelayCommand(() => TryApply(), () => CanApply);
        CancelCommand = new RelayCommand(Cancel);
    }

    public TimetableImportMode Mode { get; }
    public string ModeLabel => Mode == TimetableImportMode.School ? "학교 시간표 가져오기" : "표준 양식 가져오기";
    public bool IsSchool => Mode == TimetableImportMode.School;
    public IReadOnlyList<TimetableImportCandidate> Candidates => _candidates;
    public TimetableImportCandidate? SelectedCandidate
    {
        get => _selectedCandidate;
        set
        {
            if (IsClosed || _applying || ReferenceEquals(value, _selectedCandidate)) return;
            if (value is not null && !_candidates.Contains(value)) throw new ArgumentException("Unknown candidate.", nameof(value));
            _selectedCandidate = value;
            _preview = value is null ? null : new WeeklyTimetableViewModel(value.Timetable);
            _mappingConfirmed = false;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Preview));
            OnPropertyChanged(nameof(Evidence));
            OnPropertyChanged(nameof(MappingConfirmed));
            RefreshApply();
        }
    }
    public WeeklyTimetableViewModel? Preview => _preview;
    public string Evidence => SelectedCandidate?.Evidence ?? "가져올 후보를 선택하면 35셀 미리보기가 표시됩니다.";
    public bool MappingConfirmed
    {
        get => _mappingConfirmed;
        set { if (!IsClosed && !_applying && SetProperty(ref _mappingConfirmed, value)) RefreshApply(); }
    }
    public string ErrorText => _errorText;
    public bool IsClosed { get; private set; }
    public bool IsApplied { get; private set; }
    public bool CanApply => !IsClosed && !_applying && SelectedCandidate is not null && (!IsSchool || MappingConfirmed);
    public RelayCommand ApplyCommand { get; }
    public RelayCommand CancelCommand { get; }
    public event EventHandler? Completed;

    public void LoadText(string text)
    {
        if (IsClosed || _applying) return;
        ClearCandidates();
        SetProperty(ref _errorText, "", nameof(ErrorText));
        try
        {
            var table = ClipboardTable.Parse(text);
            _candidates = Mode == TimetableImportMode.School
                ? SchoolTimetableImporter.Import(table)
                : Array.AsReadOnly(new[] { CanonicalTimetableImporter.Import(table) });
            OnPropertyChanged(nameof(Candidates));
            if (!IsSchool) SelectedCandidate = _candidates[0];
        }
        catch (FormatException exception) { ReportReadError(exception.Message); }
    }

    public void ReportReadError(string error)
    {
        if (IsClosed || _applying) return;
        ClearCandidates();
        SetProperty(ref _errorText, error, nameof(ErrorText));
    }

    public bool TryApply()
    {
        if (!CanApply) return false;
        _applying = true;
        RefreshApply();
        try
        {
            if (!_tryCommit!(SelectedCandidate!.Timetable))
            {
                SetProperty(ref _errorText, "현재 시간표가 변경되었거나 셀 편집 중입니다. 취소 후 다시 가져와 주세요.", nameof(ErrorText));
                return false;
            }
            IsApplied = true;
            Complete();
            return true;
        }
        finally { _applying = false; RefreshApply(); }
    }

    public void Cancel() { if (!IsClosed && !_applying) Complete(); }
    private void Complete()
    {
        IsClosed = true;
        _tryCommit = null;
        RefreshApply();
        Completed?.Invoke(this, EventArgs.Empty);
        Completed = null;
    }
    private void ClearCandidates()
    {
        SelectedCandidate = null;
        _candidates = Array.Empty<TimetableImportCandidate>();
        OnPropertyChanged(nameof(Candidates));
        MappingConfirmed = false;
        RefreshApply();
    }
    private void RefreshApply()
    {
        OnPropertyChanged(nameof(CanApply));
        ApplyCommand.NotifyCanExecuteChanged();
    }
}
