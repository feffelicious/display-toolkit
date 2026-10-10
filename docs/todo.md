# To do

Requests and ideas that aren't built yet, with what's missing to build them.

## Needs a monitor we don't have

- **Crop mode on the PG27UCWM** (the 24.5" mode). Requested on Reddit. The VCP code is unknown and ASUS's own CLI
  ([ASUS-Display/asus-display-control](https://github.com/ASUS-Display/asus-display-control)) doesn't expose it either:
  no property for crop, aspect or screen size in its docs or binaries. Ask an owner to run
  `DisplayToolkit.Probe --watch`, switch crop mode on and off in the monitor's menu, and send the output. Codes our
  PG32UCWM advertises but we haven't mapped (`0xDF`, `0xE9`, `0xFA`, `0xFC` bits 7 and 11) are worth checking first,
  but the PG27UCWM may use a different one.

## Settings ASUS's CLI offers that Display Toolkit doesn't

Found in the ASUS CLI reference. It names the settings but not their VCP codes, so each one needs
`DisplayToolkit.Probe --watch` on a monitor that has it (most should be on the PG32UCWM).

- **Power indicator** (power LED on/off).
- **Key lock** and **power key lock**.
- **OSD transparency** and **OSD timeout**.
- **Dynamic dimming** and **ASCR** (ASUS Smart Contrast Ratio). Probably not on OLED models.

## Open protocol questions

See [Open questions](research/asus-ddc-protocol.md#open-questions) in the protocol notes.
