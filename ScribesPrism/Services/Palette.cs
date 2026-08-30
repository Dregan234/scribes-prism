using System.Collections.Generic;

namespace ScribesPrism.Services;

public readonly record struct LetterColor(byte Red, byte Green, byte Blue);

public sealed class Palette
{
    public static readonly (char Letter, string Name, uint Rgb)[] Defaults =
    [
        ('w', "White", 0xFFFFFF),
        ('g', "Green", 0x00FF00),
        ('r', "Red", 0xFF0000),
        ('b', "Blue", 0x0000FF),
        ('y', "Yellow", 0xFFFF00),
        ('p', "Purple", 0xFF00FF),
        ('o', "Orange", 0xFF8000),
    ];

    private readonly Configuration configuration;
    private readonly Dictionary<char, uint> defaultColors = new();

    public Palette(Configuration configuration)
    {
        this.configuration = configuration;
        foreach (var (letter, _, rgb) in Defaults)
        {
            this.defaultColors[char.ToLowerInvariant(letter)] = rgb;
        }
    }

    public ColorSpec Resolve(char letter)
    {
        var rgb = this.GetRgb(letter);
        return rgb is { } c ? new ColorSpec(c.Red, c.Green, c.Blue) : ColorSpec.Default;
    }

    public LetterColor? GetRgb(char letter)
    {
        var lower = char.ToLowerInvariant(letter);
        if (!this.defaultColors.TryGetValue(lower, out var rgb))
        {
            return null;
        }

        if (this.configuration.LetterColorOverrides.TryGetValue(lower, out var overrideRgb))
        {
            rgb = overrideRgb;
        }

        return new LetterColor((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }
}