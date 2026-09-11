using System.Text.Json;
using System.Text.Json.Serialization;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;

namespace SchoolTimetableWidget.Desktop.Features.Fonts;

public sealed record FontCatalogEntry(FontSourceKind Source, string Id, string DisplayName, string FamilyName,
    string Version, string SourceCommit, string Upstream, string SourceUrl, string Sha256, string FileName,
    long Size, string License, string LicenseFile, string ReservedFontName, string Notice)
{
    public FontSelection Selection => new(Source, FamilyName, Id);
}

/// <summary>App-owned pinned metadata, independent of profile data and network availability.</summary>
public static class FontCatalog
{
    public static IReadOnlyList<FontCatalogEntry> Entries { get; } = Read();
    public static FontCatalogEntry? Find(FontSelection selection) => Entries.FirstOrDefault(e =>
        e.Source == selection.Source && e.Id == selection.FamilyId && e.FamilyName == selection.Family);
    public static bool Contains(FontSelection selection) => Find(selection) is not null;
    public static FontCatalogEntry Get(FontSourceKind source, string id) => Entries.Single(e => e.Source == source && e.Id == id);
    private static IReadOnlyList<FontCatalogEntry> Read()
    {
        using var stream = typeof(FontCatalog).Assembly.GetManifestResourceStream(
            "SchoolTimetableWidget.Desktop.Assets.Fonts.catalog.json")!;
        var options = new JsonSerializerOptions(); options.Converters.Add(new JsonStringEnumConverter());
        return Array.AsReadOnly(JsonSerializer.Deserialize<FontCatalogEntry[]>(stream, options)!);
    }
}
