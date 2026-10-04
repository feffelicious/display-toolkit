# ASUS monitor control protocol: notes

What ASUS monitors understand over DDC/CI, worked out by talking to a **ROG Swift PG32UCWM** (MCCS 2.2, ASUS VCP v2
rev 0x17): reading registers, changing settings in the monitor's own menu and watching what changed, and writing values
to see the effect. Enum names agree with ASUS's public, Apache-2.0
[CLI property reference](https://github.com/ASUS-Display/asus-display-control/blob/main/cli/docs/CLI_REFERENCE.md).

## TL;DR

No driver, service or special channel is needed. Everything goes through standard **DDC/CI over the Windows
`dxva2.dll` Monitor Configuration API**:

- `GetPhysicalMonitorsFromHMONITOR` → physical monitor handle
- `CapabilitiesRequestAndCapabilitiesReply` → MCCS capabilities string (self-describing feature list)
- `GetVCPFeatureAndVCPFeatureReply` / `SetVCPFeature` → read/write a VCP code

ASUS features live in standard MCCS codes plus the manufacturer range `0xE0–0xFF`.
Some codes are **bitmask registers** (`0xFC`, `0xFD`), so you have to read, modify, then write them.
**Feature support is advertised by the monitor itself** in the capabilities string, so no per-model database is required.

## Transport notes

- DDC/CI is slow and fragile. One call at a time per monitor, with about **20 ms** between calls, works reliably;
  calls that arrive faster get dropped now and then.
- Reads fail occasionally for no visible reason. Retrying (a couple of times, briefly) is enough.
- `0xEB` (EZ-OSD) drives the OSD joystick (see below), so its writes are time-sensitive.

## Capabilities string (PG32UCWM, raw)

```
(prot(monitor)type(LCD)model(PG32UCWM)cmds(01 02 03 07 0C E3 F3)vcp(02 04 05 08 10 12 14(03 04 05 06 07 08 09 0B)
16 18 1A 47 52 59 5A 5B 5C 5D 5E 5F(0001) 60(0F 11 12 1A) 61(01 02 03) 62 72(5000 6400 7800 8C00 A000) 87 8A 8D(01 02)
A8 AC AE B2 B6 C0 C5(00 01) C6 C8 CC(...) D1(00 01 02 03) D6(01 05) DC(0000 0100 0200 01 02 04 05 06 07 08 09 0A) DF
E1(00 01) E2(0000 0100 0200 01 02 03 04 05 06 07) E3(00 07 08 09 0A 0B 0C 0D 0E 0F) E4(00 01 02 03 04 05) E5(00 01 02 03 04)
E6 E7(00 01) E8(01 02 03 04 05 06 07 08) E9 EA(00 01 02) EB(00 01 02 03 04 05 06 07 08 0A 0B 0D 0E 0F) EC(01 FF)
ED(0500 0A00 0F00 00 01 02 03 FF) EE(00 01) EF F2(...) F3(02 03 04) F4(00 01 02 03 04 05) F5(...) F6(...) F7(...)
F8(00 02 04 08) F9 FA(01 02 03 04) FB(...) FC(089F) FD(6979) FF)mswhql(1)mccs_ver(2.2)asset_eep(40)
```

The value lists in parentheses tell us which enum values the model supports. For the bitmask registers (`FC`, `FD`),
the single value is the **supported-bits mask**.

## VCP map

✅ = read back from the PG32UCWM and matched what the monitor's menu showed.

### Standard MCCS

| Code | Meaning | Notes |
|---|---|---|
| `0x04` / `0x08` | Factory reset / color reset | write 1 |
| `0x10` | Brightness | ✅ 70/100 |
| `0x12` | Contrast | ✅ 80/100 |
| `0x14` | Color temperature preset | ASUS: 3=4000K … 8=9300K, 9=10000K, 11=User |
| `0x16/18/1A` | R/G/B gain | high word = factory default on some models |
| `0x6C/6E/70` | R/G/B offset (black level) | |
| `0x59–0x5E` | Six-axis saturation R,Y,G,C,B,M | low word = OSD value, high word = "physical" value |
| `0x9B–0xA0` | Six-axis hue R,Y,G,C,B,M | same split-word encoding |
| `0x60` | Input source | 0x0F DP1, 0x11 HDMI1, 0x12 HDMI2, 0x1A USB-C1 |
| `0x62` | Audio volume | |
| `0x72` | Gamma | 0x50=1.8, 0x64=2.0, 0x78=2.2, 0x8C=2.4, 0xA0=2.6 |
| `0x87` | Sharpness | |
| `0x8A` | Saturation | |
| `0x8D` | Audio mute | |
| `0xAA` | Orientation (read) | |
| `0xCC` | OSD language | |
| `0xD6` | Power mode (DPM) | |

### ASUS-specific (gaming line, "new VCP" = `0xEF` version 2)

| Code | Meaning | Values (PG32UCWM caps) |
|---|---|---|
| `0x45` | OLED protection intensity | |
| `0x47` | OLED motion sensitivity | |
| `0x4B` | Triple mode | |
| `0xC5` | OLED anti-flicker | 0/1 |
| `0xDC` | **GameVisual mode** | 1 Cinema, 2 Scenery, 3 sRGB, 4 User ✅, 5 Racing, 6 RTS/RPG, 7 FPS, 8 MOBA, 9 Night Vision, 10 sRGB Cal |
| `0xE0` | Overdrive | |
| `0xE1` | Power saving (v2) | 0/1 |
| `0xE2` | **HDR mode** (v2) | high byte = group (0x00 off, 0x01 HDR10, 0x02 Dolby Vision), low byte = preset: 1 Cinema HDR, 2 Gaming HDR, 3 Console HDR, 4 HDR400 / True Black, 5 DV Bright, 6 DV Dark, 7 DV Gaming |
| `0xE3` | Crosshair | 0 off, 7–15 styles |
| `0xE4` | GamePlus timer | 0 off, 1–5 = 30/40/50/60/90 s |
| `0xE5` | Shadow Boost | 0–4 ✅ |
| `0xE6` | Blue light filter | 0–4 ✅ |
| `0xE7` | Display alignment | 0/1 |
| `0xE8` | GamePlus overlay position | 1–8 (directions) |
| `0xEA` | FPS counter | 0 off, 1 number, 2 bar |
| `0xEB` | **EZ-OSD** (drive the OSD) | 0 close, 1 show, 2 up, 3 down, 4 right, 5 left, 6 enter, 7 back, 8 input select, … |
| `0xEC` | Reset current mode | |
| `0xED` | Neo proximity sensor | `(minutes << 8) \| distance` |
| `0xEE` | ELMB | 0/1 |
| `0xEF` | ASUS VCP version (read) | max = `(version << 8) \| revision` → `0x0217` |
| `0xF2` | Aura / black level / dual display | model-line dependent |
| `0xF3` | KVM | |
| `0xF4–0xF6` | PiP/PbP mode, source, color | |
| `0xF8` | OLED pixel-cleaning reminder | hours: 0, 2, 4, 8 ✅ |
| `0xF9` | OLED screen move | 0–3 |
| `0xFB` | OSD shortcut assignment | |
| `0xFC` | Toggle register 1 (bitmask) | supported mask `0x089F`, see below |
| `0xFD` | **Toggle register 2 (bitmask)** | see below |
| `0xFF` | Firmware version (max) / panel ID (low byte of current) | |

### `0xFC` toggle register 1

PG32UCWM: current `0x0811`, supported `0x089F`. Decoded by toggling OSD options while a watcher polled the register.

| Bit | Mask | Meaning | Source |
|---|---|---|---|
| 0 | `0x0001` | Power indicator LED | ✅ observed |
| 1 | `0x0002` | Power key lock | ✅ observed |
| 2 | `0x0004` | Key lock (OSD buttons) | ✅ observed |
| 3 | `0x0008` | Sound mute | not verified |
| 4 | `0x0010` | Input auto-detection | ✅ observed |
| 7 | `0x0080` | unknown | |
| 8 / 9 | `0x0100` / `0x0200` | Ambient-light brightness / color temp (sensor models) | not verified |
| 11 | `0x0800` | unknown (currently on) | |

### `0xFD` toggle register 2

PG32UCWM: current `0x6829`, supported `0x6979`. All bits marked ✅ match the OLED Care screen.

| Bit | Mask | Gaming/OLED meaning | PG32UCWM |
|---|---|---|---|
| 0 | `0x0001` | Variable refresh rate (VRR) | on |
| 1 | `0x0002` | ASCR | — |
| 2 | `0x0004` | Sniper | — |
| 3 | `0x0008` | OLED display saver ("Screen Dimming Control") | ✅ on |
| 4 | `0x0010` | **Start pixel cleaning** (≈6 min, screen goes dark) | off |
| 5 | `0x0020` | Logo detection | ✅ on |
| 6 | `0x0040` | Uniform brightness | ✅ off |
| 7 | `0x0080` | Clear pixel | — |
| 8 | `0x0100` | Frame rate boost (dual mode) | supported, off |
| 9 | `0x0200` | AI GameVisual | — |
| 11 | `0x0800` | Taskbar detection | ✅ on |
| 12 | `0x1000` | Boundary detection | not supported |
| 13 | `0x2000` | Outer dimming control | ✅ on |
| 14 | `0x4000` | Global dimming control | ✅ on |

These meanings are for the gaming/OLED line. Other ASUS product lines (ProArt, portable monitors) may use some codes
differently; they aren't covered here yet.

## Public reference

ASUS publishes an Apache-2.0 **property reference** (not source) for its `dwc.exe` command-line tool:
<https://github.com/ASUS-Display/asus-display-control/blob/main/cli/docs/CLI_REFERENCE.md>.
It agrees with the map above and is a citable source for enum names.

## Write verification (2026-10-03, PG32UCWM)

Each test read the original value, wrote a test value, held it 3 s, then restored and verified. Every effect was visible on screen.

| Code | Test | Result |
|---|---|---|
| `0x10` | Brightness 70 → 40 → 70 | ✅ visible, restored |
| `0xE3` | Crosshair `0x0B` → off | ✅ visible, restored |
| `0xEA` | FPS counter 1 → off | ✅ visible, restored |
| `0xFD` | Read, flip bit 6 (uniform brightness), write | ✅ other bits untouched, restored |

`SetVCPFeature` takes about **80 ms** per call. Sliders therefore need optimistic UI plus coalesced (latest-value-wins) writes on a per-monitor worker.

## HDR behavior (observed)

Toggling Windows HDR (Win+Alt+B), with no DDC writes from us:

| Code | SDR | HDR | Note |
|---|---|---|---|
| `0xE2` | `0x0000` | `0x0102` | HDR10 group, Gaming HDR |
| `0xDC` | 4 | 5 | changes on its own, so its meaning in HDR is unclear |
| `0x10` | 70 | 100 | brightness forced to max |
| `0x14` | 11 | 5 | color temp forced to 6500K |
| `0xE5` | 0 | `0xFE` | **`0xFE` = not available in the current mode** |
| `0xF4` | 0 | `0xFE` | same |

All values restored on their own when HDR turned off.

HDR presets seen while switching in ASUS DisplayWidget Center (DWC): `0x0101` Cinema HDR, `0x0102` Gaming HDR. Selecting
Console HDR / HDR400 there did **not** change `0xE2`, and DWC then showed "Racing" (`0xDC` reads 5 in HDR). A watcher was
polling the bus at the same time, which may have caused collisions.

**Direct-write test** (DWC closed, Windows HDR on):

| Value | Preset | Result |
|---|---|---|
| `0x0101` | Cinema HDR | works (seen via DWC) |
| `0x0102` | Gaming HDR | ✅ |
| `0x0103` | Console HDR | ✅ instant, no side effects |
| `0x0104` | HDR400 / True Black | ✅ applies, **but the panel mode switch makes Windows re-enumerate the display**: existing physical-monitor handles go stale (all calls fail), and for several seconds afterwards `0xE2` reads `0x0000` even though HDR is active |

App rules that follow from this:
1. On DDC failure, re-enumerate monitors and retry with fresh handles. Also listen for `WM_DISPLAYCHANGE` / device-change events.
2. After writing `0xE2`, verify only after a settle delay with retries. Never trust an immediate read-back.
3. In HDR, `0xDC` reads 5. Don't interpret it as the SDR Racing preset (DWC shows "Racing" there).

`0x52` (MCCS Active Control) did **not** update on any `0xE2` change (stayed `0x14`), so it is not a reliable change notification.
Refresh by re-reading on flyout or window open, plus light polling. The app should treat `0xFE` as "locked", and profiles need separate SDR and HDR parts.

## GameVisual presets store their own settings (verified)

Writing `0xDC` from the host works instantly. Each preset keeps its own image settings in the monitor:

| | User (4) | Cinema (1) | FPS (7) |
|---|---|---|---|
| Brightness | 70 | 90 | 100 |
| Contrast | 80 | 80 | 80 |
| Color temp (`0x14`) | 11 User | 6 (7500K) | 5 (6500K) |
| Gamma (`0x72`) | `0x7800` (2.2) | `0x7800` | `0x6400` (2.0) |
| Shadow Boost | 0 | 0 | 3 |

Switching back to User restored the original values exactly. Gamma is encoded in the **high byte** (`0x50` 1.8, `0x64` 2.0,
`0x78` 2.2, `0x8C` 2.4, `0xA0` 2.6).

What this means for profiles: a profile = preset plus optional overrides. Apply the preset first, then the overrides. Overrides persist
into that preset's stored settings in the monitor.

## Neo proximity sensor & crosshair styles

`0xED` = `(screenOffMinutes << 8) | distance`. Distance: 0 off, 3 = max 60 cm, 2 = max 90 cm, 1 = max 120 cm, 0xFF = Tailored mode.
Screen-off minutes come from the caps high bytes (`0500 0A00 0F00` → 5/10/15). PG32UCWM read `0x0501` = 5 min @ 120 cm ✅.
Sensitivity = `0x47` (levels 1–5 in the OSD, max 5), only meaningful when distance ≠ 0. Changing it in the OSD reads back
correctly (1, 2, 3 …), but **any DDC/CI write resets it to 0**: values 1, 3, 5 and `0x0300` all read back as 0, and so did
a change from DWC's slider. Treat it as read-only.

**Tailored (`0xFF`) needs confirmation on the monitor.** Writing it opens an OSD prompt; the register keeps the old distance
until the user confirms, then the monitor calibrates (about 10 s, DDC reads fail intermittently) and reports `0xFF`.
Without confirmation the prompt times out and nothing changes. Write it once and poll; re-sending restarts the prompt.

`0xE3` crosshair, "new" style set (caps contains 07+): 7 Blue Dot, 8 Green Dot, 9 Blue Mini Duplex, 10 Green Mini Duplex,
11 Blue Heavy Duplex, 12 Green Heavy Duplex. Legacy set (1–6): red/green point, dial, crosshair. Caps also lists 13–15, which
the monitor's menu doesn't offer. Untested.

## Availability caveats (observed)

- With the user away from the desk (Neo proximity sensor set to "screen off after 5 min"), the capabilities request failed
  every time with `0xC0262589` (`ERROR_GRAPHICS_DDCCI_INVALID_MESSAGE_COMMAND`), and single reads started failing
  intermittently. `0xD6` still reported "on". Closing DWC didn't help. Likely cause: the panel was dimmed or off. Not yet
  confirmed with the user present.
- A long multi-packet capabilities transfer is the most fragile DDC operation. The app should cache the capabilities string
  per monitor model and re-request it only when the cache is missing.
- DWC running at the same time as another DDC client is a collision risk. The app should detect it and warn.

## More quirks found while building the app

- **`0x60` input source is unreliable** while input auto-detection (`0xFC` bit 4) is on. Three reads in a row returned
  HDMI 2, DisplayPort, HDMI 1 while the monitor stayed on DisplayPort, apparently the input being scanned. The app takes
  the connection type and refresh rate from Windows (CCD API) instead.
- **Leaving HDR always returns the monitor to the Racing preset (`0xDC` = 5)**, whatever was active before. The app
  remembers the SDR preset and restores it after an HDR-to-SDR switch (including Win+Alt+B).
- **OLED anti-flicker (`0xC5`) is a mode switch.** Windows drops and re-detects the monitor (handles go stale), and with
  anti-flicker on, the monitor sat in the True Black 400 HDR preset with Windows HDR on. Afterwards, with HDR off,
  anti-flicker read Off again. It likely needs HDR; this isn't confirmed whether the switch-off was automatic.
- In HDR, brightness reads a normal value (100) but writes have no effect. Unlike Shadow Boost it doesn't report `0xFE`.

## Open questions

- Crosshair styles 13–15 (`0xE3`): advertised, not in the menu, untested.
- Dolby Vision presets (`0xE2` group 2): need a Dolby Vision signal to test.
- When pixel cleaning (`0xFD` bit 4) finishes: the bit doesn't clear reliably, so the app uses the ~6 minute duration.
- `0xFC` bits 7 and 11.
