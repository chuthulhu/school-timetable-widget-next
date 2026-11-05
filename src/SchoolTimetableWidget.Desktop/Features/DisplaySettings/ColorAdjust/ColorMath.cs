namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

/// <summary>Hue 0–360°, saturation and value 0–1. Used only by the color adjustment window (ADR 0035).</summary>
public readonly record struct HsvColor(double Hue, double Saturation, double Value);

/// <summary>Color conversions and WCAG 2.x contrast for opaque RGB values.</summary>
public static class ColorMath
{
    public static RgbColor ToRgb(HsvColor hsv)
    {
        var h = ((hsv.Hue % 360) + 360) % 360;
        var s = Math.Clamp(hsv.Saturation, 0, 1);
        var v = Math.Clamp(hsv.Value, 0, 1);
        byte Channel(double n)
        {
            var k = (n + h / 60) % 6;
            var c = v - v * s * Math.Max(0, Math.Min(Math.Min(k, 4 - k), 1));
            return (byte)Math.Round(c * 255);
        }
        return new(Channel(5), Channel(3), Channel(1));
    }

    /// <summary>Converts to HSV. Grays have no hue, so <paramref name="fallbackHue"/> keeps the slider where it was.</summary>
    public static HsvColor ToHsv(RgbColor rgb, double fallbackHue = 0)
    {
        double r = rgb.R / 255d, g = rgb.G / 255d, b = rgb.B / 255d;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;
        var hue = fallbackHue;
        if (delta > 0)
        {
            hue = max == r ? 60 * (((g - b) / delta) % 6)
                : max == g ? 60 * ((b - r) / delta + 2)
                : 60 * ((r - g) / delta + 4);
            if (hue < 0) hue += 360;
        }
        return new(hue, max == 0 ? 0 : delta / max, max);
    }

    public static double RelativeLuminance(RgbColor color)
    {
        static double Linear(byte channel)
        {
            var c = channel / 255d;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
    }

    /// <summary>WCAG contrast ratio, from 1 (identical) to 21 (black on white).</summary>
    public static double ContrastRatio(RgbColor a, RgbColor b)
    {
        var x = RelativeLuminance(a);
        var y = RelativeLuminance(b);
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }
}
