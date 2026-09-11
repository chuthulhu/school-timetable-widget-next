using System.IO;
using System.Windows;
using System.Windows.Media;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

/// <summary>WPF system family enumeration and safe local resolution; no paths or downloads.</summary>
public sealed class SystemFontCatalog
{
    private readonly Dictionary<string, FontFamily> _families;
    private static readonly Lazy<SystemFontCatalog> Shared = new(() => new(Enumerate()));
    public static SystemFontCatalog Current => Shared.Value;
    public SystemFontCatalog(IEnumerable<FontFamily> families)
    {
        _families = new(StringComparer.OrdinalIgnoreCase);
        foreach (var family in families)
        {
            _families.TryAdd(family.Source, family);
            foreach (var name in family.FamilyNames.Values) _families.TryAdd(name, family);
        }
        Families = Array.AsReadOnly(_families.Keys.Order(StringComparer.CurrentCultureIgnoreCase).ToArray());
    }
    public IReadOnlyList<string> Families { get; }
    public bool IsAvailable(string family) => _families.ContainsKey(family);
    public FontFamily Resolve(FontSelection selection) =>
        selection.Source == FontSourceKind.System && _families.TryGetValue(selection.Family, out var family)
            ? family : _families.GetValueOrDefault("Segoe UI") ?? SystemFonts.MessageFontFamily;
    public static FontWeight Weight(DisplayFontWeight weight) => weight switch
    {
        DisplayFontWeight.Thin => FontWeights.Thin,
        DisplayFontWeight.Medium => FontWeights.Medium,
        DisplayFontWeight.Bold => FontWeights.Bold,
        _ => FontWeights.Normal
    };
    public static FontStyle Style(DisplayFontStyle style) => style == DisplayFontStyle.Italic ? FontStyles.Italic : FontStyles.Normal;
    private static IEnumerable<FontFamily> Enumerate()
    {
        try { return System.Windows.Media.Fonts.SystemFontFamilies.ToArray(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or System.Security.SecurityException)
        {
            System.Diagnostics.Debug.WriteLine(error);
            return [SystemFonts.MessageFontFamily];
        }
    }
}
