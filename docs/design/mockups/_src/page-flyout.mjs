import { I, G, css, page, presCss, presTop } from './lib.mjs';
import { fly, head, band, sliderRow, profiles, tile, tiles, slot, footDefault, footEdit, footPlain, subHead, opt } from './flyout.mjs';

const P = ['Desktop', 'Night', 'Gaming', 'Movie'];

const sceneCss = `
.scenes{display:grid;grid-template-columns:1fr 1fr;gap:28px;margin-top:24px}
.scene{position:relative;height:660px;border-radius:12px;overflow:hidden;box-shadow:0 0 0 1px rgba(0,0,0,.08),0 12px 32px rgba(0,0,0,.12)}
.wall-l{background:
  radial-gradient(520px 380px at 18% 92%,rgba(64,128,220,.55),transparent 70%),
  radial-gradient(460px 420px at 62% 38%,rgba(166,196,242,.75),transparent 70%),
  radial-gradient(380px 300px at 96% 4%,rgba(220,214,246,.9),transparent 70%),
  linear-gradient(165deg,#E7EDF8,#B9CDEE 70%,#9DB9E8)}
.wall-d{background:
  radial-gradient(520px 380px at 18% 96%,rgba(30,92,190,.55),transparent 70%),
  radial-gradient(460px 420px at 60% 40%,rgba(36,62,120,.65),transparent 70%),
  radial-gradient(360px 300px at 98% 0%,rgba(70,60,130,.45),transparent 70%),
  linear-gradient(165deg,#0E1527,#0B1530 60%,#08101F)}
.scene .fly{position:absolute;right:12px;bottom:60px}
.taskbar{position:absolute;left:0;right:0;bottom:0;height:48px;display:flex;align-items:center;padding:0 10px 0 0;-webkit-backdrop-filter:blur(40px) saturate(1.4);backdrop-filter:blur(40px) saturate(1.4)}
.light .taskbar{background:rgba(238,240,244,.86);border-top:1px solid rgba(0,0,0,.06)}
.dark .taskbar{background:rgba(28,29,32,.86);border-top:1px solid rgba(255,255,255,.06)}
.taskbar .apps{display:flex;gap:6px;align-items:center;margin-left:120px}
.taskbar .app{width:40px;height:40px;border-radius:4px;display:flex;align-items:center;justify-content:center}
.taskbar .app i.sq{width:24px;height:24px;border-radius:5px;display:block}
.win{display:grid;grid-template-columns:10px 10px;gap:1.5px}
.win b{width:10px;height:10px;background:linear-gradient(135deg,#3AA0F5,#1767D6);border-radius:1px}
.tray{margin-left:auto;display:flex;align-items:center;gap:2px}
.tray .ti{height:40px;min-width:32px;padding:0 8px;border-radius:4px;display:flex;align-items:center;justify-content:center;gap:10px}
.tray .ti.act{background:var(--subtle-1)}
.tray .clk{font-size:12px;line-height:16px;text-align:right;padding:0 8px}
.trayicon{position:relative}
.stages{display:grid;grid-template-columns:repeat(3,1fr);gap:24px;margin-top:24px;align-items:start}
.stages.two{grid-template-columns:repeat(2,1fr)}
.stage{border-radius:12px;padding:28px 22px;display:flex;flex-direction:column;align-items:center;min-height:200px;position:relative}
.stage.light{background:linear-gradient(170deg,#DCE5F4,#C9D7EF)}
.stage.dark{background:linear-gradient(170deg,#141C2E,#0E1424)}
.stage .fly{flex:none}
.stage .cap{align-self:stretch}
.light.stage .cap{color:rgba(0,0,0,.62)}
.dark.stage .cap{color:rgba(255,255,255,.7)}
.dark.stage .cap b{color:#fff}
.dark.stage .cap code{background:rgba(255,255,255,.08);color:#fff}
.anat{display:grid;grid-template-columns:420px 1fr;gap:40px;margin-top:24px;align-items:start}
table.spec{border-collapse:collapse;width:100%;font-size:13px;line-height:18px}
table.spec td,table.spec th{text-align:left;padding:8px 10px;border-bottom:1px solid rgba(0,0,0,.08);vertical-align:top}
table.spec th{font-weight:600;color:rgba(0,0,0,.62);font-size:12px}
table.spec td:first-child{font-weight:600;white-space:nowrap}
table.spec td:nth-child(2){white-space:nowrap}
.hudrow{display:flex;gap:28px;margin-top:24px}
.hudrow .stage{flex:1;min-height:150px;justify-content:center}
`;

const taskbar = ({ active = true } = {}) => `<div class="taskbar">
  <div class="apps">
    <span class="app"><span class="win"><b></b><b></b><b></b><b></b></span></span>
    <span class="app">${I(G.search, 's20')}</span>
    <span class="app"><i class="sq" style="background:linear-gradient(160deg,#FFD35C,#E8A92B)"></i></span>
    <span class="app"><i class="sq" style="background:linear-gradient(160deg,#5ED0A0,#1E8E6A)"></i></span>
    <span class="app"><i class="sq" style="background:linear-gradient(160deg,#55595F,#2C2F33)"></i></span>
    <span class="app"><i class="sq" style="background:linear-gradient(160deg,#9B8CF2,#5B48C9)"></i></span>
  </div>
  <div class="tray">
    <span class="ti">${I(G.chevU, 's12')}</span>
    <span class="ti trayicon ${active ? 'act' : ''}" title="Display Toolkit">${I(G.monitor)}</span>
    <span class="ti">${I(G.wifi)}${I(G.volume)}</span>
    <span class="clk tnum">20:47<br>03/10/2026</span>
  </div>
</div>`;

/* ---------- Flyout variants ---------- */
const defaultTiles = (o = {}) => tiles([
  tile({ icon: G.palette, label: o.mode || 'Scenery', kind: 'pick' }),
  tile({ hdr: true, label: 'HDR', kind: 'split', on: !!o.hdr }),
  tile({ icon: G.moon, label: 'Blue light', kind: 'split', on: o.blue !== false }),
  tile({ icon: G.shadow, label: 'Shadow boost', kind: 'split', on: !!o.shadow, warn: !!o.warn, focus: !!o.focusShadow }),
  tile({ icon: G.target, label: 'Crosshair', kind: 'split', on: !!o.cross }),
  tile({ icon: G.focus, label: 'Target mode', on: !!o.target }),
]);

const fDefault = fly(`${head()}${band(45)}${profiles(P, 'Night')}${defaultTiles()}`);

const fDefaultDark = fly(
  `${head({ hdr: true })}${band(80)}${profiles(P, 'Gaming')}${defaultTiles({ mode: 'Gaming HDR', hdr: true, blue: false, shadow: true, cross: true })}`,
  { foot: footDefault('Gaming while Forza Horizon 5 runs') },
);

const editBody = (dark) => `${head(dark ? { hdr: true } : {})}
<div class="editable">${band(dark ? 80 : 45)}<span class="pinned" title="Always shown">${I(G.pin, 's12')}</span></div>
${profiles(P, dark ? 'Gaming' : 'Night', { auto: false, extra: `<b class="unpin">${I(G.unpin)}</b>` })}
${tiles([
  tile({ icon: G.palette, label: dark ? 'Gaming HDR' : 'Scenery', kind: 'pick', edit: true }),
  slot(),
  tile({ icon: G.moon, label: 'Blue light', kind: 'split', on: !dark, edit: true }),
  tile({ icon: G.shadow, label: 'Shadow boost', kind: 'split', on: dark, edit: true }),
  tile({ icon: G.target, label: 'Crosshair', kind: 'split', on: dark, edit: true }),
  tile({ icon: G.focus, label: 'Target mode', edit: true }),
])}`;

/* Edit state: HDR tile is being dragged from slot 2 */
const fEditLight = `<div style="position:relative">${fly(editBody(false), { foot: footEdit() })}
<div style="position:absolute;left:130px;top:174px;width:100px">${tile({ hdr: true, label: 'HDR', kind: 'split', lift: true, edit: true })}</div></div>`;

const addList = fly(`${subHead('Add to quick settings')}
<div class="grp" style="margin-top:2px">Picture</div>
<div class="addrow">${I(G.contrast)}<span>Contrast</span><span class="sp"></span><span class="k">Slider</span><span class="fb">${I(G.add)}</span></div>
<div class="addrow hov">${I(G.thermo)}<span>Color temperature</span><span class="sp"></span><span class="k">List</span><span class="fb">${I(G.add)}</span></div>
<div class="addrow">${I(G.vrr)}<span>Variable refresh rate</span><span class="sp"></span><span class="k">Toggle</span><span class="fb">${I(G.add)}</span></div>
<div class="grp">GamePlus</div>
<div class="addrow">${I(G.fps)}<span>FPS counter</span><span class="sp"></span><span class="k">List</span><span class="fb">${I(G.add)}</span></div>
<div class="addrow">${I(G.timer)}<span>Timer</span><span class="sp"></span><span class="k">List</span><span class="fb">${I(G.add)}</span></div>
<div class="grp">OLED care</div>
<div class="addrow">${I(G.sparkle)}<span>Pixel cleaning</span><span class="sp"></span><span class="k">Action</span><span class="fb">${I(G.add)}</span></div>
<div class="addrow">${I(G.radar)}<span>Proximity sensor</span><span class="sp"></span><span class="k">Toggle</span><span class="fb">${I(G.add)}</span></div>
<div class="grp">Sound and input</div>
<div class="addrow">${I(G.volume)}<span>Volume</span><span class="sp"></span><span class="k">Slider</span><span class="fb">${I(G.add)}</span></div>
<div class="addrow">${I(G.input)}<span>Input source</span><span class="sp"></span><span class="k">List</span><span class="fb">${I(G.add)}</span></div>`,
{ foot: footEdit() });

const fPicture = fly(`${subHead('Picture mode')}
${opt('Scenery', { sel: true })}${opt('Racing')}${opt('Cinema')}${opt('RTS/RPG', { focus: true })}${opt('FPS')}${opt('MOBA')}${opt('Night Vision')}${opt('sRGB Cal')}${opt('User')}
<div class="morel">${I(G.sliders)}<span>Color and picture settings</span></div>`, { foot: footDefault() });

const fHdr = fly(`${subHead('HDR')}
<div class="orow"><div><div>Use HDR</div><div class="caption t2">Windows HDR for this display</div></div><span class="toggle on"></span></div>
<div class="grp">HDR10 presets</div>
${opt('Cinema HDR')}${opt('Gaming HDR', { sel: true })}${opt('Console HDR')}${opt('True Black 400')}
<p class="note">Dolby Vision presets appear here while Dolby Vision content is playing.</p>`, { foot: footDefault('Gaming while Forza Horizon 5 runs') });

const fBlue = fly(`${subHead('Blue light filter')}
<div class="orow"><div>Blue light filter</div><span class="toggle on"></span></div>
<div style="margin-top:14px" class="caption t2">Strength</div>
<div class="seg" style="display:flex;margin-top:8px"><span style="flex:1">1</span><span class="on" style="flex:1">2</span><span style="flex:1">3</span><span style="flex:1">4</span></div>
<p class="note" style="margin:12px 0 0">Level 4 matches TÜV low blue light. The tile remembers the last level when you turn it back on.</p>`, { foot: footDefault() });

const fPending = fly(`${head()}${band(62, { pending: true })}${profiles(P, 'Night', { auto: false })}
${defaultTiles({ warn: true })}
<div class="infobar caution" style="margin-top:18px">${I(G.warn, 'lead')}<div style="flex:1"><div class="strong">Shadow boost didn't change</div><div class="caption t2" style="margin-top:2px">The monitor didn't respond.</div></div><span class="btn" style="height:28px">Retry</span></div>`,
{ foot: footDefault('Manual: automation resumes at sunrise') });

const fDisconnected = fly(`${head({ meta: 'Not responding' })}
<div class="empty"><div class="big">${I(G.monitor)}<span class="sub">${I(G.warn)}</span></div>
<div class="strong">Can't reach the monitor</div>
<p>Turn the monitor on, or check that DDC/CI is turned on in its on-screen menu.</p>
<div class="btns"><span class="btn accent">Try again</span><span class="btn">Get help</span></div></div>`, { foot: footPlain('') });

const fLoading = fly(`${head({ meta: 'Reading settings…' })}
<div class="sk" style="height:40px;border-radius:6px"></div>
<div class="prof" style="margin-top:12px">${'<span class="sk" style="box-shadow:none"></span>'.repeat(4)}</div>
<div class="tiles">${Array.from({ length: 6 }, () => '<div class="tile"><div class="sk" style="width:100%;height:48px"></div><div class="sk" style="width:56px;height:10px;margin-top:3px"></div></div>').join('')}</div>`,
{ foot: footPlain('') });

const fMulti = `<div style="position:relative">${fly(`${head({ multi: true, open: true })}${band(80)}${profiles(P, 'Gaming')}${defaultTiles({ mode: 'FPS', blue: false, cross: true })}`, { foot: footDefault('Gaming while Forza Horizon 5 runs') })}
<div class="mmenu">
  <div class="mi sel">${I(G.monitor)}<div><div>ROG Swift PG32UCWM</div><div class="c">Main display, DisplayPort</div></div></div>
  <div class="mi">${I(G.monitor)}<div><div>Dell U2723QE</div><div class="c">Brightness and contrast only</div></div></div>
  <div class="mi" style="opacity:.6">${I(G.monitor)}<div><div>Built-in display</div><div class="c">Use Windows settings for this display</div></div></div>
</div></div>`;

const hud = (theme, icon, v, label) => `<div class="stage ${theme}"><div class="hud">${I(icon)}${label
  ? `<span style="flex:1">${label}</span>`
  : `<div class="slider"><div class="rail"></div><div class="fill" style="width:${v}%"></div></div><span class="sval">${v}</span>`}</div></div>`;

export default () => page({
  title: 'Tray Flyout',
  desc: 'Display Toolkit tray flyout mockups: default, edit mode, sub-pages and states, light and dark.',
  cssText: css('base.css') + css('flyout.css') + presCss + sceneCss,
  body: `<div class="pres light">${presTop('tray-flyout.html')}
<main class="pwrap">
<div class="pintro"><h1 class="title">Tray flyout</h1>
<p>The everyday surface. One click on the tray icon, one gesture to change what matters: brightness, profile, picture mode, HDR. Everything else lives one level deeper or in the main window.</p></div>

<section class="psec" style="margin-top:8px"><h2>Default</h2>
<p class="lede">Anchored 12 px above the tray, right-aligned to the work area. Left: evening, the Night profile was applied by the sunset rule. Right: a game is running, HDR is on, so the picture-mode tile shows HDR presets.</p>
<div class="scenes">
  <div><div class="scene light wall-l">${fDefault}${taskbar()}</div></div>
  <div><div class="scene dark wall-d">${fDefaultDark}${taskbar()}</div></div>
</div></section>

<section class="psec"><h2>Customize</h2>
<p class="lede">The pencil in the footer switches to edit mode, the same model as Windows quick settings. Tiles get an unpin badge, everything except brightness can be dragged, and Add lists only what this monitor supports and isn't already shown.</p>
<div class="stages">
  <div class="stage light">${fEditLight}<div class="cap"><b>Edit mode, dragging</b>The lifted tile follows the pointer; the dashed slot shows where it lands. Brightness is pinned. Keyboard: Space picks up, arrows move, Space drops, Delete unpins.</div></div>
  <div class="stage dark">${addList}<div class="cap"><b>Add</b>Grouped by area, with the control type on the right. Unsupported features never appear. Tap + to append; the list stays open so several can be added.</div></div>
  <div class="stage light"><div class="cap" style="margin-top:0;max-width:none"><b>Customization model</b>
  <ul class="notes" style="margin-top:8px">
  <li>Layout = one ordered list of items, saved per monitor model (<code>%AppData%\\DisplayToolkit\\flyout.json</code>).</li>
  <li>Item kinds: <code>slider</code> (full width row), <code>profiles</code> (full width row), <code>tile</code> (one of three grid cells).</li>
  <li>Brightness is always first and can't be removed. Profiles row and sliders sit above the tile grid; they can be removed but not dragged into the grid.</li>
  <li>Tiles flow in a 3-column grid, max 4 rows (12 tiles); the body scrolls beyond that.</li>
  <li>Removing a tile never changes the monitor setting; it only hides the shortcut.</li>
  <li>Esc in edit mode cancels changes; Done or closing the flyout commits.</li>
  <li>Default layout: Brightness, Profiles, Picture mode, HDR, Blue light, Shadow boost, Crosshair, Target mode. Items the monitor doesn't support are skipped and the grid closes up.</li>
  </ul></div></div>
</div></section>

<section class="psec"><h2>One level deeper</h2>
<p class="lede">Chevrons open a sub-page inside the flyout (slide left 200 ms). Back, Alt+Left or Backspace returns. The footer stays put so the flyout never feels like it navigated away.</p>
<div class="stages">
  <div class="stage light">${fPicture}<div class="cap"><b>Picture mode (SDR)</b>The nine GameVisual modes, in the monitor's own order. Keyboard focus shown on RTS/RPG. The link at the bottom opens the Display page in the main window.</div></div>
  <div class="stage dark">${fHdr}<div class="cap"><b>HDR</b>Turning HDR on here flips the Windows HDR setting. Presets follow the incoming signal: HDR10 presets for HDR10, Dolby Vision presets only while Dolby Vision is playing.</div></div>
  <div class="stage light">${fBlue}<div class="cap"><b>Split tiles with levels</b>The left part toggles, the chevron opens levels. Shadow boost and Crosshair use the same pattern (levels 1–4, crosshair styles).</div></div>
</div></section>

<section class="psec"><h2>States</h2>
<p class="lede">No spinners. DDC/CI is slow, so the UI moves first and the monitor catches up. Problems are local to the control that failed.</p>
<div class="stages">
  <div class="stage light">${fPending}<div class="cap"><b>Pending and failed</b>Brightness shows the pending dot only if a write takes over 300 ms. Shadow boost failed after a retry: the tile reverts, gets a warning badge, and an info bar offers Retry. Manual changes pause automation, and the footer says until when.</div></div>
  <div class="stage dark">${fDisconnected}<div class="cap"><b>Monitor not responding</b>Shown after 3 failed reads or when the monitor disappears. The app retries on display-change events; Try again forces a capability re-read.</div></div>
  <div class="stage light">${fLoading}<div class="cap"><b>First read</b>Only on the very first launch for a monitor. After that the flyout opens instantly with cached values and refreshes them silently.</div></div>
</div>
<div class="stages two" style="margin-top:28px">
  <div class="stage dark">${fMulti}<div class="cap"><b>More than one monitor</b>The header becomes a menu. The flyout opens on the monitor the taskbar is on. Monitors with limited DDC/CI get a reduced flyout; ones without it are listed but can't be selected.</div></div>
  <div class="stage light" style="justify-content:flex-start"><div class="cap" style="margin-top:0;max-width:none"><b>Anatomy</b>
  <table class="spec" style="margin-top:10px">
  <tr><th>Part</th><th>Size</th><th>Notes</th></tr>
  <tr><td>Window</td><td>360 × auto</td><td>Max height 80% of work area. Corner 8. Transient (Acrylic) backdrop. 1 px surface stroke + shadow from DWM.</td></tr>
  <tr><td>Body padding</td><td>16 / 18 / 20</td><td>Top / sides / bottom.</td></tr>
  <tr><td>Header</td><td>40 high</td><td>36 px glyph plate, name Body Strong, meta Caption secondary. 14 below.</td></tr>
  <tr><td>Brightness band</td><td>324 × 40</td><td>Corner 6. Fill = accent, min 40 px so the icon always sits on it.</td></tr>
  <tr><td>Profiles row</td><td>4 × 32</td><td>Gap 6. Max 4 visible; a 5th+ profile turns the last cell into More.</td></tr>
  <tr><td>Tile</td><td>100 × 48</td><td>Corner 4. Grid gap 12 horizontal, 14 vertical. Label Caption, 2 lines max, 6 below the button. Chevron zone 30 wide.</td></tr>
  <tr><td>Footer</td><td>48 high</td><td>Status (Caption secondary) + Edit + Settings icon buttons 36 × 36.</td></tr>
  </table></div></div>
</div></section>

<section class="psec"><h2>Hotkey feedback</h2>
<p class="lede">Global hotkeys show a small pill above the taskbar, centered, like the Windows volume indicator. It fades after 1.5 s and never takes focus.</p>
<div class="hudrow">
  ${hud('light', G.brightness, 55)}
  ${hud('dark', G.brightness, 55)}
  ${hud('light', G.moon, 0, 'Night')}
  ${hud('dark', G.target, 0, 'Crosshair on')}
</div></section>
</main></div>`,
});
