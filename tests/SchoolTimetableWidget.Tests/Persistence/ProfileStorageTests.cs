using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;

namespace SchoolTimetableWidget.Tests.Persistence;

public class ProfileStorageTests
{
    internal static readonly DateOnly Monday = new(2026, 9, 7);
    internal static ProfileSnapshot Sample(bool lunch = true)
    {
        string[] texts = ["", "물리", " 😀 e\u0301 漢字", "\r\n줄\n바꿈\r", " leading", "trailing ", " \t "];
        var week = new WeeklyTimetable(WeeklyTimetable.Empty().Cells.Select((c, i) =>
            new TimetableCell(c.Day, c.PeriodNumber, new(texts[i % texts.Length], texts[(i + 1) % texts.Length]))));
        var schedule = new PeriodSchedule(DefaultPeriodSchedule.Periods.Select(p =>
            new PeriodDefinition(p.PeriodNumber, p.Start.Add(TimeSpan.FromTicks(123)), p.End.Add(TimeSpan.FromTicks(456)))));
        var day = DayTimetable.FromBase(week, SchoolDay.Monday);
        return new(week, schedule, [new(Monday, day, null), new(Monday.AddDays(1), null, schedule),
            new(Monday.AddDays(2), day, schedule)], lunch);
    }

    [Theory]
    [InlineData("ko-KR", true)] [InlineData("en-US", false)] [InlineData("ar-SA", true)]
    public void FullProfileRoundTripIsExactAndCultureIndependent(string culture, bool lunch)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            var original = Sample(lunch);
            var bytes = ProfileJson.Serialize(original);
            CultureInfo.CurrentCulture = new(culture);
            Assert.Equal(bytes, ProfileJson.Serialize(ProfileJson.Deserialize(bytes)));
            Assert.Equal(lunch, ProfileJson.Deserialize(bytes).ShowLunch);
            Assert.False(bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble));
            var parsed = JsonNode.Parse(bytes)!;
            Assert.Equal(3, (int)parsed["schemaVersion"]!);
            Assert.Equal("2026-09-07", (string?)parsed["profile"]!["dateOverrides"]![0]!["date"]);
            Assert.Equal("09:00:00.0000123", (string?)parsed["profile"]!["periodSchedule"]![0]!["start"]);
            Assert.Equal(new[] { "schemaVersion", "profile" }, parsed.AsObject().Select(p => p.Key));
            Assert.Equal(new[] { "timetable", "periodSchedule", "dateOverrides", "presentation", "display", "displayPresets" }, parsed["profile"]!.AsObject().Select(p => p.Key));
        }
        finally { CultureInfo.CurrentCulture = originalCulture; }
    }

    [Fact]
    public void MissingDoesNotCreateProfileAndFirstSaveRestoresEmptyDefaults()
    {
        using var temp = new TempProfile();
        using (var store = new JsonProfileStore(temp.Directory))
        {
            var load = store.Load();
            Assert.Equal(ProfileLoadState.Missing, load.State); Assert.True(load.CanWrite);
            Assert.False(File.Exists(temp.File));
            store.Save(load.Snapshot);
        }
        using var next = new JsonProfileStore(temp.Directory);
        var restored = next.Load();
        Assert.Equal(ProfileLoadState.Loaded, restored.State); Assert.True(restored.CanWrite);
        Assert.All(restored.Snapshot.Timetable.Cells, c => Assert.Equal(new("", ""), c.Value));
        Assert.Empty(restored.Snapshot.Overrides); Assert.False(restored.Snapshot.ShowLunch);
    }

    public static IEnumerable<object[]> InvalidDocuments()
    {
        yield return ["malformed", "{", ProfileLoadState.Invalid];
        yield return ["root null", "null", ProfileLoadState.Invalid];
        yield return ["root array", "[]", ProfileLoadState.Invalid];
        yield return ["version absent", "{}", ProfileLoadState.Invalid];
        yield return ["duplicate version", "{\"schemaVersion\":1,\"schemaVersion\":2}", ProfileLoadState.Invalid];
        (string Name, Action<JsonNode> Mutate)[] mutations =
        [
            ("missing slot", p => p["timetable"]!.AsArray().RemoveAt(0)),
            ("duplicate slot", p => p["timetable"]![1] = p["timetable"]![0]!.DeepClone()),
            ("invalid day", p => p["timetable"]![0]!["schoolDay"] = "Sunday"),
            ("numeric day", p => p["timetable"]![0]!["schoolDay"] = "0"),
            ("bad period", p => p["timetable"]![0]!["periodNumber"] = 8),
            ("null text", p => p["timetable"]![0]!["subjectText"] = null),
            ("missing class", p => p["timetable"]![0]!.AsObject().Remove("classText")),
            ("null cell", p => p["timetable"]![0] = null),
            ("null timetable", p => p["timetable"] = null),
            ("missing schedule", p => p.AsObject().Remove("periodSchedule")),
            ("null period", p => p["periodSchedule"]![0] = null),
            ("short schedule", p => p["periodSchedule"]!.AsArray().RemoveAt(0)),
            ("overlap", p => p["periodSchedule"]![0]!["end"] = "10:30:00.0000000"),
            ("reverse interval", p => p["periodSchedule"]![0]!["start"] = "19:00:00.0000000"),
            ("unordered schedule", p => p["periodSchedule"]![0]!["periodNumber"] = 7),
            ("bad time", p => p["periodSchedule"]![0]!["start"] = "9시"),
            ("bad date", p => p["dateOverrides"]![0]!["date"] = "2026-02-30"),
            ("weekend", p => p["dateOverrides"]![0]!["date"] = "2026-09-12"),
            ("wrong weekday", p => p["dateOverrides"]![0]!["timetable"]![0]!["schoolDay"] = "Tuesday"),
            ("short override", p => p["dateOverrides"]![0]!["timetable"]!.AsArray().RemoveAt(0)),
            ("duplicate override slot", p => p["dateOverrides"]![0]!["timetable"]![1]!["periodNumber"] = 1),
            ("null override cell", p => p["dateOverrides"]![0]!["timetable"]![0] = null),
            ("empty override", p => p["dateOverrides"]![0]!["timetable"] = null),
            ("null entry", p => p["dateOverrides"]![0] = null),
            ("duplicate date", p => p["dateOverrides"]!.AsArray().Add(p["dateOverrides"]![0]!.DeepClone())),
            ("bad override schedule", p => p["dateOverrides"]![1]!["periodSchedule"]![0]!["end"] = "18:00:00.0000000"),
            ("null option", p => p["presentation"]!["showLunchBetweenPeriods4And5"] = null),
            ("numeric option", p => p["presentation"]!["showLunchBetweenPeriods4And5"] = 1),
            ("missing option", p => p["presentation"]!.AsObject().Clear()),
            ("extra state", p => p["currentPeriod"] = 5),
            ("null profile child", p => p["presentation"] = null)
        ];
        foreach (var (name, mutate) in mutations)
        {
            var document = JsonNode.Parse(ProfileJson.Serialize(Sample()))!;
            mutate(document["profile"]!);
            yield return [name, document.ToJsonString(), ProfileLoadState.Invalid];
        }
        var future = JsonNode.Parse(ProfileJson.Serialize(Sample()))!;
        future["schemaVersion"] = 4;
        yield return ["future version", future.ToJsonString(), ProfileLoadState.Unsupported];
    }

    [Theory]
    [MemberData(nameof(InvalidDocuments))]
    public void InvalidLoadAndExitPreserveExactOriginalBytes(string name, string json, ProfileLoadState state)
    {
        Assert.NotEmpty(name);
        using var temp = new TempProfile();
        var bytes = Encoding.UTF8.GetBytes(json);
        File.WriteAllBytes(temp.File, bytes);
        var modified = File.GetLastWriteTimeUtc(temp.File);
        using (var store = new JsonProfileStore(temp.Directory))
        {
            var load = store.Load();
            Assert.Equal(state, load.State); Assert.False(load.CanWrite);
            Assert.Equal(ProfileJson.Serialize(ProfileSnapshot.Defaults()), ProfileJson.Serialize(load.Snapshot));
            Assert.Throws<IOException>(() => store.Save(Sample()));
            Assert.Contains("임시 실행", load.Notice); Assert.Contains(temp.File, load.Notice);
            Assert.Equal(bytes, File.ReadAllBytes(temp.File));
        }
        Assert.Equal(bytes, File.ReadAllBytes(temp.File));
        Assert.Equal(modified, File.GetLastWriteTimeUtc(temp.File));
        Assert.Equal(new[] { "profile.json", "profile.lock" }, System.IO.Directory.GetFiles(temp.Directory).Select(Path.GetFileName).Order());
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void FailedSaveKeepsPreviousCompleteFileAndCleansOwnedTemporary(int failureStage)
    {
        using var temp = new TempProfile();
        var fail = false;
        using var store = new JsonProfileStore(temp.Directory, stage =>
        { if (fail && (int)stage == failureStage) throw new IOException("injected"); });
        store.Load();
        store.Save(ProfileSnapshot.Defaults());
        store.Save(Sample());
        var previous = File.ReadAllBytes(temp.File);
        fail = true;
        Assert.Throws<IOException>(() => store.Save(Sample(false)));
        Assert.Equal(previous, File.ReadAllBytes(temp.File));
        Assert.Empty(System.IO.Directory.GetFiles(temp.Directory, "*.tmp"));
        fail = false; store.Save(Sample(false));
        Assert.False(ProfileJson.Deserialize(File.ReadAllBytes(temp.File)).ShowLunch);
    }

    [Fact]
    public void ActualRenameSharingFailurePreservesFileAndAllowsRetry()
    {
        using var temp = new TempProfile();
        using var store = new JsonProfileStore(temp.Directory); store.Load(); store.Save(Sample());
        var previous = File.ReadAllBytes(temp.File);
        using (var blocker = new FileStream(temp.File, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var error = Record.Exception(() => store.Save(Sample(false)));
            Assert.True(error is IOException or UnauthorizedAccessException);
        }
        Assert.Equal(previous, File.ReadAllBytes(temp.File));
        Assert.Empty(System.IO.Directory.GetFiles(temp.Directory, "*.tmp"));
        store.Save(Sample(false));
    }

    [Fact]
    public void SecondStoreCannotWriteAndExternalEditsAreNotOverwritten()
    {
        using var temp = new TempProfile();
        using var a = new JsonProfileStore(temp.Directory); a.Load(); a.Save(Sample());
        using (var b = new JsonProfileStore(temp.Directory))
        { Assert.Equal(ProfileLoadState.Unavailable, b.Load().State); Assert.Throws<IOException>(() => b.Save(Sample(false))); }
        File.WriteAllText(temp.File, "externally changed");
        Assert.Throws<IOException>(() => a.Save(Sample(false)));
        Assert.Equal("externally changed", File.ReadAllText(temp.File));
    }
}

internal sealed class TempProfile : IDisposable
{
    public string Directory { get; } = Path.Combine(Path.GetTempPath(), "school-profile-test-" + Guid.NewGuid().ToString("N"));
    public string File => Path.Combine(Directory, "profile.json");
    public TempProfile() => System.IO.Directory.CreateDirectory(Directory);
    public void Dispose()
    {
        var path = Path.GetFullPath(Directory);
        if (!path.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(path).StartsWith("school-profile-test-", StringComparison.Ordinal))
            throw new InvalidOperationException("Unsafe diagnostic cleanup.");
        System.IO.Directory.Delete(path, recursive: true);
    }
}
