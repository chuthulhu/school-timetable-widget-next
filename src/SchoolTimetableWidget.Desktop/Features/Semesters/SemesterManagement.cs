using CommunityToolkit.Mvvm.ComponentModel;
using SchoolTimetableWidget.Core.Features.Semesters;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.Persistence;

namespace SchoolTimetableWidget.Desktop.Features.Semesters;

/// <summary>Semester transactions; persistence accepts a whole candidate before runtime publication.</summary>
public sealed class SemesterManagement(ProfileSession session, Action<ProfileSnapshot> publish, Func<bool> canChange) : ObservableObject
{
    private bool _busy;
    public IReadOnlyList<SemesterSet> Items => session.Current.SemesterSets;
    public Guid ActiveSemesterId => session.Current.ActiveSemesterId;
    public bool CanChange => !_busy && session.LoadResult.CanWrite && canChange();
    public bool CanDelete(Guid id) => CanChange && Items.Count > 1 && id != ActiveSemesterId && Items.Any(s => s.SemesterId == id);
    public string? Activate(Guid id) => id == ActiveSemesterId ? null : Change(p => p.WithSemesters(p.SemesterSets, id));
    public string? Create(string name, bool copyTimetable) => Change(p =>
    {
        var source = p.ActiveSemester;
        var item = new SemesterSet(Guid.NewGuid(), name.Trim(), copyTimetable ? source.Timetable : WeeklyTimetable.Empty(), source.Schedule, []);
        return p.WithSemesters(p.SemesterSets.Append(item), item.SemesterId);
    });
    public string? Rename(Guid id, string name) => Change(p => p.ReplaceSemester(
        p.SemesterSets.Single(s => s.SemesterId == id).WithName(name.Trim())));
    public string? Delete(Guid id, bool confirmed)
    {
        if (!confirmed) return "삭제가 취소되었습니다.";
        if (!CanDelete(id)) return "현재 학기와 마지막 학기는 삭제할 수 없습니다. 먼저 다른 학기를 선택해 주세요.";
        return Change(p => p.WithSemesters(p.SemesterSets.Where(s => s.SemesterId != id), p.ActiveSemesterId));
    }
    private string? Change(Func<ProfileSnapshot, ProfileSnapshot> create)
    {
        if (!CanChange) return "현재는 학기를 변경할 수 없습니다. 열린 편집 창을 닫고 저장 상태를 확인해 주세요.";
        _busy = true;
        try
        {
            ProfileSnapshot candidate;
            try { candidate = create(session.Current); }
            catch (ArgumentException e) { return e.Message; }
            catch (InvalidOperationException) { return "학기를 찾을 수 없습니다. 목록을 다시 확인해 주세요."; }
            var error = session.SaveSemesters(candidate);
            if (error is not null) return error;
            publish(candidate);
            return null;
        }
        finally { _busy = false; NotifyRestored(); }
    }
    internal void NotifyRestored()
    {
        OnPropertyChanged(nameof(Items)); OnPropertyChanged(nameof(ActiveSemesterId)); OnPropertyChanged(nameof(CanChange));
    }
}
