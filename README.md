# Display Toolkit

A small, fast, open-source tray app for controlling ASUS monitors on Windows 11. Think
[Lenovo Legion Toolkit](https://github.com/BartoszCichecki/LenovoLegionToolkit), but for displays.

It replaces ASUS DisplayWidget Center with something calmer: brightness, picture modes, HDR presets, OLED care and
GamePlus overlays one click away in the tray, plus profiles that switch automatically by game, time of day or sunset.

> **Status:** early development. The first target is the ROG Swift PG32UCWM; other ASUS gaming/OLED monitors that
> speak the same protocol should mostly work, because the app only shows what the monitor reports it supports.

## How it works

Everything goes over **DDC/CI**, the standard monitor-control channel that Windows exposes through `dxva2.dll`.
No driver, no service, no ASUS software, no account and no telemetry. See the
[protocol notes](docs/research/asus-ddc-protocol.md) for the full register map, verified on real hardware.

## Requirements

- Windows 11
- A monitor with **DDC/CI enabled** in its on-screen menu, connected over DisplayPort or HDMI

Nothing else: releases are self-contained.

## Building

```
dotnet build
dotnet test
```

Requires the .NET 10 SDK. The solution is `DisplayToolkit.slnx`:

| Project | Purpose |
|---|---|
| `DisplayToolkit.Core` | Monitor discovery, capabilities, DDC/CI sessions. No UI. |
| `DisplayToolkit.Automation` | Profiles, rules and triggers. No UI. |
| `DisplayToolkit.App` | WPF tray app, flyout and main window. |

See [docs/architecture.md](docs/architecture.md) for the design and [docs/design](docs/design/design-spec.md) for the UI spec.

## License

[MIT](LICENSE). Display Toolkit is an independent project and is not affiliated with or endorsed by ASUS.
"ASUS", "ROG" and "GameVisual" are trademarks of ASUSTeK Computer Inc.
