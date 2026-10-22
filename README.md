# ExplorerBlurMica-Tray

A system tray app and settings window for [ExplorerBlurMica](https://github.com/Maplespe/ExplorerBlurMica) by [Maplespe](https://github.com/Maplespe).

ExplorerBlurMica adds Blur, Acrylic and Mica backgrounds to Windows File Explorer and is configured with a `config.ini` file. This project gives it a graphical front end: pick an effect, tune the colors and apply the change from the tray without editing files or running scripts by hand.

> **All visual effects are made by ExplorerBlurMica.** This project does not modify, rebuild or redistribute ExplorerBlurMica. It downloads the official release from the author's GitHub page and only manages its configuration. If you like the effect, please star [the original project](https://github.com/Maplespe/ExplorerBlurMica).

## Status

Early development. Nothing is released yet.

## Planned features

- Tray icon with quick actions: turn the effect on or off, switch between Mica, MicaAlt, Acrylic and Blur (Clear), apply changes
- Settings window for every ExplorerBlurMica option:
  - effect type
  - blend color and opacity, separately for light and dark mode
  - `clearAddress`, `clearBarBg`, `clearWinUIBg`, `showLine`
- Presets: save, load and switch color and effect combinations
- Apply changes by reopening open Explorer windows on the same folders
- Install, update and uninstall ExplorerBlurMica from the app; administrator rights are requested only for registering and unregistering the DLL
- Start with Windows

## How it works

1. On first run the app downloads `Release_x64.zip` from the [official ExplorerBlurMica releases](https://github.com/Maplespe/ExplorerBlurMica/releases) and checks its hash.
2. The DLL is registered with `regsvr32`, the same way the original `register.cmd` does it.
3. Settings from the UI are written to ExplorerBlurMica's `config.ini`.
4. Open Explorer windows are reopened so the new settings take effect.

## Requirements

- Windows 10 or Windows 11, 64-bit
- Mica and MicaAlt require Windows 11
- "Transparency effects" must be enabled in Settings → Personalization → Colors

Supported effects depend on the Windows version; see the [ExplorerBlurMica README](https://github.com/Maplespe/ExplorerBlurMica#readme) for details.

## Building

Requires the .NET 10 SDK on Windows 10 or 11 (x64).

```
dotnet build ExplorerBlurMicaTray.slnx
dotnet test ExplorerBlurMicaTray.slnx
```

The solution holds:

- `ExplorerBlurMicaTray.App` — the tray app (WinUI 3, Native AOT). Left click on the tray icon opens the flyout: pick Default, Mica, Mica Alt, Acrylic or Blur, set the tint and press Apply. Right click opens a menu. The flyout's own background follows the material you pick.
- `ExplorerBlurMicaTray.Core` — installing, configuring and applying ExplorerBlurMica.
- `ebmt` — a small command-line tool over the core, also used by the installer:

```
ebmt status                     ExplorerBlurMica state, transparency, open Explorer windows
ebmt latest                     latest official release on GitHub
ebmt install                    download, verify and register (asks for administrator rights)
ebmt uninstall                  unregister and delete (asks for administrator rights)
ebmt enable / disable           turn the effect on or off, keeping the files (asks for administrator rights)
ebmt config                     show config.ini
ebmt config set effect Acrylic  change a setting
ebmt reopen                     close and reopen Explorer windows (applies config changes)
ebmt restart-explorer           restart explorer.exe (needed after install, update or uninstall)
```

"Default" turns ExplorerBlurMica off by unregistering it, since its config has no "no effect" value. Explorer only loads it from a machine-wide registration, so switching to and from Default asks for administrator rights and restarts Explorer; switching between materials does neither.

A fresh install uses the Acrylic effect: Mica, ExplorerBlurMica's own default, draws nothing on recent Windows 11 builds (upstream issue [#299](https://github.com/Maplespe/ExplorerBlurMica/issues/299)).

### Installer

The installer is built with [Inno Setup 7](https://jrsoftware.org/isinfo.php):

```
.\installer\build.ps1 -Version 0.1.0
```

It publishes the app with Native AOT and writes `artifacts\ExplorerBlurMica-Tray-Setup-<version>.exe`. Setup downloads the official ExplorerBlurMica release from GitHub, checks its SHA-256 and registers it, so a single administrator prompt covers the whole install. If the download fails, the app is still installed.

ExplorerBlurMica is installed to `%ProgramFiles%\ExplorerBlurMica-Tray\ebm\<version>`. It is registered for every account on the PC, so its DLL must sit where only administrators can replace it. Users get write access to `config.ini` only, which holds nothing but colors and flags.

## Troubleshooting

If File Explorer crashes after enabling the effect, hold `Esc` while opening Explorer: ExplorerBlurMica will not load, and you can uninstall it from the app or with the original `uninstall.cmd`.

## Credits

- [ExplorerBlurMica](https://github.com/Maplespe/ExplorerBlurMica) by [Maplespe](https://github.com/Maplespe), which does all of the actual work. It is licensed under the [GNU LGPL v3](https://github.com/Maplespe/ExplorerBlurMica/blob/main/COPYING.LESSER).

## License

The code in this repository is licensed under the [MIT License](LICENSE). ExplorerBlurMica itself is not part of this repository and keeps its own license.
