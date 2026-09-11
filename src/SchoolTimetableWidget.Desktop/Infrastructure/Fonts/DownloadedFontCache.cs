using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using SchoolTimetableWidget.Desktop.Features.Fonts;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Fonts;

public interface IFontDownloadTransport
{
    Task DownloadAsync(Uri uri, Stream destination, long maximumBytes, CancellationToken cancellationToken);
}

/// <summary>Only called by explicit Settings download. Redirects are disabled; the catalog uses raw HTTPS assets.</summary>
public sealed class HttpsFontDownloadTransport : IFontDownloadTransport
{
    private static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = false })
        { Timeout = TimeSpan.FromSeconds(45) };
    public async Task DownloadAsync(Uri uri, Stream destination, long maximumBytes, CancellationToken cancellationToken)
    {
        if (uri.Scheme != Uri.UriSchemeHttps) throw new InvalidDataException("HTTPS is required.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        cancellationToken = timeout.Token;
        using var response = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > maximumBytes) throw new InvalidDataException("Font is too large.");
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var buffer = new byte[16384]; long total = 0; int count;
        while ((count = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
        {
            total += count;
            if (total > maximumBytes) throw new InvalidDataException("Font is too large.");
            await destination.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
        }
    }
}

public enum FontCacheWriteStage { BeforeCreate, BeforeFlush, BeforeMove }

/// <summary>Regenerable cache: bounded bytes, sfnt header, exact size/hash, close, then same-directory rename.</summary>
public sealed class DownloadedFontCache
{
    public const long MaximumBytes = 8 * 1024 * 1024;
    private readonly string _root;
    private readonly IFontDownloadTransport _transport;
    private readonly Action<FontCacheWriteStage>? _fault;
    private readonly SemaphoreSlim _gate = new(1, 1);
    public DownloadedFontCache(string profileDirectory, IFontDownloadTransport? transport = null,
        Action<FontCacheWriteStage>? fault = null)
    {
        _root = Path.Combine(Path.GetFullPath(profileDirectory), "fonts");
        _transport = transport ?? new HttpsFontDownloadTransport();
        _fault = fault;
    }
    // Resolve callers cannot supply a path or URL. All filesystem components come from app-owned metadata.
    public string PathFor(FontSelection selection)
    {
        var entry = Approved(selection);
        return Path.Combine(_root, entry.Id, entry.SourceCommit, entry.FileName);
    }
    private static FontCatalogEntry Approved(FontSelection selection) =>
        FontCatalog.Find(selection) is { Source: FontSourceKind.OnlineDownloaded } entry ? entry
            : throw new ArgumentException("등록된 온라인 글꼴을 선택해 주세요.");
    public string? FindValid(FontSelection selection)
    {
        var entry = Approved(selection); var path = PathFor(selection);
        try { return Validate(path, entry) ? path : null; }
        catch (Exception error) when (IsFileFailure(error)) { return null; }
    }
    private static bool Validate(string path, FontCatalogEntry entry)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length != entry.Size || file.Length < 12 || file.Length > MaximumBytes) return false;
        Span<byte> header = stackalloc byte[4]; file.ReadExactly(header);
        if (!header.SequenceEqual(new byte[] { 0, 1, 0, 0 }) && !header.SequenceEqual("OTTO"u8)) return false;
        file.Position = 0;
        return Convert.ToHexString(SHA256.HashData(file)).Equals(entry.Sha256, StringComparison.OrdinalIgnoreCase);
    }
    public async Task DownloadAsync(FontSelection selection, CancellationToken cancellationToken = default)
    {
        var entry = Approved(selection);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        string? temporary = null;
        try
        {
            if (FindValid(selection) is not null) return;
            var destination = PathFor(selection); var directory = Path.GetDirectoryName(destination)!;
            Directory.CreateDirectory(directory);
            // Cross-instance/profile cooperation without waiting indefinitely or publishing partial bytes.
            await using var lease = new FileStream(Path.Combine(directory, "download.lock"), FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None);
            if (FindValid(selection) is not null) return;
            temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
            _fault?.Invoke(FontCacheWriteStage.BeforeCreate);
            await using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await _transport.DownloadAsync(new Uri(entry.SourceUrl), file, MaximumBytes, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                _fault?.Invoke(FontCacheWriteStage.BeforeFlush);
                file.Flush(true);
            }
            if (!Validate(temporary, entry)) throw new InvalidDataException("다운로드한 글꼴의 검증에 실패했습니다. 다시 시도해 주세요.");
            cancellationToken.ThrowIfCancellationRequested();
            _fault?.Invoke(FontCacheWriteStage.BeforeMove);
            File.Move(temporary, destination, true);
        }
        finally
        {
            if (temporary is not null)
                try { File.Delete(temporary); } catch (Exception error) when (IsFileFailure(error)) { System.Diagnostics.Debug.WriteLine(error); }
            _gate.Release();
        }
    }
    internal static bool IsFileFailure(Exception error) => error is IOException or UnauthorizedAccessException or
        System.Security.SecurityException or ArgumentException or NotSupportedException;
}
