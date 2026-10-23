# ExplorerBlurMica-Tray

A system tray app and settings window for [ExplorerBlurMica](https://github.com/Maplespe/ExplorerBlurMica) by [Maplespe](https://github.com/Maplespe).

ExplorerBlurMica adds Blur, Acrylic and Mica backgrounds to Windows File Explorer and is configured with a `config.ini` file. This project gives it a graphical front end: pick an effect, tune the colors and apply the change from the tray without editing files or running scripts by hand.

> **All visual effects are made by ExplorerBlurMica.** This project does not modify, rebuild or redistribute ExplorerBlurMica. It downloads the official release from the author's GitHub page and only manages its configuration. If you like the effect, please star [the original project](https://github.com/Maplespe/ExplorerBlurMica).

## Download

Get `ExplorerBlurMica-Tray-Setup-<version>.exe` from [Releases](https://github.com/Duduka-INC/ExplorerBlurMica-Tray/releases) and run it. Setup asks for administrator rights once, downloads the official ExplorerBlurMica release, checks its SHA-256, registers it and restarts File Explorer.

The installer is not code-signed, so Windows SmartScreen may warn about an unknown publisher: choose **More info → Run anyway**.

## Features

- **Tray icon.** A left click opens a small panel: pick Default, Mica, Mica Alt, Acrylic or Blur, set the blend color density, choose a preset and press Apply. A right click opens a menu with the settings window, a Default switch, reopening Explorer windows and exit. The panel's own background follows the material you pick.
- **Settings → Effect.** A preview of a File Explorer window over your wallpaper, the material, the blend color for the current Windows theme (or the system color), and the extra options: transparent top bar (tabs, address bar, search, command bar), no scroll bar background, divider line.
- **Settings → Presets.** Built-in presets and your own: save the current settings under a name, apply, rename, delete. Your presets also show up in the tray panel. They are kept in `%LocalAppData%\ExplorerBlurMica-Tray\presets.json`.
- **Settings → General.** Start with Windows, display language (English or Russian), what a left click on the tray icon opens, ExplorerBlurMica status with update check, reinstall and remove.
- Changes take effect by reopening open Explorer windows on the same folders. Only switching to or from Default restarts Explorer.

## Requirements

- Windows 10 or Windows 11, 64-bit
- Mica and Mica Alt require Windows 11
- "Transparency effects" must be enabled in Settings → Personalization → Colors

Supported effects depend on the Windows version; see the [ExplorerBlurMica README](https://github.com/Maplespe/ExplorerBlurMica#readme) for details.

## Notes

- **Default** turns ExplorerBlurMica off by unregistering it, since its config has no "no effect" value. Explorer only loads it from a machine-wide registration, so switching to and from Default asks for administrator rights and restarts Explorer; switching between materials does neither.
- A fresh install uses Acrylic: Mica, ExplorerBlurMica's own default, draws nothing on recent Windows 11 builds (upstream issue [#299](https://github.com/Maplespe/ExplorerBlurMica/issues/299)).
- The blend color is drawn only with Acrylic. A density of 0 keeps the system acrylic tint.
- Windows hides new tray icons behind the arrow. To keep the icon visible, turn it on under Settings → Personalization → Taskbar → Other system tray icons (Settings → General has a shortcut).

## Troubleshooting

If File Explorer crashes after enabling the effect, hold `Esc` while opening Explorer: ExplorerBlurMica will not load, and you can remove it from the app (Settings → General) or with the original `uninstall.cmd`.

## Building

Requires the .NET 10 SDK on Windows 10 or 11 (x64).

```
dotnet build ExplorerBlurMicaTray.slnx
dotnet test ExplorerBlurMicaTray.slnx
```

The solution holds:

- `ExplorerBlurMicaTray.App` — the tray app and settings window (WinUI 3, unpackaged, Native AOT).
- `ExplorerBlurMicaTray.Core` — installing, configuring and applying ExplorerBlurMica, autostart and presets.
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

### Installer

The installer is built with [Inno Setup 7](https://jrsoftware.org/isinfo.php):

```
.\installer\build.ps1 -Version 0.1.0
```

It publishes the app with Native AOT and writes `artifacts\ExplorerBlurMica-Tray-Setup-<version>.exe`. If the ExplorerBlurMica download fails during setup, the app is still installed and can fetch it later.

ExplorerBlurMica is installed to `%ProgramFiles%\ExplorerBlurMica-Tray\ebm\<version>`. It is registered for every account on the PC, so its DLL must sit where only administrators can replace it. Users get write access to `config.ini` only, which holds nothing but colors and flags.

## Credits

- [ExplorerBlurMica](https://github.com/Maplespe/ExplorerBlurMica) by [Maplespe](https://github.com/Maplespe), which does all of the actual work. It is licensed under the [GNU LGPL v3](https://github.com/Maplespe/ExplorerBlurMica/blob/main/COPYING.LESSER).

## License

The code in this repository is licensed under the [MIT License](LICENSE). ExplorerBlurMica itself is not part of this repository and keeps its own license.
