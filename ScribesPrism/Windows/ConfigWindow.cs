using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using ScribesPrism.Services;

namespace ScribesPrism.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public ConfigWindow(Plugin plugin)
        : base("Scribe's Prism Settings###ScribesPrismSettings")
    {
        Size = new Vector2(440, 440);
        SizeCondition = ImGuiCond.FirstUseEver;
        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var enabled = this.plugin.Configuration.Enabled;
        if (ImGui.Checkbox("Colorize macro chat text", ref enabled))
        {
            this.plugin.Configuration.Enabled = enabled;
            this.plugin.Configuration.Save();
        }

        ImGui.Spacing();
        ImGui.TextWrapped("Only chat lines you sent that contain a color tag are recolored, and only in your own chat log. Other players still see the raw tag text.");
        ImGui.Separator();

        ImGui.Text("Letter palette");
        ImGui.Spacing();
        foreach (var (letter, name, _) in Palette.Defaults)
        {
            var rgb = this.plugin.Palette.GetRgb(letter);
            var color = rgb is { } c
                ? new Vector4(c.Red / 255f, c.Green / 255f, c.Blue / 255f, 1f)
                : new Vector4(1f, 1f, 1f, 1f);

            ImGui.PushID($"letter{letter}");
            ImGui.ColorButton($"##swatch{letter}", color);
            ImGui.SameLine();
            if (ImGui.ColorEdit4($"<{letter}>##edit", ref color, ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.NoInputs))
            {
                this.plugin.Configuration.LetterColorOverrides[char.ToLowerInvariant(letter)] = ToRgb(color);
                this.plugin.Configuration.Save();
            }

            ImGui.SameLine();
            var hasOverride = this.plugin.Configuration.LetterColorOverrides.ContainsKey(char.ToLowerInvariant(letter));
            if (hasOverride && ImGui.SmallButton("Reset##" + letter))
            {
                this.plugin.Configuration.LetterColorOverrides.Remove(char.ToLowerInvariant(letter));
                this.plugin.Configuration.Save();
            }

            ImGui.SameLine();
            ImGui.Text($"{name}");
            ImGui.PopID();
        }

        ImGui.Separator();
        ImGui.TextWrapped("Tags color the rest of the line until the next tag; a newline resets the color.");
        ImGui.TextWrapped("Escape a tag as \\<r> to print it literally. Native tags such as <se.1> and <t> pass through untouched.");
        ImGui.Spacing();
        ImGui.Text("Example:");
        ImGui.TextWrapped("<w>WARNING <g>GO <r>STOP <#7F00FF>Custom");
        ImGui.Spacing();
        ImGui.TextWrapped("Use /prismtest <text> to print a colorized preview in chat.");
    }

    private static uint ToRgb(Vector4 color)
    {
        var r = (uint)Math.Clamp((int)Math.Round(color.X * 255), 0, 255);
        var g = (uint)Math.Clamp((int)Math.Round(color.Y * 255), 0, 255);
        var b = (uint)Math.Clamp((int)Math.Round(color.Z * 255), 0, 255);
        return (r << 16) | (g << 8) | b;
    }
}