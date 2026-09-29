using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using SchoolTimetableWidget.Desktop.Features.Semesters;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Desktop.Features.DateOverrides;
using SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;
using SchoolTimetableWidget.Desktop.Features.Timetable;

namespace SchoolTimetableWidget.Desktop.Features.Persistence;

/// <summary>Composes existing feature owners with the same durable transaction callbacks.</summary>
public sealed class ProfileRuntime
{
    private readonly Action _refresh;
    public ProfileRuntime(ProfileSession session, Action refresh, Action<DateOnly> refreshDate, FontLibrary? fonts = null)
    {
        Session = session;
        _refresh = refresh;
        var initial = session.Current;
        Display = new(initial.Display, initial.DisplayPresets, session.SaveDisplay, fonts);
        Display.AllowEditing = () => !session.IsRecoveryRequired;
        Timetable = new(initial.Timetable, session.SaveTimetable);
        Schedule = new(initial.Schedule, session.SaveSchedule);
        Overrides = new(initial.Overrides, session.SaveOverrides);
        Timetable.GetSemesterId = () => Session.Current.ActiveSemesterId;
        Timetable.GetSemesterName = () => Session.Current.ActiveSemester.DisplayName;
        Schedule.GetSemesterId = () => Session.Current.ActiveSemesterId;
        Overrides.GetSemesterId = () => Session.Current.ActiveSemesterId;
        Semesters = new(session, PublishSemester, () => CanReplace);
        Timetable.ConfigureDateOverrides(Overrides.Get);
        Lunch = new(refresh, initial.ShowLunch, session.SaveLunch);
        DateEditor = new(Overrides, () => Timetable.CommittedTimetable, () => Schedule.Current, date =>
        {
            if (Timetable.DisplayedDates.Contains(date)) Timetable.RefreshDisplayedWeek();
            refreshDate(date);
        });
        Timetable.Editor.DateEditor = DateEditor;
        ScheduleEditor = new(Schedule, refresh);
    }
    public SemesterManagement Semesters { get; }
    public RuntimeDisplaySettings Display { get; }
    public ProfileSession Session { get; }
    public WeeklyTimetableViewModel Timetable { get; }
    public RuntimePeriodSchedule Schedule { get; }
    public RuntimeDateOverrides Overrides { get; }
    public LunchPresentationOption Lunch { get; }
    public DateOverrideEditor DateEditor { get; }
    public PeriodScheduleEditor ScheduleEditor { get; }
    public bool CanReplace => !Display.HasActiveSession && Timetable.Editor.ActiveSession is null;
    public string? Restore(ProfileSnapshot candidate)
    {
        if (!CanReplace) return "편집 창을 닫은 뒤 복원해 주세요.";
        var error = Session.Restore(candidate, Publish);
        if (error is not null)
        {
            try { Publish(Session.Current); }
            catch (Exception refreshError) { System.Diagnostics.Debug.WriteLine(refreshError); }
        }
        return error;
    }
    public string? Recover()
    {
        if (!CanReplace) return "편집 창을 닫은 뒤 복구해 주세요.";
        var error = Session.Recover(Publish);
        if (error is not null)
        {
            try { Publish(Session.Current); }
            catch (Exception refreshError) { System.Diagnostics.Debug.WriteLine(refreshError); }
        }
        return error;
    }
    private void RestoreValues(ProfileSnapshot value)
    {
        // Set every canonical owner before any observer is notified.
        Overrides.RestoreValues(value.Overrides);
        Schedule.RestoreValue(value.Schedule);
        Lunch.RestoreValue(value.ShowLunch);
        Display.RestoreValues(value.Display, value.DisplayPresets);
        Timetable.RestoreValue(value.Timetable);
    }
    private void PublishSemester(ProfileSnapshot value)
    {
        Overrides.RestoreValues(value.Overrides);
        Schedule.RestoreValue(value.Schedule);
        Timetable.RestoreValue(value.Timetable);
        Timetable.NotifyRestored();
        _refresh();
    }
    private void Publish(ProfileSnapshot value)
    {
        RestoreValues(value);
        var failures = new List<Exception>();
        foreach (var notify in new Action[] { Timetable.NotifyRestored, Display.NotifyRestored, Lunch.NotifyRestored, Semesters.NotifyRestored, _refresh })
            try { notify(); } catch (Exception error) { failures.Add(error); }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
    public EffectiveDayConfiguration Resolve(DateOnly date)
    {
        var semester = Session.Current.ActiveSemester;
        return EffectiveDayResolver.Resolve(date, semester.Timetable, semester.Schedule,
            semester.Overrides.FirstOrDefault(e => e.Date == date)) with { SemesterId = semester.SemesterId };
    }
}
