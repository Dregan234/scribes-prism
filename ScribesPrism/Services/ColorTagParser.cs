using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ScribesPrism.Services;

public readonly record struct ColorSpec(byte? Red, byte? Green, byte? Blue)
{
    public static ColorSpec Default => new(null, null, null);

    public bool IsColor => this.Red.HasValue;
}

public sealed class ColorSegment
{
    public string Text { get; init; } = string.Empty;

    public ColorSpec Color { get; init; }
}

public static class ColorTagParser
{
    private static readonly Regex ColorTagRegex = new(
        @"<(?:red|green|blue|yellow|purple|orange|white|w|g|y|o)>|#[0-9a-fA-F]{6}(?:[0-9a-fA-F]{2})?>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static bool ContainsColorTag(string text) => ColorTagRegex.IsMatch(text);

    public static List<ColorSegment> Parse(string text, Palette palette)
    {
        var segments = new List<ColorSegment>();
        var currentColor = ColorSpec.Default;
        var builder = new StringBuilder(text.Length);
        var index = 0;

        while (index < text.Length)
        {
            var c = text[index];

            if (c == '\n')
            {
                builder.Append(c);
                Flush();
                currentColor = ColorSpec.Default;
                index++;
            }
            else if (c == '\\' && index + 1 < text.Length && text[index + 1] == '<')
            {
                builder.Append('<');
                index += 2;
            }
            else if (c == '<' && TryParseTag(text, index, palette, out var spec, out var consumed))
            {
                Flush();
                currentColor = spec;
                index += consumed;
            }
            else
            {
                builder.Append(c);
                index++;
            }
        }

        Flush();
        return segments;

        void Flush()
        {
            if (builder.Length == 0)
            {
                return;
            }

            segments.Add(new ColorSegment { Text = builder.ToString(), Color = currentColor });
            builder.Clear();
        }
    }

    private static bool TryParseTag(string text, int index, Palette palette, out ColorSpec spec, out int consumed)
    {
        spec = ColorSpec.Default;
        consumed = 0;

        if (index + 1 >= text.Length)
        {
            return false;
        }

        var close = text.IndexOf('>', index + 1);
        if (close < 0)
        {
            return false;
        }

        var inner = text.AsSpan(index + 1, close - index - 1);
        if (inner.Length == 0)
        {
            return false;
        }

        if (inner[0] == '#')
        {
            var hex = inner[1..];
            if (hex.Length is not (6 or 8))
            {
                return false;
            }

            if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            {
                return false;
            }

            byte r;
            byte g;
            byte b;
            if (hex.Length == 6)
            {
                r = (byte)(value >> 16);
                g = (byte)(value >> 8);
                b = (byte)value;
            }
            else
            {
                r = (byte)(value >> 24);
                g = (byte)(value >> 16);
                b = (byte)(value >> 8);
            }

            spec = new ColorSpec(r, g, b);
            consumed = close - index + 1;
            return true;
        }

        if (!char.IsLetter(inner[0]))
        {
            return false;
        }

        var resolved = palette.Resolve(inner.ToString());
        if (!resolved.IsColor)
        {
            return false;
        }

        spec = resolved;
        consumed = close - index + 1;
        return true;
    }
}