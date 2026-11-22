# Birthly

A lightweight birthday tracker for Windows with a Liquid Glass theme and a Basic theme. It can run either as a desktop widget or as a normal window in your taskbar.

Written in C# (.NET Framework 4.x) with no external dependencies.

## Features

- **Themes:** Live blurred Liquid Glass (with Clear, Ice, Rose, Mint, Amber, Violet, and Obsidian variants), Light/Dark mode, and a flat Basic mode.
- **Widget or Window Mode:** Keep it pinned to the desktop (with optional layering rules for apps like Rainmeter or Spotify) or switch it to a regular taskbar / Alt+Tab window.
- **Countdowns & Stats:** Live days/hours/minutes/seconds countdown, current & turning age, total days lived, and optional birthday animations and banners.
- **Calendar & List:** Monthly calendar view, search bar, 9 sorting options, and one-click wish copying.
- **Languages:** Searchable dropdown with 45 languages (including a few fictional ones).
- **Customization:** Custom window size, built-in pop click sound (or your own `.wav`), Glass/System/custom cursor support, and single-file `.birthly` backup export/import.
- **Standalone Setup:** Comes as a single `BirthlySetup.exe` installer with its own uninstaller.

## Installation

1. Download `BirthlySetup.exe` from the [Releases](../../releases) page.
2. Run it, pick your theme and language, and click Install.

## Building from Source

Birthly compiles with the built-in Windows .NET Framework 4.x C# compiler (`csc.exe`):

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize+ /out:Birthly.exe /win32icon:Birthly.ico /r:System.Runtime.Serialization.dll LiquidGlassWidget.cs
```

## License

MIT License. See [LICENSE](LICENSE) for details. You are free to download, use, and modify this project as long as you keep the original copyright and credit.
