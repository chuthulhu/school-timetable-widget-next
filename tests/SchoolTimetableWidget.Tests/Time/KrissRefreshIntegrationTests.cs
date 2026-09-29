using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Desktop.Infrastructure.Time;
using SchoolTimetableWidget.Tests.Timetable;
using SchoolTimetableWidget.Tests.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using System.IO;
using System.Text.Json;

namespace SchoolTimetableWidget.Tests.Time;

public class KrissRefreshIntegrationTests
{
    [Fact]
    public void ExistingProfileAndMachineLocalBytesStayUntouchedBySyncRefresh() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile();
        using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store);
        Assert.Null(session.SaveLunch(true));
        var paths = new[] { store.FilePath, Path.Combine(temp.Directory, "window-state.json"), Path.Combine(temp.Directory, "tray-state.json") };
        File.WriteAllText(paths[1], "window sentinel"); File.WriteAllText(paths[2], "tray sentinel");
        var before = paths.Select(p => (Bytes: File.ReadAllBytes(p), Modified: File.GetLastWriteTimeUtc(p))).ToArray();
        var runtime = new ProfileRuntime(session, () => { }, _ => { });
        var time = new ManualTimeProvider(); var clock = KrissClockTests.Create(time);
        using var loop = new CurrentStatusRefreshLoop(clock, runtime.Resolve, new(), runtime.Timetable, () => runtime.Lunch.Enabled);
        loop.Start(); clock.Apply(NtpSampleSelectionTests.Sample(1000, 10)); loop.RefreshNow();
        clock.Fail("Offline"); loop.RefreshNow();
        for (var i = 0; i < paths.Length; i++)
        {
            Assert.Equal(before[i].Bytes, File.ReadAllBytes(paths[i]));
            Assert.Equal(before[i].Modified, File.GetLastWriteTimeUtc(paths[i]));
        }
        using var json = JsonDocument.Parse(before[0].Bytes);
        Assert.Equal(5, json.RootElement.GetProperty("schemaVersion").GetInt32());
    });

    [Fact]
    public void ActualSourceSwapDuringRefreshKeepsWholeFrameThenChangesDateTogether() => HighlightTestDispatcher.Run(() =>
    {
        var time = new ManualTimeProvider();
        var fallback = new FakeApplicationClock(new(new(2026, 9, 28, 9, 49, 59, TimeSpan.FromHours(-7)), ApplicationTimeSource.PcLocalFallback, 0));
        var clock = new SynchronizedApplicationClock(fallback, time);
        var header = new CurrentStatusHeaderViewModel(); var timetable = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var changedDate = new DateOnly(2026, 9, 29);
        var entry = new DateSpecificOverride(changedDate, null, new PeriodSchedule(DefaultPeriodSchedule.Periods));
        timetable.ConfigureDateOverrides(d => d == changedDate ? entry : null);
        var resolutions = new List<DateOnly>();
        using var loop = new CurrentStatusRefreshLoop(clock, date =>
        {
            resolutions.Add(date);
            // Complete a real synchronized reference after this frame has already captured PC time.
            clock.Apply(new(TimeSpan.Zero, TimeSpan.Zero, new(2026, 9, 29, 1, 0, 0, TimeSpan.Zero), 0));
            return EffectiveDayResolver.Resolve(date, timetable.CommittedTimetable, new(DefaultPeriodSchedule.Periods), date == changedDate ? entry : null);
        }, header, timetable, () => false);
        loop.RefreshNow();
        Assert.Equal(1, fallback.ReadCount);
        Assert.Equal("2026년 09월 28일", header.CurrentDateText);
        Assert.Equal("09:49:59", header.CurrentTimeText);
        Assert.Equal("1교시 · 종료까지 1분 미만", header.StatusText);
        Assert.Same(timetable.Cells[0], Assert.Single(timetable.Cells, c => c.IsCurrent));
        var viewedWeek = timetable.ViewedWeekStart;
        loop.RefreshNow();
        Assert.Equal("2026년 09월 29일", header.CurrentDateText);
        Assert.Equal("10:00:00", header.CurrentTimeText);
        Assert.Equal("2교시 · 종료까지 50분", header.StatusText);
        Assert.Same(timetable.Cells[6], Assert.Single(timetable.Cells, c => c.IsCurrent));
        Assert.Same(entry, loop.CurrentConfiguration!.DateOverride);
        Assert.Equal(changedDate, Assert.Single(timetable.Columns, c => c.IsToday).Date);
        Assert.Equal(viewedWeek, timetable.ViewedWeekStart);
        timetable.PreviousWeekCommand.Execute(null); viewedWeek = timetable.ViewedWeekStart;
        loop.RefreshNow();
        Assert.Equal(viewedWeek, timetable.ViewedWeekStart);
        Assert.DoesNotContain(timetable.Cells, c => c.IsCurrent);
        Assert.Equal(1, fallback.ReadCount);
        Assert.Equal(new[] { new DateOnly(2026, 9, 28), changedDate, changedDate }, resolutions);
    });

    [Theory]
    [InlineData(ProfileLoadState.Loaded)] [InlineData(ProfileLoadState.Invalid)] [InlineData(ProfileLoadState.RecoveryRequired)]
    public void SyncAndFailureNeverSaveOrChangeSemesterOrRecoveryState(ProfileLoadState state) => HighlightTestDispatcher.Run(() =>
    {
        var store = new GuardedStore(state); var session = new ProfileSession(store);
        var current = session.Current; var load = session.LoadResult;
        var runtime = new ProfileRuntime(session, () => { }, _ => { });
        var time = new ManualTimeProvider(); var clock = KrissClockTests.Create(time);
        using var loop = new CurrentStatusRefreshLoop(clock, runtime.Resolve, new(), runtime.Timetable, () => runtime.Lunch.Enabled);
        loop.Start(); clock.Apply(NtpSampleSelectionTests.Sample(0, 10)); loop.RefreshNow();
        clock.BeginSynchronization(); clock.Fail("Offline"); loop.RefreshNow();
        Assert.Same(current, session.Current);
        Assert.Same(load, session.LoadResult);
        Assert.Equal(current.ActiveSemesterId, loop.CurrentConfiguration!.SemesterId);
        Assert.Equal(0, store.Saves);
    });

    private sealed class GuardedStore(ProfileLoadState state) : IProfileStore
    {
        public int Saves;
        public ProfileLoadResult Load() => new(ProfileSnapshot.Defaults(), state, "test-only");
        public void Save(ProfileSnapshot snapshot) { Saves++; throw new InvalidOperationException("Clock cannot save."); }
    }
}
