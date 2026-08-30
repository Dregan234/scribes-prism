# Scribe's Prism

Scribe's Prism is a private Dalamud development plugin that colorizes chat lines sent by your macros, only for you.

It is based on GoatCorp's SamplePlugin template and remains licensed under AGPL-3.0-or-later. It is not intended for submission to the official Dalamud repository.

## How it works

FFXIV does not render color markup in chat or macros natively, so tags like `<r>WARNING` would normally appear as literal text. Scribe's Prism watches incoming chat lines that you sent, and whenever one contains a color tag, it rebuilds that line with real foreground-color payloads. Because the recoloring happens on your client's chat-log path, the colors only appear to you — other players still see the raw tag text.

## Color tags

| Tag | Effect |
| --- | --- |
| `<w>` | White |
| `<g>` | Green |
| `<r>` | Red |
| `<b>` | Blue |
| `<y>` | Yellow |
| `<p>` | Purple |
| `<o>` | Orange |
| `<#RRGGBB>` | Custom color, e.g. `<#7F00FF>` |

Rules:

- A tag colors the rest of the line until the next color tag. A newline resets the color to the channel default.
- `\<r>` (a backslash before a tag) prints the tag literally as text.
- Native FFXIV tags such as `<se.1>`, `<t>`, `<me>`, and `<item:...>` pass through untouched.

Example macro line:

```
/p <w>WARNING <g>GO <r>STOP <#7F00FF>Custom
```

Only chat lines that (a) are sent by you and (b) contain at least one color tag are processed; everything else is left alone.

## Installation

### From Custom Repository

1. In FFXIV, type `/xlsettings` to open Dalamud Settings
2. Go to the **Experimental** tab
3. Under **Custom Plugin Repositories**, paste this URL:
   ```
   https://raw.githubusercontent.com/dregan234/scripes-prism/main/repo.json
   ```
4. Click the **+** button and **Save**
5. Type `/xlplugins` to open the Plugin Installer
6. Search for "Scribe's Prism" and click **Install**

## Development setup

Install XIVLauncher, Dalamud, and a compatible .NET SDK. Build from the repository root:

```powershell
dotnet build ScribesPrism.sln --configuration Debug
```

The development DLL is written to `ScribesPrism/bin/Debug/ScribesPrism.dll`. In `/xlsettings`, add its full path to **Experimental > Dev Plugin Locations**, then enable Scribe's Prism from `/xlplugins`.

Use `/prism` to open the settings window, and `/prismtest <text>` to print a colorized echo for testing tag parsing without crafting an in-game macro.