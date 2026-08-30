using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace ScribesPrism;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool Enabled { get; set; } = true;

    public Dictionary<string, uint> LetterColorOverrides { get; set; } = new();

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}