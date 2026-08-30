using System;
using System.Collections.Generic;
using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;

namespace ScribesPrism.Services;

public sealed class MacroChatColorizer : IDisposable
{
    private static readonly HashSet<XivChatType> TextChannels = new()
    {
        XivChatType.Say,
        XivChatType.Shout,
        XivChatType.Yell,
        XivChatType.Party,
        XivChatType.CrossParty,
        XivChatType.Alliance,
        XivChatType.FreeCompany,
        XivChatType.Ls1,
        XivChatType.Ls2,
        XivChatType.Ls3,
        XivChatType.Ls4,
        XivChatType.Ls5,
        XivChatType.Ls6,
        XivChatType.Ls7,
        XivChatType.Ls8,
        XivChatType.CrossLinkShell1,
        XivChatType.CrossLinkShell2,
        XivChatType.CrossLinkShell3,
        XivChatType.CrossLinkShell4,
        XivChatType.CrossLinkShell5,
        XivChatType.CrossLinkShell6,
        XivChatType.CrossLinkShell7,
        XivChatType.CrossLinkShell8,
        XivChatType.TellOutgoing,
        XivChatType.CustomEmote,
        XivChatType.StandardEmote,
        XivChatType.Echo,
    };

    private readonly Configuration configuration;
    private readonly Palette palette;
    private readonly IChatGui chatGui;
    private readonly IObjectTable objectTable;
    private readonly IPluginLog log;

    public MacroChatColorizer(Configuration configuration, Palette palette, IChatGui chatGui, IObjectTable objectTable, IPluginLog log)
    {
        this.configuration = configuration;
        this.palette = palette;
        this.chatGui = chatGui;
        this.objectTable = objectTable;
        this.log = log;

        chatGui.CheckMessageHandled += this.OnChatMessage;
    }

    public void Dispose()
    {
        this.chatGui.CheckMessageHandled -= this.OnChatMessage;
    }

    public static SeString BuildColorized(string text, Palette palette)
    {
        var builder = new SeStringBuilder();
        foreach (var segment in ColorTagParser.Parse(text, palette))
        {
            if (segment.Text.Length == 0)
            {
                continue;
            }

            AppendSegment(builder, segment);
        }

        return builder.Build();
    }

    private void OnChatMessage(IHandleableChatMessage message)
    {
        try
        {
            if (!this.configuration.Enabled)
            {
                return;
            }

            if (!TextChannels.Contains(message.LogKind))
            {
                return;
            }

            if (!this.IsSelfOrNoSender(message))
            {
                return;
            }

            if (!ColorTagParser.ContainsColorTag(message.Message.TextValue))
            {
                return;
            }

            var colorized = BuildColorized(message.Message, this.palette);
            if (colorized is not null)
            {
                message.Message = colorized;
            }
        }
        catch (Exception ex)
        {
            this.log.Error(ex, "Failed to colorize chat message.");
        }
    }

    private bool IsSelfOrNoSender(IHandleableChatMessage message)
    {
        var sender = message.Sender?.TextValue?.Trim();
        if (string.IsNullOrEmpty(sender))
        {
            return true;
        }

        var localName = this.objectTable[0]?.Name.TextValue;
        return !string.IsNullOrEmpty(localName) &&
               sender.Equals(localName, StringComparison.OrdinalIgnoreCase);
    }

    private static SeString? BuildColorized(SeString original, Palette palette)
    {
        var builder = new SeStringBuilder();
        var changed = false;

        foreach (var payload in original.Payloads)
        {
            if (payload is TextPayload textPayload)
            {
                foreach (var segment in ColorTagParser.Parse(textPayload.Text ?? string.Empty, palette))
                {
                    if (segment.Text.Length == 0)
                    {
                        continue;
                    }

                    if (segment.Color.IsColor)
                    {
                        changed = true;
                    }

                    AppendSegment(builder, segment);
                }
            }
            else
            {
                builder.Add(payload);
            }
        }

        return changed ? builder.Build() : null;
    }

    private static void AppendSegment(SeStringBuilder builder, ColorSegment segment)
    {
        if (segment.Color.Red is byte r && segment.Color.Green is byte g && segment.Color.Blue is byte b)
        {
            builder.Add(RawColorPush(r, g, b));
            builder.Add(RawEdgeColorPush(r, g, b));
            builder.AddText(segment.Text);
            builder.Add(RawEdgeColorPop);
            builder.Add(RawColorPop);
            return;
        }

        builder.AddText(segment.Text);
    }

    private static readonly RawPayload RawColorPop = new(new byte[] { 0x02, 0x13, 0x00, 0xEC, 0x03 });
    private static readonly RawPayload RawEdgeColorPop = new(new byte[] { 0x02, 0x14, 0x00, 0xEC, 0x03 });

    private static RawPayload RawColorPush(byte r, byte g, byte b)
    {
        var value = 0xFF000000u | (uint)((r << 16) | (g << 8) | b);
        return RawColorChunk(0x13, value);
    }

    private static RawPayload RawEdgeColorPush(byte r, byte g, byte b)
    {
        var value = 0xFF000000u | (uint)(((r / 2) << 16) | ((g / 2) << 8) | (b / 2));
        return RawColorChunk(0x14, value);
    }

    private static RawPayload RawColorChunk(byte chunkType, uint value)
    {
        var packed = PackInteger(value);
        var chunk = new byte[packed.Length + 4];
        chunk[0] = 0x02;
        chunk[1] = chunkType;
        chunk[2] = 0x00;
        Array.Copy(packed, 0, chunk, 3, packed.Length);
        chunk[^1] = 0x03;
        return new RawPayload(chunk);
    }

    private static byte[] PackInteger(uint value)
    {
        if (value < 0xCF)
        {
            return [(byte)(value + 1)];
        }

        var bytes = BitConverter.GetBytes(value);
        var ret = new List<byte> { 0xF0 };
        for (var i = 3; i >= 0; i--)
        {
            if (bytes[i] == 0)
            {
                continue;
            }

            ret.Add(bytes[i]);
            ret[0] |= (byte)(1 << i);
        }

        ret[0] -= 1;
        return ret.ToArray();
    }
}