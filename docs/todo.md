# To do

Requests and ideas that aren't built yet, with what's missing to build them.

## Needs a monitor we don't have

- **Crop mode on the PG27UCWM** (the 24.5" mode, under Aspect Control). Requested on Reddit. An owner probed it in
  [#5](https://github.com/feffelicious/display-toolkit/issues/5): no advertised code holds the setting, only side
  effects change (see [PG27UCWM](research/asus-ddc-protocol.md#pg27ucwm)). ASUS's own CLI
  ([ASUS-Display/asus-display-control](https://github.com/ASUS-Display/asus-display-control)) doesn't expose it
  either. Left to try: reading unadvertised codes, or pressing the menu keys through `0xEB`.

## Settings ASUS's CLI offers that Display Toolkit doesn't

Found in the ASUS CLI reference. It names the settings but not their VCP codes, so each one needs
`DisplayToolkit.Probe --watch` on a monitor that has it (most should be on the PG32UCWM).

- **OSD transparency** and **OSD timeout**.
- **Dynamic dimming** and **ASCR** (ASUS Smart Contrast Ratio). Probably not on OLED models.

## Automation

- **App rules that apply only while the app is in front.** Requested in
  [#2](https://github.com/feffelicious/display-toolkit/issues/2): a game's profile while you play, your normal settings
  as soon as you switch to the desktop. App rules apply while the app runs (and the full-screen game rule while a game
  fills the screen), so a game left open in the background keeps its profile. `SystemSampler` already looks at the
  foreground window for full-screen games; an option on app rules could do the same.

## Open protocol questions

See [Open questions](research/asus-ddc-protocol.md#open-questions) in the protocol notes.
