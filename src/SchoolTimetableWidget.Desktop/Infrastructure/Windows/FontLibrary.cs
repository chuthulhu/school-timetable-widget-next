using System.IO;
using System.Windows.Media;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Features.Fonts;
using SchoolTimetableWidget.Desktop.Infrastructure.Fonts;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

/// <summary>WPF resource/private font boundary. No system installation or network in resolution.</summary>
public sealed class FontLibrary
{
    // Safe for uncomposed views/tests: no production profile/cache access and no download capability.
    public static FontLibrary LocalOnly { get; } = new();
    private readonly DownloadedFontCache? _cache;
    public FontLibrary(DownloadedFontCache? cache = null) => _cache = cache;
    public event EventHandler? Changed;
    public IReadOnlyList<string> SystemFamilies => SystemFontCatalog.Current.Families;
    public FontFamily Resolve(FontSelection selection) => TryResolve(selection) ??
        SystemFontCatalog.Current.Resolve(new(FontSourceKind.System, "Segoe UI"));
    public bool IsAvailable(FontSelection selection) => TryResolve(selection) is not null;
    private FontFamily? TryResolve(FontSelection selection)
    {
        if (selection.Source == FontSourceKind.System)
            return SystemFontCatalog.Current.IsAvailable(selection.Family) ? SystemFontCatalog.Current.Resolve(selection) : null;
        if (FontCatalog.Find(selection) is not { } entry) return null;
        try
        {
            FontFamily family;
            if (entry.Source == FontSourceKind.Bundled)
            {
                // Initialize WPF's resource package even when used before an Application/window exists.
                using var resource = System.Windows.Application.GetResourceStream(new Uri(
                    $"/SchoolTimetableWidget.Desktop;component/Assets/Fonts/{entry.Id}/{entry.FileName}", UriKind.Relative)).Stream;
                family = new(System.IO.Packaging.PackUriHelper.Create(new Uri("application:///")),
                    $"./SchoolTimetableWidget.Desktop;component/Assets/Fonts/{entry.Id}/#{entry.FamilyName}");
            }
            else
            {
                var path = _cache?.FindValid(selection);
                if (path is null) return null;
                family = new(new Uri(Path.GetDirectoryName(path)! + Path.DirectorySeparatorChar), $"./#{entry.FamilyName}");
            }
            // Construction alone is insufficient: force the requested private typeface to load.
            return new Typeface(family, System.Windows.FontStyles.Normal, System.Windows.FontWeights.Normal,
                System.Windows.FontStretches.Normal).TryGetGlyphTypeface(out var glyphs) &&
                glyphs.CharacterToGlyphMap.ContainsKey('0') ? family : null;
        }
        catch (Exception error) when (DownloadedFontCache.IsFileFailure(error) || error is InvalidOperationException or FileFormatException)
        {
            System.Diagnostics.Debug.WriteLine(error); return null;
        }
    }
    public string Status(FontSelection selection)
    {
        if (!IsAvailable(selection)) return selection.Source == FontSourceKind.OnlineDownloaded
            ? "온라인 글꼴을 다시 다운로드해야 합니다. 현재 기본 글꼴로 표시됩니다."
            : "선택한 글꼴을 찾을 수 없어 기본 글꼴을 사용 중";
        var notice = FontCatalog.Find(selection)?.Notice ?? "";
        return selection.Source == FontSourceKind.OnlineDownloaded ? "사용 가능 · " + notice : notice;
    }
    public async Task DownloadAsync(FontSelection selection, CancellationToken cancellationToken)
    {
        if (_cache is null) throw new IOException("다운로드할 글꼴 저장 위치를 사용할 수 없습니다.");
        await _cache.DownloadAsync(selection, cancellationToken);
        if (!IsAvailable(selection)) throw new InvalidDataException("이 글꼴을 표시할 수 없습니다. 기본 글꼴을 사용합니다.");
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
