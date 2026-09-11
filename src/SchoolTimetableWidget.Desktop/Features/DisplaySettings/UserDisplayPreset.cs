using System.Collections.ObjectModel;
using System.Text;

namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

/// <summary>A built-in identity or a stable user identity; names are never keys.</summary>
public sealed record DisplayPresetReference
{
    private DisplayPresetReference(DisplayPreset? builtIn, Guid? userId) { BuiltIn = builtIn; UserId = userId; }
    public DisplayPreset? BuiltIn { get; }
    public Guid? UserId { get; }
    public static DisplayPresetReference BuiltInPreset(DisplayPreset value) => Enum.IsDefined(value)
        ? new(value, null) : throw new ArgumentException("표시 스타일을 확인해 주세요.");
    public static DisplayPresetReference User(Guid id) => id != Guid.Empty
        ? new(null, id) : throw new ArgumentException("내 프리셋을 확인해 주세요.");
    public static implicit operator DisplayPresetReference(DisplayPreset value) => BuiltInPreset(value);
}

/// <summary>Immutable saved template. Its configuration carries this preset's identity.</summary>
public sealed record UserDisplayPreset
{
    public const int MaxNameLength = 60;
    public UserDisplayPreset(Guid id, string name, DisplayConfiguration display)
    {
        Id = id;
        Name = NormalizeName(name);
        Display = display with { Preset = DisplayPresetReference.User(id) };
        Display.Validate();
    }
    public Guid Id { get; }
    public string Name { get; }
    public DisplayConfiguration Display { get; }
    public static string NormalizeName(string name)
    {
        var value = (name ?? "").Trim().Normalize(NormalizationForm.FormC);
        if (value.Length == 0) throw new ArgumentException("프리셋 이름을 입력해 주세요.");
        if (value.Length > MaxNameLength) throw new ArgumentException($"이름은 {MaxNameLength}자 이내로 입력해 주세요.");
        if (value.Any(char.IsControl)) throw new ArgumentException("이름에는 줄바꿈이나 제어 문자를 사용할 수 없습니다.");
        return value;
    }
}

/// <summary>Copied collection of immutable values; validates the entire library and reference.</summary>
public sealed class UserDisplayPresetLibrary
{
    public static UserDisplayPresetLibrary Empty { get; } = new([]);
    public UserDisplayPresetLibrary(IEnumerable<UserDisplayPreset> items)
    {
        var copy = items.ToArray();
        if (copy.Any(p => p is null) || copy.Select(p => p.Id).Distinct().Count() != copy.Length)
            throw new ArgumentException("내 프리셋 목록을 확인해 주세요.");
        if (copy.Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != copy.Length)
            throw new ArgumentException("같은 이름의 내 프리셋이 있습니다. 다른 이름을 입력해 주세요.");
        Items = Array.AsReadOnly(copy);
    }
    public ReadOnlyCollection<UserDisplayPreset> Items { get; }
    public UserDisplayPreset Get(Guid id) => Items.FirstOrDefault(p => p.Id == id)
        ?? throw new ArgumentException("내 프리셋을 찾을 수 없습니다.");
    public DisplayConfiguration Resolve(DisplayPresetReference reference) => reference.BuiltIn is { } builtIn
        ? DisplayPresets.Create(builtIn) : Get(reference.UserId!.Value).Display;
    public void ValidateReference(DisplayConfiguration display)
    {
        display.Validate();
        if (display.Preset.UserId is { } id) _ = Get(id);
    }
}
