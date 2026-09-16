using System.IO;
using System.Text.Json.Nodes;
using SchoolTimetableWidget.Desktop.Features.WindowPlacement;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.Persistence;

namespace SchoolTimetableWidget.Tests.WindowPlacement;

public class WindowPlacementStateTests
{
    [Fact]
    public void MissingStateUsesDefaultWithoutCreatingFiles()
    {
        using var temp = new TempProfile();
        var store = new JsonWindowStateStore(temp.Directory);
        var session = new WindowPlacementSession(store);
        Assert.Equal(PreferredWindowBounds.Default, session.Preferred);
        session.Flush();
        Assert.False(File.Exists(store.FilePath));
    }

    [Fact]
    public void CompletedMoveAndResizeRoundTripWithoutTouchingKoreanProfile()
    {
        using var temp = new TempProfile();
        var path = Path.Combine(temp.Directory, "profile.json");
        File.WriteAllText(path, "한글 시간표 원본\n교과/반");
        var bytes = File.ReadAllBytes(path); var modified = File.GetLastWriteTimeUtc(path);
        var store = new JsonWindowStateStore(temp.Directory);
        var session = new WindowPlacementSession(store);
        var start = session.Preferred with { MonitorHint = "LEFT" };
        session.Begin(start); session.Moving();
        var moved = start with { Left = -20, Top = 300 };
        session.End(moved);
        Assert.Equal(moved, new WindowPlacementSession(store).Preferred);
        session.Begin(moved); session.Sizing(8);
        var resized = moved with { Width = 780, Height = 700 };
        session.End(resized);
        Assert.Equal(resized, new WindowPlacementSession(store).Preferred);
        Assert.Equal(bytes, File.ReadAllBytes(path));
        Assert.Equal(modified, File.GetLastWriteTimeUtc(path));
    }

    [Fact]
    public void AutoGrowthAndRepositionNeverBecomePreferredEvenAtExitOrRestart()
    {
        using var temp = new TempProfile();
        var store = new JsonWindowStateStore(temp.Directory);
        var preferred = new PreferredWindowBounds(780, 700, 120, 300, "MAIN");
        Assert.True(store.Save(preferred));
        var session = new WindowPlacementSession(store);
        var before = File.ReadAllBytes(store.FilePath); var modified = File.GetLastWriteTimeUtc(store.FilePath);
        var area = new WindowWorkArea("MAIN", 0, 0, 1400, 1100, 1);
        for (var i = 0; i < 100; i++)
        {
            var applied = WindowBoundsCalculator.Fit(session.Preferred, area, 400, 1020);
            Assert.Equal(1020, applied.Height); Assert.Equal(80, applied.TopPixels);
            session.Flush();
        }
        Assert.Equal(before, File.ReadAllBytes(store.FilePath));
        Assert.Equal(modified, File.GetLastWriteTimeUtc(store.FilePath));
        var restarted = new WindowPlacementSession(store);
        var shortContent = WindowBoundsCalculator.Fit(restarted.Preferred, area, 400, 500);
        Assert.Equal(700, shortContent.Height); Assert.Equal(300, shortContent.TopPixels);
    }

    [Fact]
    public void MoveAfterAutoAdjustmentUpdatesPositionWithoutPersistingAutoSize()
    {
        var store = new MemoryWindowStore(new(780, 700, 120, 300, "MAIN"));
        var session = new WindowPlacementSession(store);
        var auto = session.Preferred with { Height = 980, Top = 80 };
        session.Begin(auto); session.Moving();
        for (var i = 0; i < 100; i++) session.Moving();
        Assert.Equal(0, store.Writes);
        session.End(auto with { Top = 120 });
        Assert.Equal(120, session.Preferred.Top); Assert.Equal(700, session.Preferred.Height);
        Assert.Equal(1, store.Writes);
    }

    [Theory]
    [InlineData(2, 850, 980, 850, 700)]
    [InlineData(6, 780, 1000, 780, 1000)]
    [InlineData(8, 850, 1000, 850, 1000)]
    [InlineData(6, 780, 980, 780, 700)]
    public void ResizeRecordsOnlyAcceptedChangedAxes(int edge, double width, double height, double expectedWidth, double expectedHeight)
    {
        var session = new WindowPlacementSession(new MemoryWindowStore(new(780, 700, 120, 300, "MAIN")));
        var auto = session.Preferred with { Height = 980, Top = 80 };
        session.Begin(auto); session.Sizing(edge); session.End(auto with { Width = width, Height = height });
        Assert.Equal(expectedWidth, session.Preferred.Width); Assert.Equal(expectedHeight, session.Preferred.Height);
        Assert.Equal(300, session.Preferred.Top);
    }

    [Fact]
    public void MonitorCrossingMovePreservesLogicalSizeIncludingAutoGrownHeight()
    {
        var session = new WindowPlacementSession(new MemoryWindowStore(new(780, 700, 40, 300, "A")));
        session.Begin(session.Preferred with { Height = 980, Top = 80 }); session.Moving();
        session.End(new(780, 980, 55, 20, "B"));
        Assert.Equal(new(780, 700, 55, 20, "B"), session.Preferred);
    }

    [Fact]
    public void ProgrammaticEventsMissingBeginClickWithoutMovementAndNonNormalEndDoNotWrite()
    {
        var store = new MemoryWindowStore(PreferredWindowBounds.Default);
        var session = new WindowPlacementSession(store);
        session.Moving(); session.Sizing(8); session.End(new(900, 900, 0, 0, "B"));
        session.Begin(session.Preferred); session.End(session.Preferred);
        session.Begin(session.Preferred); session.Moving(); session.End(null);
        Assert.Equal(0, store.Writes); Assert.Equal(PreferredWindowBounds.Default, session.Preferred);
    }

    [Theory]
    [InlineData(1)] [InlineData(1.25)] [InlineData(1.5)]
    public void PhysicalNegativeMonitorOriginAndDipSizeFitAtEachDpi(double scale)
    {
        var area = new WindowWorkArea("LEFT", -1920, -240, 1920, 1200, scale);
        var preferred = new PreferredWindowBounds(780, 700, 100, 80, "LEFT");
        var applied = WindowBoundsCalculator.Fit(preferred, area, 400, 500);
        Assert.Equal(780, applied.Width); Assert.Equal(700, applied.Height);
        Assert.Equal(-1920 + 100 * scale, applied.LeftPixels); Assert.Equal(-240 + 80 * scale, applied.TopPixels);
        Assert.True(applied.TopPixels + applied.Height * scale <= 960);
    }

    [Theory]
    [InlineData(800, 600)] [InlineData(2560, 1400)] [InlineData(1920, 1000)]
    public void CurrentResolutionAndTaskbarReservationsWinWithoutChangingPreference(double width, double height)
    {
        var preferred = new PreferredWindowBounds(1800, 1200, 300, 300, "MAIN");
        var area = new WindowWorkArea("MAIN", 40, 60, width, height, 1.25);
        var bounds = WindowBoundsCalculator.Fit(preferred, area, 400, 1500);
        Assert.InRange(bounds.LeftPixels, area.LeftPixels, area.LeftPixels + width);
        Assert.InRange(bounds.TopPixels, area.TopPixels, area.TopPixels + height);
        Assert.True(bounds.LeftPixels + bounds.Width * area.Scale <= area.LeftPixels + width);
        Assert.True(bounds.TopPixels + bounds.Height * area.Scale <= area.TopPixels + height);
        Assert.Equal(1200, preferred.Height);
    }

    [Theory]
    [InlineData("windowStateVersion", "2")]
    [InlineData("preferredWidth", "0")]
    [InlineData("preferredHeight", "-1")]
    [InlineData("preferredWidth", "100000000")]
    [InlineData("left", "1e100")]
    [InlineData("top", "-1e100")]
    [InlineData("preferredHeight", "\"NaN\"")]
    [InlineData("preferredHeight", "\"Infinity\"")]
    [InlineData("preferredWidth", "null")]
    public void InvalidStateFallsBackAndLeavesOriginalUntouched(string field, string value)
    {
        using var temp = new TempProfile(); var store = new JsonWindowStateStore(temp.Directory);
        Assert.True(store.Save(PreferredWindowBounds.Default));
        var json = JsonNode.Parse(File.ReadAllText(store.FilePath))!;
        json[field] = JsonNode.Parse(value);
        File.WriteAllText(store.FilePath, json.ToJsonString());
        var before = File.ReadAllBytes(store.FilePath);
        Assert.Equal(PreferredWindowBounds.Default, new WindowPlacementSession(store).Preferred);
        Assert.Null(store.Load()); Assert.Equal(before, File.ReadAllBytes(store.FilePath));
    }

    [Theory]
    [InlineData("{")] [InlineData("null")] [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"windowStateVersion\":1,\"windowStateVersion\":2}")]
    public void MalformedOrIncompleteStateDoesNotPreventStartup(string json)
    {
        using var temp = new TempProfile(); var store = new JsonWindowStateStore(temp.Directory);
        File.WriteAllText(store.FilePath, json);
        Assert.Equal(PreferredWindowBounds.Default, new WindowPlacementSession(store).Preferred);
        Assert.Equal(json, File.ReadAllText(store.FilePath));
    }

    [Fact]
    public void FailedRenameKeepsCompletePreviousStateAndRetriesOnlyDirtyIntent()
    {
        using var temp = new TempProfile(); var store = new JsonWindowStateStore(temp.Directory);
        Assert.True(store.Save(PreferredWindowBounds.Default));
        var before = File.ReadAllBytes(store.FilePath);
        store.BeforeRename = () => throw new IOException("injected rename failure");
        var session = new WindowPlacementSession(store);
        session.Begin(session.Preferred); session.Moving(); session.End(session.Preferred with { Top = 120 });
        Assert.Equal(before, File.ReadAllBytes(store.FilePath));
        Assert.Empty(Directory.GetFiles(temp.Directory, ".stw-*.tmp"));
        store.BeforeRename = null; session.Flush();
        Assert.Equal(120, store.Load()!.Top);
    }

    [Fact]
    public void ResetPersistsDefaultsAndKeepsProfilePresetsAndRecoveryEvidenceUnchanged()
    {
        using var temp = new TempProfile();
        using var profile = new JsonProfileStore(temp.Directory);
        var session = new SchoolTimetableWidget.Desktop.Features.Persistence.ProfileSession(profile);
        var candidate = ProfileBackupFileTests.Sample();
        Assert.Null(session.Restore(candidate, _ => { }));
        var profilePath = Path.Combine(temp.Directory, "profile.json");
        var bytes = File.ReadAllBytes(profilePath);
        var store = new JsonWindowStateStore(temp.Directory);
        Assert.True(store.Save(new(900, 850, 800, 900, "MISSING")));
        var placement = new WindowPlacementSession(store); placement.Reset("MAIN");
        Assert.Equal(PreferredWindowBounds.Default with { MonitorHint = "MAIN" }, store.Load());
        Assert.Equal(bytes, File.ReadAllBytes(profilePath));
    }
}

internal sealed class MemoryWindowStore(PreferredWindowBounds? saved = null) : IWindowStateStore
{
    public PreferredWindowBounds? Saved { get; private set; } = saved;
    public int Writes { get; private set; }
    public PreferredWindowBounds? Load() => Saved;
    public bool Save(PreferredWindowBounds bounds) { Writes++; Saved = bounds; return true; }
}
