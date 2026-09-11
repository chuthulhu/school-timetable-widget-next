using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Desktop.Features.DateOverrides;
using SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;
using SchoolTimetableWidget.Desktop.Features.Timetable;

namespace SchoolTimetableWidget.Desktop.Features.Persistence;

/// <summary>Composes existing feature owners with the same durable transaction callbacks.</summary>
public sealed class ProfileRuntime
{
    public ProfileRuntime(ProfileSession session, Action refresh, Action<DateOnly> refreshDate)
    {
        Session = session;
        var initial = session.Current;
        Timetable = new(initial.Timetable, session.SaveTimetable);
        Schedule = new(initial.Schedule, session.SaveSchedule);
        Overrides = new(initial.Overrides, session.SaveOverrides);
        Lunch = new(refresh, initial.ShowLunch, session.SaveLunch);
        DateEditor = new(Overrides, () => Timetable.CommittedTimetable, () => Schedule.Current, refreshDate);
        Timetable.Editor.DateEditor = DateEditor;
        ScheduleEditor = new(Schedule, refresh);
    }
    public ProfileSession Session { get; }
    public WeeklyTimetableViewModel Timetable { get; }
    public RuntimePeriodSchedule Schedule { get; }
    public RuntimeDateOverrides Overrides { get; }
    public LunchPresentationOption Lunch { get; }
    public DateOverrideEditor DateEditor { get; }
    public PeriodScheduleEditor ScheduleEditor { get; }
    public EffectiveDayConfiguration Resolve(DateOnly date) =>
        EffectiveDayResolver.Resolve(date, Timetable.CommittedTimetable, Schedule.Current, Overrides.Get(date));
}
