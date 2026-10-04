# Display Toolkit: design spec

Status: the original v1 design, 2026-10-03. Target: .NET 10 WPF, built-in Fluent theme, Windows 11.

> The app follows this spec closely, with some differences: Target mode, the setting search, tray-wheel brightness and
> multi-monitor switching aren't built yet; rules are reordered from their menu rather than dragged; the location is
> entered as coordinates; and Follow the sun (brightness and warmth that follow the sun) was added later. The decisions in
> §13 win over the rest.
Mockups: [`mockups/index.html`](mockups/index.html) (open in Edge or Chrome on Windows 11 at 100% zoom; they use the locally installed Segoe UI Variable and Segoe Fluent Icons).
The mockups are generated from `mockups/_src/*.mjs` (`node mockups/_src/build.mjs`). Edit the sources, not the HTML.

---

## 1. What we're building

A small tray app that controls ASUS monitors over DDC/CI and replaces ASUS DisplayWidget Center (DWC).
v1 targets the ROG Swift PG32UCWM, but nothing in the UI is model-specific: every control is driven by what the monitor reports (see §8).

Two surfaces, plus a HUD:

| Surface | Job | Share of use |
|---|---|---|
| **Tray flyout** | Brightness, profile, picture mode, HDR, a few user-chosen toggles | ~90% |
| **Main window** | Color tuning, OLED care, GamePlus, profiles and rules, app settings | ~10% |
| **Hotkey HUD** | Feedback for global shortcuts and tray-wheel brightness | passive |

## 2. Principles

1. **The flyout is the product.** Daily tasks take one click to open and one gesture to do. No scrolling in the default layout.
2. **Only what this monitor can do.** Unsupported features are hidden, never greyed out. Controls that depend on state (HDR signal, color temperature = User) appear and disappear with that state. The only disabled controls in the app are during a temporary lock (pixel cleaning in progress).
3. **Instant, then confirmed.** The UI moves first; the monitor catches up. Pending is a quiet dot, not a spinner. Failure reverts the control and says what happened, on that control.
4. **Native before novel.** Windows 11 Settings and Quick Settings patterns, Fluent tokens, system accent color, system theme. The single signature element is the **brightness band** in the flyout.
5. **Plain words.** "Picture mode", not "GameVisual". "Can't reach the monitor", not "DDC/CI error 0x…". Sentence case everywhere.

## 3. Information architecture

```
Tray icon
├─ Left click / Ctrl+Alt+D ............ Tray flyout
│   ├─ Header (monitor; menu if >1 monitor)
│   ├─ Brightness band (always)
│   ├─ Optional sliders (Contrast, Volume)
│   ├─ Profiles row (optional)
│   ├─ Tile grid (user-arranged)
│   │   └─ Sub-pages: Picture mode · HDR · Blue light · Shadow boost · Crosshair · FPS counter · Timer · Input · Color temp · Screen move · Proximity
│   ├─ Edit mode → Add list
│   └─ Footer: automation status · Edit · Open app
├─ Mouse wheel over icon ............... Brightness ±5 with HUD
├─ Right click ......................... Context menu
│   Open Display Toolkit · Profiles ▸ (radio list) · Pause automation ▸ (1 hour / Until tomorrow / Until I resume) · Exit
└─ Double click ........................ Main window

Main window (NavigationView, left pane)
├─ [Monitor card + switcher]
├─ Find a setting (search over all setting names + synonyms)
├─ Display
│   └─ Six-axis color (sub-page) · Monitor information (sub-page)
├─ Profiles & automation
├─ OLED care
├─ GamePlus
└─ Settings (pinned to bottom)
```

There is no separate "Overview" page: Display *is* the overview (monitor card on top, most-used controls first).

## 4. Tray flyout

Mockup: `mockups/tray-flyout.html`.

### 4.1 Window behavior

| Property | Value |
|---|---|
| Width | 360 px (DIPs). Height = content, max 80% of the work area; the body scrolls beyond that, header and footer stay. |
| Position | Right edge 12 px from work-area right, bottom edge 12 px above work-area bottom. Mirror for top/left/right taskbars (use `SHAppBarMessage(ABM_GETTASKBARPOS)`). Open on the monitor that hosts the taskbar that was clicked. |
| Chrome | `WindowStyle=None`, `ResizeMode=NoResize`, `ShowInTaskbar=False`, `Topmost=True`, `WS_EX_TOOLWINDOW`. **`AllowsTransparency=False`** (layered windows lose the DWM backdrop). `WindowChrome` with `GlassFrameThickness=-1`, `CaptionHeight=0`. |
| Backdrop | `DwmSetWindowAttribute(DWMWA_SYSTEMBACKDROP_TYPE = 38, DWMSBT_TRANSIENTWINDOW = 3)` (Acrylic). `DWMWA_USE_IMMERSIVE_DARK_MODE = 20` follows theme. `DWMWA_WINDOW_CORNER_PREFERENCE = 33, DWMWCP_ROUND`. Root background transparent. Fallback (Win10, or transparency effects off): solid `SolidBackgroundFillColorBase`. |
| Close | On `Deactivated`, Esc, clicking the tray icon again, or opening the main window. Pending writes continue after close. |
| Focus | Activates on open; initial focus on the brightness band so arrow keys work immediately. |
| Re-open | Same sub-page is **not** remembered: always opens on the main page. Edit mode never persists across open/close (closing commits). |

### 4.2 Layout (main page)

```
┌───────────────────────────── 360 ──────────────────────────────┐
│ 16                                                              │
│ [▭] ROG Swift PG32UCWM                                          │  header 40
│     DisplayPort, 240 Hz  [HDR]                                  │
│ 14                                                              │
│ [☀ ███████████████████                                   45 ]   │  band 40, corner 6
│ 12                                                              │
│ [ Desktop ][  Night  ][ Gaming ][ Movie  ]                      │  profiles 32, gap 6
│ 20                                                              │
│ [ ◐   › ] [ HDR  › ] [ ☾   › ]                                  │  tiles 100×48, gap 12×14
│  Scenery     HDR     Blue light                                 │  label Caption, 6 below
│ [ ☀   › ] [ ⌖   › ] [   ▣   ]                                   │
│ Shadow boost Crosshair Target mode                              │
│ 20                                                              │
├─────────────────────────────────────────────────────────────────┤
│ ⚡ Night since sunset, 18:21                       [✎]  [⚙]     │  footer 48
└─────────────────────────────────────────────────────────────────┘
side padding 18
```

- **Header**: 36 px glyph plate (`SubtleFillColorSecondary`, corner 6) with the monitor glyph; name in Body Strong; meta in Caption secondary: `<input>, <refresh> Hz` plus an accent `HDR` badge when an HDR signal is active. With more than one controllable monitor the plate+name become a button with a chevron that opens a menu (see 4.8).
- **Brightness band** (custom control, §11): full-width 40 px track, accent fill from the left (min 40 px so the sun glyph always sits on accent), value right-aligned in Body Strong tabular figures. Always present, always first, cannot be removed.
- **Optional slider rows**: Contrast, Volume. Standard Fluent `Slider` with leading 16 px glyph and trailing value (28 px, right-aligned). 36 px row, 6 px above.
- **Profiles row**: up to 4 equal cells, 32 px, `ControlFillColorDefault` with `ControlStrokeColorDefault`; the active profile is accent-filled. With 5+ profiles shown, the 4th cell becomes **More** (opens a menu). It is a single radio group for UIA. If a profile was applied by automation, the footer says so; the chip itself carries no extra mark.
- **Tile grid**: 3 columns, gap 12 horizontal / 14 vertical. Up to 4 rows visible before the body scrolls.
- **Footer**: `--acrylic-foot` (slightly darker layer over the acrylic), 1 px divider on top. Left: automation status (Caption secondary, bolt glyph 12 px). Right: Edit (pencil) and Open app (gear), 36×36 subtle icon buttons with tooltips.

Footer status strings (one line, truncate with ellipsis, full text in tooltip):

| Situation | Text |
|---|---|
| Schedule rule applied | `Night since sunset, 18:21` / `Desktop since 07:00` |
| App rule active | `Gaming while Forza Horizon 5 runs` |
| User picked a profile or changed an included setting | `Manual: automation resumes at sunrise` |
| Automation paused | `Automation paused until 21:47` |
| No rules | (empty, icons only) |

### 4.3 Tile kinds

| Kind | Visual | Main area click | Chevron | Label |
|---|---|---|---|---|
| **Toggle** | Icon centered | Toggles | — | Feature name |
| **Split** | Icon left zone + 30 px chevron zone with 1 px divider | Toggles (restores the last non-zero level) | Opens sub-page with levels/styles | Feature name |
| **Picker** | Icon + chevron, no divider | Opens sub-page | (same) | **Current value** (e.g. `Scenery`, `Gaming HDR`, `HDMI 1`), like Wi-Fi shows the network name |
| **Action** | Icon centered | Runs action (may confirm) | — | Action name |

On = accent fill (`AccentFillColorDefault`), glyph and chevron `TextOnAccentFillColorPrimary`, divider 28% white (light) / 20% black (dark). Off = `ControlFillColorDefault` + `ControlStrokeColorDefault`. Pickers are never "on".

### 4.4 Tile catalog

Tiles appear in the Add list only when the feature is reported by the monitor (or is app-side). "Default" marks the default layout.

| Id | Name / label | Kind | Glyph | Backing | Sub-page | Default |
|---|---|---|---|---|---|---|
| `picture-mode` | value | Picker | `E790` | VCP `0xDC` (SDR) / `0xE2` (HDR) | Mode list | ✓ |
| `hdr` | HDR | Split | text glyph "HDR" | Windows advanced color state | Use HDR + HDR presets | ✓ |
| `blue-light` | Blue light | Split | `E708` | `0xE6` 0–4 | Levels 1–4 | ✓ |
| `shadow-boost` | Shadow boost | Split | `E793` | `0xE5` 0–4 | Levels 1–4 | ✓ |
| `crosshair` | Crosshair | Split | `F272` | `0xE3` (0, 7–15) | Style grid | ✓ |
| `target-mode` | Target mode | Toggle | `E9A6` | App overlay | — | ✓ |
| `vrr` | Variable refresh rate | Toggle | `EC4A` | `0xFD` bit 0 | — | |
| `fps-counter` | FPS counter | Picker | `F404` | `0xEA` | Off / Number / Bar | |
| `timer` | Timer | Picker | `E916` | `0xE4` | Off / 30 / 40 / 50 / 60 / 90 s | |
| `align` | Alignment | Toggle | `E80A` | `0xE7` | — | |
| `color-temp` | value | Picker | `E9CA` | `0x14` | Presets list | |
| `input` | value | Picker | `EA4E` | `0x60` | Inputs (confirm, §6.6) | |
| `mute` | Mute | Toggle | `E74F` | `0x8D` | — | |
| `proximity` | Proximity | Split | `E95A` | `0xED` | Off / 5 / 10 / 15 min, distance | |
| `screen-move` | Screen move | Split | `E7C2` | `0xF9` | Off / Light / Middle / Strong | |
| `pixel-clean` | Pixel cleaning | Action | `F4A5` | `0xFD` bit 4 | Confirm dialog | |
| `anti-flicker` | Anti-flicker | Toggle | `E890` | `0xC5` | — | |
| `frame-boost` | Frame-rate boost | Toggle | `E945` | TBD (see open questions) | — | |
| `slider:contrast` | — | Slider row | `E7A1` | `0x12` | — | |
| `slider:volume` | — | Slider row | `E767` | `0x62` | — | |
| `profiles` | — | Profiles row | — | App | — | ✓ |

If a default item is unsupported it is skipped and the grid closes up.

### 4.5 Customization (edit mode)

Entered with the footer pencil (or `Ctrl+E` in the flyout). Same mental model as Windows quick settings.

- **Model**: one ordered list of item ids per monitor *model* (`%AppData%\DisplayToolkit\layout.json`), e.g.
  ```json
  { "PG32UCWM": ["brightness", "slider:contrast", "profiles", "picture-mode", "hdr", "blue-light", "shadow-boost", "crosshair", "target-mode"] }
  ```
  Full-width items (`brightness`, `slider:*`, `profiles`) always render above the grid in list order; tiles render in the grid in list order.
- **Edit visuals**: every removable item gets a 22 px circular **unpin badge** (`E77A`, `ControlSolidFillColorDefault` + `ControlStrokeColorSecondary`) overlapping its top-right corner by 8 px. Brightness shows a small pin glyph in `TextFillColorTertiary` instead (not removable). Tiles stay fully rendered; their click actions are disabled.
- **Reorder**: press-and-drag on a tile (4 px threshold). The tile lifts (translate with pointer, 1.5° tilt, shadow `0 12 24 / 22%`); a dashed 1.5 px `ControlStrongStrokeColorDefault` slot at 55% opacity shows the drop position; other tiles reflow (250 ms decelerate). Full-width rows reorder among themselves the same way, vertically. Tiles can't be dropped into the full-width area and vice versa.
- **Remove**: click the badge. The item collapses (167 ms) and the grid closes up. Never changes the monitor setting.
- **Add**: footer button `+ Add` replaces the body with the Add page: grouped list (Picture, Gaming, GamePlus, OLED care, Sound and input, App) of items that are supported **and** not shown. Row = 16 px glyph, name, control kind in Caption tertiary (`Slider`, `Toggle`, `List`, `Action`), and a 32 px `+` button. Adding appends to the end and keeps the list open; the row disappears. Back returns to edit mode.
- **Commit**: `Done` (accent), Enter, or closing the flyout commits. **Esc cancels** all edits since entering edit mode. A `Reset to default` command lives in the Add page's overflow (`…`) menu.
- **Keyboard**: arrows move focus between items; **Space** picks up the focused item, arrows move it, Space drops, Esc cancels the move; **Delete** unpins. Each move is announced via a UIA live region ("Shadow boost, moved to position 3 of 6").

### 4.6 Sub-pages

Opened from a chevron or picker tile. The body slides (see §12); the footer stays.

- Header row: 32 px back button (`E72B`) + title in Body Strong. Back: button, `Alt+Left`, `Backspace`, or `Esc` (Esc on a sub-page goes back, it doesn't close).
- Option lists: 36 px rows, corner 4, hover `SubtleFillColorSecondary`; selected row has `SubtleFillColorSecondary` + 3×16 accent pill on the left + accent check (`E73E`) on the right. Selecting applies immediately and stays on the page (so the user sees the result); Back to leave.
- Group headings (Caption, semibold, secondary) only where the monitor has real groups (HDR10 / Dolby Vision).
- A link row at the bottom where deeper settings exist ("Color and picture settings" opens Display in the main window).

| Sub-page | Content |
|---|---|
| Picture mode (SDR) | Modes from caps for `0xDC`, monitor order: Scenery, Racing, Cinema, RTS/RPG, FPS, MOBA, Night Vision, sRGB Cal, User. |
| HDR | Card row `Use HDR` + toggle (Windows HDR). Then presets for the active signal group only: HDR10 → Cinema HDR, Gaming HDR, Console HDR, True Black 400; Dolby Vision → Bright, Dark, Gaming. Note: "Dolby Vision presets appear here while Dolby Vision content is playing." With HDR off, only the toggle row and a short note. |
| Blue light / Shadow boost | Toggle row + segmented 1–4. Blue light note: "Level 4 matches TÜV low blue light." |
| Crosshair | Toggle row + 5×2 style grid (same artwork as GamePlus page) + "Position" link. |
| FPS counter, Timer, Color temp, Input | Option lists. |

### 4.7 States

| State | Trigger | Flyout shows |
|---|---|---|
| **Cached open** (normal) | Any open after the first read | Last known values instantly. Background re-read of brightness, picture/HDR mode, blue light, shadow boost (~6 reads). Changed values cross-fade (83 ms). Never overwrite a control the user is touching. |
| **First read** | No cache for this monitor | Header meta `Reading settings…`; skeleton blocks (`SubtleFillColorSecondary`, no shimmer) in the exact layout. Capabilities string read (~1–2 s) then values. |
| **Pending** | A write is unconfirmed for >300 ms | 6 px accent dot left of the band value / top-right inside a tile. Pulses opacity 1↔0.25 at 1.2 s; static at 60% with reduced motion. |
| **Write failed** | Write + 2 retries failed, or read-back mismatch | Control animates back to the last confirmed value (250 ms). Tile gets a 20 px caution badge (`E7BA` on `SystemFillColorCautionBackground`) at its top-right. Caution InfoBar at the bottom of the body: title "*Shadow boost* didn't change", body "The monitor didn't respond.", button `Retry`. Auto-dismiss after 8 s or on success; badge clears on next success. |
| **Not responding** | 3 consecutive read failures, or monitor removed | Header meta `Not responding`. Body replaced by empty state: 56 px plate with monitor glyph + caution sub-badge, title "Can't reach the monitor", body "Turn the monitor on, or check that DDC/CI is turned on in its on-screen menu.", `Try again` (accent) + `Get help` (opens docs). Footer keeps only the gear. Auto-recovers on `WM_DISPLAYCHANGE` / device arrival. |
| **No DDC/CI** | Monitor never answered a capability request | Same empty state, title "This monitor can't be controlled", body "It doesn't support DDC/CI, or the feature is turned off.", button `Open Windows display settings`. |
| **Pixel cleaning** | Started from the app | Body replaced: sparkle glyph plate, "Pixel cleaning", "About 4 minutes left. The screen is dark until it finishes.", determinate `ProgressBar` (time-based). Everything else hidden. |
| **HDR switching** | User toggled HDR | HDR tile shows pending; picture-mode tile label shows `…` until the new mode is read (Windows HDR switch takes ~1–2 s and blanks the screen). |

### 4.8 Multiple monitors

- The flyout opens on the monitor where the clicked taskbar lives, and selects that monitor if it's controllable; otherwise the last selected one.
- Header becomes a menu button. Menu items: glyph, name, second line (`Main display, DisplayPort` / `Brightness and contrast only` / `Use Windows settings for this display`). Built-in laptop panels and non-DDC monitors are listed but disabled.
- Layout and profiles are per monitor model; a reduced monitor (e.g. Dell with brightness/contrast only) just shows fewer items.
- No "all monitors" mode in v1 (open question).

### 4.9 Hotkey HUD

Topmost, non-activating (`WS_EX_NOACTIVATE | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW`), Acrylic, 236×48, corner 24, centered horizontally 24 px above the taskbar of the active monitor. Content: 16 px glyph, thin slider (no thumb), value; or glyph + text for discrete changes (`Night`, `Crosshair on`). Shows for 1.5 s after the last change. Never shown while the flyout is open. Can be turned off in Settings.

## 5. Main window

Mockups: `mockups/main-window.html`, `mockups/profiles.html`.

### 5.1 Shell

- `Window` with Fluent theme (`ThemeMode="System"`), Mica (`DWMSBT_MAINWINDOW = 2`), default 1200×800, min 760×560. Remembers size/position.
- Title bar: 48 px custom (`WindowChrome`), 16 px app icon + "Display Toolkit" in Caption; standard caption buttons (46×32).
- **NavigationView-style left pane** (custom; WPF has no NavigationView): 288 px. Top: monitor card (64 px illustration + model + `ROG Swift, DisplayPort`), click opens the monitor switcher when >1. Then search box (AutoSuggestBox look), nav items (36 px, corner 4, 16 px glyph, gap 16, selected = `SubtleFillColorSecondary` + 3×16 accent pill), Settings pinned bottom. Below 1000 px width the pane collapses to 48 px icons; below 760 px it becomes a hamburger overlay.
- **Content**: padding 4/40/40/40, max content width 1000 (centered beyond), page title in Title (28/36), `crumb` style for sub-pages: `Display › Six-axis color` (parent in secondary, clickable).
- **Settings cards** (custom `SettingsCard` / `SettingsExpander`, matching the Windows Community Toolkit visuals): min height 68 (52 for compact), padding 16, corner 4, `CardBackgroundFillColorDefault` + `CardStrokeColorDefault`, 2 px gap between cards in a group, group header Body Strong with 8 px below and 26 px above. Header icon 20 px; title Body; description Caption secondary; actions right-aligned with 12 px gaps; a nav card ends with a 12 px chevron. Expander sub-rows: 56 px left inset, `CardBackgroundFillColorSecondary`, no gap.

### 5.2 Display page

Top: **monitor hero card** (corner 8): illustration (vector, 150 px), Subtitle model name, `32-inch QD-OLED, 3840 × 2160, 240 Hz`, a fact row (green dot `Connected`, bolt + active profile + why, HDR badge + signal type when HDR), and on the right **Input source** combo.

| Group | Card | Control | Shown when |
|---|---|---|---|
| Picture | Brightness | Slider 260 + value | `0x10` |
| | Contrast | Slider 260 + value | `0x12`, SDR |
| | Picture mode | ComboBox | `0xDC`, SDR. Desc: "Each mode keeps its own color settings" |
| | Use HDR | ToggleSwitch | Display supports advanced color |
| | ↳ HDR mode | ComboBox (sub-row) | HDR on |
| | Color (expander, collapsed by default) | — | SDR and any of the below |
| | ↳ Color temperature | ComboBox (4000K … 10000K, User) | `0x14` |
| | ↳ Red / Green / Blue | Slider each, colored 12 px dot as icon | temperature = User |
| | ↳ Gamma | Segmented 1.8 2.0 2.2 2.4 2.6 | `0x72` |
| | ↳ Saturation, Sharpness | Slider | `0x8A`, `0x87` |
| | ↳ Reset *Scenery* mode | Button `Reset` (confirm flyout) | `0xEC` |
| | Six-axis color | Nav card → sub-page | `0x59–0x5E` |
| Gaming and comfort | Blue light filter | Segmented Off 1 2 3 4 | `0xE6` |
| | Shadow boost | Segmented Off 1 2 3 4 | `0xE5` |
| | Variable refresh rate | Toggle | `0xFD` bit 0 |
| | Frame-rate boost | Toggle | TBD |
| Sound | Volume | Mute icon button + slider | `0x62`/`0x8D` |
| Related | Windows display settings (opens `ms-settings:display`), Monitor information (sub-page: caps, firmware, VCP version, every detected feature incl. ones the app doesn't use) | Nav cards | always |

**HDR signal active**: Contrast, Picture mode and the Color group are removed (not disabled); an informational InfoBar appears under the Picture group: "Picture mode and color settings are managed by the monitor while HDR is on. They come back when you turn HDR off."

**Six-axis color** sub-page: intro line naming the active mode; one card with a 3-column grid (Color with 12 px swatch dot / Hue slider / Saturation slider), 52 px rows; `Reset six-axis color` button bottom-right. Hue slider is centered (bipolar, tick at center).

### 5.3 OLED care

- **Pixel cleaning** card (corner 4 top, 48 px sparkle plate in accent text color): title, description "Refreshes the panel to reduce temporary image retention. The screen goes dark for about 6 minutes.", button `Run pixel cleaning`. Sub-row: `Remind me after` ComboBox (Off, 2 hours, 4 hours, 8 hours) with description "The monitor shows a reminder after this much use".
- **Confirmation** (`ContentDialog`-style, 480 px): title "Run pixel cleaning now?", body "The screen turns off for about 6 minutes while the panel refreshes. Leave the monitor plugged in and don't turn it off.", secondary line "Display Toolkit can't change any settings until cleaning finishes.", primary (accent, default) `Run pixel cleaning`, `Cancel`. Primary is **not** focused by default; Cancel is (Esc/Enter safety). After starting: page shows the in-progress state (§4.7), all controls disabled, flyout likewise; the app polls `0xFD` bit 4 every 15 s to detect completion and re-reads state after.
- **Panel protection** group: Screen dimming control, Logo detection, Taskbar detection, Outer dimming, Global dimming, Uniform brightness, OLED anti-flicker (toggles, compact cards); Screen move (segmented Off / Light / Middle / Strong).
- **Proximity sensor** group: `Turn the screen off when I leave` ComboBox (Off, After 5 / 10 / 15 minutes); `Detection distance` segmented Near / Medium / Far (hidden when Off).

### 5.4 GamePlus

Intro: "Overlays drawn by the monitor. They work with any input and don't appear in screenshots or recordings."
- **Crosshair** expander (expanded): header toggle + description showing its shortcut; sub-area with a 10-cell style picker (Off + 9 styles; 64×56 cells, corner 4, selected = 2 px accent inner stroke). Artwork = vector paths per style id.
- FPS counter: segmented Off / Number / Bar. Timer: ComboBox. Display alignment: toggle. Overlay position: 3×3 miniature-screen picker (8 positions, center cell shows a monitor glyph and isn't selectable), 40×26 cells.

### 5.5 Settings

General (Start with Windows, App theme: Use system setting / Light / Dark, Show changes from shortcuts) · Keyboard shortcuts (keycap display + edit button that opens an inline shortcut recorder; Brightness step ComboBox 1/2/5/10; `Add a shortcut` for picture mode, input, HDR, any profile) · Target mode (Dim other windows %, shortcut) · Monitor communication (Delay between commands NumberBox, default 20 ms; Read settings from the monitor; Export / Import) · About (version, license, "Not affiliated with ASUS.", GitHub link, check for updates: off by default).

Shortcut recorder: clicking edit turns the keycaps into a focused field "Press a shortcut", records modifier+key, Esc cancels, Backspace clears. Conflicts (already registered by Windows or another app, i.e. `RegisterHotKey` fails) show inline: "Another app uses this shortcut."

### 5.6 Profiles & automation

Layout (see `profiles.html`):

1. **Profiles**: master–detail in one card (corner 8). Left list 232 px: 28 px glyph plate, name, second line = how it's triggered (`Sunset rule`, `Ctrl + Alt + 4`). `+ New profile` at the end (new profile = capture of current settings, name field focused).
   **Editor**: profile glyph (40 px accent plate; click to choose from ~12 glyphs) + name TextBox; actions `Capture current` (overwrites the checked settings' values with the monitor's current values), `Apply now` (accent), `…` (Duplicate, Delete). Fields: Shortcut (keycaps), Show in quick settings (toggle). **Settings in this profile**: rows with a checkbox, name and an inline compact control for the stored value. Unchecked rows are 45% opacity and their control is inert; caption "Unchecked settings stay as they are". First 7 rows (most common) + `Show N more settings`.
2. **Automation**: a **day strip** card (custom `DayTimeline` control): title "Today", status line "Night is active, set by the sunset rule at 18:21", `Pause automation` button (menu: 1 hour / Until tomorrow / Until I resume). The strip maps 00:00–24:00 to width, shows segments of the *schedule baseline* (profile glyph + name, accent fill for the current profile's segments, past part at 55%), sunrise/sunset markers above with times, a `Now` marker, and an axis at 6 h steps. Footnote with a hatched swatch: "App rules interrupt the day while the app runs, then the schedule takes over again."
3. **Rules** list: SettingsCards with drag grip (`E76F`), trigger glyph, sentence title ("A game is running in full screen", "VLC media player is open", "At sunset", "Weekdays at 08:00"), description with the concrete next time or end condition, a profile chip (pill, glyph + name), toggle, `…` (Edit, Duplicate, Delete). Disabled rules at 50%. `Add rule` button top-right of the group.
4. **Add rule** dialog, two steps: (1) trigger cards (2 columns): An app is open · A game is in full screen · At a time · At sunrise or sunset · Power source changes (only on devices with a battery) · HDR turns on or off. Caption: "Want a key combination instead? Set a shortcut on the profile itself." (2) trigger details + `Use profile` + Days chips. Sun rules show location source and the resolved time today ("Stockholm, from Windows location. Today this runs at 17:51." + `Change`). App rules: running-app list with icons + `Browse…`; option "Return to the previous profile when it closes" (on by default).

#### Arbitration (how the effective profile is chosen)

- **Schedule rules** (time, sunrise/sunset) are *events*: each one sets the **baseline** profile when it fires. The baseline at any moment = profile of the most recent fired schedule rule (looking back up to 7 days); if none, the default profile (Settings, default `Desktop`).
- **Condition rules** (app open, full-screen game, power source, HDR) are *states*: active while their condition holds.
- **Effective profile** = the highest condition rule in list order that is active; otherwise the baseline. List order only matters among condition rules ("Higher rules win when two apply at the same time").
- **Manual override**: picking a profile (flyout, tray menu, hotkey, Apply now) or changing a setting that the effective profile includes sets *Manual* until the **next rule event** (a schedule rule fires, or any condition rule starts or ends). Footer: "Manual: automation resumes at sunrise".
- **Pause** suspends evaluation entirely for the chosen time.
- Applying a profile writes only its checked settings, in dependency order: Windows HDR → picture/HDR mode → color temperature → RGB gains → everything else. One combined pending state on the profile chip; partial failure shows one InfoBar: "Night applied, except Color temperature." with Retry.

## 6. Interaction details

### 6.1 DDC writes (applies everywhere)

- One serialized worker per physical monitor, 20 ms inter-command delay (configurable). Writes **coalesce per VCP code** (latest value wins; queued older values are dropped).
- Sliders: throttle to one write per 100 ms while dragging (trailing), plus the final value on release. Keyboard steps and wheel: debounce 150 ms.
- Confirmation: after the final write for a code, read back once (150 ms later). Mismatch counts as a failure.
- Pending indicator only if confirmation hasn't arrived 300 ms after the last user change. No spinners anywhere for DDC.
- Failure: 2 automatic retries (200 ms apart), then the failed state (§4.7). The model keeps "last confirmed" per code to revert to.
- Bitmask registers (`0xFC`, `0xFD`): read-modify-write inside the worker, never from UI code.

### 6.2 External changes

The monitor doesn't notify. Re-read the visible values on flyout open, on main-window page navigation, after resume from sleep, after `WM_DISPLAYCHANGE`, and after an input switch. No background polling otherwise (keeps the bus free).

### 6.3 Capability adaptation

At first connection: parse the MCCS capabilities string → `MonitorCapabilities` (supported VCP codes + allowed enum values + supported bits for bitmask registers). A UI item is created only if its backing feature is present (and, for enums, only the advertised values are offered). Then state gates apply:

| Gate | Effect |
|---|---|
| HDR signal active | Hide Contrast, Picture mode, Color group; show HDR mode; picture-mode tile shows HDR preset |
| HDR group = Dolby Vision | HDR mode list shows DV presets only |
| Color temperature ≠ User | Hide RGB gains |
| Proximity sensor = Off | Hide distance |
| Pixel cleaning running | Disable everything (only disabled state in the app) |

The Monitor information page lists every detected feature (supported or not) for transparency and bug reports, with a `Copy diagnostics` button.

### 6.4 Tray icon

- Monochrome 16 px glyph (monitor outline), light/dark variants following the taskbar theme (`SystemUsesLightTheme`), high-contrast variant.
- Tooltip: `PG32UCWM: brightness 45, Night` (one line per monitor).
- Mouse wheel over the icon: brightness ±5 (Settings step) with HUD. Requires the notify-icon window to receive `WM_MOUSEWHEEL`; if unreliable on a Windows build, drop it (open question).

### 6.5 Target mode

App-side overlay: per monitor, a click-through, non-activating, layered topmost window (`WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`, here `AllowsTransparency=True` is fine) painting black at the configured opacity (default 70%) with a cut-out for the foreground window's extended frame bounds (`DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)`), tracked via `SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_OBJECT_LOCATIONCHANGE)`. Cut-out edges animate 120 ms when focus changes. Auto-suspends for exclusive full-screen apps. Excludes the flyout, HUD and Start/taskbar.

### 6.6 Input source switch

Selecting another input shows a confirm dialog: "Switch to HDMI 1? This screen will show whatever is connected to HDMI 1. To switch back, use the monitor's input button" (+ the shortcut if one is set). Primary `Switch`. No auto-revert (the user may not see this PC anymore).

## 7. Keyboard and accessibility

### 7.1 Flyout

| Key | Action |
|---|---|
| `Ctrl+Alt+D` (configurable) | Open/close flyout |
| Tab / Shift+Tab | Header menu (if any) → band → slider rows → profiles row → tile grid → footer buttons. Profiles row and tile grid are **one tab stop each**. |
| ← → ↑ ↓ on band | ±1; PageUp/PageDown ±10; Home/End 0/100; mouse wheel ±2 |
| Arrows in profiles row | Move selection and apply (radio group semantics); Enter not needed |
| Arrows in tile grid | 2-D focus movement. Split tiles expose two focus targets: main then chevron |
| Space / Enter on tile | Toggle (main), open sub-page (chevron/picker) |
| Esc | Sub-page → back; edit mode → cancel; otherwise close |
| Alt+Left, Backspace | Back from sub-page |
| Ctrl+E | Enter edit mode |
| Ctrl+, | Open main window (Settings page) |

Focus visual: Fluent double stroke (2 px `FocusStrokeColorOuter` outside, 1 px `FocusStrokeColorInner` inside), corner = control corner + 2. Shown for keyboard focus only.

### 7.2 Main window

Standard Fluent focus order. `Ctrl+F` focuses search. `Alt+Left` back from sub-pages. `Ctrl+1…5` switch nav pages. `F5` re-reads the monitor.

### 7.3 UIA

- Band: `Slider` pattern, name "Brightness", value in percent; pending/failed exposed via `HelpText` ("Sending to monitor", "Didn't change. The monitor didn't respond.").
- Tiles: `Toggle` pattern (toggle/split main), `Invoke` (picker, chevron); name = feature name, plus value for pickers ("Picture mode, Scenery"). Chevron name "*Feature* options".
- Profiles row: `SelectionPattern` (radio group).
- Edit-mode moves and InfoBars: `LiveSetting=Polite`.
- Day timeline: exposes a list of segments ("Night, 18:21 to 07:12").

### 7.4 Other

- High contrast: all custom controls use `SystemColors` brushes in HC; accent fill → `Highlight`, text on it → `HighlightText`; skeletons become outlines.
- Text scaling (Settings › Accessibility › Text size): layouts grow vertically; tile labels may wrap to 2 lines then ellipsize; flyout width stays 360 and becomes scrollable.
- Reduced motion (`SystemParameters.ClientAreaAnimation == false`): see §12.
- Minimum hit target 32×32 (tiles 48 tall, footer buttons 36).

## 8. Copy

- Sentence case. No exclamation marks. No "Please". Errors say what happened and what to do.
- Use: Profile · Rule · Picture mode · HDR mode · Blue light filter · Shadow boost · Pixel cleaning · Screen move · Proximity sensor · Crosshair · FPS counter · Target mode · Quick settings (the flyout, when named at all).
- Avoid: GameVisual, VCP, DDC/CI (except in Monitor information and the "can't reach" hint), "Preset" (except HDR presets), "Scene".
- Buttons name the action: `Run pixel cleaning`, `Save rule`, `Apply now`, `Switch`, `Try again`.
- Times: system short time format; durations "about 6 minutes".

## 9. Visual design tokens

Use the Fluent theme resources directly; the names below are the WPF Fluent resource keys (verify spelling against the .NET 10 `Fluent.xaml`). Custom tokens are marked ★ and should live in `Themes/Tokens.Light.xaml` / `Tokens.Dark.xaml`.

### 9.1 Typography

Segoe UI Variable (optical sizes: Small ≤12, Text 13–19, Display ≥20). Use the Fluent TextBlock styles.

| Style | Size / line | Weight | Used for |
|---|---|---|---|
| Caption | 12 / 16 | Regular | Descriptions, tile labels, meta, footer status |
| Body | 14 / 20 | Regular | Default text, card titles, options |
| Body Strong | 14 / 20 | Semibold | Flyout monitor name, sub-page titles, group headers, band value |
| Subtitle | 20 / 28 | Semibold | Dialog titles, hero model name |
| Title | 28 / 36 | Semibold | Page titles |
| Title Large | 40 / 52 | Semibold | (unused in v1) |

Numbers that change (band value, slider values, times) use tabular figures (`Typography.NumeralAlignment="Tabular"`).

### 9.2 Spacing and size

4 px grid. Common values: 2 (card gap), 4, 6 (tile label gap, profiles gap), 8, 12 (tile column gap, action gap), 14 (tile row gap), 16 (card padding), 18 (flyout side padding), 20, 24 (dialog padding), 26 (group spacing), 40 (page side padding).

| Radius | Value | Fluent key |
|---|---|---|
| Controls, cards, tiles, chips in rows | 4 | `ControlCornerRadius` |
| Flyout, dialogs, menus, hero cards, master-detail card | 8 | `OverlayCornerRadius` |
| ★ Brightness band, flyout glyph plate | 6 | `BandCornerRadius` |
| ★ HUD | 24 | `HudCornerRadius` |
| ★ Profile chip in rules | 14 | — |

### 9.3 Color

System accent is the only accent. All values are the Fluent defaults (shown with the default blue accent).

| Token | Fluent key | Light | Dark |
|---|---|---|---|
| Text primary | `TextFillColorPrimaryBrush` | `#E4000000` | `#FFFFFF` |
| Text secondary | `TextFillColorSecondaryBrush` | `#9E000000` | `#C5FFFFFF` |
| Text tertiary | `TextFillColorTertiaryBrush` | `#72000000` | `#8BFFFFFF` |
| Accent fill | `AccentFillColorDefaultBrush` | `#005FB8` (AccentDark1) | `#60CDFF` (AccentLight2) |
| Accent text | `AccentTextFillColorPrimaryBrush` | `#003E92` | `#99EBFF` |
| Text on accent | `TextOnAccentFillColorPrimaryBrush` | `#FFFFFF` | `#000000` |
| Card | `CardBackgroundFillColorDefaultBrush` | `#B3FFFFFF` | `#0DFFFFFF` |
| Card secondary | `CardBackgroundFillColorSecondaryBrush` | `#80F6F6F6` | `#08FFFFFF` |
| Card stroke | `CardStrokeColorDefaultBrush` | `#0F000000` | `#19000000` |
| Control fill | `ControlFillColorDefaultBrush` | `#B3FFFFFF` | `#0FFFFFFF` |
| Control stroke | `ControlStrokeColorDefaultBrush` | `#0F000000` | `#12FFFFFF` |
| Control stroke (bottom) | `ControlStrokeColorSecondaryBrush` | `#29000000` | `#18FFFFFF` |
| Strong stroke (slot, toggle off) | `ControlStrongStrokeColorDefaultBrush` | `#72000000` | `#8BFFFFFF` |
| Subtle hover / selected row | `SubtleFillColorSecondaryBrush` | `#09000000` | `#0FFFFFFF` |
| Divider | `DividerStrokeColorDefaultBrush` | `#0F000000` | `#15FFFFFF` |
| Caution | `SystemFillColorCautionBrush` | `#9D5D00` | `#FCE100` |
| Caution background | `SystemFillColorCautionBackgroundBrush` | `#FFF4CE` | `#433519` |
| Critical | `SystemFillColorCriticalBrush` | `#C42B1C` | `#FF99A4` |
| Success (connected dot) | `SystemFillColorSuccessBrush` | `#0F7B0F` | `#6CCB5F` |
| ★ Band track | — | `ControlFillColorSecondary` (`#80F9F9F9`) | `#12FFFFFF` |
| ★ Tile off fill (flyout) | — | `ControlFillColorDefault` | `#12FFFFFF` (slightly above Fluent default; acrylic is darker than Mica) |
| ★ Flyout footer layer | — | `#DCE8EAEE` | `#E01E1F22` |
| ★ Pending dot | — | `AccentFillColorDefault` | same |
| ★ Lifted tile shadow | — | `0 12 24 #38000000` | same |

Mica/Acrylic come from DWM; never paint an opaque window background except as the fallback.

### 9.4 Elevation

Cards: stroke only, no shadow. Flyout, menus, dialogs, HUD: DWM shadow (transient windows) or `DropShadowEffect` only on in-window popups (menus: `0 8 16 / 14%`; dialogs: `0 32 64 / 19%` + `0 2 21 / 15%`). Dialog smoke: `SmokeFillColorDefault` (`#4D000000`).

## 10. Iconography

Segoe Fluent Icons (`FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets"`), 16 px in controls, 20 px in settings card headers, 12 px for chevrons/inline, 10 px for caption buttons. Glyphs as used in the mockups:

| Concept | Glyph | Concept | Glyph |
|---|---|---|---|
| Monitor | `E7F4` | Brightness | `E706` |
| Contrast | `E7A1` | Picture mode | `E790` |
| Color temperature | `E9CA` | Blue light (night) | `E708` |
| Shadow boost | `E793` | Crosshair | `F272` |
| Target mode | `E9A6` | VRR / gauge | `EC4A` |
| FPS counter | `F404` | Timer | `E916` |
| Alignment grid | `E80A` | Screen move / position | `E7C2` |
| Proximity | `E95A` | Pixel cleaning | `F4A5` |
| OLED care (nav) | `E95E` | Profiles & automation (nav) | `E945` |
| Input | `EA4E` | Volume / Mute | `E767` / `E74F` |
| Sunrise / sunset | `ED39` | Time | `E823` |
| Game | `E7FC` | App | `ECA5` |
| Battery | `EBAA` | Film | `E714` |
| Settings | `E713` | Edit | `E70F` |
| Add | `E710` | Unpin / pin | `E77A` / `E718` |
| Back | `E72B` | Chevron right / down / up | `E76C` / `E70D` / `E70E` |
| Check | `E73E` | Warning | `E7BA` |
| Info | `E946` | More | `E712` |
| Drag grip | `E76F` | Six-axis / sliders | `E9E9` |
| Open external | `E8A7` | Refresh | `E72C` |

**HDR glyph**: no Fluent glyph exists; render the text "HDR" (9 px, bold, 0.03 em tracking) inside a 1.5 px rounded outline, 16 px tall, using `currentColor`. Implement as a small `HdrGlyph` control so it inherits Foreground like a FontIcon.

**Crosshair artwork**: 28×28 vector paths, 1.8 px stroke, round caps (one per monitor style id).

**Monitor illustration**: vector (`Path`/`Rectangle`): screen 76% of height, 1.5 px bezel `#2A2C31`, near-black panel with a soft accent-blue bloom (two radial gradients), stand in `#8F949C`/`#7D828A` (light) or `#4A4D53`/`#3C3F44` (dark).

## 11. Custom controls for WPF

The Fluent theme styles the standard controls. These need to be built:

| Control | Notes |
|---|---|
| `TrayFlyoutWindow` | Window host per §4.1; positioning helper; DWM attributes; `Deactivated` close; page navigation with the transitions in §12. |
| `BrightnessBand` | `Slider` subclass with a custom template: Border (corner 6) + fill Rectangle bound to value (min 40) + glyph + value TextBlock + pending dot. Click-to-position and drag anywhere on the track (`IsMoveToPointEnabled`). |
| `QuickTile` | Kinds Toggle / Split / Picker / Action; properties `Glyph`, `Label`, `IsOn`, `IsPending`, `HasError`, `IsEditing`; split has two focusable parts. |
| `TileGrid` | Panel (3 columns) with drag-to-reorder, placeholder slot, reflow animation, keyboard pick-up/move. |
| `ProfileBar` | Radio `ListBox` with `UniformGrid` panel, overflow "More". |
| `SettingsCard`, `SettingsExpander` | Port of the Windows Community Toolkit visuals. |
| `Segmented` | `ListBox` with custom item container (selected = card + accent pill). |
| `NavigationPane` | Left nav (WPF has no `NavigationView`): selection pill, collapse modes. |
| `InfoBar` | Severity (Info, Caution, Critical), title, message, action button, close. |
| `KeyCaps` / `ShortcutRecorder` | Display and capture of global hotkeys. |
| `DayTimeline` | Canvas-based 24 h strip with segments, sun markers, now marker; re-renders every minute. |
| `CrosshairPicker`, `PositionPicker` | `ListBox` with custom panels. |
| `HudWindow` | Non-activating topmost Acrylic pill. |
| `TargetOverlayWindow` | Per-monitor layered click-through window (§6.5). |
| `HdrGlyph`, `MonitorIllustration` | Small visual controls. |
| `ContentDialog` | In-window modal with smoke (not a separate window), Fluent dialog styling. |

## 12. Motion

Fluent durations and curves. Decelerate = `cubic-bezier(0,0,0,1)` (`KeySpline 0,0 0,1`). Accelerate = `cubic-bezier(1,0,1,1)`.

| Moment | Animation | Duration | Curve |
|---|---|---|---|
| Flyout open | Window appears with DWM's default fade; **content** translates 12 px from the taskbar side to 0 and fades 0→1 | 200 ms | Decelerate |
| Flyout close | Content fades to 0, then hide | 83 ms | Accelerate |
| Sub-page forward | New page from +24 px X, fade in; old page fades out | 167 ms / 83 ms | Decelerate |
| Sub-page back | Mirror (from −24 px) | same | same |
| Edit mode enter | Unpin badges scale 0.6→1 + fade | 167 ms | Decelerate |
| Tile lift / drop | Scale 1→1.04, shadow in; drop reverses | 83 ms | Decelerate |
| Grid reflow | Tiles move to new slots | 250 ms | Decelerate |
| Item removed | Collapse width/height + fade | 167 ms | Accelerate |
| Toggle on/off | Fill color cross-fade | 83 ms | Linear |
| Slider (keyboard/hotkey) | Fill animates to new value | 100 ms | Decelerate (pointer drag: no animation) |
| Failure revert | Value animates back; badge fades in | 250 ms | Decelerate |
| Pending dot | Appears after 300 ms; opacity pulse | 1.2 s loop | Sine |
| InfoBar | Height expand + fade | 167 ms | Decelerate |
| HUD | Fade + scale 0.96→1; hold 1.5 s; fade out | 167 ms | Decelerate |
| External value update | Value text cross-fade | 83 ms | Linear |

Reduced motion: drop all translations and scales; keep opacity fades ≤ 83 ms; pending dot static.

Do not animate the flyout window's `Top`/`Left` (top-level window moves stutter) or `Window.Opacity` (requires `AllowsTransparency`, which kills the backdrop).

## 13. Decisions (product owner, 2026-10-03)

These override anything above that conflicts with them.

1. **Frame-rate boost** = `0xFD` bit 8 (`0x0100`), the monitor's dual-mode / frame-rate boost. Toggle in Display › Gaming and comfort; optional tile.
2. **Crosshair styles** (new set, `0xE3`): 7 Blue dot, 8 Green dot, 9 Blue mini duplex, 10 Green mini duplex, 11 Blue heavy duplex, 12 Green heavy duplex. Caps also lists 13–15; verify on hardware during development before exposing them. The picker shows only advertised styles.
3. **Proximity distance** (`0xED` low byte): 3 = 60 cm, 2 = 90 cm, 1 = 120 cm, 255 = Tailored. Labels "Up to 60 cm / 90 cm / 120 cm / Tailored". Sensitivity = `0x47` (0–5), shown when distance ≠ Off.
4. **Pixel cleaning completion**: still unknown. Start with time-based progress plus a re-read afterwards; verify on hardware.
5. **Input switching**: **one click, no dialog**. It lives in the main window (hero card) and is **not** a default tile. It can still be added to the flyout.
6. **HDR tile toggles Windows HDR.** Accepted, even though the screen blanks for 1–2 s.
7. **Profiles are per monitor.**
8. **Target mode is not in v1.** Remove it from the default tiles and Settings; it moves to 1.1.
9. **Location**: Windows location service, with a manually entered city as fallback.
10. **Tray-wheel brightness**: keep it, and drop it if it proves unreliable.
11. **Flyout hotkey** `Ctrl+Alt+D` by default.
12. **Windows 11 only.**
13. Name stays **Display Toolkit**. The icon is to be designed.
14. **`0xFC` bits are decoded** (power LED, power key lock, key lock, mute, input auto-detection). They go in v1 under Settings › Monitor, not in the flyout.

Default flyout layout for v1: brightness, profiles, picture mode, HDR, blue light, shadow boost, crosshair, OLED anti-flicker.
