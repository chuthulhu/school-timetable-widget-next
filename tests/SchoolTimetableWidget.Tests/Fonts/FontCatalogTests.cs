using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using SchoolTimetableWidget.Desktop.Features.Fonts;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Infrastructure.Fonts;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Tests.DisplaySettings;
using SchoolTimetableWidget.Tests.Persistence;

namespace SchoolTimetableWidget.Tests.Fonts;

public class FontCatalogTests
{
    [Fact]
    public void CatalogIdentitiesAndPinnedMetadataAreCompleteAndSafe()
    {
        var entries = FontCatalog.Entries;
        Assert.Equal(3, entries.Count(e => e.Source == FontSourceKind.Bundled));
        Assert.Equal(2, entries.Count(e => e.Source == FontSourceKind.OnlineDownloaded));
        Assert.Equal(entries.Count, entries.Select(e => (e.Source, e.Id)).Distinct().Count());
        foreach (var entry in entries)
        {
            Assert.Matches("^[a-z0-9-]+$", entry.Id);
            Assert.Matches("^[a-f0-9]{40}$", entry.SourceCommit);
            Assert.Matches("^[a-f0-9]{64}$", entry.Sha256);
            Assert.Equal("https", new Uri(entry.SourceUrl).Scheme);
            Assert.Equal("https", new Uri(entry.Upstream).Scheme);
            Assert.EndsWith(".ttf", entry.FileName);
            Assert.Equal(Path.GetFileName(entry.FileName), entry.FileName);
            Assert.InRange(entry.Size, 12, DownloadedFontCache.MaximumBytes);
            Assert.Equal("SIL Open Font License 1.1", entry.License);
            Assert.NotEmpty(entry.LicenseFile); Assert.NotEmpty(entry.ReservedFontName);
            Assert.NotEmpty(entry.Version); Assert.NotEmpty(entry.FamilyName); Assert.NotEmpty(entry.DisplayName);
            Assert.Equal(entry, FontCatalog.Find(entry.Selection));
            Assert.Null(FontCatalog.Find(entry.Selection with { Source = FontSourceKind.System }));
            if (entry.Source == FontSourceKind.OnlineDownloaded)
            {
                Assert.Equal("raw.githubusercontent.com", new Uri(entry.SourceUrl).Host);
                Assert.Contains(entry.SourceCommit, entry.SourceUrl);
            }
        }
    }
    [Theory]
    [InlineData(FontSourceKind.Bundled, "missing", "Missing")]
    [InlineData(FontSourceKind.Bundled, "pretendard", "Wrong name")]
    [InlineData(FontSourceKind.OnlineDownloaded, "../orbitron", "Orbitron")]
    [InlineData(FontSourceKind.System, "wrong-id", "Segoe UI")]
    public void InvalidCanonicalIdentityIsRejected(FontSourceKind source, string id, string family)
    {
        var display = DisplayPresets.Create(DisplayPreset.Digital);
        Assert.Throws<ArgumentException>(() => (display with { Time = display.Time with { Font = new(source, family, id) } }).Validate());
    }
    [Fact]
    public void SameFamilyDifferentSourceHasDistinctIdentity()
    {
        var bundled = FontCatalog.Get(FontSourceKind.Bundled, "pretendard").Selection;
        Assert.NotEqual(bundled, new FontSelection(FontSourceKind.System, "Pretendard"));
    }
    [Fact]
    public void FixedV3WithUserPresetLoadsWritableWithoutRewriteAndUpgradesToV4()
    {
        using var temp = new TempProfile(); var bytes = UserPresetPersistenceTests.Fixture(3);
        File.WriteAllBytes(temp.File, bytes); var modified = File.GetLastWriteTimeUtc(temp.File);
        using var store = new JsonProfileStore(temp.Directory); var session = new ProfileSession(store);
        Assert.Equal(ProfileLoadState.Loaded, session.LoadResult.State); Assert.True(session.LoadResult.CanWrite);
        Assert.Single(session.Current.DisplayPresets.Items);
        Assert.Equal(bytes, File.ReadAllBytes(temp.File)); Assert.Equal(modified, File.GetLastWriteTimeUtc(temp.File));
        var before = session.Current;
        Assert.Null(session.SaveLunch(before.ShowLunch));
        var saved = File.ReadAllBytes(temp.File); Assert.Equal(4, JsonNode.Parse(saved)!["schemaVersion"]!.GetValue<int>());
        var after = ProfileJson.Deserialize(saved);
        Assert.Equal(before.Display, after.Display); Assert.Equal(before.DisplayPresets.Items, after.DisplayPresets.Items);
        var expected = JsonNode.Parse(bytes)!["profile"]!.AsObject(); expected.Remove("display"); expected.Remove("displayPresets");
        var actual = JsonNode.Parse(saved)!["profile"]!.AsObject(); actual.Remove("display"); actual.Remove("displayPresets");
        Assert.True(JsonNode.DeepEquals(expected, actual));
    }
    [Theory]
    [InlineData(2)] [InlineData(3)]
    public void OldSchemasStillRejectNewSourceKindsAndNewFields(int version)
    {
        var node = JsonNode.Parse(UserPresetPersistenceTests.Fixture(version))!;
        var settings = version == 2 ? node["profile"]!["display"]! : node["profile"]!["display"]!["settings"]!;
        settings["time"]!["font"]!["source"] = "Bundled";
        Assert.Throws<JsonException>(() => ProfileJson.Deserialize(System.Text.Encoding.UTF8.GetBytes(node.ToJsonString())));
        settings["time"]!["font"]!["source"] = "System";
        settings["time"]!["font"]!["familyId"] = "Segoe UI";
        Assert.Throws<JsonException>(() => ProfileJson.Deserialize(System.Text.Encoding.UTF8.GetBytes(node.ToJsonString())));
    }
    [Theory]
    [InlineData("missing")][InlineData("null")][InlineData("url")][InlineData("path")]
    public void V4RejectsMalformedFontId(string kind)
    {
        var json = JsonNode.Parse(ProfileJson.Serialize(ProfileStorageTests.Sample()))!;
        var font = json["profile"]!["display"]!["settings"]!["time"]!["font"]!.AsObject();
        if (kind == "missing") font.Remove("familyId");
        else font["familyId"] = kind switch { "null" => null, "url" => "https://example.com/font.ttf", _ => "C:/font.ttf" };
        Assert.ThrowsAny<Exception>(() => ProfileJson.Deserialize(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString())));
    }
}

internal sealed class FakeFontTransport : IFontDownloadTransport
{
    public int Calls { get; private set; }
    public byte[] Bytes { get; set; } = Fixture();
    public bool Fail { get; set; }
    public TaskCompletionSource? Wait { get; set; }
    public static byte[] Fixture()
    {
        using var stream = typeof(FakeFontTransport).Assembly.GetManifestResourceStream("SchoolTimetableWidget.Tests.Fonts.Orbitron-Light.ttf")!;
        using var output = new MemoryStream(); stream.CopyTo(output); return output.ToArray();
    }
    public async Task DownloadAsync(Uri uri, Stream destination, long maximumBytes, CancellationToken cancellationToken)
    {
        Calls++;
        if (Wait is not null) await Wait.Task.WaitAsync(cancellationToken);
        if (Fail) throw new System.Net.Http.HttpRequestException("simulated offline");
        await destination.WriteAsync(Bytes, cancellationToken);
    }
}

public class FontDownloadTests
{
    private static FontSelection Selection => FontCatalog.Get(FontSourceKind.OnlineDownloaded, "orbitron").Selection;
    [Fact]
    public async Task ExactBytesCommitAndExistingValidCacheReusesWithoutNetwork()
    {
        using var temp = new TempProfile(); var fake = new FakeFontTransport(); var cache = new DownloadedFontCache(temp.Directory, fake);
        Assert.Null(cache.FindValid(Selection)); await cache.DownloadAsync(Selection, TestContext.Current.CancellationToken);
        Assert.Equal(fake.Bytes, File.ReadAllBytes(cache.FindValid(Selection)!));
        fake.Fail = true; await cache.DownloadAsync(Selection, TestContext.Current.CancellationToken); Assert.Equal(1, fake.Calls);
        Assert.Empty(Directory.GetFiles(temp.Directory, "*.tmp", SearchOption.AllDirectories));
    }
    [Theory]
    [InlineData("hash")][InlineData("truncated")][InlineData("oversize")][InlineData("header")][InlineData("empty")]
    public async Task InvalidBytesNeverBecomeUsableAndRetryWorks(string failure)
    {
        using var temp = new TempProfile(); var fake = new FakeFontTransport();
        switch (failure)
        {
            case "hash": fake.Bytes[^1] ^= 1; break;
            case "header": fake.Bytes[0] = 99; break;
            case "truncated": fake.Bytes = fake.Bytes[..100]; break;
            case "empty": fake.Bytes = []; break;
            default: fake.Bytes = new byte[DownloadedFontCache.MaximumBytes + 1]; break;
        }
        var cache = new DownloadedFontCache(temp.Directory, fake);
        await Assert.ThrowsAsync<InvalidDataException>(() => cache.DownloadAsync(Selection, TestContext.Current.CancellationToken));
        Assert.Null(cache.FindValid(Selection)); Assert.False(File.Exists(cache.PathFor(Selection)));
        Assert.Empty(Directory.GetFiles(temp.Directory, "*.tmp", SearchOption.AllDirectories));
        fake.Bytes = FakeFontTransport.Fixture(); await cache.DownloadAsync(Selection, TestContext.Current.CancellationToken); Assert.NotNull(cache.FindValid(Selection));
    }
    [Theory]
    [InlineData(FontCacheWriteStage.BeforeCreate)][InlineData(FontCacheWriteStage.BeforeFlush)][InlineData(FontCacheWriteStage.BeforeMove)]
    public async Task WriteFaultPreservesExistingBytesAndCleansOnlyOwnedTemporary(FontCacheWriteStage stage)
    {
        using var temp = new TempProfile(); var fake = new FakeFontTransport();
        var cache = new DownloadedFontCache(temp.Directory, fake, at => { if (stage == at) throw new IOException("fault"); });
        var path = cache.PathFor(Selection); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "previous corrupt bytes"); var original = File.ReadAllBytes(path);
        var other = Path.Combine(Path.GetDirectoryName(path)!, "unowned.tmp"); File.WriteAllText(other, "leave alone");
        await Assert.ThrowsAsync<IOException>(() => cache.DownloadAsync(Selection, TestContext.Current.CancellationToken));
        Assert.Equal(original, File.ReadAllBytes(path)); Assert.Null(cache.FindValid(Selection));
        Assert.Equal(new[] { other }, Directory.GetFiles(temp.Directory, "*.tmp", SearchOption.AllDirectories));
    }
    [Fact]
    public async Task NetworkFailureAndRetryAreIndependentOfProfile()
    {
        using var temp = new TempProfile(); var profile = ProfileJson.Serialize(ProfileStorageTests.Sample()); File.WriteAllBytes(temp.File, profile);
        var fake = new FakeFontTransport { Fail = true }; var cache = new DownloadedFontCache(temp.Directory, fake);
        await Assert.ThrowsAsync<System.Net.Http.HttpRequestException>(() => cache.DownloadAsync(Selection, TestContext.Current.CancellationToken));
        Assert.Equal(profile, File.ReadAllBytes(temp.File)); Assert.Null(cache.FindValid(Selection));
        fake.Fail = false; await cache.DownloadAsync(Selection, TestContext.Current.CancellationToken); Assert.NotNull(cache.FindValid(Selection));
    }
    [Fact]
    public async Task CorruptionIsDetectedAndExplicitDownloadRepairsIt()
    {
        using var temp = new TempProfile(); var fake = new FakeFontTransport(); var cache = new DownloadedFontCache(temp.Directory, fake);
        await cache.DownloadAsync(Selection, TestContext.Current.CancellationToken); var data = File.ReadAllBytes(cache.PathFor(Selection)); data[^1] ^= 1;
        File.WriteAllBytes(cache.PathFor(Selection), data); Assert.Null(cache.FindValid(Selection));
        await cache.DownloadAsync(Selection, TestContext.Current.CancellationToken); Assert.NotNull(cache.FindValid(Selection)); Assert.Equal(2, fake.Calls);
    }
    [Fact]
    public async Task ConcurrentRequestsCommitOnlyOnceAndCancellationLeavesNoPartialFile()
    {
        using var temp = new TempProfile(); var fake = new FakeFontTransport { Wait = new() }; var cache = new DownloadedFontCache(temp.Directory, fake);
        var first = cache.DownloadAsync(Selection, TestContext.Current.CancellationToken); var second = cache.DownloadAsync(Selection, TestContext.Current.CancellationToken);
        fake.Wait.SetResult(); await Task.WhenAll(first, second); Assert.Equal(1, fake.Calls);
        File.Delete(cache.PathFor(Selection)); fake.Wait = new(); using var cancel = new CancellationTokenSource();
        var canceled = cache.DownloadAsync(Selection, cancel.Token); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
        Assert.Null(cache.FindValid(Selection)); Assert.Empty(Directory.GetFiles(temp.Directory, "*.tmp", SearchOption.AllDirectories));
    }
    [Fact]
    public void NonCatalogSelectionCannotReachPathOrTransport()
    {
        using var temp = new TempProfile(); var fake = new FakeFontTransport(); var cache = new DownloadedFontCache(temp.Directory, fake);
        Assert.Throws<ArgumentException>(() => cache.PathFor(new(FontSourceKind.OnlineDownloaded, "Orbitron", "../orbitron")));
        Assert.Throws<ArgumentException>(() => cache.PathFor(new(FontSourceKind.System, "Orbitron")));
        Assert.Equal(0, fake.Calls);
    }
}
