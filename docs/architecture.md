# Display Toolkit: architecture

How the code is organized and why. For the monitor protocol see the [protocol notes](research/asus-ddc-protocol.md); for the
UI see the [design spec](design/design-spec.md).

## 1. Goals and constraints

- A tray-first Windows 11 app that replaces ASUS DisplayWidget Center. Built and tested on the **ROG Swift PG32UCWM**, but
  driven by the capabilities the monitor reports, so other ASUS gaming/OLED models mostly work without changes.
- **Users need nothing pre-installed.** Releases are self-contained. NuGet packages are fine when they earn their place.
- Readability and maintainability come first: it's open source, so newcomers must be able to find their way around.
- No ASUS code. The app is written from our own protocol notes, the MCCS standard and ASUS's Apache-2.0 CLI reference.

## 2. Tech stack

| Concern | Choice | Why |
|---|---|---|
| Runtime | **.NET 10**, `net10.0-windows10.0.22621.0` | LTS; the Windows target gives WinRT projections (geolocation) without packages |
| UI | **WPF** with the built-in **Fluent theme** | Native Windows 11 look without a UI library |
| MVVM | **CommunityToolkit.Mvvm** (source generators) | No property-changed or command boilerplate |
| Hosting | **Microsoft.Extensions.Hosting** | Dependency injection, logging and lifetime in the standard way |
| Logging | Microsoft.Extensions.Logging with a small file logger of our own | One log file to attach to bug reports |
| Interop | `LibraryImport` source-generated P/Invoke | No runtime marshalling surprises |
| Storage | System.Text.Json with source-generated contexts, files in `%AppData%\DisplayToolkit` | Readable, diffable JSON |
| Tests | **xUnit v3** on Microsoft.Testing.Platform, hand-written fakes | A simulated monitor is clearer than mocks |
| Release | Self-contained single-file exe, zip and **WiX** MSI (WiX from NuGet) | Nothing to install for users or for the build |

The tray icon, global hotkeys, the Windows HDR switch, sunrise and sunset, and window backdrops are small Win32 wrappers of
our own rather than dependencies.

## 3. Solution layout

```
display-toolkit/
├─ DisplayToolkit.slnx
├─ Directory.Build.props        # target, nullable, analyzers as errors, metadata
├─ Directory.Packages.props     # central package versions
├─ src/
│  ├─ DisplayToolkit.Core/          # talking to monitors. No UI.
│  ├─ DisplayToolkit.Automation/    # profiles, rules, sun, arbitration. No UI.
│  └─ DisplayToolkit.App/           # WPF: tray, quick settings, main window, services
├─ tests/
│  ├─ DisplayToolkit.Core.Tests/        # against a simulated PG32UCWM
│  └─ DisplayToolkit.Automation.Tests/  # against a hand-driven clock
├─ tools/DisplayToolkit.Probe/      # console dump of what a monitor reports
├─ installer/                       # WiX package (built by build/publish.ps1)
├─ build/publish.ps1                # release build: exe, zip, msi
└─ .github/workflows/               # CI on push, release on v* tags
```

Dependencies point one way: `App → Automation → Core`. Core and Automation never reference WPF, so they're unit-tested
directly.

## 4. Core: talking to the monitor

```
MonitorSession          per monitor: capabilities, current values, verified writes, ValueChanged events
  └─ DdcWorker          one thread per monitor; work items with the same key coalesce (latest wins)
       └─ ResilientChannel   spacing between calls, retries, re-finds the monitor when its handle goes stale
            └─ IDdcChannel   Get(code) / Set(code, value) / GetCapabilities()
                 └─ Dxva2DdcChannel   the Windows Monitor Configuration API (dxva2.dll)
```

- **`MonitorSession`** keeps a confirmed and a visible value per feature. A write shows up at once as `Pending`, then
  becomes `Confirmed`, `Failed` (with the last confirmed value back) or `AwaitingConfirmation` (the monitor asks the user to
  confirm in its menu). `Locked` means the monitor reports `0xFE`, or the setting is ignored while HDR is on. Writes are read
  back; how long to wait depends on the feature (`WriteSettling`): picture-mode-like switches make Windows re-detect the
  monitor and need patience.
- **Features** are declared once, as data, in `FeatureCatalog`: `RangeFeature`, `EnumFeature`, `SwitchFeature`,
  `FlagFeature` (one bit of a bitmask register), `ByteFieldFeature` (one byte of a two-setting register) and
  `HdrModeFeature`. A feature exists for a monitor only if its capabilities say so. Everything above Core refers to
  features by **stable string id**, never by VCP code. `Vcp` names every code; it's the only place raw numbers appear.
- **`CapabilitiesParser`** turns the MCCS capabilities string into `MonitorCapabilities` (codes, allowed values, supported
  bits). It's pure and tested against the real PG32UCWM string.
- **Discovery** (`Win32MonitorEnumerator`) joins the physical monitors with the display configuration (CCD) API for the
  friendly name, EDID model and device path. `MonitorId` (model + instance) keys caches, layouts and profiles. Built-in
  laptop panels are skipped.
- **`WindowsHdr`** reads and switches Windows HDR for one display; **`DisplayLink`** reports how it's connected
  ("DisplayPort, 240 Hz").

## 5. Automation: profiles, rules and the sun

- **`Profile`**: name, glyph, optional `Shortcut`, whether it's in quick settings, and only the settings it includes
  (`ProfileSettings` lists what a profile can hold, Windows HDR included). **`ProfileApplier`** writes them in stages,
  Windows HDR, then picture or HDR mode, then color temperature, then the rest, because the monitor stores settings per
  mode. It reports which settings failed.
- **Rules** pair a `Trigger` with a profile. Schedule triggers (`TimeTrigger`, `SunTrigger`) are moments; condition
  triggers (`AppTrigger`, `FullscreenGameTrigger`, `PowerTrigger`, `HdrTrigger`) are states.
- **`AutomationEngine`** decides which profile applies (design spec §5.6): the most recent schedule rule sets the
  baseline, the highest active condition rule wins over it, a manual choice holds until the next rule event, and a pause
  stops everything. It's pure logic over a `TimeProvider`, so tests run whole days in milliseconds.
- **Conditions** come from `SystemSampler` (running apps, borderless or exclusive full-screen games, power source) and are
  matched by `ConditionEvaluator`.
- **The sun**: `SunCalculator` gives sunrise, sunset and the sun's height. `SunCycle` and `SunCycleCalculator` drive
  Follow the sun (brightness, and warmth via RGB gains from `ColorTemperature`), either around sunrise and sunset or all
  day by the sun's height.
- **`AutomationJson`** stores profiles, rules and the sun cycle per monitor model.

## 6. App (WPF)

- **Startup** (`App.xaml.cs`): single instance (a named mutex, and an event that asks the running instance to open quick
  settings), generic host, then the tray, the monitor scan, automation and the update check.
- **Services**: `MonitorService` (sessions, rescans after display changes), `MonitorContext` (the monitor the app
  controls; with several, the user's pick, remembered),
  `AutomationService` (runs the engine against the monitor: applies profiles, samples conditions, the sun cycle, profile
  shortcuts), `ShortcutService` (the app's own shortcuts), `TargetMode` (a click-through Win32 overlay with a hole
  that follows the foreground window), `LocationService`, `UpdateService` (GitHub releases), `SettingsTransfer` (export and import), and small stores
  for settings, layouts and the capabilities cache.
- **Tray** (`TrayIcon`, `TrayController`): `Shell_NotifyIconW` with its own message window; left click opens quick
  settings, right click the menu; display changes and resume trigger a rescan. While the pointer is over the icon a low-level
  mouse hook watches the wheel for brightness. The icon is drawn from `IconArt`.
- **Quick settings** (`Flyout/`): Acrylic, anchored by the taskbar; brightness band, profiles row and a tile grid the user
  arranges.
- **Main window** (`Views/`): Mica, a navigation pane and settings-card pages: Display, Profiles & automation, OLED care,
  GamePlus, Settings.
- **ViewModels** wrap each feature in a `FeatureState` that the controls bind to; nothing in the UI talks to DDC directly.
- **Controls** (`Controls/`, themes in `Themes/`): brightness band, tiles, settings cards and expanders, focus ring,
  shortcut recorder, and the panel behind the day strip.

## 7. Code quality guardrails

- Nullable on, analyzers at `latest-recommended` with warnings as errors, `.editorconfig` enforced in the build.
- File-scoped namespaces, `sealed` by default, records for data, interop in `Native/` folders.
- Persisted records use settable properties for anything with a default: the JSON source generator would otherwise reset
  missing values when reading files from older versions.
- Tests cover the capabilities parser and feature catalog, `MonitorSession` behavior (coalescing, bitmask writes, locks,
  failed writes, reconnects, confirmations), the automation engine, sun maths, conditions, profile application and storage.
- Debug builds have `--snapshot <folder>` to render every page to PNG, `DISPLAYTOOLKIT_DATA` to run on sample data, and
  `DISPLAYTOOLKIT_DEMO_MONITOR=1` to add an in-memory second monitor.
  Runs on sample data never write to the monitor.
