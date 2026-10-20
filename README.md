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

## Troubleshooting

If File Explorer crashes after enabling the effect, hold `Esc` while opening Explorer: ExplorerBlurMica will not load, and you can uninstall it from the app or with the original `uninstall.cmd`.

## Credits

- [ExplorerBlurMica](https://github.com/Maplespe/ExplorerBlurMica) by [Maplespe](https://github.com/Maplespe), which does all of the actual work. It is licensed under the [GNU LGPL v3](https://github.com/Maplespe/ExplorerBlurMica/blob/main/COPYING.LESSER).

## License

The code in this repository is licensed under the [MIT License](LICENSE). ExplorerBlurMica itself is not part of this repository and keeps its own license.
