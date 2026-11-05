using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

/// <summary>Recently chosen colors for this run only (ADR 0035): newest first, at most eight, never stored.</summary>
public sealed class RecentColors
{
    public const int Capacity = 8;
    public static RecentColors Shared { get; } = new();
    private readonly List<RgbColor> _items = [];
    public IReadOnlyList<RgbColor> Items => _items;
    public void Add(RgbColor color)
    {
        _items.Remove(color);
        _items.Insert(0, color);
        if (_items.Count > Capacity) _items.RemoveRange(Capacity, _items.Count - Capacity);
    }
}

public enum ContrastLevel { Good, Caution, Poor }

/// <summary>One text/background pair. It informs and never blocks (ADR 0035).</summary>
public sealed record ContrastCheck(string Title, ContrastLevel Level, double Ratio, string OpacityNote);

/// <summary>A clickable color in the recommended or recent rows.</summary>
public sealed record ColorChoice(RgbColor Color)
{
    public string Hex => Color.ToString();
}

/// <summary>
/// Drives the in-app color adjustment window for one color of the display settings Draft (ADR 0035).
/// Every change writes the Draft's <see cref="ColorDraft.Text"/>, so the widget previews at once.
/// 선택 keeps the color; 취소 restores the color the window opened with. Neither saves.
/// </summary>
public sealed class ColorAdjustModel : ObservableObject
{
    // Indices of DisplaySettingsSession.Colors.
    private const int HeaderBackground = 0, TodayHeaderBackground = 1, CellBackground = 2, CurrentBackground = 3,
        HeaderText = 5, CellText = 6, ClockText = 7;

    public static IReadOnlyList<ColorChoice> Recommended { get; } = new[]
    {
        "#000000", "#1F2937", "#3F4958", "#6B7280", "#BABEC5", "#E3E3E3", "#F0F0F0", "#FFFFFF",
        "#FFF3CD", "#FDE2D2", "#FCE7F3", "#EDE7FB", "#E5EFF6", "#E0ECEA", "#E6F4DD", "#F9F8F5",
        "#8A5A00", "#B4471F", "#A0225A", "#5B3FA8", "#1D4ED8", "#0F6E66", "#3F7D20", "#14213A"
    }.Select(hex => new ColorChoice(RgbColor.Parse(hex))).ToArray();

    private readonly DisplaySettingsSession _session;
    private readonly int _index;
    private readonly RecentColors _recent;
    private HsvColor _hsv;
    private RgbColor _current;
    private string _hexText;
    private bool _finished;

    public ColorAdjustModel(DisplaySettingsSession session, int index, RecentColors? recent = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (index < 0 || index >= session.Colors.Count) throw new ArgumentOutOfRangeException(nameof(index));
        _session = session; _index = index; _recent = recent ?? RecentColors.Shared;
        Before = RgbColor.Parse(Target.Swatch); // the last valid color, even while the box holds invalid text
        _beforeText = Target.Text;
        _current = Before;
        _hsv = ColorMath.ToHsv(Before);
        _hexText = Before.ToString();
        RecentChoices = new(_recent.Items.Select(c => new ColorChoice(c)));
    }

    private readonly string _beforeText;
    private ColorDraft Target => _session.Colors[_index];
    public string Title => $"{Target.Label} 색 조정";
    public RgbColor Before { get; }
    public RgbColor Current => _current;
    public string BeforeHex => Before.ToString();
    public string CurrentHex => Current.ToString();
    public ObservableCollection<ColorChoice> RecentChoices { get; }
    public bool HasRecent => RecentChoices.Count > 0;
    public bool IsFinished => _finished;

    public double Hue => _hsv.Hue;
    public double Saturation => _hsv.Saturation;
    public double Value => _hsv.Value;
    /// <summary>The fully saturated hue behind the saturation/brightness square.</summary>
    public string HueHex => ColorMath.ToRgb(new(_hsv.Hue, 1, 1)).ToString();
    public string HsvText => $"색상 {Math.Round(_hsv.Hue)}° · 채도 {Math.Round(_hsv.Saturation * 100)}% · 밝기 {Math.Round(_hsv.Value * 100)}%";

    public string HexText
    {
        get => _hexText;
        set
        {
            if (!SetProperty(ref _hexText, value ?? "")) return;
            if (RgbColor.TryParse(_hexText, out var color))
            {
                _hsv = ColorMath.ToHsv(color, _hsv.Hue);
                Write(color);
            }
            OnPropertyChanged(nameof(HexError));
        }
    }
    public string HexError => RgbColor.TryParse(_hexText, out _) ? "" : "#RRGGBB 형식으로 입력해 주세요.";

    public void SetSaturationValue(double saturation, double value) =>
        SetHsv(_hsv with { Saturation = Math.Clamp(saturation, 0, 1), Value = Math.Clamp(value, 0, 1) });
    public void SetHue(double hue) => SetHsv(_hsv with { Hue = ((hue % 360) + 360) % 360 });
    public void Pick(RgbColor color)
    {
        _hsv = ColorMath.ToHsv(color, _hsv.Hue);
        _hexText = color.ToString();
        Write(color);
    }

    /// <summary>선택: keeps the current color in the Draft and remembers it for this run.</summary>
    public void Confirm()
    {
        if (_finished) return;
        _finished = true;
        if (Current != Before) _recent.Add(Current);
    }
    /// <summary>취소 / Esc: restores exactly what the box held when the window opened.</summary>
    public void Cancel()
    {
        if (_finished) return;
        _finished = true;
        Target.Text = _beforeText;
    }

    public IReadOnlyList<ContrastCheck> Contrast => Pairs().Select(pair => Check(pair.Other, pair.Background)).ToArray();
    public string ContrastNote => _index == ClockText
        ? "시계 글자는 바탕화면 위에 바로 표시되어 대비를 계산할 수 없습니다. 위젯 미리보기로 확인해 주세요."
        : Pairs().Any() ? "" : "테두리는 글자 대비를 계산하지 않습니다.";

    private void SetHsv(HsvColor hsv)
    {
        _hsv = hsv;
        var color = ColorMath.ToRgb(hsv);
        _hexText = color.ToString();
        Write(color);
    }
    private void Write(RgbColor color)
    {
        _current = color;
        if (!_finished) Target.Text = color.ToString();
        OnPropertyChanged(string.Empty);
    }

    private IEnumerable<(int Other, int Background)> Pairs() => _index switch
    {
        HeaderText => new[] { (HeaderBackground, HeaderBackground), (TodayHeaderBackground, TodayHeaderBackground) },
        CellText => new[] { (CellBackground, CellBackground), (CurrentBackground, CurrentBackground) },
        HeaderBackground or TodayHeaderBackground => new[] { (HeaderText, _index) },
        CellBackground or CurrentBackground => new[] { (CellText, _index) },
        _ => Array.Empty<(int, int)>()
    };
    private ContrastCheck Check(int other, int background)
    {
        var ratio = ColorMath.ContrastRatio(Current, RgbColor.Parse(_session.Colors[other].Swatch));
        var level = ratio >= 4.5 ? ContrastLevel.Good : ratio >= 3 ? ContrastLevel.Caution : ContrastLevel.Poor;
        var verdict = level switch
        {
            ContrastLevel.Good => "잘 읽힙니다",
            ContrastLevel.Caution => "작은 글자는 읽기 어려울 수 있습니다",
            _ => "읽기 어렵습니다 · 저장은 할 수 있습니다"
        };
        var opacity = background switch
        {
            HeaderBackground or TodayHeaderBackground => _session.HeaderOpacity,
            CellBackground => _session.CellOpacity,
            _ => _session.CurrentOpacity
        };
        var note = opacity < 1
            ? $"배경 불투명도 {Math.Round(opacity * 100)}% — 실제 대비는 바탕화면에 따라 달라집니다."
            : "";
        return new($"{_session.Colors[other].Label} 대비 {ratio:0.0} : 1 · {verdict}", level, ratio, note);
    }
}
