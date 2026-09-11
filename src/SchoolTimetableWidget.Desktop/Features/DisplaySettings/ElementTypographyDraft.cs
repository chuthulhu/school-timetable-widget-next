using System.Globalization;
using System.IO;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using SchoolTimetableWidget.Desktop.Features.Fonts;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;

namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

public sealed class ElementTypographyDraft : ObservableObject, IDisposable
{
    private static readonly IReadOnlyDictionary<FontSourceKind, string[]> CatalogFamilies =
        FontCatalog.Entries.GroupBy(e => e.Source).ToDictionary(g => g.Key, g => g.Select(e => e.FamilyName).ToArray());
    private string _family;
    private string _sizeText;
    private DisplayFontWeight _weight;
    private DisplayFontStyle _style;
    private FontSourceKind _source;
    private FontSelection _selection;
    private readonly FontLibrary _fonts;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _disposed;
    private bool _downloading;
    private string _downloadError = "";
    public ElementTypographyDraft(string label, ElementTypography value, FontLibrary? fonts = null)
    {
        Label = label; _fonts = fonts ?? FontLibrary.LocalOnly;
        _selection = value.Font;
        _source = value.Font.Source;
        _family = value.Font.Family;
        _sizeText = value.Size.ToString(CultureInfo.CurrentCulture);
        _weight = value.Weight; _style = value.Style;
        System.Windows.WeakEventManager<FontLibrary, EventArgs>.AddHandler(_fonts, nameof(FontLibrary.Changed), FontsChanged);
    }
    public string Label { get; }
    public FontSourceKind Source
    {
        get => _source;
        set
        {
            if (_disposed || _source == value || !Enum.IsDefined(value)) return;
            _source = value;
            _family = value == _selection.Source ? _selection.Family : Families.FirstOrDefault() ?? "Segoe UI";
            Choose();
        }
    }
    public IReadOnlyList<string> Families => Source == FontSourceKind.System ? _fonts.SystemFamilies :
        CatalogFamilies[Source];
    public bool IsSystem => Source == FontSourceKind.System;
    public string Family
    {
        get => _family;
        set { if (Source != FontSourceKind.System && !Families.Contains(value)) return; if (!_disposed && _family != value) { _family = value; Choose(); } }
    }
    private FontSelection? Candidate => Source == FontSourceKind.System ? new(Source, Family) :
        FontCatalog.Entries.FirstOrDefault(e => e.Source == Source && e.FamilyName == Family)?.Selection;
    private void Choose()
    {
        _downloadError = "";
        if (Candidate is { } candidate && (Source != FontSourceKind.OnlineDownloaded || _fonts.IsAvailable(candidate)))
            _selection = candidate;
        OnPropertyChanged(string.Empty);
    }
    private void FontsChanged(object? sender, EventArgs e) { if (!_disposed) OnPropertyChanged(string.Empty); }
    public string FontStatus => _downloadError.Length > 0 ? _downloadError : _downloading ? "다운로드 중…" :
        Candidate is not { } candidate ? "등록된 글꼴을 선택해 주세요." :
        candidate.Source == FontSourceKind.OnlineDownloaded && !_fonts.IsAvailable(candidate) && candidate != _selection
            ? "다운로드 후 미리보기에 적용됩니다. 아직 이전 글꼴을 사용 중입니다." : _fonts.Status(candidate);
    public bool CanDownload => !_disposed && !_downloading && Candidate is { Source: FontSourceKind.OnlineDownloaded } c && !_fonts.IsAvailable(c);
    public bool IsOnline => Source == FontSourceKind.OnlineDownloaded;
    public string DownloadLabel => _downloading ? "다운로드 중…" : Candidate is { } c && _fonts.IsAvailable(c) ? "사용 가능" : "다운로드";
    public string FontInformation => Candidate is { } c && FontCatalog.Find(c) is { } entry
        ? $"{entry.DisplayName} · {entry.Version}\n{entry.License}\n출처: {entry.Upstream}\n라이선스: Assets/Fonts/{entry.LicenseFile}"
        : "Windows에 설치된 글꼴입니다.";
    public async Task DownloadAsync()
    {
        if (!CanDownload || Candidate is not { } candidate) return;
        _downloading = true; _downloadError = ""; OnPropertyChanged(string.Empty);
        try
        {
            await _fonts.DownloadAsync(candidate, _lifetime.Token);
            if (!_disposed && Candidate == candidate) _selection = candidate;
        }
        catch (OperationCanceledException) { if (!_disposed) _downloadError = "다운로드가 취소되었거나 시간이 초과되었습니다. 다시 시도해 주세요."; }
        catch (Exception error) when (error is IOException or HttpRequestException or UnauthorizedAccessException or ArgumentException or System.Security.SecurityException)
        {
            System.Diagnostics.Debug.WriteLine(error);
            if (!_disposed) _downloadError = "글꼴을 다운로드하지 못했습니다. 인터넷 연결을 확인하고 다시 시도해 주세요. 기존 표시는 유지됩니다.";
        }
        finally { _downloading = false; if (!_disposed) OnPropertyChanged(string.Empty); }
    }
    public string SizeText { get => _sizeText; set => SetProperty(ref _sizeText, value); }
    public DisplayFontWeight Weight { get => _weight; set => SetProperty(ref _weight, value); }
    public DisplayFontStyle Style { get => _style; set => SetProperty(ref _style, value); }
    public ElementTypography Create()
    {
        if (!double.TryParse(SizeText, NumberStyles.Float, CultureInfo.CurrentCulture, out var size))
            throw new ArgumentException($"{Label} 글자 크기를 숫자로 입력해 주세요.");
        return new(_selection, size, Weight, Style);
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _lifetime.Cancel(); _lifetime.Dispose();
        System.Windows.WeakEventManager<FontLibrary, EventArgs>.RemoveHandler(_fonts, nameof(FontLibrary.Changed), FontsChanged);
    }
}
