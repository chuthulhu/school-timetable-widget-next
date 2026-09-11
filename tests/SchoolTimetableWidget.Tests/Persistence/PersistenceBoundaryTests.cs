using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SchoolTimetableWidget.Desktop.Development;
using SchoolTimetableWidget.Desktop.Features.DateOverrides;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.Persistence;

public class PersistenceBoundaryTests
{
    [Fact]
    public void DegradedLunchMenuReturnsToCommittedCheckStateAfterObjectClick() => HighlightTestDispatcher.Run(() =>
    {
        using var temp = new TempProfile(); File.WriteAllText(temp.File, "{");
        using var store = new JsonProfileStore(temp.Directory);
        var runtime = new ProfileRuntime(new(store), () => throw new Exception("No refresh"), _ => { });
        var view = new WeeklyTimetableView { DataContext = runtime.Timetable, LunchOption = runtime.Lunch };
        view.ContextMenu.PlacementTarget = view;
        view.ContextMenu.Measure(new(800, 600)); view.ContextMenu.Arrange(new Rect(0, 0, 800, 600));
        view.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        var menu = Assert.Single(view.ContextMenu.Items.OfType<MenuItem>(), item => item.Command == DateOverrideCommands.Lunch);
        Assert.False(menu.IsChecked);
        typeof(MenuItem).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(menu, null);
        view.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        Assert.False(runtime.Lunch.Enabled); Assert.False(menu.IsChecked);
        Assert.Contains("저장", runtime.Lunch.ErrorText);
        Assert.Equal("{", File.ReadAllText(temp.File));
    });

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void FirstWriteFailureDoesNotLeaveAnEmptyDestination(int stage)
    {
        using var temp = new TempProfile();
        using var store = new JsonProfileStore(temp.Directory, point => { if ((int)point == stage) throw new IOException("injected"); });
        var session = new ProfileSession(store); var old = session.Current;
        Assert.NotNull(session.SaveLunch(true)); Assert.Same(old, session.Current);
        Assert.False(File.Exists(temp.File)); Assert.Empty(Directory.GetFiles(temp.Directory, "*.tmp"));
    }

    [Fact]
    public void DirectoryAndProfileAccessFailuresAreNotTreatedAsMissing()
    {
        using var temp = new TempProfile();
        var blocker = Path.Combine(temp.Directory, "not-a-directory"); File.WriteAllText(blocker, "keep");
        using var badDirectory = new JsonProfileStore(blocker);
        Assert.Equal(ProfileLoadState.Unavailable, badDirectory.Load().State);
        Assert.Equal("keep", File.ReadAllText(blocker));
        Directory.CreateDirectory(temp.File);
        using var badFile = new JsonProfileStore(temp.Directory);
        Assert.Equal(ProfileLoadState.Unavailable, badFile.Load().State);
        Assert.True(Directory.Exists(temp.File));
    }

    [Fact]
    public void StaleFeatureTargetsDoNotSaveAndPreviewSeedCannotReplaceLoadedData()
    {
        using var temp = new TempProfile(); var saves = 0;
        using (var store = new JsonProfileStore(temp.Directory, stage => { if (stage == ProfileWriteStage.BeforeRename) saves++; }))
        {
            var runtime = new ProfileRuntime(new(store), () => { }, _ => { });
            var a = runtime.ScheduleEditor.CreateSession(); var b = runtime.ScheduleEditor.CreateSession();
            a.Rows[0].StartText = "08:00"; Assert.True(a.TryApply());
            var before = File.ReadAllBytes(temp.File);
            Assert.False(b.TryApply()); Assert.Equal(1, saves); Assert.Equal(before, File.ReadAllBytes(temp.File));
        }
        using var loaded = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(loaded, ProfileStorageTests.Sample());
        Assert.Empty(session.Current.Overrides); Assert.False(session.Current.ShowLunch);
        Assert.Equal(new TimeOnly(8, 0), session.Current.Schedule.Periods[0].Start);
    }

    [Fact]
    public void SnapshotCopiesOverrideCollectionAndRejectsDuplicateDates()
    {
        var sample = ProfileStorageTests.Sample(); var entries = sample.Overrides.ToList();
        var copy = new ProfileSnapshot(sample.Timetable, sample.Schedule, entries, sample.ShowLunch);
        entries.Clear(); Assert.Equal(3, copy.Overrides.Count);
        Assert.Throws<ArgumentException>(() => new ProfileSnapshot(sample.Timetable, sample.Schedule,
            [sample.Overrides[0], sample.Overrides[0]], false));
    }

    [Fact]
    public void DevelopmentPathsAreExplicitTemporary()
    {
        Assert.Null(DevelopmentProfileLocation.FromArguments([]));
        using var temp = new TempProfile();
        Assert.Equal(temp.Directory, DevelopmentProfileLocation.FromArguments(["--dev-profile-directory=" + temp.Directory]));
        Assert.Throws<ArgumentException>(() => DevelopmentProfileLocation.FromArguments(["--dev-profile-directory=relative"]));
        Assert.Throws<ArgumentException>(() => DevelopmentProfileLocation.FromArguments(["--dev-profile-directory=" + Path.GetPathRoot(temp.Directory)]));
        var a = DevelopmentProfileLocation.CreateTemporary(); var b = DevelopmentProfileLocation.CreateTemporary();
        Assert.NotEqual(a, b); Assert.StartsWith(Path.GetTempPath(), a); Assert.False(Directory.Exists(a));
    }
}
