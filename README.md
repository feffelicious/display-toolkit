# Display Toolkit

A small, fast, open-source tray app for controlling ASUS monitors on Windows 11. Think
[Lenovo Legion Toolkit](https://github.com/BartoszCichecki/LenovoLegionToolkit), but for displays.

It replaces ASUS DisplayWidget Center with something calmer: brightness, picture modes, HDR presets, OLED care and
GamePlus overlays one click away in the tray, plus profiles that switch by game, app, time of day or sunset, and
brightness and warmth that follow the sun.

<p align="center">
  <img src="docs/images/quick-settings.png" width="360" alt="Quick settings from the tray: brightness, profiles and tiles">
</p>

> **Supported monitors:** built and tested on the ROG Swift PG32UCWM. Other ASUS gaming and OLED monitors that speak
> the same protocol should mostly work, because the app only shows what the monitor reports it supports.

## Features

- **Quick settings** from the tray icon or **Ctrl+Alt+D**: brightness, profiles and a tile grid you arrange yourself
  (picture mode, HDR, blue light filter, shadow boost, crosshair, OLED anti-flicker and more). Scroll over the tray
  icon to change brightness.
- **Shortcuts** for brightness, the next picture mode, HDR, Target mode and each input, all changeable in Settings,
  with a small on-screen confirmation.
- **Target mode** dims every screen except the window you're using.
- **A keypad for the monitor's own menu**, so you don't have to reach for its buttons.
- **The full set of monitor settings** in the main window: picture and color (color gamut, six-axis saturation),
  upscaling sharpness, OLED care (pixel cleaning, panel protection, proximity sensor), GamePlus (including the overlay
  position), Aura lighting, and the monitor's own system options. A button resets the current picture mode to its
  factory settings.
- **Profiles**: a named set of settings, applied in the right order (HDR, then picture mode, then color, then the
  rest). Pick one in quick settings or the tray menu, or give it a shortcut; press the shortcut again to go back to
  automatic.
- **Rules** switch profiles for you: at a time, at sunrise or sunset, while an app or a full-screen game runs, on
  battery, or while HDR is on. A day strip shows what today will look like.
- **Follow the sun**: brightness and warmth fade between day and night values around sunrise and sunset.
- **Find a setting** (Ctrl+F) in the main window, and a monitor switch when several monitors are connected.
- **Export and import** everything to move to a new PC.
- **Update reminders** when a new version is out (once a day, from GitHub; can be turned off).

Settings only appear when the monitor reports them, so every model shows what it actually has.

## Screenshots

| | |
|---|---|
| ![All monitor settings in the main window](docs/images/display.png) | ![Profiles with the settings each one includes](docs/images/profiles.png) |
| **Display**: every setting the monitor offers, grouped the way Windows Settings does it. | **Profiles**: pick which settings a profile includes and give it a shortcut. |
| ![Today's schedule and Follow the sun](docs/images/automation.png) | ![Adding a rule](docs/images/add-rule.png) |
| **Automation**: what the schedule does today, and brightness and warmth that follow the sun. | **Rules**: switch profiles at a time, at sunset, or while an app or game runs. |

## Install

Download from [Releases](../../releases):

- **`DisplayToolkit-<version>-win-x64.msi`**: the installer. It installs for your account only (no admin rights),
  adds Display Toolkit to the Start menu and to Installed apps, and updates an older version in place.
- **`DisplayToolkit-<version>-win-x64.zip`**: the portable version. Unzip it anywhere and run `DisplayToolkit.exe`.

Either way it's one self-contained app: no .NET or anything else to install. To start it with Windows, turn that on
in Settings.

The app isn't code-signed yet, so the first time you run it Windows SmartScreen may say "Windows protected your PC".
Choose **More info**, then **Run anyway**. The source and the build that made the release are both on GitHub.

Requirements:

- Windows 11
- A monitor with **DDC/CI enabled** in its on-screen menu, connected over DisplayPort or HDMI

Display Toolkit and ASUS DisplayWidget Center both talk to the monitor and can get in each other's way; close the
ASUS app while you use this one.

Settings, profiles and the log live in `%AppData%\DisplayToolkit`. Delete that folder to start fresh.

## Helping add a monitor or a setting

Both downloads include **`DisplayToolkit.Probe.exe`**, a small tool that only reads from the monitor and never changes
anything. Its output tells us what your monitor supports, and its `--watch` mode finds the code behind a setting the
app doesn't have yet. To find one:

1. Update Display Toolkit to the latest version (1.4.2 or newer), which installs the probe next to the app.
2. Quit Display Toolkit (right-click the tray icon, then **Exit**) and close ASUS DisplayWidget Center, so nothing
   else talks to the monitor while the probe runs.
3. Open **Terminal** (press the Windows key, type `Terminal`, press Enter).
4. Paste this and press Enter:

   ```powershell
   & "$env:LOCALAPPDATA\Programs\Display Toolkit\DisplayToolkit.Probe.exe" --watch
   ```

   With the portable zip, run `.\DisplayToolkit.Probe.exe --watch` from the folder you unzipped it to instead.
5. Wait until it says **Change one setting with the monitor's own buttons**. Reading everything takes up to a minute.
6. Use the monitor's own buttons to change the setting (for example, turn crop mode on). Close the monitor's menu,
   then press **Enter** in Terminal. The probe lists the codes that changed.
7. Change the setting back (crop mode off) and press **Enter** again. Doing both directions confirms the code.
8. Type `q` and press Enter to quit.
9. Copy everything in the window (**Ctrl+Shift+A** selects it all, then **Ctrl+C**) and paste it into an
   [issue](../../issues/new/choose), with the name of the setting you changed each time.

Without `--watch`, the probe just prints what the monitor supports, which is useful for any bug report.

## How it works

Everything goes over **DDC/CI**, the standard monitor-control channel that Windows exposes through `dxva2.dll`.
No driver, no service, no ASUS software, no account and no telemetry. The only personal data it uses is your location
for sunrise and sunset, which comes from Windows (rounded to about a kilometer and kept on your PC) or is typed in.
See the [protocol notes](docs/research/asus-ddc-protocol.md) for the register map, verified on real hardware.

## Building

```
dotnet build
dotnet test
```

Requires the .NET 10 SDK. The solution is `DisplayToolkit.slnx`:

| Project | Purpose |
|---|---|
| `DisplayToolkit.Core` | Monitor discovery, capabilities, DDC/CI sessions. No UI. |
| `DisplayToolkit.Automation` | Profiles, rules, sunrise and sunset, and the engine that decides which profile applies. No UI. |
| `DisplayToolkit.App` | WPF tray app, flyout and main window. |
| `tools/DisplayToolkit.Probe` | Console tool that dumps what a monitor reports, shipped next to the app. Handy for bug reports and new models. `--all` reads every code the monitor advertises; `--watch` lists the codes that change when you change a setting in the monitor's menu. |

`./build/publish.ps1` builds the release zip and the installer (WiX, restored from NuGet) into `artifacts/`. Pushing a tag like `v0.2.0` makes GitHub Actions
build, test and publish a release.

See [docs/architecture.md](docs/architecture.md) for the design and [docs/design](docs/design/design-spec.md) for the UI spec. Planned work is in [docs/todo.md](docs/todo.md).

## License

[MIT](LICENSE). Display Toolkit is an independent project and is not affiliated with or endorsed by ASUS.
"ASUS", "ROG" and "GameVisual" are trademarks of ASUSTeK Computer Inc.
