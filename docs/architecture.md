# Display Toolkit: architecture & implementation plan

Status: proposal for review, 2026-10-03.
Inputs: [protocol research](research/asus-ddc-protocol.md) · [design spec](design/design-spec.md) (§13 holds the product decisions).

## 1. Goals and constraints

- A tray-first Windows 11 app that replaces ASUS DisplayWidget Center. v1 targets the **ROG Swift PG32UCWM**, but the code
  is driven by capabilities, so other ASUS gaming/OLED models mostly work automatically.
- **Users need nothing pre-installed.** Publish self-contained. NuGet packages are fine when they earn their place.
- Code quality, readability and maintainability come first. It's open source, so outsiders must be able to find their way around.
- No ASUS code in the repo. We implement from our own protocol notes, the MCCS standard and ASUS's Apache-2.0 CLI reference.

## 2. Tech stack

| Concern | Choice | Why |
|---|---|---|
| Runtime | **.NET 10** (LTS), `net10.0-windows10.0.22621.0` | LTS; the Windows TFM gives WinRT projections (geolocation) without packages |
| UI | **WPF** with the built-in **Fluent theme** (`ThemeMode="System"`) | Native Windows 11 look with no UI library; proven by Legion Toolkit |
| MVVM | **CommunityToolkit.Mvvm** (source generators) | Removes INotifyPropertyChanged/command boilerplate at compile time; widely known |
| DI / hosting | **Microsoft.Extensions.Hosting** | Standard composition, logging and lifetime; keeps services testable |
| Logging | Microsoft.Extensions.Logging + a small rolling **file logger of our own** | One log file for bug reports, no Serilog needed |
| Interop | `LibraryImport` source-generated P/Invoke, `SafeHandle`s | AOT-friendly, no marshalling surprises, handles can't leak |
| Settings | System.Text.Json with **source-generated** contexts, JSON in `%AppData%\DisplayToolkit\` | Human-readable, diffable, trim-safe |
| Tests | **xUnit v3** + hand-written fakes (no mocking library) | Fakes of the DDC bus are clearer than mocks |
| Packaging | `dotnet publish` self-contained, win-x64 (arm64 later), zip first; installer (Inno Setup or Velopack) later | No prerequisites for users |

Everything else is written in-house: tray icon, global hotkeys, Windows HDR toggle, sunrise/sunset maths, window backdrop.
Each piece is a small Win32 wrapper; none needs a dependency.

## 3. Solution layout

```
display-toolkit/
├─ DisplayToolkit.slnx
├─ Directory.Build.props        # nullable, warnings-as-errors, analyzers, LangVersion, common metadata
├─ Directory.Packages.props     # central package versions
├─ .editorconfig                # style + analyzer severities
├─ src/
│  ├─ DisplayToolkit.Core/          # monitor control. No UI, no WPF.
│  ├─ DisplayToolkit.Automation/    # profiles, rules, triggers. No UI.
│  └─ DisplayToolkit.App/           # WPF: tray, flyout, main window, HUD, hotkeys, settings storage
├─ tests/
│  ├─ DisplayToolkit.Core.Tests/
│  └─ DisplayToolkit.Automation.Tests/
├─ docs/  (research, design, architecture)
└─ .github/workflows/ci.yml     # build + test on push/PR
```

The dependency direction is strictly `App → Automation → Core`. Core and Automation never reference WPF, so they're
unit-testable and could later back a CLI or service without changes.

## 4. Core: talking to the monitor

### 4.1 Layers

```
Monitor (public, per physical monitor)      typed API: monitor.SetAsync(Features.Brightness, 40)
  └─ MonitorSession                          one worker thread + command queue, coalescing, verify, retry, reconnect
       └─ IDdcChannel                        Get(code) / Set(code, value) / GetCapabilities()
            ├─ Dxva2DdcChannel               real: dxva2.dll Monitor Configuration API
            └─ FakeDdcChannel (tests)        simulated PG32UCWM incl. 0xFE locks, bitmasks, handle invalidation
```

- **`IDdcChannel`** is the only seam to hardware. It is synchronous and blocking, which is fine because only the session's
  worker thread calls it.
- **`MonitorSession`** owns one dedicated background thread per physical monitor (DDC calls block for ~40–80 ms) and a
  `Channel<Command>`. Its rules come straight from our measurements:
  - **Spacing**: a configurable delay between commands (default 20 ms).
  - **Coalescing**: a newer write to the same code replaces a queued older one, so the latest value wins.
  - **Verify**: after a write, read it back once after a short delay. A mismatch counts as a failure → 2 retries → `Failed`.
  - **Bitmask registers** (`0xFC`, `0xFD`): read-modify-write happens inside the worker, atomically with respect to other commands.
  - **Settle-sensitive writes** (`0xE2` HDR presets, input switch): longer settle and verify window, tolerating transient values.
  - **Reconnect**: any call failing with an invalid handle triggers re-enumeration (HDR400 does this). The worker matches the
    monitor by stable identity (below), swaps in the fresh handle and retries the command once.
- **`Monitor`** exposes typed operations and an observable **`MonitorState`**. Each feature's value carries a status:
  `Confirmed`, `Pending`, `Failed`, `Locked` (the monitor returned `0xFE`) or `Unsupported`.

### 4.2 Capabilities → features

- `CapabilitiesParser` parses the MCCS string into `MonitorCapabilities`: supported codes, allowed values per code, and the
  supported-bits mask for bitmask registers. It's pure, so it can be thoroughly unit-tested against real capability strings.
- `FeatureCatalog` declares every feature **once**, as data:
  ```csharp
  public static readonly RangeFeature Brightness   = new("brightness",   Vcp.Brightness);
  public static readonly EnumFeature<GameVisual> PictureMode = new("picture-mode", Vcp.AsusGameVisual);
  public static readonly FlagFeature UniformBrightness = new("uniform-brightness", Vcp.AsusToggles2, bit: 6);
  ```
  Feature kinds: `RangeFeature` (0–max), `EnumFeature<T>` (advertised values only), `FlagFeature` (one bit of a bitmask
  register), plus a few composites such as `ProximityFeature` (the minutes and distance fields in one code).
  A feature is supported only if the capabilities say so. The UI and profiles reference features by **stable string id**,
  never by VCP code.
- `Vcp` holds named constants for every code in the protocol notes. It's the only place raw numbers appear.

### 4.3 Monitor discovery and identity

- `MonitorEnumerator`: `EnumDisplayMonitors` → `GetPhysicalMonitorsFromHMONITOR`, joined with **`QueryDisplayConfig` /
  `DisplayConfigGetDeviceInfo`**. That gives the friendly name, EDID manufacturer and product code, and the device path.
- `MonitorId` = EDID manufacturer + product code + connector instance. It's stable across reboots and handle changes, and it's
  the key for profiles and layouts.
- `DisplayChangeWatcher` reacts to `WM_DISPLAYCHANGE`, device arrival/removal and resume from sleep by re-enumerating.

### 4.4 Windows HDR

`WindowsHdr` uses `DisplayConfigGetDeviceInfo(ADVANCED_COLOR_INFO)` to read HDR state and `DISPLAYCONFIG_DEVICE_INFO_SET_HDR_STATE`
to switch it (Win11 24H2+; on older builds it falls back to `SET_ADVANCED_COLOR_STATE`). An `HdrChanged` event feeds both the UI
(swap picture mode for HDR presets) and automation.

## 5. Automation: profiles and rules

- **`Profile`**: id, name, glyph, optional hotkey, `ShowInFlyout`, and a list of `(featureId, value)` entries containing only
  the ticked settings. Profiles are per `MonitorId`.
- **`ProfileApplier`** writes entries in dependency order: Windows HDR → picture/HDR mode → colour temp → RGB → everything
  else. It reports per-setting success, so the UI can say "Night applied, except Colour temperature". This order is required
  because the monitor stores settings per preset (verified): a preset switch would overwrite overrides written before it.
- **Triggers** produce events or states. Each is a small class behind `ITrigger`:
  - `TimeOfDayTrigger` and `SunTrigger`: events. `SunCalculator` is the NOAA algorithm with no I/O.
  - `ForegroundAppTrigger`: `SetWinEventHook(EVENT_SYSTEM_FOREGROUND)`, process name and path.
  - `FullscreenGameTrigger`: `SHQueryUserNotificationState` plus foreground window covering the monitor.
  - `PowerSourceTrigger` and `HdrTrigger`.
- **`AutomationEngine`** implements the arbitration rules in design spec §5.6 (schedule baseline, condition rules by list
  order, manual override until the next rule event, pause). It's pure logic over a `TimeProvider`, so tests can simulate a
  whole day in milliseconds.
- **Location**: `Windows.Devices.Geolocation`, with a manual city/latitude-longitude fallback stored in settings.

## 6. App (WPF)

- **Startup**: generic host; single instance via a named mutex plus a named pipe that tells the running instance to show
  itself. Start with Windows via `HKCU\…\Run` (no elevation needed; DDC/CI works as a normal user).
- **Tray**: own `NotifyIcon` on `Shell_NotifyIconW` with a message-only `HwndSource`: left click opens the flyout, right
  click the menu, wheel changes brightness, theme-aware icon.
- **Flyout**: borderless, Acrylic via `DwmSetWindowAttribute`, positioned from `SHAppBarMessage(ABM_GETTASKBARPOS)`. Tiles
  come from `layout.json` (spec §4.5) filtered by capabilities.
- **Main window**: Mica, custom navigation pane, settings-card pages (Display, Profiles & automation, OLED care, GamePlus,
  Settings).
- **Custom controls** (spec §11): BrightnessBand, Tile/TileGrid (drag reorder), SettingsCard/Expander, SegmentedControl,
  InfoBar, DayTimeline, HUD window. Each control is a lookless control with its theme in its own `Themes/*.xaml`.
- **ViewModels** bind to `MonitorState` through a thin adapter that marshals to the dispatcher. Sliders apply the throttle and
  debounce rules from spec §6.1. ViewModels never touch DDC directly.
- **Hotkeys**: `RegisterHotKey` on the message-only window; conflicts surface in the shortcut recorder.

## 7. Code quality guardrails

- `Nullable` enabled, `TreatWarningsAsErrors`, `AnalysisLevel=latest-recommended`, `.editorconfig` enforced in the build.
- File-scoped namespaces, one type per file, `sealed` by default, records for immutable data, no static mutable state.
- Interop isolated in `Native/` folders: one `static partial class` per DLL, everything `internal`.
- Tests: the capabilities parser and catalog against the real PG32UCWM string; `MonitorSession` behaviour (coalescing,
  bitmask read-modify-write, `0xFE` lock, verify mismatch, reconnect after handle loss) against `FakeDdcChannel`; automation
  arbitration and sun maths with a fake `TimeProvider`.
- A **hardware smoke test** project, excluded from CI and run manually: it reads all features and does reversible writes,
  like the scripts from this research session.
- CI on GitHub Actions: restore, build, test, and a `publish` artifact on tags.

## 8. Milestones

| # | Milestone | Done when |
|---|---|---|
| M0 | Repo scaffolding | Solution, props, editorconfig, CI, README, license; empty projects build green |
| M1 | Core DDC | Enumerate + identify the PG32UCWM, parse capabilities, typed get/set for all v1 features, session worker with coalescing, verify, reconnect; unit tests + hardware smoke test pass |
| M2 | Tray + flyout | Tray icon, flyout with brightness band, picture mode, HDR (incl. Windows HDR toggle), tile grid with sub-pages, live state, failure states |
| M3 | Main window | Display, OLED care, GamePlus, Settings (incl. Settings › Monitor for `0xFC` toggles), Monitor information page |
| M4 | Profiles & automation | Profile editor, rules, day strip, app/game/time/sun/power/HDR triggers, hotkeys, HUD |
| M5 | Polish & ship | Flyout edit mode, start with Windows, export/import, logging, self-contained publish, first GitHub release |

Each milestone ends with something runnable on your monitor.

## 9. Decisions needed before M0

1. **License**: MIT (permissive; anyone can reuse it, including commercially) or GPL-3.0 (forks must stay open, like Legion Toolkit)?
2. **Git/GitHub**: should I `git init` here and create a GitHub repo (name: `display-toolkit`)? Public now, or private until v1?
3. **Distribution**: a single `.exe` (self-contained single-file, ~70 MB, slower first start) or a zip folder (faster start)?
   I recommend zip first and an installer at M5.
