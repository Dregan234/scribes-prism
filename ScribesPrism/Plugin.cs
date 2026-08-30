using System;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ScribesPrism.Services;
using ScribesPrism.Windows;

namespace ScribesPrism;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/prism";
    private const string TestCommandName = "/prismtest";

    public Configuration Configuration { get; init; }
    public Palette Palette { get; }
    public MacroChatColorizer Colorizer { get; }

    public readonly WindowSystem WindowSystem = new("ScribesPrism");
    private ConfigWindow ConfigWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Palette = new Palette(Configuration);
        Colorizer = new MacroChatColorizer(Configuration, Palette, ChatGui, ObjectTable, Log);

        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Scribe's Prism settings."
        });

        CommandManager.AddHandler(TestCommandName, new CommandInfo(OnTestCommand)
        {
            HelpMessage = "Print a colorized macro text preview. Usage: /prismtest <red>Hello <blue>World"
        });

        // Tell the UI system that we want our windows to be drawn through the window system
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;

        // This adds a button to the plugin installer entry of this plugin which allows
        // toggling the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        // Adds another button doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUi;

        Log.Information("Scribe's Prism loaded.");
    }

    public void Dispose()
    {
        Colorizer.Dispose();

        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleConfigUi;

        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(TestCommandName);
    }

    private void OnCommand(string command, string args)
    {
        ConfigWindow.Toggle();
    }

    private void OnTestCommand(string command, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            return;
        }

        ChatGui.Print(MacroChatColorizer.BuildColorized(args, Palette));
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
}