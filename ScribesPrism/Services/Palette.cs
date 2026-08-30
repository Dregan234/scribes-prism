using System.Collections.Generic;

namespace ScribesPrism.Services;

public readonly record struct LetterColor(byte Red, byte Green, byte Blue);

public sealed class Palette
{
    private static readonly Dictionary<string, string> Aliases = new()
    {
        ["w"] = "white",
        ["g"] = "green",
        ["y"] = "yellow",
        ["o"] = "orange",
    };

    public static readonly (string Name, uint Rgb)[] Defaults =
    [
        ("red", 0xFF0000),
        ("green", 0x00FF00),
        ("blue", 0x0000FF),
        ("yellow", 0xFFFF00),
        ("purple", 0xFF00FF),
        ("orange", 0xFF8000),
        ("white", 0xFFFFFF),
    ];

    private readonly Configuration configuration;
    private readonly Dictionary<string, uint> defaultColors = new();

    public Palette(Configuration configuration)
    {
        this.configuration = configuration;
        foreach (var (name, rgb) in Defaults)
        {
            this.defaultColors[name] = rgb;
        }
    }

    public ColorSpec Resolve(string name)
    {
        var rgb = this.GetRgb(name);
        return rgb is { } c ? new ColorSpec(c.Red, c.Green, c.Blue) : ColorSpec.Default;
    }

    public LetterColor? GetRgb(string name)
    {
        var canonical = CanonicalName(name);
        if (canonical is null || !this.defaultColors.TryGetValue(canonical, out var rgb))
        {
            return null;
        }

        if (this.configuration.LetterColorOverrides.TryGetValue(canonical, out var overrideRgb))
        {
            rgb = overrideRgb;
        }

        return new LetterColor((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }

    private static string? CanonicalName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        var lower = name.ToLowerInvariant();
        return Aliases.TryGetValue(lower, out var canonical) ? canonical : lower;
    }
}